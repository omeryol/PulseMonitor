using System.Net.Http;

namespace PulseMonitor.Core;

/// <summary>Tek, paylaşılan HttpClient. Kimliğini belirten User-Agent gönderir (Nominatim kullanım koşulu).</summary>
internal static class Http
{
    public static HttpClient Client { get; } = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        string version = typeof(Http).Assembly.GetName().Version?.ToString(3) ?? "0";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"PulseMonitor/{version} (personal desktop app)");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr");
        return client;
    }
}
