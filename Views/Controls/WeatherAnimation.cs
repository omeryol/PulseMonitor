using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Hava kartının içinde küçük animasyon: yağmur damlaları, kar taneleri, sis bantları, gök gürültüsünde şimşek.
/// Hafif olsun diye saniyede 15 kare, birkaç parçacık; yalnızca görünürken, tür "None" değilken ve Windows
/// animasyonlara izin verirken (<see cref="SystemMotion"/>) çalışır.
/// </summary>
public sealed class WeatherAnimation : FrameworkElement
{
    private const int Fps = 15;
    private static readonly Brush RainBrush = Frozen(Color.FromArgb(170, 96, 165, 250));
    private static readonly Brush SnowBrush = Frozen(Color.FromArgb(220, 224, 242, 254));
    private static readonly Brush FogBrush = Frozen(Color.FromArgb(46, 203, 213, 225));
    private static readonly Brush FlashBrush = Frozen(Color.FromArgb(60, 255, 255, 240));
    private static readonly Pen RainPen = FrozenPen(RainBrush, 1.1);

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(WeatherAnimationKind), typeof(WeatherAnimation),
        new PropertyMetadata(WeatherAnimationKind.None, (d, _) => ((WeatherAnimation)d).UpdateTimer()));

    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<Point> _particles = new();
    private int _frame;

    public WeatherAnimation()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / Fps) };
        _timer.Tick += (_, _) => Step();
        IsVisibleChanged += (_, _) => UpdateTimer();
        Unloaded += (_, _) => { _timer.Stop(); SystemMotion.Changed -= OnMotionChanged; };
        Loaded += (_, _) => { SystemMotion.Changed += OnMotionChanged; UpdateTimer(); };
    }

    public WeatherAnimationKind Kind
    {
        get => (WeatherAnimationKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private void OnMotionChanged(object? sender, EventArgs e) => Dispatcher.Invoke(UpdateTimer);

    private void UpdateTimer()
    {
        bool run = Kind != WeatherAnimationKind.None && IsVisible && IsLoaded && SystemMotion.Allowed;
        _particles.Clear();
        if (run && !_timer.IsEnabled) _timer.Start();
        else if (!run && _timer.IsEnabled) _timer.Stop();
        InvalidateVisual();
    }

    private void Step()
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        int count = Kind switch { WeatherAnimationKind.Fog => 3, WeatherAnimationKind.Snow => 9, _ => 12 };
        while (_particles.Count < count) _particles.Add(new Point(_random.NextDouble() * w, _random.NextDouble() * h));

        (double dx, double dy) = Kind switch
        {
            WeatherAnimationKind.Snow => (0.0, 0.7),
            WeatherAnimationKind.Fog => (0.35, 0.0),
            _ => (-0.8, 3.2),
        };
        for (int i = 0; i < _particles.Count; i++)
        {
            Point p = _particles[i];
            double sway = Kind == WeatherAnimationKind.Snow ? Math.Sin((_frame + i * 7) / 6.0) * 0.4 : 0;
            p = new Point(p.X + dx + sway, p.Y + dy);
            if (p.Y > h + 4) p = new Point(_random.NextDouble() * w, -4);
            if (p.X > w + 30) p = new Point(-30, p.Y);
            if (p.X < -4) p = new Point(w + 2, p.Y);
            _particles[i] = p;
        }
        _frame++;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (Kind == WeatherAnimationKind.None || w <= 0 || h <= 0) return;

        foreach (Point p in _particles)
        {
            switch (Kind)
            {
                case WeatherAnimationKind.Snow:
                    dc.DrawEllipse(SnowBrush, null, p, 1.2, 1.2);
                    break;
                case WeatherAnimationKind.Fog:
                    dc.DrawRoundedRectangle(FogBrush, null, new Rect(p.X - 14, p.Y % h, 30, 4), 2, 2);
                    break;
                default:
                    dc.DrawLine(RainPen, p, new Point(p.X - 1, p.Y + 4));
                    break;
            }
        }

        // Şimşek: yaklaşık her 4 saniyede bir, 2 kare süren hafif parlama.
        if (Kind == WeatherAnimationKind.Thunder && _frame % (Fps * 4) is 0 or 1)
            dc.DrawRectangle(FlashBrush, null, new Rect(0, 0, w, h));
    }

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Pen FrozenPen(Brush b, double thickness)
    {
        var p = new Pen(b, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        p.Freeze();
        return p;
    }
}
