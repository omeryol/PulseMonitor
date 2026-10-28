using System.Globalization;
using System.IO;
using System.Text;

namespace PulseMonitor.Core;

/// <summary>Basit, iş parçacığı güvenli dosya günlüğü. 1 MB'ı geçince bir önceki dosyaya devreder.</summary>
internal static class Log
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object Sync = new();

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string line = $"{time} [{level}] {message}{(ex is null ? "" : Environment.NewLine + ex)}{Environment.NewLine}";
            lock (Sync)
            {
                Directory.CreateDirectory(AppPaths.LogDir);
                var file = new FileInfo(AppPaths.LogFile);
                if (file.Exists && file.Length > MaxBytes)
                    File.Move(file.FullName, file.FullName + ".old", overwrite: true);
                File.AppendAllText(AppPaths.LogFile, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Günlük yazılamıyorsa uygulama yine de çalışmaya devam etmeli.
        }
    }
}
