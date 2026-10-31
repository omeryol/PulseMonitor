namespace PulseMonitor.Weather.Warnings;

/// <summary>
/// Raporun saatlik (12 saat) ve günlük (5 gün) tahmininden sert hava olaylarını bulur. Saf mantık: ayar, arayüz ve saat
/// bilmez, yalnızca raporu okur. Eşikler sabittir; seviye hangi olayların listeye gireceğini belirler.
/// Tek saatlik model gürültüsü sayılmasın diye yağışlı olaylar yağış olasılığına bağlıdır.
/// </summary>
public static class WeatherWarningDetector
{
    public const double ExtremeGustKmh = 90, StrongGustKmh = 65;
    public const double HeatC = 40, ColdC = -10;
    public const double HourlySwingC = 8, DayShiftC = 10;
    public const double AnomalyMinC = 6, AnomalySdFactor = 1.5;

    /// <param name="level">1 yalnızca aşırı, 2 dengeli, 3 hassas; 0 kapalı (boş liste).</param>
    /// <returns>Önem sırasıyla (önce aşırı, aynı seviyede erken olan).</returns>
    public static List<WeatherWarning> Detect(WeatherReport r, int level)
    {
        var found = new List<WeatherWarning>();
        if (level < 1) return found;

        DateTime now = r.LocalNow;
        List<HourForecast> hours = r.Hours.Where(h => h.Time > now.AddHours(-1)).OrderBy(h => h.Time).ToList();

        Hourly(hours, found);
        Gust(hours, found);
        Extremes(r, now, found);
        Swing(hours, found);
        DayShift(r, now, found);
        Anomaly(r, now, found);

        return found.Where(w => w.MinLevel <= level).OrderBy(w => w.MinLevel).ThenBy(w => w.At).ToList();
    }

    private static void Hourly(List<HourForecast> hours, List<WeatherWarning> found)
    {
        foreach (HourForecast h in hours)
        {
            int p = h.PrecipitationProbability ?? 100;
            switch (h.WeatherCode)
            {
                case 95 or 96 or 99 when p >= 50:
                    AddOnce(found, new WeatherWarning(h.WeatherCode == 95 ? WarningKind.Thunder : WarningKind.Hail, 1, h.Time));
                    break;
                case 66 or 67 when p >= 40:
                    AddOnce(found, new WeatherWarning(WarningKind.FreezingRain, 1, h.Time));
                    break;
                case 65 or 82 when p >= 60:
                    AddOnce(found, new WeatherWarning(WarningKind.HeavyRain, 2, h.Time));
                    break;
                case 75 or 86 when p >= 50:
                    AddOnce(found, new WeatherWarning(WarningKind.HeavySnow, 2, h.Time));
                    break;
            }
        }
    }

    private static void Gust(List<HourForecast> hours, List<WeatherWarning> found)
    {
        HourForecast? peak = hours.Where(h => h.WindGustKmh >= StrongGustKmh).MaxBy(h => h.WindGustKmh);
        if (peak is null) return;
        double kmh = peak.WindGustKmh!.Value;
        found.Add(new WeatherWarning(WarningKind.Gust, kmh >= ExtremeGustKmh ? 1 : 2, peak.Time, kmh));
    }

    /// <summary>Bugün ve yarın için aşırı sıcak / soğuk.</summary>
    private static void Extremes(WeatherReport r, DateTime now, List<WeatherWarning> found)
    {
        foreach (DayForecast d in r.Days.Where(d => d.Date.Date >= now.Date && d.Date.Date <= now.Date.AddDays(1)))
        {
            if (d.High >= HeatC) found.Add(new WeatherWarning(WarningKind.Heat, 1, d.Date.Date, d.High));
            if (d.Low <= ColdC) found.Add(new WeatherWarning(WarningKind.Cold, 1, d.Date.Date, d.Low));
        }
    }

    /// <summary>Önümüzdeki saatlerde şimdiki sıcaklığa göre en büyük ani sapma.</summary>
    private static void Swing(List<HourForecast> hours, List<WeatherWarning> found)
    {
        if (hours.Count < 2) return;
        double start = hours[0].Temperature;
        HourForecast? worst = hours.Skip(1).MaxBy(h => Math.Abs(h.Temperature - start));
        if (worst is null) return;
        double delta = worst.Temperature - start;
        if (Math.Abs(delta) < HourlySwingC) return;
        found.Add(new WeatherWarning(delta < 0 ? WarningKind.Drop : WarningKind.Rise, 2, worst.Time, Math.Abs(delta)));
    }

    /// <summary>Yarının en yüksek sıcaklığı bugünküden belirgin farklıysa.</summary>
    private static void DayShift(WeatherReport r, DateTime now, List<WeatherWarning> found)
    {
        DayForecast? today = r.Days.FirstOrDefault(d => d.Date.Date == now.Date);
        DayForecast? tomorrow = r.Days.FirstOrDefault(d => d.Date.Date == now.Date.AddDays(1));
        if (today is null || tomorrow is null) return;
        double delta = tomorrow.High - today.High;
        if (Math.Abs(delta) < DayShiftC) return;
        found.Add(new WeatherWarning(delta < 0 ? WarningKind.DayDrop : WarningKind.DayRise, 2, tomorrow.Date.Date, Math.Abs(delta)));
    }

    /// <summary>
    /// Bugün ve sonraki iki günden, son 30 günün ortalamasından hem en az 6 °C hem de 1,5 standart sapma ayrışan en uç gün.
    /// Mevsim geçişinde ortalama geride kalır; bu yüzden eşik yüksektir ve uyarı yalnızca kartta gösterilir.
    /// </summary>
    private static void Anomaly(WeatherReport r, DateTime now, List<WeatherWarning> found)
    {
        if (r.RecentHighMean is not { } mean || r.RecentHighSd is not { } sd) return;
        double needed = Math.Max(AnomalyMinC, AnomalySdFactor * sd);
        DayForecast? day = r.Days.Where(d => d.Date.Date >= now.Date && d.Date.Date <= now.Date.AddDays(2))
            .Where(d => Math.Abs(d.High - mean) >= needed)
            .MaxBy(d => Math.Abs(d.High - mean));
        if (day is null) return;
        double delta = day.High - mean;
        found.Add(new WeatherWarning(delta > 0 ? WarningKind.AnomalyWarm : WarningKind.AnomalyCool, 3, day.Date.Date, Math.Abs(delta)));
    }

    /// <summary>Aynı tür ve gün için yalnızca ilk (en erken) saat eklenir.</summary>
    private static void AddOnce(List<WeatherWarning> found, WeatherWarning w)
    {
        if (found.All(f => f.Key != w.Key)) found.Add(w);
    }
}
