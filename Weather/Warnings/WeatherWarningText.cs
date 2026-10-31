using System.Globalization;
using PulseMonitor.Core;
using PulseMonitor.Formatting;

namespace PulseMonitor.Weather.Warnings;

/// <summary>Uyarının ekrandaki metni (lang/*.json → "Weather.Warn.&lt;tür&gt;"). Saat ve gün konumun yerel zamanına göredir.</summary>
internal static class WeatherWarningText
{
    private static CultureInfo Tr => UiLanguage.Culture;

    public static string Format(WeatherWarning w, DateTime localNow, bool fahrenheit)
    {
        string time = w.At.ToString("HH:mm", Tr);
        string day = DayLabel(w.At, localNow);
        string delta = ValueFormat.DegreeDelta(w.Value, fahrenheit);
        string template = Strings.Get($"Weather.Warn.{w.Kind}");
        return w.Kind switch
        {
            WarningKind.Gust => string.Format(Tr, template, time, Math.Round(w.Value)),
            WarningKind.Heat or WarningKind.Cold => string.Format(Tr, template, day, ValueFormat.Degrees(w.Value, fahrenheit)),
            WarningKind.Drop or WarningKind.Rise => string.Format(Tr, template, time, delta),
            WarningKind.DayDrop or WarningKind.DayRise => string.Format(Tr, template, delta),
            WarningKind.AnomalyWarm or WarningKind.AnomalyCool => string.Format(Tr, template, day, delta),
            _ => string.Format(Tr, template, time),
        };
    }

    /// <summary>Bugün · Yarın · gün adı.</summary>
    private static string DayLabel(DateTime date, DateTime localNow) =>
        (date.Date - localNow.Date).Days switch
        {
            0 => Strings.Get("Weather.Today"),
            1 => Strings.Get("Weather.Tomorrow"),
            _ => date.ToString("dddd", Tr),
        };
}
