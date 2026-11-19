using System.Windows;
using System.Windows.Media;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

internal sealed class WinterPrecipitation
{
    private struct Particle
    {
        public double X, Y, Radius, Fall, Drift, Phase;
        public bool Bounced;
    }

    private static readonly Brush[] Snow = [Brush(100), Brush(175), Brush(225)];
    private static readonly Brush Ice = Brush(220);
    private readonly Particle[] _particles = new Particle[72];
    private readonly Random _random = new();
    private int _count, _windFrames;
    private double _wind, _targetWind;

    public static bool Supports(WeatherAnimationKind kind) => kind is
        WeatherAnimationKind.LightSnow or WeatherAnimationKind.Snow or WeatherAnimationKind.HeavySnow
        or WeatherAnimationKind.SnowGrains or WeatherAnimationKind.SnowShower or WeatherAnimationKind.HeavySnowShower
        or WeatherAnimationKind.Hail or WeatherAnimationKind.HeavyHail;

    public void Clear()
    {
        _count = _windFrames = 0;
        _wind = _targetWind = 0;
    }

    public void Step(WeatherAnimationKind kind, double width, double height)
    {
        var (density, gust) = kind switch
        {
            WeatherAnimationKind.LightSnow => (7, 0.25),
            WeatherAnimationKind.HeavySnow => (29, 0.85),
            WeatherAnimationKind.SnowShower => (19, 2.1),
            WeatherAnimationKind.HeavySnowShower => (36, 3.2),
            WeatherAnimationKind.SnowGrains => (16, 0.35),
            WeatherAnimationKind.Hail => (12, 0.65),
            WeatherAnimationKind.HeavyHail => (22, 1.1),
            _ => (15, 0.55),
        };
        if (--_windFrames <= 0)
        {
            _windFrames = _random.Next(12, 48);
            _targetWind = (_random.NextDouble() * 1.6 - 0.6) * gust;
        }
        _wind += (_targetWind - _wind) * 0.14;
        bool hail = kind is WeatherAnimationKind.Hail or WeatherAnimationKind.HeavyHail;
        bool grains = kind == WeatherAnimationKind.SnowGrains;
        int wanted = (int)Math.Clamp(Math.Round(density * width * height / 3040), 3, _particles.Length);
        for (int i = 0; i < wanted; i++)
        {
            ref Particle p = ref _particles[i];
            if (i >= _count || p.Y > height + p.Radius)
            {
                double depth = _random.NextDouble();
                p.Radius = grains ? 0.45 + depth * 0.34 : 0.5 + depth * (kind == WeatherAnimationKind.HeavyHail ? 1.75 : 1.1);
                p.X = _random.NextDouble() * width;
                p.Y = i >= _count ? _random.NextDouble() * height : -p.Radius;
                p.Fall = hail ? 1.6 + depth * 2.3 : 0.28 + depth * 0.85;
                p.Drift = _random.NextDouble() * 0.4 - 0.2;
                p.Phase = _random.NextDouble() * Math.Tau;
                p.Bounced = false;
            }
            p.Phase += 0.065;
            p.X += _wind + p.Drift + (hail || grains ? 0 : Math.Sin(p.Phase) * p.Radius * 0.42);
            if (p.X < -p.Radius) p.X = width + p.Radius;
            else if (p.X > width + p.Radius) p.X = -p.Radius;
            if (hail) p.Fall += 0.26;
            p.Y += p.Fall;
            if (hail && !p.Bounced && p.Y + p.Radius >= height)
            {
                p.Y = height - p.Radius;
                p.Fall = -Math.Min(2.7, p.Fall * (0.32 + _random.NextDouble() * 0.18));
                p.Drift += _random.NextDouble() - 0.5;
                p.Bounced = true;
            }
        }
        _count = wanted;
    }

    public void Render(DrawingContext dc, WeatherAnimationKind kind)
    {
        bool hail = kind is WeatherAnimationKind.Hail or WeatherAnimationKind.HeavyHail;
        for (int i = 0; i < _count; i++)
        {
            ref readonly Particle p = ref _particles[i];
            Brush brush = hail ? Ice : Snow[p.Radius < 0.8 ? 0 : p.Radius < 1.25 ? 1 : 2];
            double ry = hail ? p.Radius * 0.85 : p.Radius * (0.68 + 0.32 * Math.Abs(Math.Cos(p.Phase)));
            dc.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Radius, ry);
        }
    }

    private static Brush Brush(byte alpha)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, 221, 236, 249));
        brush.Freeze();
        return brush;
    }
}
