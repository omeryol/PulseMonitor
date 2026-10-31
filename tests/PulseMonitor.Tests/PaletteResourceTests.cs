using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PulseMonitor.Tests;

/// <summary>
/// Her palet her öğe için iki temada da renk içermeli: eksik anahtar uygulamayı açılışta çökertir (FindResource).
/// Palet kimlikleri ve öğe listesi doğrudan Settings/StyleOptions.cs'den okunur, böylece yeni palet/öğe eklenince test de güncel kalır.
/// </summary>
public class PaletteResourceTests
{
    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "PulseMonitor.csproj"))) dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("PulseMonitor.csproj bulunamadı");
    }

    private static string Options => File.ReadAllText(Path.Combine(RepoRoot(), "Settings", "StyleOptions.cs"));

    private static string[] Palettes() =>
        Regex.Matches(Options, @"Palette(\w+) = ""(\w+)""").Select(m => m.Groups[2].Value).Where(id => id != "Custom").ToArray();

    private static string[] Items()
    {
        Match m = Regex.Match(Options, @"ColorItems = \[(.*?)\];", RegexOptions.Singleline);
        return Regex.Matches(m.Groups[1].Value, @"""(\w+)""").Select(x => x.Groups[1].Value).ToArray();
    }

    [Fact]
    public void Palette_and_item_lists_are_found()
    {
        Assert.Contains("Plain", Palettes());
        Assert.Contains("Download", Items());
    }

    [Fact]
    public void Every_palette_has_every_item_in_both_themes()
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = XDocument.Load(Path.Combine(RepoRoot(), "Resources", "Palettes.xaml")).Descendants()
            .Select(e => (string?)e.Attribute(x + "Key")).Where(k => k is not null).ToHashSet();

        var missing = (from palette in Palettes() from item in Items() from theme in new[] { "OnDark", "OnLight" }
                       let key = $"Palette.{palette}.{item}.{theme}" where !keys.Contains(key) select key).ToList();
        Assert.True(missing.Count == 0, "Eksik palet rengi: " + string.Join(", ", missing));
    }

    [Fact]
    public void Plain_palette_uses_the_theme_text_colors_only()
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var plain = XDocument.Load(Path.Combine(RepoRoot(), "Resources", "Palettes.xaml")).Descendants()
            .Where(e => ((string?)e.Attribute(x + "Key"))?.StartsWith("Palette.Plain.") == true)
            .Select(e => (Key: (string)e.Attribute(x + "Key")!, Value: e.Value)).ToList();

        Assert.NotEmpty(plain);
        Assert.All(plain, p => Assert.Equal(p.Key.EndsWith("OnDark") ? "#F3F4F6" : "#111827", p.Value));
    }
}
