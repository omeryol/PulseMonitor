using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Film şeridi (yan yana kareler) PNG'sini saniyede 10 kare oynatır (tools/render_icons.ps1 üretir).
/// <see cref="Animate"/> kapalıysa ya da görünmüyorsa sabit resmi çizer ve zamanlayıcı durur.
/// </summary>
public sealed class SpriteIcon : FrameworkElement
{
    private const int Fps = 10;

    public static readonly DependencyProperty StripProperty = Register(nameof(Strip), typeof(BitmapSource));
    public static readonly DependencyProperty StillProperty = Register(nameof(Still), typeof(BitmapSource));
    public static readonly DependencyProperty AnimateProperty = Register(nameof(Animate), typeof(bool));

    private readonly DispatcherTimer _timer;
    private readonly List<ImageSource> _frames = new();
    private int _frame;

    public SpriteIcon()
    {
        IsHitTestVisible = false;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / Fps) };
        _timer.Tick += (_, _) =>
        {
            _frame = (_frame + 1) % Math.Max(1, _frames.Count);
            InvalidateVisual();
        };
        IsVisibleChanged += (_, _) => Restart();
        Unloaded += (_, _) => _timer.Stop();
        Loaded += (_, _) => Restart();
    }

    public BitmapSource? Strip { get => (BitmapSource?)GetValue(StripProperty); set => SetValue(StripProperty, value); }
    public BitmapSource? Still { get => (BitmapSource?)GetValue(StillProperty); set => SetValue(StillProperty, value); }
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }

    private static DependencyProperty Register(string name, Type type) =>
        DependencyProperty.Register(name, type, typeof(SpriteIcon),
            new FrameworkPropertyMetadata(type == typeof(bool) ? false : null, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((SpriteIcon)d).Restart()));

    private void Restart()
    {
        _frames.Clear();
        _frame = 0;
        if (Strip is { PixelHeight: > 0 } strip && Animate)
        {
            int size = strip.PixelHeight, count = strip.PixelWidth / size;
            for (int i = 0; i < count; i++)
            {
                var crop = new CroppedBitmap(strip, new Int32Rect(i * size, 0, size, size));
                crop.Freeze();
                _frames.Add(crop);
            }
        }
        bool run = _frames.Count > 1 && IsVisible && IsLoaded;
        if (run && !_timer.IsEnabled) _timer.Start();
        else if (!run && _timer.IsEnabled) _timer.Stop();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        ImageSource? image = _frames.Count > 0 ? _frames[_frame] : Still;
        if (image is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.DrawImage(image, new Rect(0, 0, ActualWidth, ActualHeight));
    }
}
