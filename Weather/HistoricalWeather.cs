namespace PulseMonitor.Weather;

/// <summary>Open-Meteo yeniden-analizinden gelen günlük, geçmiş hava kaydı.</summary>
public sealed record HistoricalWeatherDay(DateOnly Date, double Low, double High, double PrecipitationMm, int WeatherCode);

public sealed record HistoricalWeatherReport(IReadOnlyList<HistoricalWeatherDay> Days)
{
    public double PrecipitationTotalMm => Days.Sum(day => day.PrecipitationMm);
    public double? AverageHigh => Days.Count == 0 ? null : Days.Average(day => day.High);
}

/// <summary>Tarihsel istek sınırları: tek konum, en fazla 366 gün ve yalnızca tamamlanmış günler.</summary>
public readonly record struct HistoricalWeatherRange(DateOnly Start, DateOnly End)
{
    public const int MaxDays = 366;

    public static bool TryCreate(DateTime? start, DateTime? end, out HistoricalWeatherRange range)
    {
        range = default;
        if (start is not { } from || end is not { } to) return false;
        DateOnly first = DateOnly.FromDateTime(from);
        DateOnly last = DateOnly.FromDateTime(to);
        DateOnly latest = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        if (first > last || last > latest || last.DayNumber - first.DayNumber + 1 > MaxDays) return false;
        range = new HistoricalWeatherRange(first, last);
        return true;
    }
}
