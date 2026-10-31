using System.Windows;
using System.Windows.Media;
using PulseMonitor.Weather;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Gökyüzü animasyonları (<see cref="WeatherAnimation"/> kullanır): parçalı/az bulutta bulutlar soldan sağa süzülüp
/// belirir ve kaybolur, kapalıda daha büyük ve koyu bulutlar geçer, açık havada gündüz güneş ışıltıları, gece yıldızlar
/// göz kırpar. Öğe sayısı sütun genişliğiyle orantılıdır; renkler her iki temada da görünsün diye nötr tonlardadır.
/// </summary>
internal sealed class SkyLayer
{
    private sealed class Item { public double X, Y, Size, Speed, Phase; }

    private const double CloudWidth = 40, CloudHeight = 18; // birim bulut şekli; ölçekle büyütülür
    private static readonly Geometry CloudShape = BuildCloud();

    private readonly List<Item> _items = new();
    private readonly Random _random = new();

    public static bool IsSky(WeatherAnimationKind kind) =>
        kind is WeatherAnimationKind.Clouds or WeatherAnimationKind.Overcast or WeatherAnimationKind.Sun or WeatherAnimationKind.Stars;

    public void Clear() => _items.Clear();

    public void Step(WeatherAnimationKind kind, double w, double h)
    {
        int count = Count(kind, w);
        while (_items.Count < count) _items.Add(Spawn(kind, w, h, initial: true));
        while (_items.Count > count) _items.RemoveAt(_items.Count - 1);

        for (int i = 0; i < _items.Count; i++)
        {
            Item it = _items[i];
            if (kind is WeatherAnimationKind.Clouds or WeatherAnimationKind.Overcast)
            {
                it.X += it.Speed;
                if (it.X > w) _items[i] = Spawn(kind, w, h, initial: false);
            }
            else
            {
                it.Phase += 0.07;
                if (it.Phase > Math.PI) _items[i] = Spawn(kind, w, h, initial: false);
            }
        }
    }

    public void Render(DrawingContext dc, WeatherAnimationKind kind, double w)
    {
        foreach (Item it in _items)
        {
            switch (kind)
            {
                case WeatherAnimationKind.Clouds or WeatherAnimationKind.Overcast:
                    DrawCloud(dc, it, w, kind == WeatherAnimationKind.Overcast);
                    break;
                case WeatherAnimationKind.Sun:
                    DrawGlint(dc, it);
                    break;
                default:
                    DrawStar(dc, it);
                    break;
            }
        }
    }

    private static int Count(WeatherAnimationKind kind, double w)
    {
        double wide = Math.Clamp(w / 100.0, 1, 2);
        return kind switch
        {
            WeatherAnimationKind.Overcast => (int)Math.Round(3 * wide),
            WeatherAnimationKind.Clouds => (int)Math.Round(2 * wide),
            WeatherAnimationKind.Sun => (int)Math.Round(4 * wide),
            _ => (int)Math.Round(6 * wide),
        };
    }

    private Item Spawn(WeatherAnimationKind kind, double w, double h, bool initial)
    {
        if (kind is WeatherAnimationKind.Clouds or WeatherAnimationKind.Overcast)
        {
            double scale = kind == WeatherAnimationKind.Overcast ? 1.3 + _random.NextDouble() * 0.9 : 0.9 + _random.NextDouble() * 0.7;
            double cw = CloudWidth * scale;
            return new Item
            {
                Size = scale,
                X = initial ? -cw + _random.NextDouble() * (w + cw) : -cw,
                Y = _random.NextDouble() * Math.Max(1, h - CloudHeight * scale),
                Speed = 0.25 + _random.NextDouble() * 0.25,
            };
        }
        return new Item
        {
            X = _random.NextDouble() * w,
            Y = _random.NextDouble() * h,
            Size = kind == WeatherAnimationKind.Sun ? 3 + _random.NextDouble() * 3 : 0.7 + _random.NextDouble() * 0.7,
            Phase = initial ? -_random.NextDouble() * 3 : -_random.NextDouble() * 1.5,
        };
    }

    private static void DrawCloud(DrawingContext dc, Item it, double w, bool overcast)
    {
        double cw = CloudWidth * it.Size;
        double t = Math.Clamp((it.X + cw) / (w + cw), 0, 1); // 0 = girişte, 1 = çıkışta; ortada en belirgin
        double fade = Math.Sin(Math.PI * t);
        byte alpha = (byte)((overcast ? 75 : 55) * fade);
        if (alpha < 3) return;
        Color c = overcast ? Color.FromRgb(120, 132, 150) : Color.FromRgb(160, 174, 192);
        dc.PushTransform(new MatrixTransform(it.Size, 0, 0, it.Size, it.X, it.Y));
        dc.DrawGeometry(Solid(Color.FromArgb(alpha, c.R, c.G, c.B)), null, CloudShape);
        dc.Pop();
    }

    private static void DrawGlint(DrawingContext dc, Item it)
    {
        double a = Math.Sin(Math.Max(0, it.Phase));
        if (a <= 0.02) return;
        var pen = new Pen(Solid(Color.FromArgb((byte)(210 * a), 253, 224, 71)), 1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        double r = it.Size * (0.5 + 0.5 * a);
        dc.DrawLine(pen, new Point(it.X - r, it.Y), new Point(it.X + r, it.Y));
        dc.DrawLine(pen, new Point(it.X, it.Y - r), new Point(it.X, it.Y + r));
    }

    private static void DrawStar(DrawingContext dc, Item it)
    {
        double a = Math.Sin(Math.Max(0, it.Phase));
        if (a <= 0.02) return;
        dc.DrawEllipse(Solid(Color.FromArgb((byte)(230 * a), 226, 232, 255)), null, new Point(it.X, it.Y), it.Size, it.Size);
    }

    private static Brush Solid(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    /// <summary>Üç daire + düz tabanlı yuvarlak dikdörtgenin birleşimi (tek parça, üst üste binen yarı saydamlık koyulaşmaz).</summary>
    private static Geometry BuildCloud()
    {
        Geometry g = new RectangleGeometry(new Rect(2, 9, 36, 9), 4.5, 4.5);
        g = Geometry.Combine(g, new EllipseGeometry(new Point(13, 10), 7, 7), GeometryCombineMode.Union, null);
        g = Geometry.Combine(g, new EllipseGeometry(new Point(23, 7), 8, 8), GeometryCombineMode.Union, null);
        g = Geometry.Combine(g, new EllipseGeometry(new Point(31, 11.5), 5.5, 5.5), GeometryCombineMode.Union, null);
        g.Freeze();
        return g;
    }
}
