using System.Net.NetworkInformation;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Metrics;

/// <summary>
/// Seçili sunucuya 5 saniyede bir ping (ICMP; yönetici yetkisi gerekmez). Yalnızca "Ping" öğesi açıkken çalışır.
/// <see cref="Changed"/> arka planda çağrılır; yanıt yoksa <see cref="LatencyMs"/> null.
/// </summary>
internal sealed class PingMonitor : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private const int TimeoutMs = 2000;

    private readonly AppSettings _settings;
    private readonly Timer _timer;
    private int _busy;
    private string? _lastError;

    public double? LatencyMs { get; private set; }
    public event Action? Changed;

    public PingMonitor(AppSettings settings)
    {
        _settings = settings;
        _timer = new Timer(_ => _ = PingAsync());
    }

    public void Start() => _timer.Change(TimeSpan.Zero, Interval);

    private async Task PingAsync()
    {
        if (!_settings.IsShown(TaskbarItemId.Ping)) return;
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(_settings.PingHost, TimeoutMs);
            LatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
            _lastError = null;
        }
        catch (Exception ex) when (ex is PingException or ArgumentException or InvalidOperationException)
        {
            LatencyMs = null;
            if (ex.Message != _lastError) Log.Warn($"Ping başarısız: {_settings.PingHost}", ex);
            _lastError = ex.Message;
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
            Changed?.Invoke();
        }
    }

    public void Dispose() => _timer.Dispose();
}
