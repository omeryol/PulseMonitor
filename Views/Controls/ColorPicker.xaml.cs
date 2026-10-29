using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PulseMonitor.Settings;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Tıklanarak renk seçimi: hazır renkler ya da ton / doygunluk / parlaklık. Renk kodu yalnızca ileri düzey kutuda.
/// </summary>
public partial class ColorPicker : UserControl
{
    /// <summary>Hazır renkler: 10 ton × 4 açıklık + gri tonlar (satır satır, soldan sağa).</summary>
    private static readonly string[] Presets =
    [
        "#FCA5A5", "#FDBA74", "#FCD34D", "#BEF264", "#6EE7B7", "#5EEAD4", "#7DD3FC", "#93C5FD", "#C4B5FD", "#F9A8D4",
        "#F87171", "#FB923C", "#FBBF24", "#A3E635", "#34D399", "#2DD4BF", "#38BDF8", "#60A5FA", "#A78BFA", "#F472B6",
        "#EF4444", "#F97316", "#F59E0B", "#84CC16", "#10B981", "#14B8A6", "#0EA5E9", "#3B82F6", "#8B5CF6", "#EC4899",
        "#B91C1C", "#C2410C", "#B45309", "#4D7C0F", "#047857", "#0F766E", "#0369A1", "#1D4ED8", "#6D28D9", "#BE185D",
        "#FFFFFF", "#F3F4F6", "#E5E7EB", "#D1D5DB", "#9CA3AF", "#6B7280", "#4B5563", "#374151", "#1F2937", "#111111",
    ];

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(string), typeof(ColorPicker),
        new FrameworkPropertyMetadata("#FFFFFF", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((ColorPicker)d).OnSelectedChanged()));

    private static readonly DependencyPropertyKey PreviewBrushKey = DependencyProperty.RegisterReadOnly(
        nameof(PreviewBrush), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.White));
    public static readonly DependencyProperty PreviewBrushProperty = PreviewBrushKey.DependencyProperty;

    private bool _syncing;

    public ColorPicker()
    {
        InitializeComponent();
        Swatches.ItemsSource = Presets;
        OnSelectedChanged();
    }

    public string SelectedColor { get => (string)GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }
    public Brush PreviewBrush => (Brush)GetValue(PreviewBrushProperty);

    private void OnSelectedChanged()
    {
        if (!StyleOptions.TryParseColor(SelectedColor, out Color c)) return;
        SetValue(PreviewBrushKey, new SolidColorBrush(c));
        if (_syncing) return;
        _syncing = true;
        var (h, s, v) = ColorMath.ToHsv(c);
        Hue.Value = h;
        Saturation.Value = s;
        Brightness.Value = v;
        HexBox.Text = SelectedColor;
        _syncing = false;
    }

    private void Apply(Color c)
    {
        _syncing = true;
        SelectedColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        HexBox.Text = SelectedColor;
        _syncing = false;
    }

    private void OnSwatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex } && StyleOptions.TryParseColor(hex, out Color c))
        {
            _syncing = false;
            SelectedColor = hex;   // kaydırıcılar da yeni renge geçer
            Toggle.IsChecked = false;
        }
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !IsLoaded) return;
        Apply(ColorMath.FromHsv(Hue.Value, Saturation.Value, Brightness.Value));
    }

    private void OnHexCommitted(object sender, RoutedEventArgs e)
    {
        string text = HexBox.Text.Trim().ToUpperInvariant();
        if (!text.StartsWith('#')) text = "#" + text;
        if (StyleOptions.TryParseColor(text, out _)) SelectedColor = text;
        else HexBox.Text = SelectedColor;
    }

    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnHexCommitted(sender, e);
    }

    private void OnPopupClosed(object? sender, EventArgs e) => Toggle.IsChecked = false;
}
