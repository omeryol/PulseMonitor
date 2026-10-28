using System.Net.NetworkInformation;
using Microsoft.Win32;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Weather;

/// <summary>
/// Seçili konumun hava durumunu 15 dakikada bir yeniler. Hata olursa 2 dakika sonra yeniden dener;
/// uykudan uyanınca ve internet geri gelince hemen yeniler. <see cref="Updated"/> arka planda çağrılır.
/// </summary>
internal sealed class WeatherService : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(2);

    private readonly AppSettings _settings;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private bool _lastFailed;
    private (double Lat, double Lon)? _lastPlace;
    private bool _lastShow;

    public WeatherReport? Current { get; private set; }
    public event Action? Updated;

    public WeatherService(AppSettings settings)
    {
        _settings = settings;
        _timer = new Timer(_ => _ = RefreshAsync());
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    public void Start()
    {
        RememberSettings();
        if (_settings.WeatherLocation is { } loc) Current = WeatherCache.Load(loc);
        Updated?.Invoke();
        TimeSpan due = Current is null ? TimeSpan.Zero : RefreshInterval - Current.Age;
        _timer.Change(due < TimeSpan.Zero ? TimeSpan.Zero : due, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Kullanıcı yenileme istedi ya da bir olay (uyanma, ağ) yenilemeyi gerektiriyor.</summary>
    public void RefreshSoon(TimeSpan? delay = null) => _timer.Change(delay ?? TimeSpan.Zero, Timeout.InfiniteTimeSpan);

    /// <summary>Ayarlar kaydedildi: konum değiştiyse ya da hava durumu yeniden açıldıysa hemen yenile.</summary>
    public void SettingsChanged()
    {
        var oldPlace = _lastPlace;
        bool wasShown = _lastShow;
        RememberSettings();
        if (_lastPlace == oldPlace && !(_lastShow && !wasShown)) return;

        if (_lastPlace is null) Current = null;
        RefreshSoon();
    }

    private void RememberSettings()
    {
        WeatherLocation? loc = _settings.WeatherLocation;
        _lastPlace = loc is null ? null : (loc.Latitude, loc.Longitude);
        _lastShow = _settings.IsShown(TaskbarItemId.Weather);
    }

    private async Task RefreshAsync()
    {
        if (!await _gate.WaitAsync(0)) return;
        TimeSpan next = RefreshInterval;
        try
        {
            WeatherLocation? loc = _settings.WeatherLocation;
            if (loc is null || !_settings.IsShown(TaskbarItemId.Weather))
            {
                if (loc is null) Current = null;
                return;
            }
            if (Current is not null && !loc.SamePlace(Current.Latitude, Current.Longitude)) Current = null;

            WeatherReport report = await OpenMeteoClient.FetchAsync(loc.Latitude, loc.Longitude, _cts.Token);
            Current = report;
            WeatherCache.Save(report);
            if (_lastFailed) Log.Info("Hava durumu yeniden alınabildi.");
            _lastFailed = false;
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (!_lastFailed) Log.Warn("Hava durumu alınamadı; 2 dakika sonra yeniden denenecek.", ex);
            _lastFailed = true;
            next = RetryInterval;
        }
        finally
        {
            Updated?.Invoke();
            if (!_cts.IsCancellationRequested) _timer.Change(next, Timeout.InfiniteTimeSpan);
            _gate.Release();
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        // Uyanınca ağın açılması birkaç saniye sürebilir.
        if (e.Mode == PowerModes.Resume) RefreshSoon(TimeSpan.FromSeconds(10));
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (e.IsAvailable && _lastFailed) RefreshSoon(TimeSpan.FromSeconds(3));
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        _cts.Cancel();
        _timer.Dispose();
    }
}
