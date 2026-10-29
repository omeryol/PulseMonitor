using System.IO;
using System.Text.Json;

namespace PulseMonitor.Core;

/// <summary>Bir dil dosyası: kod (dosya adı), görünen ad ve metinler.</summary>
internal sealed record LanguageFile(string Code, string Name, string Culture, IReadOnlyDictionary<string, string> Texts);

/// <summary>
/// Dil dosyalarını okur: program klasöründeki <c>lang/*.json</c>. Türkçe programın içine de gömülüdür;
/// klasör silinse ya da dosya bozulsa bile arayüz metinsiz kalmaz. "_" ile başlayan anahtarlar bilgi alanıdır.
/// </summary>
internal static class LanguageFiles
{
    public const string BaseCode = "tr";

    private static string Folder => Path.Combine(AppContext.BaseDirectory, "lang");

    private static IReadOnlyList<LanguageFile>? _all;

    /// <summary>Kullanılabilir diller (Türkçe her zaman var); ada göre sıralı. Bir kez okunur (dil değişince program yeniden başlar).</summary>
    public static IReadOnlyList<LanguageFile> All() => _all ??= ReadAll();

    private static IReadOnlyList<LanguageFile> ReadAll()
    {
        var found = new Dictionary<string, LanguageFile>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(Folder))
        {
            foreach (string path in Directory.GetFiles(Folder, "*.json"))
            {
                LanguageFile? file = Read(Path.GetFileNameWithoutExtension(path).ToLowerInvariant(), () => File.OpenRead(path));
                if (file is not null) found[file.Code] = file;
            }
        }
        if (!found.ContainsKey(BaseCode)) found[BaseCode] = Embedded();
        return found.Values.OrderBy(f => f.Name, StringComparer.CurrentCulture).ToList();
    }

    /// <summary>Programın içine gömülü Türkçe (her durumda tam).</summary>
    public static LanguageFile Embedded() =>
        Read(BaseCode, () => typeof(LanguageFiles).Assembly.GetManifestResourceStream("PulseMonitor.lang.tr.json")
            ?? throw new FileNotFoundException("Gömülü tr.json yok."))
        ?? new LanguageFile(BaseCode, "Türkçe", "tr-TR", new Dictionary<string, string>());

    private static LanguageFile? Read(string code, Func<Stream> open)
    {
        try
        {
            using Stream stream = open();
            using JsonDocument doc = JsonDocument.Parse(stream, new JsonDocumentOptions { AllowTrailingCommas = true });
            var texts = new Dictionary<string, string>();
            string name = code, culture = code;
            foreach (JsonProperty p in doc.RootElement.EnumerateObject())
            {
                if (p.Value.ValueKind != JsonValueKind.String) continue;
                string value = p.Value.GetString() ?? "";
                if (p.Name == "_name") name = value;
                else if (p.Name == "_culture") culture = value;
                else if (!p.Name.StartsWith('_')) texts[p.Name] = value;
            }
            return new LanguageFile(code, name, culture, texts);
        }
        catch (Exception ex)
        {
            Log.Warn($"Dil dosyası okunamadı, atlandı: {code}.json", ex);
            return null;
        }
    }
}
