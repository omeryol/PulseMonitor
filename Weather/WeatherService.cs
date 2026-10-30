using System.Net.NetworkInformation;
using Microsoft.Win32;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Weather;

/// <summary>
/// Takip edilen tüm konumların hava durumunu 15 dakikada bir yeniler (sırayla, tek tek). Bir konum alınamazsa
/// eski raporu korunur ve 2 dakika sonra yeniden denenir; uykudan uyanınca ve internet geri gelince hemen yeniler.
/// <see cref="Updated"/> arka planda çağrılır.
/// </summary>
internal sealed class WeatherService : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(2);

    private readonly AppSettings _settings;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private volatile List<WeatherReport> _reports = new();
    private bool _lastFailed;
    private string _lastPlaces = "";
    private string _lastModel = "";
    private bool _lastShow;

    public event Action? Updated;

    public WeatherService(AppSettings settings)
    {
        _settings = settings;
        _timer = new Timer(_ => _ = RefreshAsync());
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    /// <summary>Konumun son raporu (seçili modelden ya da önbellekten); yoksa null.</summary>
    public WeatherReport? ReportFor(WeatherLocation loc) =>
        _reports.FirstOrDefault(r => loc.SamePlace(r.Latitude, r.Longitude));

    public void Start()
    {
        RememberSettings();
        _reports = WeatherCache.Load();
        Updated?.Invoke();
        _timer.Change(DueTime(), Timeout.InfiniteTimeSpan);
    }

    /// <summary>En eski (ya da eksik) rapor yenileme zamanını belirler.</summary>
    private TimeSpan DueTime()
    {
        WeatherSettings w = _settings.Weather;
        var reports = w.Locations.Select(ReportFor).ToList();
        if (reports.Count == 0 || reports.Any(r => r is null || r.Model != w.Model)) return TimeSpan.Zero;
        TimeSpan due = RefreshInterval - reports.Max(r => r!.Age);
        return due < TimeSpan.Zero ? TimeSpan.Zero : due;
    }

    /// <summary>Kullanıcı yenileme istedi ya da bir olay (uyanma, ağ) yenilemeyi gerektiriyor.</summary>
    public void RefreshSoon(TimeSpan? delay = null) => _timer.Change(delay ?? TimeSpan.Zero, Timeout.InfiniteTimeSpan);

    /// <summary>
    /// Ayarlar kaydedildi: konum eklendi/çıktı, model değişti ya da hava durumu yeniden açıldıysa hemen yenile.
    /// Yalnızca gösterilen konum değiştiyse eldeki raporla hemen güncellenir.
    /// </summary>
    public void SettingsChanged()
    {
        (string places, string model, bool wasShown) = (_lastPlaces, _lastModel, _lastShow);
        RememberSettings();
        Updated?.Invoke();
        if (_lastPlaces == places && _lastModel == model && !(_lastShow && !wasShown)) return;
        RefreshSoon();
    }

    private void RememberSettings()
    {
        WeatherSettings w = _settings.Weather;
        _lastPlaces = string.Join(";", w.Locations.Select(l => FormattableString.Invariant($"{l.Latitude:0.####},{l.Longitude:0.####}")));
        _lastModel = w.Model;
        _lastShow = _settings.IsShown(TaskbarItemId.Weather);
    }

    private async Task RefreshAsync()
    {
        if (!await _gate.WaitAsync(0)) return;
        TimeSpan next = RefreshInterval;
        try
        {
            WeatherSettings w = _settings.Weather;
            List<WeatherLocation> locations = w.Locations;
            if (locations.Count == 0 || !_settings.IsShown(TaskbarItemId.Weather)) return;

            string model = w.Model;
            var fresh = new List<WeatherReport>();
            Exception? error = null;
            foreach (WeatherLocation loc in locations)
            {
                try
                {
                    fresh.Add(await OpenMeteoClient.FetchAsync(loc.Latitude, loc.Longitude, model, _cts.Token));
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !_cts.IsCancellationRequested)
                {
                    error ??= ex;
                    if (ReportFor(loc) is { } old) fresh.Add(old); // eski veri soluk gösterilir
                }
            }
            _reports = fresh; // listeden çıkan konumların raporu da böylece silinir
            WeatherCache.Save(fresh);

            if (error is not null)
            {
                if (!_lastFailed) Log.Warn("Hava durumu alınamadı; 2 dakika sonra yeniden denenecek.", error);
                _lastFailed = true;
                next = RetryInterval;
            }
            else
            {
                if (_lastFailed) Log.Info("Hava durumu yeniden alınabildi.");
                _lastFailed = false;
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            return;
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
