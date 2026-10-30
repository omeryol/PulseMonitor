using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>Her konumun son raporunu diske yazar; açılışta ve internet yokken gösterilir.</summary>
internal static class WeatherCache
{
    /// <summary>0.12 ve öncesi tek rapor (nesne) yazıyordu; o biçim de okunur.</summary>
    public static List<WeatherReport> Load()
    {
        try
        {
            if (!File.Exists(AppPaths.WeatherCacheFile)) return new();
            string json = File.ReadAllText(AppPaths.WeatherCacheFile);
            if (json.TrimStart().StartsWith('{'))
                return JsonSerializer.Deserialize<WeatherReport>(json) is { } single ? [single] : new();
            return JsonSerializer.Deserialize<List<WeatherReport>>(json)?.Where(r => r is not null).ToList() ?? new();
        }
        catch (Exception ex)
        {
            Log.Warn("Hava durumu önbelleği okunamadı; yok sayıldı.", ex);
            return new();
        }
    }

    public static void Save(IEnumerable<WeatherReport> reports)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string temp = AppPaths.WeatherCacheFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(reports.ToList()));
            File.Move(temp, AppPaths.WeatherCacheFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warn("Hava durumu önbelleği yazılamadı.", ex);
        }
    }
}
