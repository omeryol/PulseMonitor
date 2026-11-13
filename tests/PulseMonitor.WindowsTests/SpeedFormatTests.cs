using System.Globalization;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;

namespace PulseMonitor.WindowsTests;

public sealed class SpeedFormatTests
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static string F(string unit, string divisor, double bps) =>
        SpeedFormat.From(unit, divisor).Format(bps, Tr);

    [Fact]
    public void FortyEightMegabitsIsSixMillionBytesPerSecond()
    {
        Assert.Equal("48,0 Mb/s", F(StyleOptions.SpeedBitsSlash, StyleOptions.Base1000, 6_000_000));
        Assert.Equal("48,0 Mbps", F(StyleOptions.SpeedBitsPs, StyleOptions.Base1000, 6_000_000));
        Assert.Equal("48,0 Mbit/s", F(StyleOptions.SpeedBitsMbit, StyleOptions.Base1000, 6_000_000));
        Assert.Equal("5,7 MB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1024, 6_000_000));
        Assert.Equal("6,0 MB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1000, 6_000_000));
    }

    [Fact]
    public void ZeroAndNegativeShowZero()
    {
        Assert.Equal("0,0 KB/s", F(StyleOptions.SpeedBytes, StyleOptions.BaseAuto, 0));
        Assert.Equal("0,0 Kbps", F(StyleOptions.SpeedBitsPs, StyleOptions.BaseAuto, -50));
    }

    [Fact]
    public void RoundingToThousandMovesUpToMega()
    {
        Assert.Equal("1,0 MB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1000, 999_950)); // 999,95 KB/s -> 1000,0 olur
        Assert.Equal("999,9 KB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1024, 1_023_900)); // 1024'te hâlâ KB
        Assert.Equal("1,0 MB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1024, 1024 * 999.96));
    }

    [Fact]
    public void RoundingMovesUpToGigaForBothDividers()
    {
        Assert.Equal("1,0 GB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1000, 999_950_000));
        Assert.Equal("1,0 GB/s", F(StyleOptions.SpeedBytes, StyleOptions.Base1024, 1024.0 * 1024 * 999.96));
        Assert.Equal("1,0 Gbit/s", F(StyleOptions.SpeedBitsMbit, StyleOptions.Base1000, 124_993_750)); // 999,95 Mbit/s
        Assert.Equal("1,0 Gb/s", F(StyleOptions.SpeedBitsSlash, StyleOptions.Base1024, 1024.0 * 1024 * 999.96 / 8));
    }

    [Fact]
    public void AutoDividerFollowsUnit()
    {
        Assert.Equal(1024, SpeedFormat.From(StyleOptions.SpeedBytes, StyleOptions.BaseAuto).Divisor);
        Assert.Equal(1000, SpeedFormat.From(StyleOptions.SpeedBitsPs, StyleOptions.BaseAuto).Divisor);
        Assert.Equal(1000, SpeedFormat.From(StyleOptions.SpeedBytes, StyleOptions.Base1000).Divisor);
        Assert.Equal(1024, SpeedFormat.From(StyleOptions.SpeedBitsMbit, StyleOptions.Base1024).Divisor);
    }

    [Fact]
    public void InvalidValuesFallBackToDefaults()
    {
        Assert.Equal(SpeedFormat.Default, SpeedFormat.From(null, null));
        Assert.Equal(SpeedFormat.Default, SpeedFormat.From("???", "7"));

        var settings = new AppSettings { Style = new StyleSettings { SpeedUnit = "Nope", SpeedBase = "999" } };
        StyleOptions.Normalize(settings);
        Assert.Equal(StyleOptions.SpeedBytes, settings.Style.SpeedUnit);
        Assert.Equal(StyleOptions.BaseAuto, settings.Style.SpeedBase);

        // Eski settings.json'da alan yok: varsayılan eski davranış.
        var old = System.Text.Json.JsonSerializer.Deserialize<StyleSettings>("{}")!;
        Assert.Equal(SpeedFormat.Default, SpeedFormat.From(old.SpeedUnit, old.SpeedBase));
    }

    [Fact]
    public void PlaceholderMatchesWidestText()
    {
        Assert.Equal("888,8 MB/s", SpeedFormat.Default.Placeholder);
        Assert.Equal("888,8 Mbit/s", SpeedFormat.From(StyleOptions.SpeedBitsMbit, StyleOptions.BaseAuto).Placeholder);
        Assert.Equal("888,8 Mbps", SpeedFormat.From(StyleOptions.SpeedBitsPs, StyleOptions.Base1024).Placeholder);
    }
}
