using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>Hava öğesinin renkli ipucu kartı (Views/Controls/WeatherItem.xaml şablonu gösterir).</summary>
public sealed record WeatherTooltipModel(
    string Place, string Context, ImageSource? Icon, string Description, string Temperature, Brush TemperatureBrush,
    string Details, IReadOnlyList<HourRow> Hours, string Updated, bool IsStale, bool IsLoading);

/// <summary>Saatlik satır: saat, renkli simge, sıcaklığa göre renkli derece, yağış olasılığı.</summary>
public sealed record HourRow(string Time, ImageSource? Icon, string Temperature, Brush TemperatureBrush, string Rain, bool HasRain);

internal static class WeatherTooltip
{
    private static CultureInfo Tr => UiLanguage.Culture;
    private const int Hours = 6;

    public static WeatherTooltipModel Loading(WeatherLocation loc) =>
        new(loc.ShownName, loc.Context, null, Strings.Get("Weather.Loading"), "", ThemeBrushes.Get("Brush.Text"),
            "", Array.Empty<HourRow>(), "", false, true);

    public static WeatherTooltipModel Build(WeatherLocation loc, WeatherReport r, bool fahrenheit, bool stale)
    {
        string Deg(double c) => ValueFormat.Degrees(c, fahrenheit);

        DateTime now = DateTime.Now;
        var hours = r.Hours.Where(h => h.Time > now).Take(Hours)
            .Select(h => new HourRow(
                h.Time.ToString("HH:mm", Tr),
                Icon(h.WeatherCode, h.IsDay),
                Deg(h.Temperature),
                TemperatureBrush(h.Temperature),
                h.PrecipitationProbability is > 0 and var p ? ValueFormat.Percent(p) : "",
                h.PrecipitationProbability is > 0))
            .ToList();

        string details = string.Format(Tr, Strings.Get("Weather.FeelsLike"), Deg(r.ApparentTemperature)) + "   ·   " +
                         string.Format(Tr, Strings.Get("Weather.HumidityWind"), r.Humidity, Math.Round(r.WindKmh));
        string updated = string.Format(Tr, Strings.Get("Weather.Updated"), r.FetchedAtUtc.ToLocalTime().ToString("HH:mm", Tr));

        return new WeatherTooltipModel(loc.ShownName, loc.Context, Icon(r.WeatherCode, r.IsDay), WeatherCodes.Describe(r.WeatherCode),
            Deg(r.Temperature), TemperatureBrush(r.Temperature), details, hours, updated, stale, false);
    }

    /// <summary>İpucunda her zaman renkli simgeler kullanılır (Resources/WeatherIcons.xaml).</summary>
    private static ImageSource? Icon(int code, bool isDay) =>
        Application.Current?.TryFindResource($"Wx.{WeatherCodes.IconKey(code, isDay)}") as ImageSource;

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
