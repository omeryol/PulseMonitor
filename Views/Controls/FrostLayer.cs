using System.Windows;
using System.Windows.Media;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

internal sealed class FrostLayer
{
    private readonly record struct Crystal(double X, double Angle, double Size, bool Top, int Delay);
    private readonly List<Crystal> _edge = [];
    private readonly Random _random = new();
    private static readonly Pen Ice = CreatePen();
    private int _age;

    public static bool Supports(WeatherAnimationKind k) => k is WeatherAnimationKind.RimeFog
        or WeatherAnimationKind.FreezingDrizzle or WeatherAnimationKind.DenseFreezingDrizzle
        or WeatherAnimationKind.FreezingRain or WeatherAnimationKind.HeavyFreezingRain;

    public void Clear() { _edge.Clear(); _age = 0; }

    public void Step(WeatherAnimationKind kind)
    {
        int count = kind is WeatherAnimationKind.HeavyFreezingRain or WeatherAnimationKind.RimeFog
            or WeatherAnimationKind.DenseFreezingDrizzle ? 16 : 8;
        while (_edge.Count < count)
            _edge.Add(new(_random.NextDouble(), -24 + _random.NextDouble() * 48,
                2.5 + _random.NextDouble() * 5, _random.Next(2) == 0, _random.Next(36)));
        _age = Math.Min(_age + 1, 90);
    }

    public void Render(DrawingContext dc, double w, double h)
    {
        foreach (var crystal in _edge)
        {
            double size = crystal.Size * Math.Clamp((_age - crystal.Delay) / 35.0, 0, 1);
            if (size < 0.3) continue;
            var matrix = Matrix.Identity;
            matrix.Rotate(crystal.Angle + (crystal.Top ? 0 : 180));
            matrix.Translate(crystal.X * w, crystal.Top ? 0 : h);
            dc.PushTransform(new MatrixTransform(matrix));
            dc.DrawLine(Ice, new Point(), new Point(0, size));
            foreach (double side in Sides)
            {
                dc.DrawLine(Ice, new Point(0, size * 0.32), new Point(side * size * 0.33, size * 0.62));
                dc.DrawLine(Ice, new Point(0, size * 0.64), new Point(side * size * 0.21, size * 0.86));
            }
            dc.Pop();
        }
    }
    private static readonly double[] Sides = [-1, 1];
    private static Pen CreatePen()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(165, 203, 232, 250)), 0.8);
        pen.Freeze();
        return pen;
    }
}
