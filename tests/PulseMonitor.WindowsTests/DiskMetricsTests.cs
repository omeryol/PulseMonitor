using PulseMonitor.Formatting;
using PulseMonitor.Metrics;
using PulseMonitor.ViewModels;

namespace PulseMonitor.WindowsTests;

public sealed class DiskMetricsTests
{
    [Fact]
    public void DriveSizeUsesUnitsThatKeepTheCardNarrow()
    {
        const double gb = 1024d * 1024 * 1024;
        Assert.EndsWith(" MB", ValueFormat.Capacity(512 * 1024 * 1024));
        Assert.EndsWith(" GB", ValueFormat.Capacity(50 * gb));
        Assert.StartsWith("465", ValueFormat.Capacity(465 * gb));
        Assert.EndsWith(" TB", ValueFormat.Capacity(2048 * gb));
        Assert.EndsWith(" MB", ValueFormat.Capacity(-5)); // negatif değer 0 sayılır
    }

    [Fact]
    public void UsedPercentIsClampedAndSafeForEmptyDrives()
    {
        Assert.Equal(75, new DiskSpace("C:", 25, 100).UsedPercent);
        Assert.Equal(0, new DiskSpace("C:", 0, 0).UsedPercent);
        Assert.Equal(100, new DiskSpace("C:", -5, 100).UsedPercent);
    }

    [Fact]
    public void RowBarIsOnlyShownWhenGiven()
    {
        Assert.False(new DetailRow("a", "b").HasBar);
        var withBar = new DetailRow("C:", "1 / 2", Bar: 40);
        Assert.True(withBar.HasBar);
        Assert.Equal(40, withBar.BarValue);
    }

    [Fact]
    public void FixedDrivesAreListedWithSensibleSizes()
    {
        IReadOnlyList<DiskSpace> drives = new DiskSpaceReader().ReadAll();
        Assert.NotEmpty(drives);
        Assert.All(drives, d =>
        {
            Assert.EndsWith(":", d.Drive);
            Assert.True(d.Total > 0 && d.Free >= 0 && d.Free <= d.Total);
        });
    }

    [Fact]
    public async Task SamplerMeasuresSpeedsOnlyWhileTheCardIsOpen()
    {
        using var sampler = new DiskSampler();
        sampler.Sample(true, true); // ilk ölçüm taban
        await Task.Delay(1200);

        DiskReading open = sampler.Sample(true, true);
        Assert.InRange(open.Percent ?? -1, 0, 100);
        Assert.True(open.ReadBps >= 0 && open.WriteBps >= 0);

        DiskReading closed = sampler.Sample(true, false);
        Assert.InRange(closed.Percent ?? -1, 0, 100);
        Assert.Null(closed.ReadBps);
        Assert.Null(closed.WriteBps);

        DiskReading off = sampler.Sample(false, false);
        Assert.Null(off.Percent);
    }
}
