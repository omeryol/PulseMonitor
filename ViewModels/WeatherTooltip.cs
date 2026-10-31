using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;
using PulseMonitor.Weather;
using PulseMonitor.Weather.Warnings;

namespace PulseMonitor.ViewModels;

/// <summary>Hava öğesinin renkli ipucu kartı (Views/Controls/WeatherItem.xaml şablonu gösterir).</summary>
public sealed record WeatherTooltipModel(
    string Place, string Context, ImageSource? Icon, string Description, string Temperature, Brush TemperatureBrush,
    string Details, IReadOnlyList<HourRow> Hours, IReadOnlyList<DayRow> Days, string Updated, bool IsStale, bool IsLoading,
    IReadOnlyList<PlaceRow> Others, string Warning = "", Brush? WarningBrush = null, ImageSource? WarningIcon = null)
{
    public bool HasOthers => Others.Count > 0;
    public bool HasWarning => Warning.Length > 0;
    public bool HasHours => Hours.Count > 0;
    public bool HasDays => Days.Count > 0;
    /// <summary>Saatlik ve günlük birlikte gösterilirken aralarına ince çizgi konur.</summary>
    public bool HasBoth => HasHours && HasDays;
}

/// <summary>Saatlik satır: saat, renkli simge, sıcaklığa göre renkli derece, yağış olasılığı.</summary>
public sealed record HourRow(string Time, ImageSource? Icon, string Temperature, Brush TemperatureBrush, string Rain, bool HasRain);

/// <summary>Günlük satır: gün adı, simge, en yüksek (sıcaklığa göre renkli) / en düşük, yağış olasılığı.</summary>
public sealed record DayRow(string Day, ImageSource? Icon, string High, Brush HighBrush, string Low, string Rain, bool HasRain);

/// <summary>Diğer takip edilen konumlar: ad, simge, sıcaklık (veri yoksa "--°").</summary>
public sealed record PlaceRow(string Name, ImageSource? Icon, string Temperature, Brush TemperatureBrush);

internal static class WeatherTooltip
{
    private static CultureInfo Tr => UiLanguage.Culture;
    private const int Hours = 6;

    public static WeatherTooltipModel Loading(WeatherLocation loc, IReadOnlyList<(WeatherLocation, WeatherReport?)> others, StyleSettings st) =>
        new(loc.ShownName, loc.Context, null, Strings.Get("Weather.Loading"), "", ThemeBrushes.Get("Brush.Text"),
            "", Array.Empty<HourRow>(), Array.Empty<DayRow>(), "", false, true, Places(others, st));

    public static WeatherTooltipModel Build(WeatherLocation loc, WeatherReport r, IReadOnlyList<(WeatherLocation, WeatherReport?)> others,
        StyleSettings st, int warningLevel, bool stale)
    {
        bool fahrenheit = st.TemperatureUnit == StyleOptions.Fahrenheit;
        string Deg(double c) => ValueFormat.Degrees(c, fahrenheit);
        string set = st.WeatherIcons;

        DateTime now = r.LocalNow;
        var hours = st.WeatherForecast == StyleOptions.ForecastDaily
            ? new List<HourRow>()
            : r.Hours.Where(h => h.Time > now).Take(Hours)
                .Select(h => new HourRow(
                    h.Time.ToString("HH:mm", Tr),
                    Icon(set, h.WeatherCode, h.IsDay),
                    Deg(h.Temperature),
                    TemperatureBrush(h.Temperature),
                    h.PrecipitationProbability is > 0 and var p ? ValueFormat.Percent(p) : "",
                    h.PrecipitationProbability is > 0))
                .ToList();

        // Eski önbellekte günlük veri yok (Days boş): bölüm gizlenir, sonraki yenilemede dolar.
        var days = st.WeatherForecast == StyleOptions.ForecastHourly
            ? new List<DayRow>()
            : r.Days.Where(d => d.Date.Date >= now.Date).Take(OpenMeteoClient.DaysAhead)
                .Select(d => new DayRow(
                    d.Date.Date == now.Date ? Strings.Get("Weather.Today") : d.Date.ToString("ddd", Tr),
                    Icon(set, d.WeatherCode, true),
                    Deg(d.High),
                    TemperatureBrush(d.High),
                    Deg(d.Low),
                    d.PrecipitationProbability is > 0 and var p ? ValueFormat.Percent(p) : "",
                    d.PrecipitationProbability is > 0))
                .ToList();

        string details = string.Format(Tr, Strings.Get("Weather.FeelsLike"), Deg(r.ApparentTemperature)) + "   ·   " +
                         string.Format(Tr, Strings.Get("Weather.HumidityWind"), r.Humidity, Math.Round(r.WindKmh));
        string updated = string.Format(Tr, Strings.Get("Weather.Updated"), r.FetchedAtUtc.ToLocalTime().ToString("HH:mm", Tr)) +
                         "   ·   " + OpenMeteoModels.SourceLabel(r.Model);

        (string warning, Brush warningBrush, ImageSource? warningIcon) = Warning(r, warningLevel, fahrenheit, set);
        return new WeatherTooltipModel(loc.ShownName, loc.Context, Icon(set, r.WeatherCode, r.IsDay), WeatherCodes.Describe(r.WeatherCode),
            Deg(r.Temperature), TemperatureBrush(r.Temperature), details, hours, days, updated, stale, false, Places(others, st),
            warning, warningBrush, warningIcon);
    }

