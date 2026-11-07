using System.Globalization;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// Bir koordinatın saat dilimi. MET Norway yalnızca UTC saat verir; konumun yerel saati için saat dilimi gerekir.
/// Open-Meteo'dan (küçük bir istek) bir kez öğrenilir ve oturum boyunca saklanır; alınamazsa boylamdan tahmin edilir
/// (tahmin saklanmaz, sonraki yenilemede yeniden denenir).
/// </summary>
internal static class WeatherTimeZone
{
    private static readonly Dictionary<string, TimeZoneInfo> Known = new();

    public static async Task<TimeZoneInfo> ResolveAsync(double lat, double lon, CancellationToken token)
    {
        string key = FormattableString.Invariant($"{lat:0.##},{lon:0.##}");
        lock (Known)
            if (Known.TryGetValue(key, out TimeZoneInfo? known)) return known;

        try
        {
            string url = FormattableString.Invariant(
                $"https://api.open-meteo.com/v1/forecast?latitude={lat:0.####}&longitude={lon:0.####}&current=is_day&forecast_days=1&timezone=auto");
            using System.Net.Http.HttpResponseMessage response = await Http.Client.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
            JsonElement root = doc.RootElement;
            string? name = root.TryGetProperty("timezone", out JsonElement n) ? n.GetString() : null;
            int offset = root.TryGetProperty("utc_offset_seconds", out JsonElement o) ? o.GetInt32() : (int)Approximate(lon).BaseUtcOffset.TotalSeconds;
            TimeZoneInfo zone = FromName(name, offset);
            lock (Known) Known[key] = zone;
            return zone;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
        {
            Log.Warn("Konumun saat dilimi alınamadı; boylamdan tahmin edildi.", ex);
            return Approximate(lon);
        }
    }

    /// <summary>IANA adı Windows'ta bulunursa o (yaz saati dahil), yoksa verilen sabit fark.</summary>
    internal static TimeZoneInfo FromName(string? name, int offsetSeconds)
    {
        if (!string.IsNullOrEmpty(name))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(name); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone(
            string.Create(CultureInfo.InvariantCulture, $"UTC{offsetSeconds}"), TimeSpan.FromSeconds(offsetSeconds), name ?? "UTC", name ?? "UTC");
    }

    /// <summary>Boylamdan tam saatlik tahmin (15° = 1 saat); yalnızca saat dilimi alınamazsa.</summary>
    internal static TimeZoneInfo Approximate(double lon) =>
        FromName(null, (int)Math.Round(Math.Clamp(lon, -180, 180) / 15) * 3600);
}
