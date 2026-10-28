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
}
