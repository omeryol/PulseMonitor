using System.Windows.Media;

namespace PulseMonitor.Settings;

/// <summary>Görünüm seçeneklerinin kimlikleri, sınırları ve doğrulaması. Ekrandaki adlar: Strings.tr.xaml → "Style.&lt;grup&gt;.&lt;kimlik&gt;".</summary>
public static class StyleOptions
{
    public const string ThemeAuto = "Auto", ThemeDark = "Dark", ThemeLight = "Light";
    public const string PaletteVivid = "Vivid", PaletteSoft = "Soft", PaletteMono = "Mono", PaletteCustom = "Custom";
    public const string TargetValue = "Value", TargetLabel = "Label", TargetBoth = "Both";
    public const string WeightNormal = "Normal", WeightSemiBold = "SemiBold", WeightBold = "Bold";
    public const string ArrowThin = "Thin", ArrowTriangle = "Triangle", ArrowDashed = "Dashed", ArrowIcon = "Icon", ArrowText = "Text";
    public const string LabelsText = "Text", LabelsIcon = "Icon", LabelsNone = "None";
    public const string SparkNone = "None", SparkBehind = "Behind", SparkSide = "Side";
    public const string KeysDim = "Dim", KeysColor = "Color";
    public const string BgNone = "None", BgSingle = "Single", BgCards = "Cards";
    public const string IconColor = "Color", IconRealistic = "Realistic", IconLine = "Line", IconMono = "Mono", IconGlyph = "Glyph";
    /// <summary>Animasyonlu setler (Assets/WeatherAnim/&lt;set&gt;, tools/render_icons.ps1): klasör adı = kimlik.</summary>
    public const string IconMeteoconsFill = "MeteoconsFill", IconMeteoconsFlat = "MeteoconsFlat",
                        IconMeteoconsLine = "MeteoconsLine", IconMakinThings = "MakinThings";
    public static readonly string[] AnimatedIconSets = [IconMeteoconsFill, IconMeteoconsFlat, IconMeteoconsLine, IconMakinThings];
    public const string Celsius = "C", Fahrenheit = "F";
    public const string WxLabelWithPlace = "WithPlace", WxLabelInstead = "Instead", WxLabelNone = "None";

    public static readonly Dictionary<string, string[]> Groups = new()
    {
        ["Theme"] = [ThemeAuto, ThemeDark, ThemeLight],
        ["Palette"] = [PaletteVivid, PaletteSoft, PaletteMono, PaletteCustom],
        ["ColorTarget"] = [TargetValue, TargetLabel, TargetBoth],
        ["ValueWeight"] = [WeightNormal, WeightSemiBold, WeightBold],
        ["Arrows"] = [ArrowThin, ArrowTriangle, ArrowDashed, ArrowIcon, ArrowText],
        ["Labels"] = [LabelsText, LabelsIcon, LabelsNone],
        ["Sparkline"] = [SparkNone, SparkBehind, SparkSide],
        ["KeyboardStyle"] = [KeysDim, KeysColor],
        ["Background"] = [BgNone, BgSingle, BgCards],
        ["WeatherIcons"] = [IconMeteoconsFill, IconMeteoconsFlat, IconMeteoconsLine, IconMakinThings,
                            IconColor, IconRealistic, IconLine, IconMono, IconGlyph],
        ["TemperatureUnit"] = [Celsius, Fahrenheit],
        ["WeatherLabel"] = [WxLabelWithPlace, WxLabelInstead, WxLabelNone],
    };

    /// <summary>Renklendirilen öğeler (Palettes.xaml anahtarları: Palette.&lt;palet&gt;.&lt;öğe&gt;.OnDark/OnLight).</summary>
    public static readonly string[] ColorItems = ["Upload", "Download", "Cpu", "Ram", "Gpu", "Disk", "Keyboard", "Ip", "Ping"];

    public const int MinFont = 9, MaxFont = 14;
    public const int MinOpacity = 0, MaxOpacity = 100;
    public const int MinColumnGap = 2, MaxColumnGap = 30;
    public const int MinLabelGap = 0, MaxLabelGap = 24;
    public const int MinScale = 70, MaxScale = 110;

    /// <summary>Elle düzenlenmiş ya da eski ayar dosyasındaki geçersiz değerleri varsayılana çeker.</summary>
    internal static void Normalize(AppSettings settings)
    {
        (settings.Alerts ??= new AlertSettings()).Normalize();
        if (string.IsNullOrWhiteSpace(settings.PingHost)) settings.PingHost = "1.1.1.1";
        StyleSettings s = settings.Style ??= new StyleSettings();
        var d = new StyleSettings();
        s.Theme = Valid("Theme", s.Theme, d.Theme);
        s.Palette = Valid("Palette", s.Palette, d.Palette);
        s.ColorTarget = Valid("ColorTarget", s.ColorTarget, d.ColorTarget);
        s.ValueWeight = Valid("ValueWeight", s.ValueWeight, d.ValueWeight);
        s.Arrows = Valid("Arrows", s.Arrows, d.Arrows);
        s.Labels = Valid("Labels", s.Labels, d.Labels);
        s.Sparkline = Valid("Sparkline", s.Sparkline, d.Sparkline);
        s.KeyboardStyle = Valid("KeyboardStyle", s.KeyboardStyle, d.KeyboardStyle);
        s.Background = Valid("Background", s.Background, d.Background);
        s.WeatherIcons = Valid("WeatherIcons", s.WeatherIcons, d.WeatherIcons);
        s.TemperatureUnit = Valid("TemperatureUnit", s.TemperatureUnit, d.TemperatureUnit);
        s.WeatherLabel = Valid("WeatherLabel", s.WeatherLabel, d.WeatherLabel);
        s.FontSize = Math.Clamp(s.FontSize, MinFont, MaxFont);
        s.BackgroundOpacity = Math.Clamp(s.BackgroundOpacity, MinOpacity, MaxOpacity);
        s.ColumnGap = Math.Clamp(s.ColumnGap, MinColumnGap, MaxColumnGap);
        s.LabelGap = Math.Clamp(s.LabelGap, MinLabelGap, MaxLabelGap);
        s.Scale = Math.Clamp(s.Scale, MinScale, MaxScale);
        s.CustomColors = (s.CustomColors ?? new())
            .Where(kv => ColorItems.Contains(kv.Key) && TryParseColor(kv.Value, out _))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public static bool TryParseColor(string? hex, out Color color)
    {
        color = default;
        if (hex is not { Length: 7 } || hex[0] != '#') return false;
        try
        {
            color = (Color)ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Valid(string group, string? value, string fallback) =>
        value is not null && Groups[group].Contains(value) ? value : fallback;
}
