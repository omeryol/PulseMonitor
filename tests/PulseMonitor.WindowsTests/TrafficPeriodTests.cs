using PulseMonitor.Traffic;

namespace PulseMonitor.WindowsTests;

public sealed class TrafficPeriodTests
{
    [Fact]
    public void PeriodUsesConfiguredDayAndCrossesMonthBoundary()
    {
        var before = TrafficPeriod.For(new DateOnly(2026, 3, 4), 5);
        var on = TrafficPeriod.For(new DateOnly(2026, 3, 5), 5);
        Assert.Equal(new DateOnly(2026, 2, 5), before.Start);
        Assert.Equal(new DateOnly(2026, 3, 5), before.EndExclusive);
        Assert.Equal(new DateOnly(2026, 3, 5), on.Start);
        Assert.Equal(new DateOnly(2026, 4, 5), on.EndExclusive);
    }

    [Fact]
    public void TotalIncludesStartButExcludesEnd()
    {
        var history = new TrafficHistory();
        history.Add(new DateOnly(2026, 2, 5), 10, 20);
        history.Add(new DateOnly(2026, 3, 4), 30, 40);
        history.Add(new DateOnly(2026, 3, 5), 50, 60);
        var total = TrafficPeriod.Total(history, TrafficPeriod.For(new DateOnly(2026, 3, 4), 5));
        Assert.Equal(new DayTraffic(40, 60), total);
    }

    [Fact]
    public void ComparisonUsesAnImmediatelyPrecedingSameLengthPeriod()
    {
        var history = new TrafficHistory();
        history.Add(new DateOnly(2026, 1, 1), 50, 50);
        history.Add(new DateOnly(2026, 1, 2), 25, 25);
        history.Add(new DateOnly(2026, 1, 3), 100, 50);
        history.Add(new DateOnly(2026, 1, 4), 100, 50);
        var comparison = TrafficComparison.For(history, new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 4));
        Assert.Equal((ulong)300, comparison.Current);
        Assert.Equal((ulong)150, comparison.Previous);
        Assert.Equal(100, comparison.PercentChange);
    }
}
