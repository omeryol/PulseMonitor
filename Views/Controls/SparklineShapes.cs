using System.Windows;
using System.Windows.Media;
using PulseMonitor.Settings;

namespace PulseMonitor.Views.Controls;

/// <summary>Ağ mini grafiğinin biçimleri: değerlerden (0–1) alan ve çizgi şekli üretir. Çizimi <see cref="Sparkline"/> yapar.</summary>
internal static class SparklineShapes
{
    /// <param name="Fill">Dolgu (yoksa null).</param>
    /// <param name="Line">Üst çizgi (yoksa null).</param>
    /// <param name="FillOpacity">Dolgunun saydamlığı.</param>
    /// <param name="Fade">Dolgu aşağı doğru solar.</param>
    public readonly record struct Shapes(Geometry? Fill, Geometry? Line, double FillOpacity, bool Fade);

    public static Shapes Build(string variant, IReadOnlyList<double> v, double w, double h)
    {
        Point[] p = Points(v, w, h);
        switch (variant)
        {
            case StyleOptions.SparkLine:
                return new(null, Polyline(p), 0, false);
            case StyleOptions.SparkSmooth:
                StreamGeometry smooth = Smooth(p, h);
                return new(Area(smooth, p, w, h, steps: false), smooth, 0.5, false);
            case StyleOptions.SparkBars:
                return new(Bars(v, w, h), null, 0.9, false);
            case StyleOptions.SparkSteps:
                StreamGeometry steps = Steps(p);
                return new(Area(steps, p, w, h, steps: true), steps, 0.5, false);
            case StyleOptions.SparkFade:
                StreamGeometry fade = Polyline(p);
                return new(Area(fade, p, w, h, steps: false), fade, 1.0, true);
            default:
                StreamGeometry area = Polyline(p);
                return new(Area(area, p, w, h, steps: false), area, 0.5, false);
        }
    }

    private static Point[] Points(IReadOnlyList<double> v, double w, double h)
    {
        var p = new Point[v.Count];
        for (int i = 0; i < v.Count; i++)
            p[i] = new Point(i * w / (v.Count - 1), h - Math.Clamp(v[i], 0, 1) * h);
        return p;
    }

    private static StreamGeometry Polyline(Point[] p)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(p[0], isFilled: false, isClosed: false);
            for (int i = 1; i < p.Length; i++) c.LineTo(p[i], isStroked: true, isSmoothJoin: true);
        }
        g.Freeze();
        return g;
    }

    private static StreamGeometry Smooth(Point[] p, double h)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(p[0], isFilled: false, isClosed: false);
            for (int i = 0; i < p.Length - 1; i++)
            {
                Point p0 = p[Math.Max(i - 1, 0)], p1 = p[i], p2 = p[i + 1], p3 = p[Math.Min(i + 2, p.Length - 1)];
                var c1 = new Point(p1.X + (p2.X - p0.X) / 6, Math.Clamp(p1.Y + (p2.Y - p0.Y) / 6, 0, h));
                var c2 = new Point(p2.X - (p3.X - p1.X) / 6, Math.Clamp(p2.Y - (p3.Y - p1.Y) / 6, 0, h));
                c.BezierTo(c1, c2, p2, isStroked: true, isSmoothJoin: true);
            }
        }
        g.Freeze();
        return g;
    }

    private static StreamGeometry Steps(Point[] p)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(p[0], isFilled: false, isClosed: false);
            foreach (Point q in StepPoints(p)) c.LineTo(q, isStroked: true, isSmoothJoin: false);
        }
        g.Freeze();
        return g;
    }

    private static IEnumerable<Point> StepPoints(Point[] p)
    {
        for (int i = 1; i < p.Length; i++)
        {
            double mid = (p[i - 1].X + p[i].X) / 2;
            yield return new Point(mid, p[i - 1].Y);
            yield return new Point(mid, p[i].Y);
        }
        yield return p[^1];
    }

    /// <summary>Üst çizginin altını dolduran kapalı şekil.</summary>
    private static StreamGeometry Area(Geometry top, Point[] p, double w, double h, bool steps)
    {
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            c.BeginFigure(new Point(0, h), isFilled: true, isClosed: true);
            c.LineTo(p[0], false, false);
            if (steps)
            {
                foreach (Point q in StepPoints(p)) c.LineTo(q, false, false);
            }
            else
            {
                PathGeometry flat = top.GetFlattenedPathGeometry(0.05, ToleranceType.Absolute);
                foreach (PathFigure f in flat.Figures)
                    foreach (PathSegment s in f.Segments)
                    {
                        if (s is PolyLineSegment pl) foreach (Point q in pl.Points) c.LineTo(q, false, false);
                        else if (s is LineSegment ls) c.LineTo(ls.Point, false, false);
                    }
            }
            c.LineTo(new Point(w, h), false, false);
        }
        g.Freeze();
        return g;
    }

    private static StreamGeometry Bars(IReadOnlyList<double> v, double w, double h)
    {
        int bars = Math.Clamp((int)(w / 3), 1, v.Count);
        double slot = w / bars, bw = Math.Max(1, slot * 0.7);
        var g = new StreamGeometry();
        using (StreamGeometryContext c = g.Open())
        {
            for (int b = 0; b < bars; b++)
            {
                int from = b * v.Count / bars, to = Math.Max(from + 1, (b + 1) * v.Count / bars);
                double sum = 0;
                for (int i = from; i < to; i++) sum += Math.Clamp(v[i], 0, 1);
                double bh = Math.Max(1, sum / (to - from) * h), x = b * slot + (slot - bw) / 2;
                c.BeginFigure(new Point(x, h - bh), true, true);
                c.LineTo(new Point(x + bw, h - bh), false, false);
                c.LineTo(new Point(x + bw, h), false, false);
                c.LineTo(new Point(x, h), false, false);
            }
        }
        g.Freeze();
        return g;
    }
}
