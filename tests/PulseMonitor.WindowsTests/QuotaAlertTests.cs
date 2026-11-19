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

    private const ulong Gib = 1024UL * 1024 * 1024;

    private static (int notifications, List<string> texts, AlertSettings settings) Run(string mode, ulong down, ulong up, int downLimit, int upLimit)
    {
        var history = new TrafficHistory();
        history.Add(DateOnly.FromDateTime(DateTime.Now), down, up);
        var settings = new AlertSettings { TrafficEnabled = true, TrafficQuotaMode = mode, TrafficLimitGb = downLimit, TrafficUploadLimitGb = upLimit, TrafficWarningPercent = 80 };
        var monitor = new AlertMonitor();
        var texts = new List<string>();
        monitor.Raised += (_, text) => texts.Add(text);
        var metrics = new MetricsSnapshot(0, 0, null, null, new NetworkSample(0, 0, 0, 0));
        monitor.Check(metrics, history, settings);
        monitor.Check(metrics, history, settings);
        return (texts.Count, texts, settings);
    }

    [Fact]
    public void CombinedCountsDownloadPlusUpload()
    {
        Assert.Equal(1, Run(StyleOptions.QuotaTotal, 5 * Gib, 4 * Gib, 10, 10).notifications);
        Assert.Equal(0, Run(StyleOptions.QuotaTotal, 5 * Gib, 2 * Gib, 10, 10).notifications);
    }

    [Fact]
    public void DownloadOnlyIgnoresUpload()
    {
        Assert.Equal(0, Run(StyleOptions.QuotaDown, 2 * Gib, 50 * Gib, 10, 10).notifications);
        Assert.Equal(1, Run(StyleOptions.QuotaDown, 9 * Gib, 0, 10, 10).notifications);
    }

    [Fact]
    public void UploadOnlyIgnoresDownload()
    {
        Assert.Equal(0, Run(StyleOptions.QuotaUp, 50 * Gib, 2 * Gib, 10, 10).notifications);
        Assert.Equal(1, Run(StyleOptions.QuotaUp, 0, 9 * Gib, 10, 10).notifications);
    }

    [Fact]
    public void SplitModeUsesSeparateLimitsAndNotifiesEachOnce()
    {
        // indirme 9/10 (aşıldı), yükleme 1/20 (aşılmadı)
        var (n1, _, s1) = Run(StyleOptions.QuotaSplit, 9 * Gib, 1 * Gib, 10, 20);
        Assert.Equal(1, n1);
        Assert.NotNull(s1.LastTrafficQuotaNotificationPeriod);
        Assert.Null(s1.LastTrafficQuotaUploadNotificationPeriod);
        // ikisi de aşıldı: iki ayrı bildirim, tekrar yok
        var (n2, _, s2) = Run(StyleOptions.QuotaSplit, 9 * Gib, 17 * Gib, 10, 20);
        Assert.Equal(2, n2);
        Assert.NotNull(s2.LastTrafficQuotaUploadNotificationPeriod);
    }

    [Fact]
    public void UnknownModeFallsBackToCombined()
    {
        var a = new AlertSettings { TrafficQuotaMode = "yok" };
        a.Normalize();
        Assert.Equal(StyleOptions.QuotaTotal, a.TrafficQuotaMode);
    }
}
