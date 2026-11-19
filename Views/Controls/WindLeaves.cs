using System.Windows;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

internal sealed class WindLeaves
{
    internal const double StartKmh = Weather.WeatherCodes.WindyKmh;
    private static readonly Geometry Shape = LeafShape();
    private static readonly Brush[] Colours = [Brushes.OliveDrab, Brushes.Peru, Brushes.DarkGoldenrod];
    private sealed class Leaf
    {
        public double X, Y, Phase, Radius, Speed, Turn;
        public int Colour;
    }
    private readonly List<Leaf> _leaves = [];
    private readonly Random _random = new();

    public void Clear() => _leaves.Clear();

    public void Step(double wind, double w, double h)
    {
        int count = wind >= StartKmh ? (int)Math.Clamp(2 + (wind - StartKmh) / 9, 2, 7) : 0;
        if (_leaves.Count > count) _leaves.RemoveRange(count, _leaves.Count - count);
        while (_leaves.Count < count) _leaves.Add(Spawn(_random.NextDouble() * w, h));
        for (int i = 0; i < count; i++)
        {
            Leaf l = _leaves[i];
            l.Phase += l.Turn;
            l.X += Math.Min(wind, 100) * l.Speed * (1 + 0.24 * Math.Sin(l.Phase));
            if (l.X > w + l.Radius * 2) _leaves[i] = Spawn(-5, h);
        }
    }

    private Leaf Spawn(double x, double h) => new()
    {
        X = x, Y = h * (0.18 + 0.64 * _random.NextDouble()), Phase = _random.NextDouble() * Math.Tau,
        Radius = 1.6 + _random.NextDouble() * 1.35, Speed = 0.036 + _random.NextDouble() * 0.035,
        Turn = 0.09 + _random.NextDouble() * 0.13, Colour = _random.Next(Colours.Length),
    };

    public void Render(DrawingContext dc)
    {
        foreach (Leaf l in _leaves)
        {
            var matrix = Matrix.Identity;
            matrix.Scale(l.Radius, l.Radius * (0.22 + Math.Abs(Math.Cos(l.Phase))));
            matrix.Rotate(35 * Math.Sin(l.Phase) + 12);
            matrix.Translate(l.X, l.Y + 3.6 * Math.Sin(l.Phase * 0.8));
            dc.PushTransform(new MatrixTransform(matrix));
            dc.DrawGeometry(Colours[l.Colour], null, Shape);
            dc.Pop();
        }
    }

    private static Geometry LeafShape()
    {
        var shape = Geometry.Parse("M -1.65,0.23 Q -0.65,-1.3 1.6,-0.36 Q 0.6,1 -1.65,0.23 Z");
        shape.Freeze();
        return shape;
    }
}
