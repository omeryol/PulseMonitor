namespace PulseMonitor.Weather;

/// <summary>Open-Meteo yeniden-analizinden gelen günlük, geçmiş hava kaydı.</summary>
public sealed record HistoricalWeatherDay(DateOnly Date, double Low, double High, double PrecipitationMm, int WeatherCode);

public sealed record HistoricalWeatherReport(IReadOnlyList<HistoricalWeatherDay> Days)
{
    public double PrecipitationTotalMm => Days.Sum(day => day.PrecipitationMm);
    public double? AverageHigh => Days.Count == 0 ? null : Days.Average(day => day.High);
}

public enum WeatherSeason { Winter, Spring, Summer, Autumn }

/// <summary>Seçilen aralıkta görülen meteorolojik mevsimin kısa özeti.</summary>
public sealed record HistoricalSeasonSummary(WeatherSeason Season, int Days, double AverageHigh, double PrecipitationMm);

/// <summary>Geçen yılın aynı günleriyle ve mevsimlerle karşılaştırmada kullanılan ölçüler.</summary>
public sealed record HistoricalWeatherAnalysis(
    IReadOnlyList<HistoricalSeasonSummary> Seasons,
    double? AverageHighDifference,
    double? PrecipitationDifference);

public static class HistoricalWeatherAnalytics
{
    public static HistoricalWeatherAnalysis Analyze(HistoricalWeatherReport current, HistoricalWeatherReport? previousYear, double latitude)
    {
        var seasons = current.Days
            .GroupBy(day => SeasonFor(day.Date.Month, latitude))
            .OrderBy(group => Array.IndexOf(OrderedSeasons, group.Key))
            .Select(group => new HistoricalSeasonSummary(group.Key, group.Count(), group.Average(day => day.High), group.Sum(day => day.PrecipitationMm)))
            .ToArray();
        return new HistoricalWeatherAnalysis(
            seasons,
            Difference(current.AverageHigh, previousYear?.AverageHigh),
            previousYear is null ? null : current.PrecipitationTotalMm - previousYear.PrecipitationTotalMm);
    }

    private static readonly WeatherSeason[] OrderedSeasons = [WeatherSeason.Winter, WeatherSeason.Spring, WeatherSeason.Summer, WeatherSeason.Autumn];

    public static WeatherSeason SeasonFor(int month, double latitude)
    {
        WeatherSeason northern = month switch
        {
            12 or 1 or 2 => WeatherSeason.Winter,
            3 or 4 or 5 => WeatherSeason.Spring,
            6 or 7 or 8 => WeatherSeason.Summer,
            _ => WeatherSeason.Autumn,
        };
        if (latitude >= 0) return northern;
        return northern switch
        {
            WeatherSeason.Winter => WeatherSeason.Summer,
            WeatherSeason.Spring => WeatherSeason.Autumn,
            WeatherSeason.Summer => WeatherSeason.Winter,
            _ => WeatherSeason.Spring,
        };
    }

    private static double? Difference(double? value, double? other) => value is null || other is null ? null : value.Value - other.Value;
}

/// <summary>Tarihsel istek sınırları: tek konum, en fazla 20 yıl ve yalnızca tamamlanmış günler.</summary>
public readonly record struct HistoricalWeatherRange(DateOnly Start, DateOnly End)
{
    // Çok yıllı görünüm yıl düzeyinde özetlenir; bu sınır hem isteği hem çizim maliyetini öngörülebilir tutar.
    public const int MaxDays = 366 * 20;

    public int DayCount => End.DayNumber - Start.DayNumber + 1;
    public bool IsMultiYear => DayCount > 366;

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
