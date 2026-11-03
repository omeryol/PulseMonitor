using System.Windows;
using System.Windows.Automation;

namespace PulseMonitor.Taskbar;

/// <summary>
/// Windows 11 görev çubuğundaki Başlat düğmesinin UI Automation ile bildirilen gerçek ekran alanı.
/// Shell'in yerleşimi (özellikle ortalı simgeler taşınca) değiştiğinde bu alan da güncel kalır.
/// </summary>
internal static class TaskbarStartButtonLocator
{
    public static bool TryGetBounds(TaskbarSnapshot taskbar, out Rect bounds)
    {
        bounds = Rect.Empty;
        try
        {
            // Aramayı görev çubuğu HWND'sine bağlamak, çok ekranlı sistemlerde başka çubuğun
            // Başlat düğmesini yanlışlıkla seçmemizi önler.
            AutomationElement bar = AutomationElement.FromHandle(taskbar.Handle);
            AutomationElement? start = bar.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "StartButton"));
            if (start is null || start.Current.IsOffscreen) return false;

            Rect candidate = start.Current.BoundingRectangle;
            if (candidate.IsEmpty || candidate.Width <= 0 || candidate.Height <= 0) return false;

            // UIA konumu fiziksel ekran pikselidir. Düğmenin gerçekten bu görev çubuğunda
            // olduğunu doğrula; kabuk yeniden başlarken eski/boş bir öğe dönebilir.
            if (candidate.Left < taskbar.Bounds.Left || candidate.Right > taskbar.Bounds.Right ||
                candidate.Top < taskbar.Bounds.Top || candidate.Bottom > taskbar.Bounds.Bottom)
                return false;

            bounds = candidate;
            return true;
        }
        catch (Exception)
        {
            // Explorer veya UIA sağlayıcısı geçici olarak hazır olmayabilir. Çağıran taraf
            // bu durumda riskli bir tahmin yapmak yerine pencereyi gizler.
            return false;
        }
    }
}
