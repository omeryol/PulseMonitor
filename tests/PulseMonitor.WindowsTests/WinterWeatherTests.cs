using System.Windows.Media;
using PulseMonitor.Views.Controls;
using PulseMonitor.Weather;

namespace PulseMonitor.WindowsTests;

public sealed class WinterWeatherTests
{
    [Theory]
    [InlineData(71, WeatherAnimationKind.LightSnow, 7)]
    [InlineData(73, WeatherAnimationKind.Snow, 15)]
    [InlineData(75, WeatherAnimationKind.HeavySnow, 29)]
    [InlineData(77, WeatherAnimationKind.SnowGrains, 16)]
    [InlineData(85, WeatherAnimationKind.SnowShower, 19)]
    [InlineData(86, WeatherAnimationKind.HeavySnowShower, 36)]
    [InlineData(96, WeatherAnimationKind.Hail, 12)]
    [InlineData(99, WeatherAnimationKind.HeavyHail, 22)]
    public void WinterCodeMovesParticlesAtTaskbarScale(int code, WeatherAnimationKind expected, int count)
    {
        var kind = WeatherCodes.Animation(code, false);
        Assert.Equal(expected, kind);
        Assert.True(WinterPrecipitation.Supports(kind));
        var layer = new WinterPrecipitation();
        layer.Step(kind, 80, 38);
        var before = Draw(dc => layer.Render(dc, kind));
        layer.Step(kind, 80, 38);
        var after = Draw(dc => layer.Render(dc, kind));
        Assert.Equal(count, after.Length);
        Assert.True(after.Where((shape, index) => shape.Center != before[index].Center).Any());
        layer.Clear();
        Assert.Empty(Draw(dc => layer.Render(dc, kind)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FogAndStormBanksMoveAndClear(bool storm)
    {
        var layer = new MistLayer();
        layer.Step(80, 38, storm);
        var before = Draw(dc => layer.Render(dc, storm));
        layer.Step(80, 38, storm);
        var after = Draw(dc => layer.Render(dc, storm));
        Assert.NotEmpty(after);
        Assert.True(after[0].Center.X > before[0].Center.X);
        layer.Clear();
        Assert.Empty(Draw(dc => layer.Render(dc, storm)));
    }

    [Fact]
    public void WindLeavesStartAtThresholdAndDisappearInCalmWeather()
    {
        var wind = new WindLeaves();
        var drawing = new DrawingGroup();
        foreach (double speed in new[] { 24.0, 25, 50, 0 })
        {
            wind.Step(speed, 80, 38);
            using (var dc = drawing.Open()) wind.Render(dc);
            Assert.Equal(speed >= 25, !drawing.Bounds.IsEmpty);
        }
    }

    [Theory]
    [InlineData(1, WeatherAnimationKind.FewClouds)]
    [InlineData(48, WeatherAnimationKind.RimeFog)]
    [InlineData(97, WeatherAnimationKind.HeavyThunder)]
    public void AdditionalWeatherCodesSelectTheirOwnEffect(int code, WeatherAnimationKind kind) =>
        Assert.Equal(kind, WeatherCodes.Animation(code, true));

    private static EllipseGeometry[] Draw(Action<DrawingContext> render)
    {
        var drawing = new DrawingGroup();
        using (var dc = drawing.Open()) render(dc);
        return drawing.Children.Cast<GeometryDrawing>().Select(d => (EllipseGeometry)d.Geometry).ToArray();
    }
}
