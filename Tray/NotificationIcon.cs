using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PulseMonitor.Interop;

namespace PulseMonitor.Tray;

/// <summary>
/// Bildirimin büyük simgesi: verilen resim, sağ alt köşede küçük sarı uyarı üçgeni rozetiyle. HICON üretir
/// (<see cref="Destroy"/> ile serbest bırakılır).
/// </summary>
internal static class NotificationIcon
{
    private const int Size = 64;

    public static IntPtr WithWarningBadge(ImageSource image)
    {
        try
        {
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Hava simgelerinin kendi boşluğu var; bildirimde simge küçük göründüğü için kenarlara taşırılıp kırpılır.
                dc.DrawImage(image, new Rect(-12, -14, Size + 22, Size + 22));
                DrawBadge(dc);
            }
            var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;
            using var drawing = new System.Drawing.Bitmap(stream);
            return drawing.GetHicon();
        }
        catch (Exception ex)
        {
            Core.Log.Warn("Bildirim simgesi oluşturulamadı.", ex);
            return IntPtr.Zero;
        }
    }

    public static void Destroy(IntPtr icon)
    {
        if (icon != IntPtr.Zero) User32.DestroyIcon(icon);
    }

    private static void DrawBadge(DrawingContext dc)
    {
        var amber = new SolidColorBrush(Color.FromRgb(0xF5, 0xB4, 0x00));
        var edge = new Pen(new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), 2.5) { LineJoin = PenLineJoin.Round };
        var triangle = new StreamGeometry();
        using (StreamGeometryContext g = triangle.Open())
        {
            g.BeginFigure(new Point(51, 36), isFilled: true, isClosed: true);
            g.LineTo(new Point(63, 62), true, false);
            g.LineTo(new Point(39, 62), true, false);
        }
        dc.DrawGeometry(amber, edge, triangle);

        var mark = new FormattedText("!", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            20, new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), 1.0);
        dc.DrawText(mark, new Point(51 - mark.Width / 2, 39));
    }
}