    /// <summary>
    /// Kartın altındaki tek satır: en önemli uyarı. Bildirimlik olanlar ⚠ ile ve uyarı renginde, "son 30 güne göre" sıra dışı
    /// günler yalnızca soluk bir not olarak. Bildirimler sessiz saatte bastırılsa bile kartta görünür.
    /// </summary>
    private static (string, Brush, ImageSource?) Warning(WeatherReport r, int level, bool fahrenheit, string set)
    {
        WeatherWarning? top = WeatherWarningDetector.Detect(r, level).FirstOrDefault();
        if (top is null) return ("", ThemeBrushes.Get("Brush.Muted"), null);
        string text = WeatherWarningText.Format(top, r.LocalNow, fahrenheit);
        ImageSource? icon = IconByKey(set, top.IconKey);
        return top.Notify ? ("⚠ " + text, Faded(ThemeBrushes.Get("Brush.Load.Hot"), 0.8), icon) : (text, ThemeBrushes.Get("Brush.Muted"), icon);
    }

    private static List<PlaceRow> Places(IReadOnlyList<(WeatherLocation Loc, WeatherReport? Report)> others, StyleSettings st) =>
        others.Select(o => o.Report is { } r && r.Age < TimeSpan.FromHours(12)
                ? new PlaceRow(o.Loc.ShownName, Icon(st.WeatherIcons, r.WeatherCode, r.IsDay),
                    ValueFormat.Degrees(r.Temperature, st.TemperatureUnit == StyleOptions.Fahrenheit), TemperatureBrush(r.Temperature))
                : new PlaceRow(o.Loc.ShownName, null, "--°", ThemeBrushes.Get("Brush.Muted")))
            .ToList();

    /// <summary>
    /// Kart simgesi seçili simge setinden gelir: resim olduğu gibi, animasyonlu sette sabit kare, Çizgi/Dolu sette şekil kart
    /// yazı rengiyle. "Karakter" setinde (ya da simge yüklenemezse) renkli simge kullanılır.
    /// </summary>
    private static ImageSource? Icon(string set, int code, bool isDay) => IconByKey(set, WeatherCodes.IconKey(code, isDay));

    private static ImageSource? IconByKey(string set, string key)
    {
        WeatherIcon icon = WeatherIcons.Resolve(set, key);
        if (icon.Image is { } image) return image;
        if (icon.Still is { } still) return still;
        if (icon.Shape is { } shape) return new DrawingImage(new GeometryDrawing(ThemeBrushes.Get("Brush.Text"), null, shape));
        return Application.Current?.TryFindResource($"Wx.{key}") as ImageSource;
    }

    /// <summary>Uyarı satırı için silik kırmızı: tema kırmızısı, biraz saydam (gözü yormasın, yine de uyarı okunsun).</summary>
    private static Brush Faded(Brush brush, double opacity)
    {
        if (brush is not SolidColorBrush solid) return brush;
        var faded = new SolidColorBrush(solid.Color) { Opacity = opacity };
        faded.Freeze();
        return faded;
    }

    /// <summary>Sıcaklığa göre renk (≤0 · &lt;10 · &lt;20 · &lt;28 · ≥28 °C), görev çubuğundakiyle aynı.</summary>
    public static Brush TemperatureBrush(double celsius) => ThemeBrushes.Get(Math.Round(celsius) switch
    {
        <= 0 => "Brush.Temp.Freezing",
        < 10 => "Brush.Temp.Cold",
        < 20 => "Brush.Temp.Mild",
        < 28 => "Brush.Temp.Warm",
        _ => "Brush.Temp.Hot",
    });
}
