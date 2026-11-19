using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Hava sütunundaki olay katmanı. Simgeler sabit tutulur; yalnızca fiziksel olarak hareket eden
/// hava olayı (yağış, sis, yıldırım) çizilir. Böylece salınma ve ritmik parlama oluşmaz.
/// </summary>
public sealed class WeatherAnimation : FrameworkElement
{
    private const int Fps = 15;
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(WeatherAnimationKind), typeof(WeatherAnimation),
        new PropertyMetadata(WeatherAnimationKind.None, (d, _) => ((WeatherAnimation)d).UpdateTimer()));

    public static readonly DependencyProperty WindKmhProperty = DependencyProperty.Register(
        nameof(WindKmh), typeof(double), typeof(WeatherAnimation), new PropertyMetadata(0.0));
    public double WindKmh { get => (double)GetValue(WindKmhProperty); set => SetValue(WindKmhProperty, value); }
    private readonly WindLeaves _leaves = new();
    private readonly FrostLayer _frost = new();
    private readonly GlassRainLayer _rain = new();
    private readonly SkyLayer _sky = new();
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly WinterPrecipitation _winter = new();
    private readonly MistLayer _mist = new();
    private bool IsStorm => Kind is WeatherAnimationKind.Thunder or WeatherAnimationKind.HeavyThunder or WeatherAnimationKind.Hail or WeatherAnimationKind.HeavyHail;
    private int _frame, _nextLightningFrame, _lightningFrames, _lightningSeed;

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

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(double), typeof(WeatherAnimation), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public WeatherAnimationKind Kind { get => (WeatherAnimationKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

    private void OnMotionChanged(object? sender, EventArgs e) => Dispatcher.Invoke(UpdateTimer);

    private void UpdateTimer()
    {
        bool run = Kind != WeatherAnimationKind.None && IsVisible && IsLoaded && SystemMotion.Allowed;
        _leaves.Clear();
        _frost.Clear();
        _winter.Clear();
        _mist.Clear();
        _sky.Clear();
        _rain.Clear();
        _frame = 0;
        _lightningFrames = 0;
        _nextLightningFrame = Fps;
        if (run && !_timer.IsEnabled) _timer.Start();
        else if (!run && _timer.IsEnabled) _timer.Stop();
        InvalidateVisual();
    }

    private int NextLightningFrame() => _frame + (Kind == WeatherAnimationKind.HeavyThunder ? _random.Next(Fps * 3, Fps * 6) : _random.Next(Fps * 5, Fps * 10));

    private void Step()
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        if (SkyLayer.IsSky(Kind))
        {
            _sky.Step(Kind, w, h);
        }
        else if (GlassRainLayer.IsRain(Kind))
        {
            _rain.Step(Kind, w, h);
        }
        else if (WinterPrecipitation.Supports(Kind)) _winter.Step(Kind, w, h);

        _leaves.Step(WindKmh, w, h);
        if (FrostLayer.Supports(Kind)) _frost.Step(Kind);
        if (Kind is WeatherAnimationKind.Fog or WeatherAnimationKind.RimeFog || IsStorm) _mist.Step(w, h, IsStorm);
        if (IsStorm)
        {
            if (_lightningFrames > 0) _lightningFrames--;
            else if (_frame >= _nextLightningFrame)
            {
                _lightningFrames = _random.Next(2, 4);
                _lightningSeed = _random.Next();
                _nextLightningFrame = NextLightningFrame();
            }
        }
        _frame++;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (Kind == WeatherAnimationKind.None || w <= 0 || h <= 0) return;

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h), CornerRadius, CornerRadius));
        if (SkyLayer.IsSky(Kind)) _sky.Render(dc, Kind, w);
        if (GlassRainLayer.IsRain(Kind)) _rain.Render(dc);
        if (Kind is WeatherAnimationKind.Fog or WeatherAnimationKind.RimeFog || IsStorm) _mist.Render(dc, IsStorm);
        if (WinterPrecipitation.Supports(Kind)) _winter.Render(dc, Kind);
        _frost.Render(dc, w, h);
        _leaves.Render(dc);
        if (IsStorm && _lightningFrames > 0) DrawLightning(dc, w, h);
        dc.Pop();
    }

    private void DrawLightning(DrawingContext dc, double w, double h)
    {
        var random = new Random(_lightningSeed);
        double x = w * (0.28 + random.NextDouble() * 0.44);
        var geometry = new StreamGeometry();
        using (StreamGeometryContext c = geometry.Open())
        {
            c.BeginFigure(new Point(x, -2), false, false);
            for (double y = 0; y < h * 0.82; y += 6)
            {
                if (y > 0 && (y == 6 || random.NextDouble() < 0.25))
                {
                    var fork = new Point(x + (random.Next(2) == 0 ? -1 : 1) * (4 + random.NextDouble() * 7), y + 4);
                    c.LineTo(fork, true, false);
                    c.LineTo(new Point(fork.X + random.Next(-4, 5), fork.Y + 3), true, false);
                    c.BeginFigure(new Point(x, y), false, false);
                }
                x += -5 + random.NextDouble() * 10;
                c.LineTo(new Point(x, y + 6), true, false);
            }
        }
        geometry.Freeze();
        dc.PushOpacity(_lightningFrames == 1 ? 0.42 : 1);
        dc.DrawEllipse(Frozen(Color.FromArgb(32, 201, 214, 255)), null, new Point(x, 0), w * 0.32, h * 0.58);
        dc.DrawGeometry(null, new Pen(Frozen(Color.FromArgb(85, 132, 190, 255)), 3), geometry);
        dc.DrawGeometry(null, new Pen(Frozen(Color.FromArgb(225, 236, 247, 255)), 1.35), geometry);
        dc.Pop();
    }

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
