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
    private static readonly Brush RainBrush = Frozen(Color.FromArgb(150, 132, 190, 250));
    private static readonly Brush SnowBrush = Frozen(Color.FromArgb(210, 230, 244, 255));
    private static readonly Brush FogBrush = Frozen(Color.FromArgb(38, 210, 220, 232));

    private sealed class Particle
    {
        public Point Position;
        public Vector Velocity;
        public double Size;
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(WeatherAnimationKind), typeof(WeatherAnimation),
        new PropertyMetadata(WeatherAnimationKind.None, (d, _) => ((WeatherAnimation)d).UpdateTimer()));

    private readonly SkyLayer _sky = new();
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<Particle> _particles = [];
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
        _particles.Clear();
        _sky.Clear();
        _frame = 0;
        _lightningFrames = 0;
        _nextLightningFrame = NextLightningFrame();
        if (run && !_timer.IsEnabled) _timer.Start();
        else if (!run && _timer.IsEnabled) _timer.Stop();
        InvalidateVisual();
    }

    private int NextLightningFrame() => _frame + _random.Next(Fps * 5, Fps * 11);

    private void Step()
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        if (SkyLayer.IsSky(Kind))
        {
            _sky.Step(Kind, w, h);
        }
        else if (Kind == WeatherAnimationKind.Thunder)
        {
            if (_lightningFrames > 0) _lightningFrames--;
            else if (_frame >= _nextLightningFrame)
            {
                _lightningFrames = _random.Next(2, 4);
                _lightningSeed = _random.Next();
                _nextLightningFrame = NextLightningFrame();
            }
        }
        else
        {
            int wanted = (int)Math.Round(Math.Clamp(w / 90.0, 1, 2) * (Kind == WeatherAnimationKind.Fog ? 3 : Kind == WeatherAnimationKind.Snow ? 9 : 12));
            while (_particles.Count < wanted) _particles.Add(Spawn(w, h, initial: true));
            while (_particles.Count > wanted) _particles.RemoveAt(_particles.Count - 1);
            foreach (Particle p in _particles)
            {
                p.Position += p.Velocity;
                if (Kind == WeatherAnimationKind.Fog && p.Position.X > w + p.Size) Reset(p, w, h, false);
                else if (Kind != WeatherAnimationKind.Fog && p.Position.Y > h + p.Size) Reset(p, w, h, false);
            }
        }

        _frame++;
        InvalidateVisual();
    }

    private Particle Spawn(double w, double h, bool initial)
    {
        var p = new Particle();
        Reset(p, w, h, initial);
        return p;
    }

    private void Reset(Particle p, double w, double h, bool initial)
    {
        switch (Kind)
        {
            case WeatherAnimationKind.Snow:
                p.Size = 0.8 + _random.NextDouble() * 0.8;
                p.Velocity = new Vector(-0.08 + _random.NextDouble() * 0.20, 0.45 + _random.NextDouble() * 0.45);
                p.Position = initial ? new Point(_random.NextDouble() * w, _random.NextDouble() * h) : new Point(_random.NextDouble() * w, -p.Size);
                break;
            case WeatherAnimationKind.Fog:
                p.Size = 24 + _random.NextDouble() * 22;
                p.Velocity = new Vector(0.10 + _random.NextDouble() * 0.12, 0);
                p.Position = initial ? new Point(_random.NextDouble() * w, _random.NextDouble() * h) : new Point(-p.Size, _random.NextDouble() * h);
                break;
            default:
                p.Size = 4.5 + _random.NextDouble() * 3.5;
                p.Velocity = new Vector(-0.35 - _random.NextDouble() * 0.35, 3.0 + _random.NextDouble() * 1.5);
                p.Position = initial ? new Point(_random.NextDouble() * w, _random.NextDouble() * h) : new Point(_random.NextDouble() * w, -p.Size);
                break;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (Kind == WeatherAnimationKind.None || w <= 0 || h <= 0) return;

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h), CornerRadius, CornerRadius));
        if (SkyLayer.IsSky(Kind)) _sky.Render(dc, Kind, w);
        foreach (Particle p in _particles)
        {
            switch (Kind)
            {
                case WeatherAnimationKind.Snow:
                    dc.DrawEllipse(SnowBrush, null, p.Position, p.Size, p.Size);
                    break;
                case WeatherAnimationKind.Fog:
                    dc.DrawRoundedRectangle(FogBrush, null, new Rect(p.Position.X - p.Size / 2, p.Position.Y, p.Size, 4), 2, 2);
                    break;
                default:
                    dc.DrawLine(new Pen(RainBrush, 1.05) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, p.Position,
                        new Point(p.Position.X - p.Velocity.X * 1.6, p.Position.Y + p.Size));
                    break;
            }
        }
        if (Kind == WeatherAnimationKind.Thunder && _lightningFrames > 0) DrawLightning(dc, w, h);
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
                x += -3 + random.NextDouble() * 6;
                c.LineTo(new Point(x, y + 6), true, false);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(Frozen(Color.FromArgb(55, 132, 190, 255)), 3), geometry);
        dc.DrawGeometry(null, new Pen(Frozen(Color.FromArgb(225, 236, 247, 255)), 1), geometry);
    }

    private static Brush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
