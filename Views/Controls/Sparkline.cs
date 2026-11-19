using System.Windows;
using System.Windows.Media;
using PulseMonitor.Settings;

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

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(string), typeof(Sparkline),
        new FrameworkPropertyMetadata(StyleOptions.SparkArea, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    // Dizi türü XAML şablonunda kullanılamadığı için arayüz türü (her güncellemede yeni liste atanır).
    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    /// <summary>Grafik biçimi (StyleOptions.Spark*): alan, çizgi, yumuşak, çubuk, basamak, solan.</summary>
    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
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

        SparklineShapes.Shapes shapes = SparklineShapes.Build(Variant, v, w, h);

        dc.PushOpacity(Opacity_);
        if (shapes.Fill is not null)
        {
            if (shapes.Fade)
            {
                var mask = new LinearGradientBrush(Color.FromArgb(255, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 90);
                mask.Freeze();
                dc.PushOpacityMask(mask);
                dc.DrawGeometry(stroke, null, shapes.Fill);
                dc.Pop();
            }
            else
            {
                dc.PushOpacity(shapes.FillOpacity);
                dc.DrawGeometry(stroke, null, shapes.Fill);
                dc.Pop();
            }
        }
        if (shapes.Line is not null) dc.DrawGeometry(null, new Pen(stroke, Variant == StyleOptions.SparkLine ? 1.5 : 1), shapes.Line);
        dc.Pop();
    }
}
