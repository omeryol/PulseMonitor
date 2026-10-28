using System.Windows;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Değerin arkasındaki küçük alan grafiği. <see cref="Values"/> 0–1 arası; her güncellemede yeni dizi atanır.
/// Hafif olsun diye şablon yerine doğrudan çizilir.
/// </summary>
public sealed class Sparkline : FrameworkElement
{
    private const double Opacity_ = 0.35;

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    // Dizi türü XAML şablonunda kullanılamadığı için arayüz türü (her güncellemede yeni liste atanır).
    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Sparkline() => IsHitTestVisible = false;

    protected override void OnRender(DrawingContext dc)
    {
        IReadOnlyList<double>? v = Values;
        Brush? stroke = Stroke;
        double w = ActualWidth, h = ActualHeight;
        if (v is not { Count: > 1 } || stroke is null || w <= 0 || h <= 0) return;

        var area = new StreamGeometry();
        var line = new StreamGeometry();
        using (StreamGeometryContext a = area.Open())
        using (StreamGeometryContext l = line.Open())
        {
            a.BeginFigure(new Point(0, h), isFilled: true, isClosed: true);
            for (int i = 0; i < v.Count; i++)
            {
                var p = new Point(i * w / (v.Count - 1), h - Math.Clamp(v[i], 0, 1) * h);
                a.LineTo(p, isStroked: false, isSmoothJoin: false);
                if (i == 0) l.BeginFigure(p, isFilled: false, isClosed: false);
                else l.LineTo(p, isStroked: true, isSmoothJoin: true);
            }
            a.LineTo(new Point(w, h), isStroked: false, isSmoothJoin: false);
        }
        area.Freeze();
        line.Freeze();

        dc.PushOpacity(Opacity_);
        dc.PushOpacity(0.5);
        dc.DrawGeometry(stroke, null, area);
        dc.Pop();
        dc.DrawGeometry(null, new Pen(stroke, 1), line);
        dc.Pop();
    }
}
