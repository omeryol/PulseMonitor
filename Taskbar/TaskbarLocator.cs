using System.Runtime.InteropServices;
using PulseMonitor.Interop;

namespace PulseMonitor.Taskbar;

/// <summary>Görev çubuğunun, sistem tepsisinin ve bulunduğu ekranın o anki konumu (fiziksel piksel).</summary>
internal readonly record struct TaskbarSnapshot(IntPtr Handle, RECT Bounds, RECT Tray, RECT Monitor)
{
    public bool IsHorizontal => Bounds.Width >= Bounds.Height;

    /// <summary>Otomatik gizlenen görev çubuğu ekran dışına kaydığında false döner.</summary>
    public bool IsOnScreen
    {
        get
        {
            int visible = IsHorizontal
                ? Math.Min(Bounds.Bottom, Monitor.Bottom) - Math.Max(Bounds.Top, Monitor.Top)
                : Math.Min(Bounds.Right, Monitor.Right) - Math.Max(Bounds.Left, Monitor.Left);
            int full = IsHorizontal ? Bounds.Height : Bounds.Width;
            return full > 0 && visible * 2 >= full;
        }
    }
}

internal static class TaskbarLocator
{
    public static TaskbarSnapshot? Find()
    {
        IntPtr bar = User32.FindWindow("Shell_TrayWnd", null);
        if (bar == IntPtr.Zero || !User32.GetWindowRect(bar, out RECT bounds)) return null;

        // Tepsi bulunamazsa görev çubuğunun bitiş kenarı kullanılır.
        RECT tray = bounds.Width >= bounds.Height
            ? new RECT(bounds.Right, bounds.Top, bounds.Right, bounds.Bottom)
            : new RECT(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);

        IntPtr notify = User32.FindWindowEx(bar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (notify != IntPtr.Zero && User32.GetWindowRect(notify, out RECT r) && r.Width > 0 && r.Height > 0)
            tray = r;

        IntPtr monitor = User32.MonitorFromWindow(bar, User32.MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!User32.GetMonitorInfo(monitor, ref info)) return null;

        return new TaskbarSnapshot(bar, bounds, tray, info.rcMonitor);
    }
}
