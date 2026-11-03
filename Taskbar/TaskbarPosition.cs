namespace PulseMonitor.Taskbar;

/// <summary>Görev çubuğu eksenindeki konumu fiziksel piksel cinsinden sınırlar.</summary>
internal static class TaskbarPosition
{
    public static int Calculate(int barStart, int trayStart, int monitorStart, int monitorEnd, int size, int gap, bool left)
    {
        int desired = left ? barStart : trayStart - size - gap;
        return Math.Clamp(desired, monitorStart, Math.Max(monitorStart, monitorEnd - size));
    }
}
