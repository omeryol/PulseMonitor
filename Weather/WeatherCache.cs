using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>Son raporu diske yazar; açılışta ve internet yokken gösterilir.</summary>
internal static class WeatherCache
{
    public static WeatherReport? Load(WeatherLocation location)
    {
        try
        {
            if (!File.Exists(AppPaths.WeatherCacheFile)) return null;
            var report = JsonSerializer.Deserialize<WeatherReport>(File.ReadAllText(AppPaths.WeatherCacheFile));
            return report is not null && location.SamePlace(report.Latitude, report.Longitude) ? report : null;
        }
        catch (Exception ex)
        {
            Log.Warn("Hava durumu önbelleği okunamadı; yok sayıldı.", ex);
            return null;
        }
    }

    public static void Save(WeatherReport report)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string temp = AppPaths.WeatherCacheFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(report));
            File.Move(temp, AppPaths.WeatherCacheFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warn("Hava durumu önbelleği yazılamadı.", ex);
        }
    }
}
