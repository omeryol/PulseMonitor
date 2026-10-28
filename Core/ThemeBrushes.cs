using System.Windows;
using System.Windows.Media;
using PulseMonitor.Settings;

namespace PulseMonitor.Core;

/// <summary>
/// Temaya ve görünüm ayarlarına bağlı fırçaları uygulama kaynaklarına koyar ("Brush.&lt;anahtar&gt;").
/// Sabit renkler: Resources/Theme.xaml, öğe paletleri: Resources/Palettes.xaml.
/// </summary>
internal static class ThemeBrushes
{
    private static readonly string[] ThemeKeys =
    [
        "Text", "Muted", "Separator", "Load.Warn", "Load.Hot",
        "Temp.Freezing", "Temp.Cold", "Temp.Mild", "Temp.Warm", "Temp.Hot",
    ];

    private static readonly SolidColorBrush HitTestBrush = Frozen(Color.FromArgb(1, 0, 0, 0));

    /// <summary>Görev çubuğu teması: ayardaki tercih, "Otomatik"te Windows'un ayarı.</summary>
    public static bool IsLight(StyleSettings style, bool windowsTaskbarIsLight) => style.Theme switch
    {
        StyleOptions.ThemeLight => true,
        StyleOptions.ThemeDark => false,
        _ => windowsTaskbarIsLight,
    };

    public static void Apply(Application app, bool light, StyleSettings style)
    {
        string sfx = light ? "OnLight" : "OnDark";
        ResourceDictionary r = app.Resources;
        foreach (string key in ThemeKeys)
            r[$"Brush.{key}"] = app.FindResource($"Brush.{key}.{sfx}");

        string palette = style.Palette == StyleOptions.PaletteCustom ? StyleOptions.PaletteVivid : style.Palette;
        foreach (string item in StyleOptions.ColorItems)
        {
            Color c = style.Palette == StyleOptions.PaletteCustom &&
                      StyleOptions.TryParseColor(style.CustomColors.GetValueOrDefault(item), out Color custom)
                ? custom
                : (Color)app.FindResource($"Palette.{palette}.{item}.{sfx}");
            r[$"Brush.Item.{item}"] = Frozen(c);
        }

        // Zemin: kartlar ya da tüm pencerede tek zemin; saydamlık ayarı kartın en fazla opaklığını çarpar.
        Color baseCard = (Color)app.FindResource($"Color.Card.{sfx}");
        double maxAlpha = light ? 0.12 : 0.25;
        var surface = Frozen(Color.FromArgb((byte)Math.Round(255 * maxAlpha * style.BackgroundOpacity / 100.0), baseCard.R, baseCard.G, baseCard.B));
        r["Brush.Card"] = style.Background == StyleOptions.BgCards ? surface : Brushes.Transparent;
        // Tamamen saydam pencere tıklamayı alttaki görev çubuğuna geçirir; en az 1/255 opaklık kalır.
        r["Brush.WindowBg"] = style.Background == StyleOptions.BgSingle && surface.Color.A > 0 ? surface : HitTestBrush;

        r["Brush.Keyboard.On"] = style.KeyboardStyle == StyleOptions.KeysColor ? r["Brush.Item.Keyboard"] : r["Brush.Text"];
    }

    public static Brush Get(string key) =>
        Application.Current?.TryFindResource(key) as Brush ?? Brushes.White;

    private static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
