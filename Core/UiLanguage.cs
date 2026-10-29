using System.Globalization;
using System.Windows;

namespace PulseMonitor.Core;

/// <summary>
/// Seçili dilin metinlerini uygulama kaynaklarına koyar (pencereler DynamicResource, kod <see cref="Strings"/> ile okur)
/// ve sayı/tarih biçimi için kültürü belirler. Eksik metin İngilizceden, o da yoksa Türkçeden gelir.
/// </summary>
internal static class UiLanguage
{
    public const string Auto = "auto";
    private const string FallbackCode = "en";

    /// <summary>Etkin dil kodu (ör. "tr", "en").</summary>
    public static string Code { get; private set; } = LanguageFiles.BaseCode;

    /// <summary>Sayı ve tarih biçimi için etkin kültür.</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Uygulanan ayar değeri ("auto" ya da dil kodu); değişince program yeniden başlatılır.</summary>
    public static string Setting { get; private set; } = Auto;

    public static void Apply(Application app, string? setting)
    {
        Setting = string.IsNullOrWhiteSpace(setting) ? Auto : setting;
        IReadOnlyList<LanguageFile> all = LanguageFiles.All();
        LanguageFile chosen = Resolve(all, Setting);

        // Katmanlar: Türkçe (gömülü, tam) → İngilizce → seçilen. Sonraki katman öncekinin üstüne yazar.
        var layers = new List<LanguageFile> { LanguageFiles.Embedded() };
        LanguageFile? en = all.FirstOrDefault(f => f.Code == FallbackCode);
        if (en is not null && chosen.Code is not (LanguageFiles.BaseCode or FallbackCode)) layers.Add(en);
        layers.Add(chosen);

        var dict = new ResourceDictionary();
        foreach (LanguageFile layer in layers)
            foreach (var (key, value) in layer.Texts) dict[key] = value;
        app.Resources.MergedDictionaries.Add(dict);

        Code = chosen.Code;
        Culture = ToCulture(chosen.Culture);
        Log.Info($"Dil: {Code} ({Setting}), {dict.Count} metin.");
    }

    /// <summary>Dil seçimi listesi (ayarlar ve menü): önce "Sistem dili", sonra her dil kendi adıyla.</summary>
    public static IReadOnlyList<LanguageOption> Options() =>
        new[] { new LanguageOption(Auto, Strings.Get("Language.Auto")) }
            .Concat(LanguageFiles.All().Select(f => new LanguageOption(f.Code, f.Name)))
            .ToList();

    /// <summary>"auto": Windows'un arayüz dili varsa o, yoksa İngilizce, o da yoksa Türkçe.</summary>
    private static LanguageFile Resolve(IReadOnlyList<LanguageFile> all, string setting)
    {
        LanguageFile? Find(string code) => all.FirstOrDefault(f => f.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (setting != Auto && Find(setting) is { } exact) return exact;
        return Find(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            ?? Find(FallbackCode)
            ?? all.First(f => f.Code == LanguageFiles.BaseCode);
    }

    private static CultureInfo ToCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException ex)
        {
            Log.Warn($"Dil dosyasındaki kültür tanınmadı: {name}", ex);
            return CultureInfo.InvariantCulture;
        }
    }
}

public sealed record LanguageOption(string Code, string Display);
