using System.Globalization;
using System.IO;
using System.Text.Json;

namespace PulseMonitor.Weather;

/// <summary>
/// MET Norway Locationforecast 2.0 (ücretsiz, anahtarsız; CC BY 4.0). Saatlik tahmin yaklaşık 2,5 gün, sonrası 6 saatlik.
/// Yerel saat için saat dilimi gerekir (<see cref="WeatherTimeZone"/>). Verilmeyen alanlar boş kalır: yağış olasılığı
/// ve rüzgâr hamlesi bazı bölgelerde yok, son 30 gün ortalaması hiç yok (sıra dışı gün uyarısı çalışmaz).
/// Günlük en yüksek/en düşük, o güne düşen ölçümlerden bulunur; bugün için yalnızca kalan saatler vardır.
/// </summary>
internal static class MetNorwayClient
{
    private const int HoursAhead = 12;
    private const double MetersPerSecondToKmh = 3.6;

    public static async Task<WeatherReport> FetchAsync(double lat, double lon, CancellationToken token)
    {
        TimeZoneInfo zone = await WeatherTimeZone.ResolveAsync(lat, lon, token);
        string json = await MetNorwayCache.GetJsonAsync(lat, lon, token);
        using JsonDocument doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement, lat, lon, zone, DateTime.UtcNow);
    }

    internal static WeatherReport Parse(JsonElement root, double lat, double lon, TimeZoneInfo zone, DateTime nowUtc)
    {
        if (!root.TryGetProperty("properties", out JsonElement props) || !props.TryGetProperty("timeseries", out JsonElement series))
            throw new InvalidDataException("MET Norway yanıtında zaman serisi yok.");

        var points = new List<Point>();
        foreach (JsonElement e in series.EnumerateArray())
            if (ReadPoint(e) is { } p) points.Add(p);
        if (points.Count == 0) throw new InvalidDataException("MET Norway bu konum için veri vermedi.");

        // Şimdiki ölçüm: zamanı geçmemiş son nokta (seri genelde içinde bulunulan saatle başlar).
        int start = Math.Max(0, points.FindLastIndex(p => p.Utc <= nowUtc));
        Point now = points[start];
        int code = MetNorwaySymbols.ToWmo(now.Symbol1h ?? now.Symbol6h)
                   ?? throw new InvalidDataException("MET Norway hava simgesi tanınmadı.");

        var report = new WeatherReport
        {
            FetchedAtUtc = DateTime.UtcNow,
            Latitude = lat,
            Longitude = lon,
            Model = WeatherSources.MetNorway,
            UtcOffsetSeconds = (int)zone.GetUtcOffset(nowUtc).TotalSeconds,
            Temperature = now.Temperature,
            ApparentTemperature = now.Apparent ?? now.Temperature,
            Humidity = (int)Math.Round(now.Humidity ?? 0),
            WindKmh = (now.WindMs ?? 0) * MetersPerSecondToKmh,
            WeatherCode = code,
            IsDay = SunPosition.IsDaytime(lat, lon, nowUtc),
        };

        foreach (Point p in points.Skip(start).Where(p => p.Symbol1h is not null).Take(HoursAhead + 1))
        {
            if (MetNorwaySymbols.ToWmo(p.Symbol1h) is not { } hourCode) continue;
            report.Hours.Add(new HourForecast
            {
                Time = TimeZoneInfo.ConvertTimeFromUtc(p.Utc, zone),
                Temperature = p.Temperature,
                WeatherCode = hourCode,
                PrecipitationProbability = p.PrecipitationProbability1h,
                IsDay = SunPosition.IsDaytime(lat, lon, p.Utc),
                WindGustKmh = p.GustMs is { } g ? g * MetersPerSecondToKmh : null,
            });
        }

        AddDays(report, points, zone, nowUtc);
        return report;
    }

    private static void AddDays(WeatherReport report, List<Point> points, TimeZoneInfo zone, DateTime nowUtc)
    {
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone).Date;
        var byDay = points
            .Select(p => (Local: TimeZoneInfo.ConvertTimeFromUtc(p.Utc, zone), Point: p))
            .Where(x => x.Local.Date >= today)
            .GroupBy(x => x.Local.Date)
            .OrderBy(g => g.Key)
            .Take(OpenMeteoClient.DaysAhead);

        foreach (var day in byDay)
        {
            // Gün simgesi: öğleye en yakın ölçümün simgesi.
            var noon = day
                .Select(x => (x.Local, Code: MetNorwaySymbols.ToWmo(x.Point.Symbol1h ?? x.Point.Symbol6h)))
                .Where(x => x.Code is not null)
                .OrderBy(x => Math.Abs(x.Local.Hour - 13))
                .FirstOrDefault();
            if (noon.Code is not { } dayCode) continue;

            double high = day.Max(x => Math.Max(x.Point.Temperature, x.Point.Max6h ?? x.Point.Temperature));
            double low = day.Min(x => Math.Min(x.Point.Temperature, x.Point.Min6h ?? x.Point.Temperature));
            report.Days.Add(new DayForecast
            {
                Date = day.Key,
                High = high,
                Low = low,
                WeatherCode = dayCode,
                PrecipitationProbability = day.Select(x => x.Point.PrecipitationProbability1h ?? x.Point.PrecipitationProbability6h)
                    .Where(p => p is not null).DefaultIfEmpty(null).Max(),
            });
        }
    }

    /// <summary>Bir zaman noktası: anlık değerler + sonraki 1 ve 6 saatin özeti. Sıcaklığı olmayan nokta atlanır.</summary>
    private sealed record Point(
        DateTime Utc, double Temperature, double? Apparent, double? Humidity, double? WindMs, double? GustMs,
        string? Symbol1h, string? Symbol6h, double? Max6h, double? Min6h, int? PrecipitationProbability1h, int? PrecipitationProbability6h);

    private static Point? ReadPoint(JsonElement e)
    {
        if (!e.TryGetProperty("time", out JsonElement time) || time.GetString() is not { } ts ||
            !DateTime.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc) ||
            !e.TryGetProperty("data", out JsonElement data) ||
            !data.TryGetProperty("instant", out JsonElement instant) || !instant.TryGetProperty("details", out JsonElement d) ||
            Number(d, "air_temperature") is not { } temperature)
            return null;

        data.TryGetProperty("next_1_hours", out JsonElement n1);
        data.TryGetProperty("next_6_hours", out JsonElement n6);
        return new Point(utc, temperature, Number(d, "apparent_air_temperature"), Number(d, "relative_humidity"),
            Number(d, "wind_speed"), Number(d, "wind_speed_of_gust"), Symbol(n1), Symbol(n6),
            Number(Details(n6), "air_temperature_max"), Number(Details(n6), "air_temperature_min"),
            Percent(Details(n1), "probability_of_precipitation"), Percent(Details(n6), "probability_of_precipitation"));
    }

    private static string? Symbol(JsonElement next) =>
        next.ValueKind == JsonValueKind.Object && next.TryGetProperty("summary", out JsonElement s) &&
        s.TryGetProperty("symbol_code", out JsonElement c) ? c.GetString() : null;

    private static JsonElement Details(JsonElement next) =>
        next.ValueKind == JsonValueKind.Object && next.TryGetProperty("details", out JsonElement d) ? d : default;

    private static double? Number(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble() : null;

    private static int? Percent(JsonElement e, string name) => Number(e, name) is { } v ? (int)Math.Round(v) : null;
}
