using System.Windows;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

internal sealed class ShowerStreaks
{
    private struct Streak
    {
        public double X, Y, Speed, Length, Lean;
        public int Depth;
    }

    private static readonly Pen[] Pens = [PenFor(85, 0.75), PenFor(146, 1), PenFor(195, 1.25)];
    private readonly Streak[] _streaks = new Streak[96];
    private int _count, _gustFrames;
    private double _strength = 1, _targetStrength = 1, _wind = 0.18, _targetWind = 0.18;

    public void Clear()
    {
        _count = _gustFrames = 0;
        _strength = _targetStrength = 1;
        _wind = _targetWind = 0.18;
    }

    public void Step(Random random, Size bounds, int density, double speed)
    {
        if (density == 0) return;
        if (--_gustFrames <= 0)
        {
            _targetStrength = 0.78 + random.NextDouble() * 0.86;
            _targetWind = -0.12 + random.NextDouble() * 0.66;
            _gustFrames = random.Next(9, 34);
        }
        _wind += (_targetWind - _wind) * 0.16;
        int wanted = (int)Math.Clamp(Math.Round(density * Math.Clamp(bounds.Width / 80, 0.65, 2) * _strength), 1, _streaks.Length);
        for (int i = 0; i < wanted; i++)
        {
            ref Streak s = ref _streaks[i];
            if (i >= _count || s.Y > bounds.Height + s.Length || s.X < -s.Length || s.X > bounds.Width + s.Length)
            {
                s.Depth = random.Next(Pens.Length);
                s.Speed = speed * (0.64 + s.Depth * 0.26 + random.NextDouble() * 0.55);
                s.Length = Math.Min(bounds.Height * 0.52, s.Speed * (1.35 + random.NextDouble()));
                s.Lean = (random.NextDouble() - 0.5) * 0.16;
                s.X = random.NextDouble() * bounds.Width;
                s.Y = i >= _count ? random.NextDouble() * bounds.Height : -random.NextDouble() * s.Length;
            }
            else
            {
                double dy = s.Speed * (0.75 + 0.25 * _strength);
                s.Y += dy;
                s.X -= dy * (_wind + s.Lean);
            }
        }
        _count = wanted;
        _strength += (_targetStrength - _strength) * 0.18;
    }

    public void Render(DrawingContext dc)
    {
        for (int i = 0; i < _count; i++)
        {
            ref readonly Streak s = ref _streaks[i];
            dc.DrawLine(Pens[s.Depth], new Point(s.X + s.Length * (_wind + s.Lean), s.Y - s.Length), new Point(s.X, s.Y));
        }
    }

    private static Pen PenFor(byte opacity, double width)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(opacity, 200, 220, 232)), width);
        pen.Freeze();
        return pen;
    }
}
