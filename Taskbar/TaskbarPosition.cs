namespace PulseMonitor.Taskbar;

/// <summary>Görev çubuğu eksenindeki konumu fiziksel piksel cinsinden sınırlar.</summary>
internal static class TaskbarPosition
{
    public static int Calculate(int barStart, int trayStart, int monitorStart, int monitorEnd, int size, int gap, bool left, double scale)
    {
        // Sol uçta Başlat/Widget alanı için yer bırakılır; diğer sabitlenmiş simgelerle çakışma hâlâ mümkündür.
        int desired = left ? barStart + (int)Math.Round(56 * scale) + gap : trayStart - size - gap;
        return Math.Clamp(desired, monitorStart, Math.Max(monitorStart, monitorEnd - size));
    }
}
