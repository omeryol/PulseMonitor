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
}
