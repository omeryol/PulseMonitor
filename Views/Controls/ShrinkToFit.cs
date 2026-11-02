using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Tek satırlık içeriği ayrılan genişliğe sığdırır: sığmazsa küçültür (en çok <see cref="MinScale"/> kadar), o da yetmezse
/// içerik üç nokta ile kesilir; sığan kısa içerik ortalanır. Kendi genişliği istemez (0); genişliği komşu (görünmez yer tutucu) belirler.
/// </summary>
public sealed class ShrinkToFit : Decorator
{
    private const double MinScale = 0.7;
    private readonly ScaleTransform _scale = new(1, 1);

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child is null) return default;
        Child.Measure(new Size(double.PositiveInfinity, constraint.Height));
        return new Size(0, Child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        if (Child is null) return arrangeSize;
        // MeasureOverride'daki doğal genişliği kullan: Arrange sırasında yeniden ölçmek yerleşim döngüsü oluşturur.
        double natural = Child.DesiredSize.Width;
        double scale = natural <= 0 || arrangeSize.Width >= natural ? 1 : Math.Max(MinScale, arrangeSize.Width / natural);
        double width = arrangeSize.Width / scale;
        _scale.ScaleX = _scale.ScaleY = scale;
        Child.RenderTransformOrigin = new Point(0, 0.5);
        Child.RenderTransform = _scale;
        double x = natural < arrangeSize.Width ? (arrangeSize.Width - natural) / 2 : 0; // kısa metin ortalanır
        Child.Arrange(new Rect(x, 0, natural < arrangeSize.Width ? natural : width, Child.DesiredSize.Height));
        return arrangeSize;
    }
}
