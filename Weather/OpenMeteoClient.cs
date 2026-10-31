using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>Open-Meteo tahmin servisi (ücretsiz, anahtarsız). Koordinat tam o noktanın yüksekliğine göre hesaplanır.</summary>
internal static class OpenMeteoClient
{
    private const int HoursAhead = 12;
    public const int DaysAhead = 5;
    private const int PastDays = 30, MinPastDays = 10;

    public static async Task<WeatherReport> FetchAsync(double lat, double lon, string model, CancellationToken token)
    {
        string url = FormattableString.Invariant(
            $"https://api.open-meteo.com/v1/forecast?latitude={lat:0.####}&longitude={lon:0.####}&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m,is_day&hourly=temperature_2m,weather_code,precipitation_probability,is_day,wind_gusts_10m&forecast_hours={HoursAhead + 1}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&past_days={PastDays}&forecast_days={DaysAhead}&timezone=auto");
        if (model != OpenMeteoModels.Auto) url += "&models=" + Uri.EscapeDataString(model);

        using HttpResponseMessage response = await Http.Client.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
        return Parse(doc.RootElement, lat, lon, model);
    }

    private static WeatherReport Parse(JsonElement root, double lat, double lon, string model)
    {
        JsonElement current = root.GetProperty("current");
        // Bazı modeller her yerde veri vermez; boş gelen değer hata sayılır (eski rapor korunur, yeniden denenir).
        if (current.GetProperty("temperature_2m").ValueKind != JsonValueKind.Number ||
            current.GetProperty("weather_code").ValueKind != JsonValueKind.Number)
            throw new InvalidDataException($"Open-Meteo bu konum için veri vermedi (model: {model}).");

        var report = new WeatherReport
        {
            FetchedAtUtc = DateTime.UtcNow,
            Latitude = lat,
            Longitude = lon,
            Model = model,
            UtcOffsetSeconds = root.TryGetProperty("utc_offset_seconds", out JsonElement off) ? off.GetInt32() : null,
            Temperature = current.GetProperty("temperature_2m").GetDouble(),
            ApparentTemperature = Number(current, "apparent_temperature") ?? current.GetProperty("temperature_2m").GetDouble(),
            Humidity = (int)Math.Round(Number(current, "relative_humidity_2m") ?? 0),
            WindKmh = Number(current, "wind_speed_10m") ?? 0,
            WeatherCode = current.GetProperty("weather_code").GetInt32(),
            IsDay = current.GetProperty("is_day").GetInt32() == 1,
        };

        JsonElement hourly = root.GetProperty("hourly");
        JsonElement times = hourly.GetProperty("time");
        JsonElement temps = hourly.GetProperty("temperature_2m");
        JsonElement codes = hourly.GetProperty("weather_code");
        JsonElement precip = hourly.GetProperty("precipitation_probability");
        JsonElement isDay = hourly.GetProperty("is_day");
        hourly.TryGetProperty("wind_gusts_10m", out JsonElement gusts);

        for (int i = 0; i < times.GetArrayLength(); i++)
        {
            if (temps[i].ValueKind != JsonValueKind.Number || codes[i].ValueKind != JsonValueKind.Number) continue;
            report.Hours.Add(new HourForecast
            {
                Time = DateTime.Parse(times[i].GetString()!, CultureInfo.InvariantCulture),
                Temperature = temps[i].GetDouble(),
                WeatherCode = codes[i].GetInt32(),
                PrecipitationProbability = precip[i].ValueKind == JsonValueKind.Number ? precip[i].GetInt32() : null,
                IsDay = isDay[i].ValueKind != JsonValueKind.Number || isDay[i].GetInt32() == 1,
                WindGustKmh = gusts.ValueKind == JsonValueKind.Array && gusts[i].ValueKind == JsonValueKind.Number ? gusts[i].GetDouble() : null,
            });
        }
        ParseDays(root, current, report);
        return report;
    }

    /// <summary>Günlük tahmin kartta ek bilgidir: eksik/boş gelirse anlık rapor yine de geçerli sayılır.</summary>
    private static void ParseDays(JsonElement root, JsonElement current, WeatherReport report)
    {
        if (!root.TryGetProperty("daily", out JsonElement daily)) return;
        if (!daily.TryGetProperty("time", out JsonElement times) ||
            !daily.TryGetProperty("temperature_2m_max", out JsonElement highs) ||
            !daily.TryGetProperty("temperature_2m_min", out JsonElement lows) ||
            !daily.TryGetProperty("weather_code", out JsonElement codes)) return;
        daily.TryGetProperty("precipitation_probability_max", out JsonElement precip);

        // Satırlar son 30 gün + bugün ve sonrası; "bugün", konumun yerel günüdür (current.time).
        DateTime today = current.TryGetProperty("time", out JsonElement t) && t.GetString() is { } ts
            ? DateTime.Parse(ts, CultureInfo.InvariantCulture).Date
            : DateTime.Today;
        var pastHighs = new List<double>();

        for (int i = 0; i < times.GetArrayLength(); i++)
        {
            if (highs[i].ValueKind != JsonValueKind.Number || lows[i].ValueKind != JsonValueKind.Number ||
                codes[i].ValueKind != JsonValueKind.Number) continue;
            DateTime date = DateTime.Parse(times[i].GetString()!, CultureInfo.InvariantCulture);
            if (date.Date < today)
            {
                pastHighs.Add(highs[i].GetDouble());
                continue;
            }
            report.Days.Add(new DayForecast
            {
                Date = date,
                High = highs[i].GetDouble(),
                Low = lows[i].GetDouble(),
                WeatherCode = codes[i].GetInt32(),
                PrecipitationProbability = precip.ValueKind == JsonValueKind.Array && precip[i].ValueKind == JsonValueKind.Number
                    ? (int)Math.Round(precip[i].GetDouble()) : null,
            });
        }

        if (pastHighs.Count < MinPastDays) return;
        double mean = pastHighs.Average();
        report.RecentHighMean = mean;
        report.RecentHighSd = Math.Sqrt(pastHighs.Average(h => (h - mean) * (h - mean)));
    }

    private static double? Number(JsonElement e, string name) =>
        e.GetProperty(name) is { ValueKind: JsonValueKind.Number } v ? v.GetDouble() : null;
}
