using System.Windows.Media;
using PulseMonitor.Views.Controls;
using PulseMonitor.Weather;

namespace PulseMonitor.WindowsTests;

public sealed class GlassRainTests
{
    [Theory]
    [InlineData(51, WeatherAnimationKind.LightDrizzle, 10, 0)]
    [InlineData(53, WeatherAnimationKind.Drizzle, 18, 0)]
    [InlineData(55, WeatherAnimationKind.DenseDrizzle, 27, 0)]
    [InlineData(56, WeatherAnimationKind.FreezingDrizzle, 10, 0)]
    [InlineData(57, WeatherAnimationKind.DenseFreezingDrizzle, 27, 0)]
    [InlineData(65, WeatherAnimationKind.SteadyHeavyRain, 29, 21)]
    [InlineData(66, WeatherAnimationKind.FreezingRain, 12, 3)]
    [InlineData(67, WeatherAnimationKind.HeavyFreezingRain, 24, 12)]
    [InlineData(61, WeatherAnimationKind.LightRain, 7, 0)]
    [InlineData(63, WeatherAnimationKind.Rain, 15, 0)]
    [InlineData(80, WeatherAnimationKind.LightShowers, 16, 17)]
    [InlineData(81, WeatherAnimationKind.Showers, 23, 26)]
    [InlineData(82, WeatherAnimationKind.HeavyRain, 30, 36)]
    public void WeatherCodeControlsDropDensityAndMovingTrails(int code, WeatherAnimationKind expected, int count, int streaks)
    {
        var kind = WeatherCodes.Animation(code, true);
        Assert.Equal(expected, kind);
        var rain = new GlassRainLayer();
        rain.Step(kind, 80, 40);
        Assert.Equal(count, Render(rain).Children.OfType<GeometryDrawing>()
            .Count(d => d.Geometry is EllipseGeometry && d.Brush is LinearGradientBrush));

        Assert.Equal(streaks, Render(rain).Children.OfType<GeometryDrawing>().Count(d => d.Geometry is LineGeometry));
        for (int i = 0; i < 8; i++) rain.Step(kind, 80, 40);
        Assert.Contains(Render(rain).Children.OfType<GeometryDrawing>(), d => d.Geometry is LineGeometry);
        rain.Clear();
        Assert.Empty(Render(rain).Children);
    }

    [Fact]
    public void ShowerGustsChangeDensityAndRenderDifferentDropLengths()
    {
        var layer = new ShowerStreaks();
        var random = new Random(25);
        var counts = new HashSet<int>();
        for (int frame = 0; frame < 100; frame++)
        {
            layer.Step(random, new System.Windows.Size(80, 40), 26, 5.5);
            var drawing = new DrawingGroup();
            using (var context = drawing.Open()) layer.Render(context);
            var lines = drawing.Children.Cast<GeometryDrawing>().Select(d => (LineGeometry)d.Geometry).ToArray();
            counts.Add(lines.Length);
            Assert.True(lines.Select(line => line.EndPoint.Y - line.StartPoint.Y).Distinct().Count() > 3);
        }
        Assert.True(counts.Max() - counts.Min() >= 5);
    }

    private static DrawingGroup Render(GlassRainLayer rain)
    {
        var drawing = new DrawingGroup();
        using (var context = drawing.Open()) rain.Render(context);
        return drawing;
    }
}
