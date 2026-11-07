using System.Globalization;
using System.Net;
using System.Net.Http;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// MET Norway kullanım koşulları: yanıtın <c>Expires</c> süresi dolmadan yeniden istenmez, sonrasında
/// <c>If-Modified-Since</c> ile sorulur (değişmediyse 304). Yanıt metni bellekte konum başına tutulur.
/// </summary>
internal static class MetNorwayCache
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
    private static readonly Dictionary<string, Entry> Entries = new();

    private sealed record Entry(string Json, DateTimeOffset Expires, string? LastModified);

    public static async Task<string> GetJsonAsync(double lat, double lon, CancellationToken token)
    {
        // MET koordinatı en çok dört ondalık ister; fazlası yüksek yük ve engelleme nedeni olabilir.
        string place = FormattableString.Invariant($"lat={lat:0.####}&lon={lon:0.####}");
        Entry? cached;
        lock (Entries) Entries.TryGetValue(place, out cached);
        if (cached is not null && DateTimeOffset.UtcNow < cached.Expires) return cached.Json;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.met.no/weatherapi/locationforecast/2.0/complete?{place}");
        if (cached?.LastModified is { } since) request.Headers.TryAddWithoutValidation("If-Modified-Since", since);
        using HttpResponseMessage response = await Http.Client.SendAsync(request, token);

        string json;
        if (response.StatusCode == HttpStatusCode.NotModified && cached is not null)
        {
            json = cached.Json;
        }
        else
        {
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync(token);
        }

        DateTimeOffset expires = response.Content.Headers.Expires is { } e && e > DateTimeOffset.UtcNow
            ? e : DateTimeOffset.UtcNow + DefaultLifetime;
        string? modified = response.Content.Headers.LastModified?.UtcDateTime.ToString("R", CultureInfo.InvariantCulture)
                           ?? cached?.LastModified;
        lock (Entries) Entries[place] = new Entry(json, expires, modified);
        return json;
    }
}
