using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using PulseMonitor.Settings;

namespace PulseMonitor.WindowsTests;

public sealed class CinematicIconTests
{
    private static string Dir()
    {
        string? d = AppContext.BaseDirectory;
        while (d is not null && !Directory.Exists(Path.Combine(d, "Assets", "WeatherAnim", "Cinematic3D"))) d = Path.GetDirectoryName(d);
        return Path.Combine(d ?? throw new DirectoryNotFoundException("Assets"), "Assets", "WeatherAnim", "Cinematic3D");
    }

    private static BitmapSource Open(string name)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(Path.Combine(Dir(), name));
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        return image;
    }

    [Theory]
    [InlineData("ClearDay")]
    [InlineData("ClearNight")]
    [InlineData("Cloudy")]
    [InlineData("Fog")]
    [InlineData("PartlyDay")]
    [InlineData("PartlyNight")]
    [InlineData("Rain")]
    [InlineData("Snow")]
    [InlineData("Thunder")]
    public void StripHasSquareFramesAndMatchingStill(string key)
    {
        BitmapSource strip = Open(key + ".png"), still = Open(key + ".static.png");
        Assert.Equal(20, strip.PixelWidth / strip.PixelHeight);
        Assert.Equal(0, strip.PixelWidth % strip.PixelHeight);
        Assert.Equal(strip.PixelHeight, still.PixelWidth);
    }

    [Theory]
    [InlineData("Rain", 7)]
    [InlineData("Thunder", 4)]
    [InlineData("ClearDay", 10)]
    [InlineData("Fog", 10)]
    [InlineData("Snow", 5)]
    public void FramesActuallyDiffer(string key, int other)
    {
        BitmapSource strip = Open(key + ".png");
        int size = strip.PixelHeight, stride = size * 4;
        byte[] a = new byte[size * stride], b = new byte[size * stride];
        strip.CopyPixels(new Int32Rect(0, 0, size, size), a, stride, 0);
        strip.CopyPixels(new Int32Rect(size * other, 0, size, size), b, stride, 0);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SettingIsAcceptedAndAnimated()
    {
        Assert.Contains(StyleOptions.IconCinematic, StyleOptions.Groups["WeatherIcons"]);
        Assert.Contains(StyleOptions.IconCinematic, StyleOptions.AnimatedIconSets);
        Assert.DoesNotContain(StyleOptions.IconCinematicStatic, StyleOptions.AnimatedIconSets);
    }
}
