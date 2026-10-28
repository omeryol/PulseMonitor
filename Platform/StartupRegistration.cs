using Microsoft.Win32;
using PulseMonitor.Core;

namespace PulseMonitor.Platform;

/// <summary>Windows açılışında başlatma (HKCU\...\Run). Doğruluğun tek kaynağı kayıt defteridir.</summary>
internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PulseMonitor";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
        return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue(ValueName, Command);
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
            Log.Info($"Windows ile başlatma: {(enabled ? "açık" : "kapalı")}");
        }
        catch (Exception ex)
        {
            Log.Error("Başlangıç kaydı değiştirilemedi.", ex);
        }
    }
}
