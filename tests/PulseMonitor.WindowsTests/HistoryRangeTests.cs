using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.WindowsTests;

public sealed class HistoryRangeTests
{
    [Fact]
    public void RangeIncludesBothEndsAndZeroDays()
    {
        var first = new DateOnly(2026, 1, 31);
        var last = new DateOnly(2026, 2, 2);
        var rows = HistoryRows.Range(new Dictionary<DateOnly, DayTraffic> { [last] = new(4, 5) }, first, last);
        Assert.Equal(3, rows.Count);
        Assert.Equal(new DayTraffic(0, 0), rows[0].Traffic);
        Assert.Equal(new DayTraffic(4, 5), rows[2].Traffic);
    }

    [Fact]
    public void RangeExportContainsOnlySelectedDaysAndRejectsInvalidRanges()
    {
        var history = new TrafficHistory();
        history.Add(new DateOnly(2026, 1, 1), 1, 2);
        history.Add(new DateOnly(2026, 1, 2), 3, 4);
        history.Add(new DateOnly(2026, 1, 3), 5, 6);
        var vm = new HistoryViewModel(history)
        {
            RangeStart = new DateTime(2026, 1, 2), RangeEnd = new DateTime(2026, 1, 3)
        };
        Assert.True(vm.ApplyRange());
        Assert.Equal(2, vm.ExportRows().Count);
        Assert.Equal(new DateOnly(2026, 1, 2), vm.ExportRows()[0].Key);
        vm.RangeEnd = new DateTime(2026, 1, 1);
        Assert.False(vm.ApplyRange());
        Assert.Equal(2, vm.ExportRows().Count);
        vm.RangeEnd = new DateTime(2028, 1, 1);
        Assert.False(vm.ApplyRange());
    }
}
