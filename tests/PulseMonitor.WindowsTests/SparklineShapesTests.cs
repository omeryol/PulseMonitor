using PulseMonitor.Settings;
using PulseMonitor.Views.Controls;

namespace PulseMonitor.WindowsTests;

public sealed class SparklineShapesTests
{
    private static readonly double[] Sample = [0, 0.2, 0.9, 0.4, 1, 0.1, 0, 0.6, 0.3, 0.8, 0.5, 0.05];

    public static IEnumerable<object[]> Variants() => StyleOptions.Groups["SparklineStyle"].Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(Variants))]
    public void ShapesStayInsideTheBox(string variant)
    {
        SparklineShapes.Shapes s = SparklineShapes.Build(variant, Sample, 60, 20);
        Assert.True(s.Fill is not null || s.Line is not null);
        foreach (var g in new[] { s.Fill, s.Line })
        {
            if (g is null) continue;
            var b = g.Bounds;
            Assert.InRange(b.Left, -0.01, 60.01);
            Assert.InRange(b.Right, -0.01, 60.01);
            Assert.InRange(b.Top, -0.01, 20.01);
            Assert.InRange(b.Bottom, -0.01, 20.01);
        }
    }

    [Fact]
    public void VariantsLookDifferent()
    {
        var shapes = StyleOptions.Groups["SparklineStyle"].ToDictionary(v => v, v => SparklineShapes.Build(v, Sample, 60, 20));
        Assert.Null(shapes[StyleOptions.SparkLine].Fill);
        Assert.Null(shapes[StyleOptions.SparkBars].Line);
        Assert.True(shapes[StyleOptions.SparkFade].Fade);
        Assert.False(shapes[StyleOptions.SparkArea].Fade);
        Assert.NotEqual(shapes[StyleOptions.SparkArea].Line!.ToString(), shapes[StyleOptions.SparkSteps].Line!.ToString());
        Assert.NotEqual(shapes[StyleOptions.SparkArea].Line!.ToString(), shapes[StyleOptions.SparkSmooth].Line!.ToString());
    }

    [Fact]
    public void DefaultIsTheOldAreaLook()
    {
        Assert.Equal(StyleOptions.SparkArea, new StyleSettings().SparklineStyle);
        SparklineShapes.Shapes s = SparklineShapes.Build("bilinmeyen", Sample, 60, 20);
        Assert.NotNull(s.Fill);
        Assert.NotNull(s.Line);
        Assert.Equal(0.5, s.FillOpacity);
    }
}
