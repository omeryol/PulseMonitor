using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// Konum araması (tüm dünya). Windows'un bölgesindeki (ör. Türkiye) yerler önce gelir; başka ülkedeki yerlerin
/// yanına ülke adı eklenir. Open-Meteo iki kez sorulur (yalnızca kendi ülken + tüm dünya), yoksa dünyadaki aynı adlı
/// büyük yerler kendi ülkendeki köyleri listeden iter. İki kaynak birlikte sorgulanır: Open-Meteo (GeoNames; köy, kasaba, ilçe) ve
/// OpenStreetMap/Nominatim (ayrıca mahalleler). Aynı yer iki kaynakta da varsa tek sonuç gösterilir.
/// Nominatim saniyede 1 isteğe izin verir; arama yalnızca kullanıcı isteyince yapılır.
/// </summary>
internal static class LocationSearch
{
    private const int Limit = 8;
    private static readonly CompareInfo TrCompare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
    private static string HomeCountry => RegionInfo.CurrentRegion.TwoLetterISORegionName;

    /// <exception cref="HttpRequestException">İki kaynak da yanıt vermezse.</exception>
    public static async Task<List<WeatherLocation>> SearchAsync(string query, CancellationToken token)
    {
        string q = Uri.EscapeDataString(query.Trim());
        Task<List<Found>> openMeteo = OpenMeteoBothAsync(q, token);
        Task<List<Found>> osm = NominatimAsync(q, token);
        await Task.WhenAll(openMeteo.ContinueWith(_ => { }), osm.ContinueWith(_ => { }));

        if (openMeteo.IsFaulted && osm.IsFaulted)
            throw new HttpRequestException("Konum servislerine ulaşılamadı.", openMeteo.Exception?.InnerException);
        if (openMeteo.IsFaulted) Log.Warn("Open-Meteo konum araması başarısız.", openMeteo.Exception?.InnerException);
        if (osm.IsFaulted) Log.Warn("Nominatim konum araması başarısız.", osm.Exception?.InnerException);

        var results = openMeteo.IsCompletedSuccessfully ? openMeteo.Result : new List<Found>();
        if (osm.IsCompletedSuccessfully)
            results.AddRange(osm.Result.Where(o => !results.Any(r => IsDuplicate(r.Location, o.Location))));
        // Kendi ülkendeki yerler önce; OrderBy kararlıdır, kaynakların kendi sırası korunur.
        return results.OrderBy(r => r.IsHome ? 0 : 1).Select(r => r.Location).ToList();
    }

    /// <summary>Önce kendi ülkendeki, sonra dünyadaki sonuçlar; ikisi de gelmezse hata.</summary>
    private static async Task<List<Found>> OpenMeteoBothAsync(string q, CancellationToken token)
    {
        Task<List<Found>> home = OpenMeteoAsync(q, HomeCountry, token);
        Task<List<Found>> world = OpenMeteoAsync(q, null, token);
        await Task.WhenAll(home.ContinueWith(_ => { }), world.ContinueWith(_ => { }));
        if (!home.IsCompletedSuccessfully && !world.IsCompletedSuccessfully) return await world; // hatayı ilet
        var list = home.IsCompletedSuccessfully ? home.Result : new List<Found>();
        if (world.IsCompletedSuccessfully)
            list.AddRange(world.Result.Where(w => !list.Any(h => IsDuplicate(h.Location, w.Location))));
        return list;
    }

    private static async Task<List<Found>> OpenMeteoAsync(string q, string? onlyCountry, CancellationToken token)
    {
        string filter = onlyCountry is null ? "" : $"&countryCode={onlyCountry}";
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={q}&count={Limit}&language={UiLanguage.Code}{filter}&format=json";
        using JsonDocument doc = await GetJsonAsync(url, token);
        var list = new List<Found>();
        if (!doc.RootElement.TryGetProperty("results", out JsonElement results)) return list;

        foreach (JsonElement r in results.EnumerateArray())
        {
            string district = Clean(Str(r, "admin2")).Replace(" İlçesi", "", StringComparison.Ordinal);
            string country = Str(r, "country_code");
            list.Add(new Found(new WeatherLocation
            {
                Name = Str(r, "name"),
                Context = JoinContext(district, Str(r, "admin1"), Abroad(country, Str(r, "country"))),
                Latitude = r.GetProperty("latitude").GetDouble(),
                Longitude = r.GetProperty("longitude").GetDouble(),
                Source = "Open-Meteo",
            }, IsHome(country)));
        }
        return list;
    }

    private static async Task<List<Found>> NominatimAsync(string q, CancellationToken token)
    {
        string url = $"https://nominatim.openstreetmap.org/search?q={q}&format=jsonv2&addressdetails=1&limit={Limit}&accept-language={UiLanguage.Code}";
        using JsonDocument doc = await GetJsonAsync(url, token);
        var list = new List<Found>();
        foreach (JsonElement r in doc.RootElement.EnumerateArray())
        {
            JsonElement a = r.TryGetProperty("address", out JsonElement addr) ? addr : default;
            string district = First(a, "town", "county", "city_district", "municipality", "city");
            string province = First(a, "province", "state", "city");
            string country = First(a, "country_code");
            list.Add(new Found(new WeatherLocation
            {
                Name = Str(r, "name"),
                Context = JoinContext(district, province, Abroad(country, First(a, "country"))),
                Latitude = double.Parse(Str(r, "lat"), CultureInfo.InvariantCulture),
                Longitude = double.Parse(Str(r, "lon"), CultureInfo.InvariantCulture),
                Source = "OpenStreetMap",
            }, IsHome(country)));
        }
        return list.Where(l => l.Location.Name.Length > 0).ToList();
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

    private static string JoinContext(params string[] parts) =>
        string.Join(", ", parts.Where(s => s.Length > 0).Distinct());

    private static bool IsHome(string countryCode) => countryCode.Equals(HomeCountry, StringComparison.OrdinalIgnoreCase);

    /// <summary>Ülke adı yalnızca yer başka ülkedeyse gösterilir.</summary>
    private static string Abroad(string countryCode, string country) => IsHome(countryCode) ? "" : country;

    /// <summary>Arama sonucu ve Windows'un bölgesinde (kendi ülkende) olup olmadığı.</summary>
    private sealed record Found(WeatherLocation Location, bool IsHome);

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
