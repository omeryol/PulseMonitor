using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>Open-Meteo tahmin servisi (ücretsiz, anahtarsız). Koordinat tam o noktanın yüksekliğine göre hesaplanır.</summary>
internal static class OpenMeteoClient
{
    private const int HoursAhead = 12;

    public static async Task<WeatherReport> FetchAsync(double lat, double lon, CancellationToken token)
    {
        string url = FormattableString.Invariant(
            $"https://api.open-meteo.com/v1/forecast?latitude={lat:0.####}&longitude={lon:0.####}&current=temperature_2m,apparent_temperature,relative_humidity_2m,weather_code,wind_speed_10m,is_day&hourly=temperature_2m,weather_code,precipitation_probability,is_day&forecast_hours={HoursAhead + 1}&timezone=auto");

        using HttpResponseMessage response = await Http.Client.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
        return Parse(doc.RootElement, lat, lon);
    }

    private static WeatherReport Parse(JsonElement root, double lat, double lon)
    {
        JsonElement current = root.GetProperty("current");
        var report = new WeatherReport
        {
            FetchedAtUtc = DateTime.UtcNow,
            Latitude = lat,
            Longitude = lon,
            Temperature = current.GetProperty("temperature_2m").GetDouble(),
            ApparentTemperature = current.GetProperty("apparent_temperature").GetDouble(),
            Humidity = (int)Math.Round(current.GetProperty("relative_humidity_2m").GetDouble()),
            WindKmh = current.GetProperty("wind_speed_10m").GetDouble(),
            WeatherCode = current.GetProperty("weather_code").GetInt32(),
            IsDay = current.GetProperty("is_day").GetInt32() == 1,
        };

        JsonElement hourly = root.GetProperty("hourly");
        JsonElement times = hourly.GetProperty("time");
        JsonElement temps = hourly.GetProperty("temperature_2m");
        JsonElement codes = hourly.GetProperty("weather_code");
        JsonElement precip = hourly.GetProperty("precipitation_probability");
        JsonElement isDay = hourly.GetProperty("is_day");

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
            });
        }
        return report;
    }
}
