using System.IO;

namespace PulseMonitor.Core;

/// <summary>Uygulamanın diskte kullandığı tüm yollar tek yerde.</summary>
internal static class AppPaths
{
    /// <summary>
    /// Veri klasörü. PULSEMONITOR_DATA_DIR ortam değişkeni verilirse o kullanılır (ekran görüntüsü / demo için:
    /// gerçek ayar ve geçmişe dokunulmaz, tools/screenshots.ps1).
    /// </summary>
    public static string DataDir { get; } =
        Environment.GetEnvironmentVariable("PULSEMONITOR_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PulseMonitor");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string TrafficHistoryFile => Path.Combine(DataDir, "traffic-history.json");
    public static string WeatherCacheFile => Path.Combine(DataDir, "weather-cache.json");
    public static string LogDir => Path.Combine(DataDir, "logs");
    public static string LogFile => Path.Combine(LogDir, "pulsemonitor.log");
    public static string TrayIconFile => Path.Combine(AppContext.BaseDirectory, "Assets", "PulseMonitor.ico");
}
