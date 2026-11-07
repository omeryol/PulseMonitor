using PulseMonitor.Metrics;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.WindowsTests;

public sealed class DetailCardRowTests
{
    private static DetailContext Context(KeyboardSnapshot keys) => new(
        new MetricsSnapshot(0, 0, null, null, new NetworkSample(0, 0, 0, 0)), keys, new DayTraffic(0, 0),
        "", "", "", new PingQuality(null, null, null, null, 0, 0), null);

    [Fact]
    public void KeyboardCardMarksEachLockKeyOnOrOff()
    {
        IReadOnlyList<DetailRow> rows = DetailCards.Rows(TaskbarItemId.Keyboard, Context(new KeyboardSnapshot(true, false, true)), ProcessDetailsSnapshot.Empty);

        Assert.Equal(3, rows.Count);
        Assert.Equal([true, false, true], rows.Select(r => r.IsOn));
    }

    [Fact]
    public void OtherCardsDoNotUseOnOffColours()
    {
        IReadOnlyList<DetailRow> rows = DetailCards.Rows(TaskbarItemId.Disk, Context(default), ProcessDetailsSnapshot.Empty);

        Assert.All(rows, r => Assert.Null(r.IsOn));
    }
}
