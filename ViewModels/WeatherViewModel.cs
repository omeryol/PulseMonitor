using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>Görev çubuğundaki hava durumu öğesi: simge, sıcaklık (+ hissedilen), yer adı, animasyon, ipucu.</summary>
public sealed class WeatherViewModel : ObservableObject, ITallItem
{
    private static CultureInfo Tr => UiLanguage.Culture;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(1);
    private static readonly TimeSpan DiscardAfter = TimeSpan.FromHours(12);

    private bool _isVisible;
    private ImageSource? _icon;
    private Geometry? _iconShape;
    private string _glyph = "";
    private BitmapSource? _strip, _still;
    private bool _animateIcon;
    private Brush _tempBrush = ThemeBrushes.Get("Brush.Text");
    private string _tempBrushKey = "Brush.Text";
    private string _temperature = "--°";
    private string _feelsLike = "";
    private string _placeholder = "-88°";
    private string _feelsPlaceholder = "";
    private string _place = "";
    private string _placePlaceholder = "";
    private WeatherTooltipModel? _tooltip;
    private Func<WeatherTooltipModel>? _buildTooltip; // tema değişince şekil simgeleri ve renkler yeniden alınır
    private double _opacity = 1;
    private WeatherAnimationKind _animation;

    public bool IsVisible { get => _isVisible; private set => Set(ref _isVisible, value); }

    /// <summary>Renkli / Gerçekçi setlerde dolu; Çizgi / Dolu setlerde <see cref="IconShape"/> dolu.</summary>
    public ImageSource? Icon { get => _icon; private set => Set(ref _icon, value); }
    public Geometry? IconShape { get => _iconShape; private set => Set(ref _iconShape, value); }

    /// <summary>Animasyonlu setlerde film şeridi ve sabit hâli; animasyon ayarı kapalıysa sabit gösterilir.</summary>
    public BitmapSource? IconStrip { get => _strip; private set => Set(ref _strip, value); }
    public BitmapSource? IconStill { get => _still; private set => Set(ref _still, value); }
    public bool AnimateIcon { get => _animateIcon; private set => Set(ref _animateIcon, value); }

    /// <summary>"Karakter" setinde simge yerine yazı karakteri (☀ ☁ ☂ …).</summary>
    public string IconGlyph { get => _glyph; private set => Set(ref _glyph, value); }

    public Brush TemperatureBrush { get => _tempBrush; private set => Set(ref _tempBrush, value); }
    public string TemperatureText { get => _temperature; private set => Set(ref _temperature, value); }

    /// <summary>"hissedilen 14°" ya da boş (ayar kapalıysa).</summary>
    public string FeelsLikeText { get => _feelsLike; private set => Set(ref _feelsLike, value); }

    /// <summary>Sıcaklık satırının en geniş hâli (iki parça); satır genişliğini sabitler (hava değişince kayma olmaz).</summary>
    public string TemperaturePlaceholder { get => _placeholder; private set => Set(ref _placeholder, value); }
    public string FeelsLikePlaceholder { get => _feelsPlaceholder; private set => Set(ref _feelsPlaceholder, value); }

    public string PlaceName { get => _place; private set => Set(ref _place, value); }

    /// <summary>
    /// Yer adı satırının görünmez en geniş hâli (tüm konumların adı + en uzun açıklama). Konum ya da hava değişince sütun genişliği
    /// oynamasın; çok uzunsa görünümde 170 px'te kesilir (WeatherItem.xaml).
    /// </summary>
    public string PlacePlaceholder { get => _placePlaceholder; private set => Set(ref _placePlaceholder, value); }
    /// <summary>Renkli ipucu kartının içeriği (ViewModels/WeatherTooltip.cs).</summary>
    public WeatherTooltipModel? Tooltip { get => _tooltip; private set => Set(ref _tooltip, value); }
    public WeatherAnimationKind Animation { get => _animation; private set => Set(ref _animation, value); }

    /// <summary>Veri eskiyse (internet yok) soluk gösterilir.</summary>
    public double Opacity { get => _opacity; private set => Set(ref _opacity, value); }

    /// <summary>Öğeye tıklandı ve birden fazla konum var: App sıradaki konuma geçer.</summary>
    public event Action? SwitchRequested;
    public void RequestSwitch() => SwitchRequested?.Invoke();
    public bool CanSwitch { get; private set; }

