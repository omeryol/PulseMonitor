using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

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

    private static readonly int[] Codes = [0, 1, 2, 3, 45, 48, 51, 53, 55, 56, 57, 61, 63, 65, 66, 67, 71, 73, 75, 77, 80, 81, 82, 85, 86, 95, 96, 97, 99, -1];

    public static IEnumerable<object[]> AllKeys() =>
        Codes.SelectMany(c => new[] { true, false }.Select(d => WeatherCodes.DetailedIconKey(c, d))).Distinct().Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void EveryWeatherCodeHasSquareFramesAndMatchingStill(string key)
    {
        BitmapSource strip = Open(key + ".png"), still = Open(key + ".static.png");
        Assert.Equal(0, strip.PixelWidth % strip.PixelHeight);
        Assert.InRange(strip.PixelWidth / strip.PixelHeight, 20, 80);
        Assert.Equal(strip.PixelHeight, still.PixelWidth);
        Assert.Equal(still.PixelHeight, still.PixelWidth);
    }

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void FramesActuallyDiffer(string key)
    {
        BitmapSource strip = Open(key + ".png");
        int size = strip.PixelHeight, stride = size * 4, count = strip.PixelWidth / size;
        byte[] a = new byte[size * stride], b = new byte[size * stride];
        strip.CopyPixels(new Int32Rect(0, 0, size, size), a, stride, 0);
        bool differs = false;
        for (int i = 1; i < count && !differs; i++)
        {
            strip.CopyPixels(new Int32Rect(size * i, 0, size, size), b, stride, 0);
            differs = !a.AsSpan().SequenceEqual(b);
        }
        Assert.True(differs);
    }

    [Fact]
    public void OtherSetsKeepBasicKeys()
    {
        Assert.Equal("Rain", WeatherCodes.IconKeyFor(StyleOptions.IconMakinThings, 65, true));
        Assert.Equal("RainHeavy", WeatherCodes.IconKeyFor(StyleOptions.IconCinematic, 65, true));
        Assert.Equal("Cloudy", WeatherCodes.IconKeyFor(StyleOptions.IconCinematic, 3, true));
    }

    [Fact]
    public void SettingIsAcceptedAndAnimated()
    {
        Assert.Contains(StyleOptions.IconCinematic, StyleOptions.Groups["WeatherIcons"]);
        Assert.Contains(StyleOptions.IconCinematic, StyleOptions.AnimatedIconSets);
        Assert.DoesNotContain(StyleOptions.IconCinematicStatic, StyleOptions.AnimatedIconSets);
    }
}
