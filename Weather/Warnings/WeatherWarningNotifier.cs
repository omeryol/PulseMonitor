using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Weather.Warnings;

/// <summary>
/// Gösterilen konumun raporundaki bildirimlik uyarıları Windows bildirimine çevirir; rahatsız etmemek için:
/// aynı olay (tür + gün) bir kez, günde en çok 3 bildirim, her denetimde en çok bir bildirim (önem sırasıyla),
/// sessiz saatlerde (23:00–07:00, ayardan kapatılır) bildirim yok, bir saatten eski rapor için bildirim yok.
/// Bastırılan olay "gösterildi" sayılmaz; sessiz saat bitince hâlâ geçerliyse gösterilir.
/// Kayıt (<see cref="WarningLedger"/>) diske yazılır, program yeniden başlayınca tekrar bildirilmez.
/// Arayüz iş parçacığında çağrılır.
/// </summary>
internal sealed class WeatherWarningNotifier
{
    private static readonly TimeSpan MaxReportAge = TimeSpan.FromHours(1);

    private readonly WarningLedger _ledger = WarningLedgerStore.Load();

    /// <summary>(başlık, metin, simge adı: Weather/WeatherCodes.cs IconKey adları).</summary>
    public event Action<string, string, string>? Raised;

    public void Check(AppSettings s, Func<WeatherLocation, WeatherReport?> reportFor)
    {
        int level = WarningLevels.From(s.Alerts.WeatherLevel);
        WeatherLocation? loc = s.Weather.Active;
        if (level == 0 || loc is null || reportFor(loc) is not { } report || report.Age > MaxReportAge) return;

        DateTime now = DateTime.Now;
        if (WarningLedger.IsQuiet(now, s.Alerts.WeatherQuietHours)) return;

        string place = $"{loc.Latitude:0.##},{loc.Longitude:0.##}";
        foreach (WeatherWarning w in WeatherWarningDetector.Detect(report, level).Where(w => w.Notify))
        {
            string key = $"{place}|{w.Key}";
            if (!_ledger.CanShow(key, now)) continue;
            _ledger.Mark(key, now);
            WarningLedgerStore.Save(_ledger);
            // Her bildirimde "resmî uyarı değildir" notu: kullanıcı ayarlara bakmadan da görsün.
            string text = WeatherWarningText.Format(w, report.LocalNow, s.Style.TemperatureUnit == StyleOptions.Fahrenheit) +
                          "\n\n" + Strings.Get("Weather.Warn.Disclaimer");
            Log.Info($"Hava bilgisi: {w.Kind} ({loc.ShownName})");
            Raised?.Invoke(string.Format(Strings.Get("Weather.Warn.Title"), loc.ShownName), text, w.IconKey);
            return; // her denetimde en çok bir bildirim
        }
    }
}