    internal void Update(AppSettings s, Func<WeatherLocation, WeatherReport?> reportFor)
    {
        WeatherLocation? loc = s.Weather.Active;
        IsVisible = s.IsShown(TaskbarItemId.Weather) && loc is not null;
        CanSwitch = s.Weather.Locations.Count > 1;
        if (loc is null) return;
        WeatherReport? report = reportFor(loc);
        var others = s.Weather.Locations.Where(l => !ReferenceEquals(l, loc)).Select(l => (l, reportFor(l))).ToList();

        StyleSettings st = s.Style;
        bool f = st.TemperatureUnit == StyleOptions.Fahrenheit;
        string widest = f ? "-188°" : "-88°";
        TemperaturePlaceholder = widest;
        FeelsLikePlaceholder = st.ShowFeelsLike ? FeelsLike(widest) : "";
        PlaceName = loc.ShownName;
        PlacePlaceholder = WidestPlace(s, st.WeatherLabel);

        if (report is null || report.Age > DiscardAfter)
        {
            (Icon, IconShape, IconStrip, IconStill) = (null, null, null, null);
            IconGlyph = "";
            TemperatureText = "--°";
            FeelsLikeText = "";
            Animation = WeatherAnimationKind.None;
            SetTemperatureBrush("Brush.Text");
            Opacity = 1;
            _buildTooltip = () => WeatherTooltip.Loading(loc, others, st);
            Tooltip = _buildTooltip();
            return;
        }

        WeatherIcon icon = WeatherIcons.Resolve(st.WeatherIcons, WeatherCodes.IconKey(report.WeatherCode, report.IsDay));
        (Icon, IconShape, IconStrip, IconStill) = (icon.Image, icon.Shape, icon.Strip, icon.Still);
        IconGlyph = st.WeatherIcons == StyleOptions.IconGlyph ? WeatherCodes.Glyph(report.WeatherCode, report.IsDay) : "";
        string description = WeatherCodes.Describe(report.WeatherCode);
        PlaceName = st.WeatherLabel switch
        {
            StyleOptions.WxLabelWithPlace => $"{loc.ShownName} · {description}",
            StyleOptions.WxLabelInstead => description,
            _ => loc.ShownName,
        };
        TemperatureText = ValueFormat.Degrees(report.Temperature, f);
        FeelsLikeText = st.ShowFeelsLike ? FeelsLike(ValueFormat.Degrees(report.ApparentTemperature, f)) : "";
        // Animasyonlu simge kendisi hareket ettiği için kart animasyonu o setlerde kapalı (üst üste binmesin).
        bool animatedSet = icon.Strip is not null;
        AnimateIcon = st.WeatherAnimation && animatedSet;
        Animation = st.WeatherAnimation && !animatedSet ? WeatherCodes.Animation(report.WeatherCode) : WeatherAnimationKind.None;
        SetTemperatureBrush(st.TemperatureColors ? TemperatureKey(report.Temperature) : "Brush.Text");
        bool stale = report.Age > StaleAfter;
        Opacity = stale ? 0.55 : 1;
        _buildTooltip = () => WeatherTooltip.Build(loc, report, others, st, stale);
        Tooltip = _buildTooltip();
    }

    private static string WidestPlace(AppSettings s, string labelMode)
    {
        string description = WeatherCodes.LongestDescription();
        return s.Weather.Locations
            .Select(l => labelMode switch
            {
                StyleOptions.WxLabelWithPlace => $"{l.ShownName} · {description}",
                StyleOptions.WxLabelInstead => description,
                _ => l.ShownName,
            })
            .MaxBy(t => t.Length) ?? "";
    }

    private static string FeelsLike(string degrees) => string.Format(Tr, Strings.Get("Weather.FeelsLikeShort"), degrees);

    /// <summary>Tema değişince sıcaklık rengi yeniden okunur.</summary>
    public void RefreshBrush()
    {
        TemperatureBrush = ThemeBrushes.Get(_tempBrushKey);
        if (_buildTooltip is not null) Tooltip = _buildTooltip();
    }

    private void SetTemperatureBrush(string key)
    {
        _tempBrushKey = key;
        TemperatureBrush = ThemeBrushes.Get(key);
    }

    /// <summary>Renk her zaman Celsius'a göre seçilir (≤0 · &lt;10 · &lt;20 · &lt;28 · ≥28).</summary>
    private static string TemperatureKey(double celsius) => Math.Round(celsius) switch
    {
        <= 0 => "Brush.Temp.Freezing",
        < 10 => "Brush.Temp.Cold",
        < 20 => "Brush.Temp.Mild",
        < 28 => "Brush.Temp.Warm",
        _ => "Brush.Temp.Hot",
    };
}
