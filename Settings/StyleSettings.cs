namespace PulseMonitor.Settings;

/// <summary>
/// Görünüm ayarları (tasarım tezgâhındaki seçeneklerin tamamı). Varsayılanlar kullanıcının tezgâhta seçtiği tasarımdır.
/// Geçerli değerler ve sınırlar: <see cref="StyleOptions"/>.
/// </summary>
public sealed class StyleSettings
{
    // Renk
    public string Theme { get; set; } = StyleOptions.ThemeAuto;
    public string Palette { get; set; } = StyleOptions.PaletteVivid;
    /// <summary>Özel palette öğe → #RRGGBB (iki temada da kullanılır).</summary>
    public Dictionary<string, string> CustomColors { get; set; } = new();
    public string ColorTarget { get; set; } = StyleOptions.TargetBoth;
    public bool LoadColors { get; set; } = true;

    // Yazı ve işaretler
    public int FontSize { get; set; } = 11;
    public string ValueWeight { get; set; } = StyleOptions.WeightSemiBold;
    public string Arrows { get; set; } = StyleOptions.ArrowTriangle;
    public string Labels { get; set; } = StyleOptions.LabelsText;
    public string Sparkline { get; set; } = StyleOptions.SparkBehind;
    public string KeyboardStyle { get; set; } = StyleOptions.KeysColor;

    // Zemin ve boyut
    public string Background { get; set; } = StyleOptions.BgNone;
    public int BackgroundOpacity { get; set; } = 90;
    public int ColumnGap { get; set; } = 13;
    public bool Separator { get; set; } = true;
    public int LabelGap { get; set; } = 1;
    public int Scale { get; set; } = 100;
    /// <summary>Fare üzerine gelince öğenin ayrıntı kartı (hava ipucu dahil).</summary>
    public bool DetailCards { get; set; } = true;

    // Hava durumu
    public string WeatherIcons { get; set; } = StyleOptions.IconMakinThings;
    /// <summary>Hava açıklaması (Güneşli, Sisli…): yer adının yanında / yerine / gösterme.</summary>
    public string WeatherLabel { get; set; } = StyleOptions.WxLabelWithPlace;
    /// <summary>Hava kartındaki tahmin: saatlik (varsayılan, eski davranış), günlük ya da ikisi.</summary>
    public string WeatherForecast { get; set; } = StyleOptions.ForecastHourly;
    public bool TemperatureColors { get; set; } = true;
    public string TemperatureUnit { get; set; } = StyleOptions.Celsius;
    public bool ShowFeelsLike { get; set; } = true;
    public bool WeatherAnimation { get; set; } = true;
}
