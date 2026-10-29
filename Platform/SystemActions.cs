using System.Diagnostics;
using PulseMonitor.Core;

namespace PulseMonitor.Platform;

internal static class SystemActions
{
    public static void OpenTaskManager()
    {
        try
        {
            Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn("Görev Yöneticisi açılamadı.", ex);
        }
    }

    /// <summary>Bağlantıyı varsayılan tarayıcıda açar.</summary>
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn($"Bağlantı açılamadı: {url}", ex);
        }
    }
}
