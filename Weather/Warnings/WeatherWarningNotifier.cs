using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Weather.Warnings;

/// <summary>
/// Gösterilen konumun raporundaki bildirimlik uyarıları Windows bildirimine çevirir; rahatsız etmemek için:
/// aynı olay (tür + gün) bir kez, günde en çok 3 bildirim, her denetimde en çok bir bildirim (önem sırasıyla),
/// sessiz saatlerde (23:00–07:00, ayardan kapatılır) bildirim yok, bir saatten eski rapor için bildirim yok.
/// Bastırılan olay "gösterildi" sayılmaz; sessiz saat bitince hâlâ geçerliyse gösterilir. Kayıt bellektedir.
/// Arayüz iş parçacığında çağrılır.
/// </summary>
internal sealed class WeatherWarningNotifier
{
    private const int DailyLimit = 3;
    private static readonly TimeSpan MaxReportAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan Remember = TimeSpan.FromHours(36);

    private readonly Dictionary<string, DateTime> _shown = new();
    private DateOnly _day;
    private int _today;

    /// <summary>(başlık, metin, simge adı: Weather/WeatherCodes.cs IconKey adları).</summary>
    public event Action<string, string, string>? Raised;

    public void Check(AppSettings s, Func<WeatherLocation, WeatherReport?> reportFor)
    {
        int level = WarningLevels.From(s.Alerts.WeatherLevel);
        WeatherLocation? loc = s.Weather.Active;
        if (level == 0 || loc is null || reportFor(loc) is not { } report || report.Age > MaxReportAge) return;

        DateTime now = DateTime.Now;
        if (s.Alerts.WeatherQuietHours && (now.Hour >= 23 || now.Hour < 7)) return;

        DateOnly day = DateOnly.FromDateTime(now);
        if (day != _day) (_day, _today) = (day, 0);
        if (_today >= DailyLimit) return;
        foreach (string old in _shown.Where(kv => now - kv.Value > Remember).Select(kv => kv.Key).ToList()) _shown.Remove(old);

        string place = $"{loc.Latitude:0.##},{loc.Longitude:0.##}";
        foreach (WeatherWarning w in WeatherWarningDetector.Detect(report, level).Where(w => w.Notify))
        {
            string key = $"{place}|{w.Key}";
            if (_shown.ContainsKey(key)) continue;
            _shown[key] = now;
            _today++;
            string text = WeatherWarningText.Format(w, report.LocalNow, s.Style.TemperatureUnit == StyleOptions.Fahrenheit);
            Log.Info($"Hava bilgisi: {w.Kind} ({loc.ShownName})");
            Raised?.Invoke(string.Format(Strings.Get("Weather.Warn.Title"), loc.ShownName), text, w.IconKey);
            return; // her denetimde en çok bir bildirim
        }
    }
}
