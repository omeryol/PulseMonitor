using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Metrics;

/// <summary>
/// Yerel IPv4 (ölçülen bağdaştırıcının; otomatikte varsayılan ağ geçidi olanın) ve dış IP.
/// Ağ adresi değişince hemen güncellenir. Dış IP yalnızca öğe açıkken, 15 dakikada bir alınır.
/// <see cref="Changed"/> arka planda çağrılır.
/// </summary>
internal sealed class IpAddressMonitor : IDisposable
{
    private const string PublicIpUrl = "https://api.ipify.org";
    private static readonly TimeSpan PublicInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(2);

    private readonly AppSettings _settings;
    private readonly Timer _publicTimer;
    private readonly CancellationTokenSource _cts = new();
    private int _publicBusy;
    private bool _publicFailed;

    public string LocalIp { get; private set; } = "--";
    public string PublicIp { get; private set; } = "--";
    public event Action? Changed;

    public IpAddressMonitor(AppSettings settings)
    {
        _settings = settings;
        _publicTimer = new Timer(_ => _ = RefreshPublicAsync());
    }

    public void Start()
    {
        NetworkChange.NetworkAddressChanged += OnAddressChanged;
        RefreshLocal();
        _publicTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Bağdaştırıcı seçimi ya da öğe görünürlüğü değişmiş olabilir.</summary>
    public void SettingsChanged()
    {
        RefreshLocal();
        if (_settings.IsShown(TaskbarItemId.PublicIp) && PublicIp == "--")
            _publicTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
    }

    private void OnAddressChanged(object? sender, EventArgs e)
    {
        RefreshLocal();
        _publicTimer.Change(TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan); // bağlantı otursun
    }

    private void RefreshLocal()
    {
        try
        {
            LocalIp = FindLocalIp(_settings.NetworkAdapterId) ?? "--";
        }
        catch (Exception ex)
        {
            Log.Warn("Yerel IP okunamadı.", ex);
            LocalIp = "--";
        }
        Changed?.Invoke();
    }

    private static string? FindLocalIp(string? selectedId)
    {
        Guid? selected = Guid.TryParse(selectedId, out Guid id) ? id : null;
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

            IPInterfaceProperties props = ni.GetIPProperties();
            if (selected is { } sel)
            {
                if (!Guid.TryParse(ni.Id, out Guid niId) || niId != sel) continue;
            }
            else if (!props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork &&
                                                      !g.Address.Equals(IPAddress.Any)))
            {
                continue; // otomatik: internete çıkan bağdaştırıcı
            }

            UnicastIPAddressInformation? v4 = props.UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
            if (v4 is not null) return v4.Address.ToString();
        }
        return null;
    }

    private async Task RefreshPublicAsync()
    {
        if (Interlocked.Exchange(ref _publicBusy, 1) == 1) return;
        TimeSpan next = PublicInterval;
        try
        {
            if (!_settings.IsShown(TaskbarItemId.PublicIp)) return;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            string text = (await Http.Client.GetStringAsync(PublicIpUrl, timeout.Token)).Trim();
            PublicIp = IPAddress.TryParse(text, out IPAddress? ip) ? ip.ToString() : "--";
            _publicFailed = false;
        }
        catch (Exception ex) when (!_cts.IsCancellationRequested)
        {
            if (!_publicFailed) Log.Warn("Dış IP alınamadı; 2 dakika sonra yeniden denenecek.", ex);
            _publicFailed = true;
            PublicIp = "--";
            next = RetryInterval;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            Changed?.Invoke();
            if (!_cts.IsCancellationRequested) _publicTimer.Change(next, Timeout.InfiniteTimeSpan);
            Interlocked.Exchange(ref _publicBusy, 0);
        }
    }

    public void Dispose()
    {
        NetworkChange.NetworkAddressChanged -= OnAddressChanged;
        _cts.Cancel();
        _publicTimer.Dispose();
    }
}
