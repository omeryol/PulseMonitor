using System.IO;
using PulseMonitor.Metrics;

namespace PulseMonitor.WindowsTests;

public sealed class PingOutagesTests
{
    [Fact]
    public void FailurePeriodIsRecordedOnceAndClosedOnRecovery()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pm-ping-{Guid.NewGuid():N}.json");
        try
        {
            var start = DateTimeOffset.UtcNow;
            var log = new PingOutages(path);
            log.Observe("example.invalid", true, start);
            log.Observe("example.invalid", false, start.AddSeconds(5));
            log.Observe("example.invalid", false, start.AddSeconds(10));
            log.Observe("example.invalid", true, start.AddSeconds(15));
            var item = Assert.Single(new PingOutages(path).Snapshot());
            Assert.Equal(start.AddSeconds(5), item.Started);
            Assert.Equal(start.AddSeconds(15), item.Recovered);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void StoppingOrChangingTargetDoesNotInventRecovery()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pm-ping-{Guid.NewGuid():N}.json");
        try
        {
            var now = DateTimeOffset.UtcNow;
            var log = new PingOutages(path);
            log.Observe("first", false, now);
            log.StopTracking();
            log.Observe("second", true, now.AddSeconds(10));
            Assert.Null(Assert.Single(log.Snapshot()).Recovered);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RangeIncludesOverlappingRecoveredPeriodsButNotOldUnknownPeriods()
    {
        var now = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new PingOutages.Outage("a", now.AddDays(-10), now.AddDays(-1)),
            new PingOutages.Outage("b", now.AddDays(-10), null),
            new PingOutages.Outage("c", now.AddHours(-1), null)
        };
        var result = PingOutages.InRange(events, now.AddDays(-7));
        Assert.Equal(new[] { "a", "c" }, result.Select(x => x.Host));
    }
}
