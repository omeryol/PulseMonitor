using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.WindowsTests;

public sealed class QuotaItemTests
{
    private const ulong Gib = 1024UL * 1024 * 1024;

    private static IReadOnlyList<QuotaUsage> Usage(string mode, ulong down, ulong up, int downLimit = 100, int upLimit = 50)
    {
        var history = new TrafficHistory();
        history.Add(DateOnly.FromDateTime(DateTime.Now), down, up);
        var a = new AlertSettings { TrafficQuotaMode = mode, TrafficLimitGb = downLimit, TrafficUploadLimitGb = upLimit };
        return TrafficQuota.Usage(a, history, DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void CombinedPercentCountsBothDirections()
    {
        var u = Assert.Single(Usage(StyleOptions.QuotaTotal, 30 * Gib, 20 * Gib));
        Assert.Equal(50, u.Percent, 6);
    }

    [Fact]
    public void SingleDirectionModesCountOnlyThatDirection()
    {
        Assert.Equal(30, Assert.Single(Usage(StyleOptions.QuotaDown, 30 * Gib, 99 * Gib)).Percent, 6);
        Assert.Equal(40, Assert.Single(Usage(StyleOptions.QuotaUp, 99 * Gib, 40 * Gib, 100, 100)).Percent, 6);
    }

    [Fact]
    public void SplitModeGivesTwoPercentagesWithOwnLimits()
    {
        var u = Usage(StyleOptions.QuotaSplit, 25 * Gib, 25 * Gib);
        Assert.Equal(2, u.Count);
        Assert.Equal(25, u[0].Percent, 6);
        Assert.Equal(50, u[1].Percent, 6);
    }

    [Fact]
    public void ItemIsHiddenByDefaultAndKnown()
    {
        Assert.Contains(TaskbarItemId.Quota, TaskbarItemId.DefaultOrder);
        Assert.False(TaskbarItemId.DefaultVisible(TaskbarItemId.Quota));
    }

    [Fact]
    public void LineShowsOneValuePerQuotaPart()
    {
        var vm = new TaskbarViewModel();
        vm.UpdateQuota(Usage(StyleOptions.QuotaSplit, 25 * Gib, 25 * Gib), 80);
        Assert.Contains(" / ", vm.Quota.Value);
        Assert.Contains(" / ", vm.Quota.Placeholder);
        vm.UpdateQuota(Usage(StyleOptions.QuotaTotal, 10 * Gib, 0), 80);
        Assert.DoesNotContain(" / ", vm.Quota.Value);
    }
}
