using System.Windows;
using System.Windows.Media;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

internal sealed class GlassRainLayer
{
    private sealed class Drop
    {
        public Point Center;
        public double Radius, Phase, Speed;
    }

    private readonly record struct Trail(Point From, Point To, int Frame);
    private const int TrailLife = 18;
    private static readonly LinearGradientBrush Water = CreateWater();
    private static readonly Pen[] TrailPens = CreateTrailPens();
    private readonly ShowerStreaks _showers = new();
    private readonly List<Drop> _drops = [];
    private readonly Queue<Trail> _trails = new();
    private readonly Random _random = new();
    private Size _bounds;
    private int _frame, _nextRun;

    public static bool IsRain(WeatherAnimationKind kind) =>
        kind is WeatherAnimationKind.LightRain or WeatherAnimationKind.Rain or WeatherAnimationKind.HeavyRain or WeatherAnimationKind.LightShowers or WeatherAnimationKind.Showers
        or WeatherAnimationKind.SteadyHeavyRain or WeatherAnimationKind.LightDrizzle or WeatherAnimationKind.Drizzle
        or WeatherAnimationKind.DenseDrizzle or WeatherAnimationKind.FreezingDrizzle or WeatherAnimationKind.DenseFreezingDrizzle
        or WeatherAnimationKind.FreezingRain or WeatherAnimationKind.HeavyFreezingRain;

    public void Clear()
    {
        _drops.Clear();
        _trails.Clear();
        _showers.Clear();
        _frame = 0;
        _nextRun = 8;
    }

    public void Step(WeatherAnimationKind kind, double width, double height)
    {
        var bounds = new Size(width, height);
        if (bounds != _bounds)
        {
            Clear();
            _bounds = bounds;
        }

        var (density, interval, streaks, speed) = kind switch
        {
            WeatherAnimationKind.LightDrizzle or WeatherAnimationKind.FreezingDrizzle => (10, 90, 0, 0.0),
            WeatherAnimationKind.Drizzle => (18, 75, 0, 0.0),
            WeatherAnimationKind.DenseDrizzle or WeatherAnimationKind.DenseFreezingDrizzle => (27, 55, 0, 0.0),
            WeatherAnimationKind.FreezingRain => (12, 28, 3, 1.4),
            WeatherAnimationKind.HeavyFreezingRain => (24, 15, 12, 2.8),
            WeatherAnimationKind.SteadyHeavyRain => (29, 8, 21, 4.0),
            WeatherAnimationKind.LightRain => (7, 65, 0, 0.0),
            WeatherAnimationKind.LightShowers => (16, 12, 17, 4.3),
            WeatherAnimationKind.Showers => (23, 8, 26, 5.5),
            WeatherAnimationKind.HeavyRain => (30, 5, 36, 6.8),
            _ => (15, 32, 0, 0.0),
        };
        _showers.Step(_random, bounds, streaks, speed);
        bool fine = kind is WeatherAnimationKind.LightDrizzle or WeatherAnimationKind.Drizzle or WeatherAnimationKind.DenseDrizzle
            or WeatherAnimationKind.FreezingDrizzle or WeatherAnimationKind.DenseFreezingDrizzle;
        int count = (int)Math.Clamp(width * height * density / 3200, 3, 70);
        while (_drops.Count < count)
        {
            _drops.Add(new Drop
            {
                Center = new Point(_random.NextDouble() * width, _random.NextDouble() * height),
                Radius = fine ? 0.35 + _random.NextDouble() * 0.55 : 0.45 + _random.NextDouble() * 1.3,
                Phase = _random.NextDouble() * Math.Tau,
            });
        }
        if (++_frame >= _nextRun)
        {
            int runners = streaks == 0 ? 1 : _random.Next(2, kind == WeatherAnimationKind.HeavyRain ? 5 : 4);
            int index = _random.Next(_drops.Count);
            for (int i = 0; i < _drops.Count && runners > 0; i++)
            {
                var start = _drops[(index + i) % _drops.Count];
                if (start.Speed > 0) continue;
                start.Radius = Math.Max(start.Radius, 1.65 + _random.NextDouble() * 0.6);
                start.Speed = 0.6 + _random.NextDouble() + speed * 0.18;
                runners--;
            }
            _nextRun = _frame + _random.Next(interval / 2, interval + 1);
        }
        while (_trails.TryPeek(out var trail) && _frame - trail.Frame >= TrailLife)
            _trails.Dequeue();

        foreach (var drop in _drops)
        {
            if (drop.Speed <= 0 || drop.Radius <= 0) continue;
            Point previous = drop.Center;
            drop.Speed = Math.Min(3.2, drop.Speed + 0.05);
            drop.Center += new Vector(0.18 * Math.Sin(drop.Phase + previous.Y / 5), drop.Speed);
            _trails.Enqueue(new Trail(previous, drop.Center, _frame));
            foreach (var bead in _drops)
            {
                if (bead.Speed > 0 || bead.Radius <= 0) continue;
                double contact = drop.Radius + bead.Radius;
                if ((bead.Center - drop.Center).LengthSquared > contact * contact) continue;
                drop.Radius = Math.Min(2.8, Math.Sqrt(drop.Radius * drop.Radius + bead.Radius * bead.Radius));
                bead.Radius = 0;
            }
            if (drop.Center.Y > height + drop.Radius * 2) drop.Radius = 0;
        }
        _drops.RemoveAll(static drop => drop.Radius <= 0);
    }

    public void Render(DrawingContext dc)
    {
        _showers.Render(dc);
        foreach (var trail in _trails)
            dc.DrawLine(TrailPens[_frame - trail.Frame], trail.From, trail.To);
        foreach (var drop in _drops)
        {
            double r = drop.Radius;
            double ry = r * (drop.Speed > 0 ? 1.42 : 1.08);
            dc.DrawEllipse(Water, null, drop.Center, r, ry);
            Point glint = drop.Center + new Vector(-0.26 * r, -0.52 * ry);
            dc.DrawEllipse(Brushes.WhiteSmoke, null, glint, r * 0.24, ry * 0.13);
        }
    }

    private static LinearGradientBrush CreateWater()
    {
        var water = new LinearGradientBrush { StartPoint = new Point(0.3, 0), EndPoint = new Point(0.7, 1) };
        water.GradientStops.Add(new GradientStop(Color.FromArgb(145, 228, 239, 244), 0));
        water.GradientStops.Add(new GradientStop(Color.FromArgb(85, 10, 17, 24), 0.3));
        water.GradientStops.Add(new GradientStop(Color.FromArgb(25, 142, 165, 174), 0.64));
        water.GradientStops.Add(new GradientStop(Color.FromArgb(195, 230, 240, 244), 0.91));
        water.GradientStops.Add(new GradientStop(Color.FromArgb(100, 19, 28, 35), 1));
        water.Freeze();
        return water;
    }

    private static Pen[] CreateTrailPens()
    {
        var pens = new Pen[TrailLife];
        for (int age = 0; age < pens.Length; age++)
        {
            byte alpha = (byte)(65 * (TrailLife - age) / TrailLife);
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 192, 208, 215)), 0.85);
            pen.Freeze();
            pens[age] = pen;
        }
        return pens;
    }
}
