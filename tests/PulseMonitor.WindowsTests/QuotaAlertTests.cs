using PulseMonitor.Alerts;
using PulseMonitor.Metrics;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;

namespace PulseMonitor.WindowsTests;

public sealed class QuotaAlertTests
{
    [Fact]
    public void QuotaThresholdFiresOncePerConfiguredPeriod()
    {
        const ulong gib = 1024UL * 1024 * 1024;
        var history = new TrafficHistory();
        history.Add(DateOnly.FromDateTime(DateTime.Now), 9 * gib, 0);
        var settings = new AlertSettings { TrafficEnabled = true, TrafficLimitGb = 10, TrafficWarningPercent = 80, TrafficCycleStartDay = 1 };
        var monitor = new AlertMonitor();
        int notifications = 0, persisted = 0;
        monitor.Raised += (_, _) => notifications++;
        monitor.QuotaNotificationRecorded += () => persisted++;
        var metrics = new MetricsSnapshot(0, 0, null, null, new NetworkSample(0, 0, 0, 0));

        monitor.Check(metrics, history, settings);
        monitor.Check(metrics, history, settings);

        Assert.Equal(1, notifications);
        Assert.Equal(1, persisted);
        Assert.NotNull(settings.LastTrafficQuotaNotificationPeriod);
    }

    [Fact]
    public void DisabledQuotaThresholdNeverFires()
    {
        const ulong gib = 1024UL * 1024 * 1024;
        var history = new TrafficHistory();
        history.Add(DateOnly.FromDateTime(DateTime.Now), 100 * gib, 0);
        var settings = new AlertSettings { TrafficEnabled = true, TrafficLimitGb = 1, TrafficWarningPercent = 0 };
        var monitor = new AlertMonitor();
        int notifications = 0;
        monitor.Raised += (_, _) => notifications++;

        monitor.Check(new MetricsSnapshot(0, 0, null, null, new NetworkSample(0, 0, 0, 0)), history, settings);

        Assert.Equal(0, notifications);
        Assert.Null(settings.LastTrafficQuotaNotificationPeriod);
    }
}
