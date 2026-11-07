using System.Diagnostics;
using PulseMonitor.Core;
using PulseMonitor.Views.Cards;

namespace PulseMonitor.Platform;

/// <summary>Sütuna çift tıklanınca ilgili Windows sayfasını açar (hata günlüğe yazılır, program çökmez).</summary>
internal static class WindowsPages
{
    public static void Open(WindowsPage page)
    {
        if (page == WindowsPage.None) return;
        if (page == WindowsPage.TaskManager) { SystemActions.OpenTaskManager(); return; }
        string? uri = CardTargets.SettingsUri(page);
        if (uri is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warn($"Windows sayfası açılamadı ({uri}).", ex);
        }
    }
}
