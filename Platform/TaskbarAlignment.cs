using Microsoft.Win32;
using PulseMonitor.Core;

namespace PulseMonitor.Platform;

/// <summary>Windows 11'de görev çubuğu simgeleri ortalı mı?</summary>
internal static class TaskbarAlignment
{
    public static bool IsCentered()
    {
        if (Environment.OSVersion.Version.Build < 22000) return false;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            return key?.GetValue("TaskbarAl") is not int value || value == 1;
        }
        catch (Exception ex) { Log.Warn("Görev çubuğu hizası okunamadı.", ex); return false; }
    }
}
