using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

internal static class Dwmapi
{
    private const int DWMWA_CLOAKED = 14;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out int value, int size);

    /// <summary>Pencere kabuk tarafından "örtülmüş" (görünür sayılır ama çizilmez) mi?</summary>
    public static bool IsCloaked(IntPtr hWnd) =>
        DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int v, sizeof(int)) == 0 && v != 0;
}
