using PulseMonitor.Interop;

namespace PulseMonitor.Taskbar;

/// <summary>Görev çubuğunun bulunduğu ekranda tam ekran bir uygulama çalışıyor mu?</summary>
internal static class FullscreenDetector
{
    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
    };

    public static bool IsActive(RECT monitor, IntPtr ownWindow)
    {
        if (Shell32.SHQueryUserNotificationState(out int state) == 0 &&
            state is Shell32.QUNS_RUNNING_D3D_FULL_SCREEN or Shell32.QUNS_PRESENTATION_MODE)
            return true;

        IntPtr fg = User32.GetForegroundWindow();
        if (fg == IntPtr.Zero || fg == ownWindow) return false;
        if (ShellClasses.Contains(User32.GetClassNameOf(fg))) return false;
        if (!User32.GetWindowRect(fg, out RECT r)) return false;

        return r.Left <= monitor.Left && r.Top <= monitor.Top &&
               r.Right >= monitor.Right && r.Bottom >= monitor.Bottom;
    }
}
