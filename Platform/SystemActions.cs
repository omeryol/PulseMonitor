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

    /// <summary>Programın yeni bir kopyasını başlatır (dil değişince yeniden başlatma için; tek kopya kilidi bırakıldıktan sonra).</summary>
    public static void StartSelf(string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!, arguments) { UseShellExecute = false });
        }
        catch (Exception ex)
        {
            Log.Error("Program yeniden başlatılamadı.", ex);
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
