using PulseMonitor.Weather;

namespace PulseMonitor.Tests;

/// <summary>Sakin bir hava raporu üretir (20 °C, açık, rüzgâr 20 km/sa); testler yalnızca değiştirdiği alanı bilir.</summary>
internal static class ReportBuilder
{
    /// <summary>Konum saati = UTC (UtcOffsetSeconds 0): saat başına yuvarlanmış "şimdi".</summary>
    public static DateTime Now { get; } = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);

    public static WeatherReport Calm(Action<WeatherReport>? report = null, Action<int, HourForecast>? hour = null)
    {
        var r = new WeatherReport { UtcOffsetSeconds = 0, Temperature = 15 };
        for (int i = 0; i < 13; i++)
        {
            var h = new HourForecast { Time = Now.AddHours(i), Temperature = 15, WeatherCode = 1, PrecipitationProbability = 0, WindGustKmh = 20 };
            hour?.Invoke(i, h);
            r.Hours.Add(h);
        }
        for (int d = 0; d < 5; d++)
            r.Days.Add(new DayForecast { Date = Now.Date.AddDays(d), High = 20, Low = 10, WeatherCode = 1 });
        r.RecentHighMean = 20;
        r.RecentHighSd = 3;
        report?.Invoke(r);
        return r;
    }
}
