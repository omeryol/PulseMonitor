using System.Windows;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

internal sealed class MistLayer
{
    private readonly record struct Bank(double X, double Y, double Width, double Height, double Speed, double Phase);
    private static readonly Brush Fog = Vapour(Color.FromArgb(64, 201, 214, 219));
    private static readonly Brush Cloud = Vapour(Color.FromArgb(138, 102, 116, 143));
    private readonly Bank[] _banks = new Bank[5];
    private readonly Random _random = new();
    private int _count;

    public void Clear() => _count = 0;

    public void Step(double width, double height, bool storm)
    {
        for (int i = 0; i < _banks.Length; i++)
        {
            var bank = _banks[i];
            if (i >= _count || bank.X - bank.Width > width)
            {
                double radius = width * (0.32 + _random.NextDouble() * 0.43);
                double y = storm ? _random.NextDouble() * height * 0.28 : height * (0.32 + _random.NextDouble() * 0.66);
                bank = new Bank(i >= _count ? width * (i + _random.NextDouble()) / _banks.Length : -radius,
                    y, radius, height * (0.14 + _random.NextDouble() * 0.25),
                    (storm ? 0.45 : 0.12) * (0.7 + _random.NextDouble()), _random.NextDouble() * Math.Tau);
            }
            _banks[i] = bank with { X = bank.X + bank.Speed, Phase = bank.Phase + 0.017 };
        }
        _count = _banks.Length;
    }

    public void Render(DrawingContext dc, bool storm)
    {
        for (int i = 0; i < _count; i++)
        {
            var bank = _banks[i];
            dc.DrawEllipse(storm ? Cloud : Fog, null,
                new Point(bank.X, bank.Y + Math.Sin(bank.Phase) * bank.Height * 0.14), bank.Width, bank.Height);
        }
    }

    private static Brush Vapour(Color centre)
    {
        var edge = centre;
        edge.A = 0;
        var brush = new RadialGradientBrush(centre, edge);
        brush.Freeze();
        return brush;
    }
}
