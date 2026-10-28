using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// Konum araması (Türkiye). İki kaynak birlikte sorgulanır: Open-Meteo (GeoNames; köy, kasaba, ilçe) ve
/// OpenStreetMap/Nominatim (ayrıca mahalleler). Aynı yer iki kaynakta da varsa tek sonuç gösterilir.
/// Nominatim saniyede 1 isteğe izin verir; arama yalnızca kullanıcı isteyince yapılır.
/// </summary>
internal static class LocationSearch
{
    private const int Limit = 8;
    private static readonly CompareInfo TrCompare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    /// <exception cref="HttpRequestException">İki kaynak da yanıt vermezse.</exception>
    public static async Task<List<WeatherLocation>> SearchAsync(string query, CancellationToken token)
    {
        string q = Uri.EscapeDataString(query.Trim());
        Task<List<WeatherLocation>> openMeteo = OpenMeteoAsync(q, token);
        Task<List<WeatherLocation>> osm = NominatimAsync(q, token);
        await Task.WhenAll(openMeteo.ContinueWith(_ => { }), osm.ContinueWith(_ => { }));

        if (openMeteo.IsFaulted && osm.IsFaulted)
            throw new HttpRequestException("Konum servislerine ulaşılamadı.", openMeteo.Exception?.InnerException);
        if (openMeteo.IsFaulted) Log.Warn("Open-Meteo konum araması başarısız.", openMeteo.Exception?.InnerException);
        if (osm.IsFaulted) Log.Warn("Nominatim konum araması başarısız.", osm.Exception?.InnerException);

        var results = openMeteo.IsCompletedSuccessfully ? openMeteo.Result : new List<WeatherLocation>();
        if (osm.IsCompletedSuccessfully)
            results.AddRange(osm.Result.Where(o => !results.Any(r => IsDuplicate(r, o))));
        return results;
    }

    private static async Task<List<WeatherLocation>> OpenMeteoAsync(string q, CancellationToken token)
    {
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={q}&count={Limit}&language=tr&countryCode=TR&format=json";
        using JsonDocument doc = await GetJsonAsync(url, token);
        var list = new List<WeatherLocation>();
        if (!doc.RootElement.TryGetProperty("results", out JsonElement results)) return list;

        foreach (JsonElement r in results.EnumerateArray())
        {
            string district = Clean(Str(r, "admin2")).Replace(" İlçesi", "", StringComparison.Ordinal);
            list.Add(new WeatherLocation
            {
                Name = Str(r, "name"),
                Context = JoinContext(district, Str(r, "admin1")),
                Latitude = r.GetProperty("latitude").GetDouble(),
                Longitude = r.GetProperty("longitude").GetDouble(),
                Source = "Open-Meteo",
            });
        }
        return list;
    }

    private static async Task<List<WeatherLocation>> NominatimAsync(string q, CancellationToken token)
    {
        string url = $"https://nominatim.openstreetmap.org/search?q={q}&format=jsonv2&addressdetails=1&limit={Limit}&countrycodes=tr&accept-language=tr";
        using JsonDocument doc = await GetJsonAsync(url, token);
        var list = new List<WeatherLocation>();
        foreach (JsonElement r in doc.RootElement.EnumerateArray())
        {
            JsonElement a = r.TryGetProperty("address", out JsonElement addr) ? addr : default;
            string district = First(a, "town", "county", "city_district", "municipality", "city");
            string province = First(a, "province", "state", "city");
            list.Add(new WeatherLocation
            {
                Name = Str(r, "name"),
                Context = JoinContext(district, province),
                Latitude = double.Parse(Str(r, "lat"), CultureInfo.InvariantCulture),
                Longitude = double.Parse(Str(r, "lon"), CultureInfo.InvariantCulture),
                Source = "OpenStreetMap",
            });
        }
        return list.Where(l => l.Name.Length > 0).ToList();
    }

    private static async Task<JsonDocument> GetJsonAsync(string url, CancellationToken token)
    {
        using HttpResponseMessage response = await Http.Client.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
    }

    /// <summary>Aynı ad (ya da biri diğerini içeriyor, ör. "Kayaköy Mahallesi") ve 3 km'den yakın.</summary>
    private static bool IsDuplicate(WeatherLocation a, WeatherLocation b)
    {
        bool sameName = TrCompare.IndexOf(a.Name, b.Name, CompareOptions.IgnoreCase) >= 0 ||
                        TrCompare.IndexOf(b.Name, a.Name, CompareOptions.IgnoreCase) >= 0;
        double dLat = (a.Latitude - b.Latitude) * 111;
        double dLon = (a.Longitude - b.Longitude) * 111 * Math.Cos(a.Latitude * Math.PI / 180);
        return sameName && dLat * dLat + dLon * dLon < 9;
    }

    private static string JoinContext(string district, string province) =>
        string.Join(", ", new[] { district, province }.Where(s => s.Length > 0).Distinct());

    private static string First(JsonElement e, params string[] keys)
    {
        if (e.ValueKind != JsonValueKind.Object) return string.Empty;
        foreach (string k in keys)
            if (Clean(Str(e, k)) is { Length: > 0 } v) return v;
        return string.Empty;
    }

    private static string Str(JsonElement e, string key) =>
        e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string Clean(string s) => s.Trim();
}
