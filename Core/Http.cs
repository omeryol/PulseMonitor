using System.Net;
using System.Net.Http;

namespace PulseMonitor.Core;

/// <summary>
/// Tek, paylaşılan HttpClient. Kimliğini ve proje adresini belirten User-Agent gönderir (Nominatim ve MET Norway
/// kullanım koşulu; boş ya da genel değer engellenir). Yanıt sıkıştırması açıktır (MET Norway ister).
/// </summary>
internal static class Http
{
    public static HttpClient Client { get; } = Create();

    private static HttpClient Create()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        string version = typeof(Http).Assembly.GetName().Version?.ToString(3) ?? "0";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"PulseMonitor/{version} (+https://github.com/omeryol/PulseMonitor)");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(UiLanguage.Code);
        return client;
    }
}
