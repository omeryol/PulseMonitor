using System.Globalization;
using System.Text;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>Hava öğesinin üzerine gelince görünen ayrıntı metni.</summary>
internal static class WeatherTooltip
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private const int Hours = 6;

    public static string Build(WeatherLocation loc, WeatherReport r, bool fahrenheit, bool stale)
    {
        string Deg(double c) => ValueFormat.Degrees(c, fahrenheit);
        string unit = fahrenheit ? "F" : "C";

        var sb = new StringBuilder();
        sb.AppendLine($"{loc.ShownName} ({loc.Context})");
        sb.AppendLine($"{WeatherCodes.Describe(r.WeatherCode)}, {Deg(r.Temperature)}{unit} " +
                      $"({string.Format(Tr, Strings.Get("Weather.FeelsLike"), Deg(r.ApparentTemperature))})");
        sb.AppendLine(string.Format(Tr, Strings.Get("Weather.HumidityWind"), r.Humidity, Math.Round(r.WindKmh)));

        DateTime now = DateTime.Now;
        var next = r.Hours.Where(h => h.Time > now).Take(Hours).ToList();
        if (next.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(Strings.Get("Weather.NextHours"));
            foreach (HourForecast h in next)
            {
                string rain = h.PrecipitationProbability is > 0 and var p ? $"  ☂ % {p}" : "";
                sb.AppendLine($"{h.Time:HH:mm}   {WeatherCodes.Glyph(h.WeatherCode, h.IsDay)}  {Deg(h.Temperature)}{rain}");
            }
        }

        sb.AppendLine();
        sb.Append(string.Format(Tr, Strings.Get("Weather.Updated"), r.FetchedAtUtc.ToLocalTime().ToString("HH:mm", Tr)));
        if (stale) sb.Append(' ').Append(Strings.Get("Weather.Stale"));
        return sb.ToString();
    }
}
