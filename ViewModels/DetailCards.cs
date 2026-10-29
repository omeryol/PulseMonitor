using System.Globalization;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Her öğenin ayrıntı kartı satırları (metinler: Resources/Strings.Details.tr.xaml).</summary>
internal static class DetailCards
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Bugünün en yüksek hızları (gece yarısı sıfırlanır).</summary>
    private static DateOnly _peakDay;
    private static double _peakDown, _peakUp;

    public static void TrackPeaks(double downBps, double upBps)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today != _peakDay) (_peakDay, _peakDown, _peakUp) = (today, 0, 0);
        _peakDown = Math.Max(_peakDown, downBps);
        _peakUp = Math.Max(_peakUp, upBps);
    }

    public static IReadOnlyList<DetailRow> Rows(string id, DetailContext c)
    {
        var m = c.Metrics;
        DetailRow R(string key, string value) => new(Strings.Get($"Detail.{key}"), value);
        string OnOff(bool on) => Strings.Get(on ? "Detail.On" : "Detail.Off");

        return id switch
        {
            TaskbarItemId.Upload =>
            [
                R("Now", ValueFormat.Speed(m.Network.UpBps)), R("PeakToday", ValueFormat.Speed(_peakUp)),
                R("TotalToday", ValueFormat.Size(c.Today.Up)), R("Adapter", m.Adapters),
            ],
            TaskbarItemId.Download =>
            [
                R("Now", ValueFormat.Speed(m.Network.DownBps)), R("PeakToday", ValueFormat.Speed(_peakDown)),
                R("TotalToday", ValueFormat.Size(c.Today.Down)), R("Adapter", m.Adapters),
            ],
            TaskbarItemId.Cpu =>
            [
                R("Usage", ValueFormat.Percent(m.CpuPercent)),
                R("Cores", Environment.ProcessorCount.ToString(Tr)),
                R("Uptime", Uptime()),
            ],
            TaskbarItemId.Ram =>
            [
                R("Usage", ValueFormat.Percent(m.RamPercent)),
                R("Used", ValueFormat.Size(m.RamTotal - m.RamAvailable)),
                R("Free", ValueFormat.Size(m.RamAvailable)),
                R("Total", ValueFormat.Size(m.RamTotal)),
            ],
            TaskbarItemId.Gpu => [R("Usage", ValueFormat.Percent(m.GpuPercent)), R("Temperature", ValueFormat.Celsius(m.GpuTempC))],
            TaskbarItemId.Disk => [R("Activity", ValueFormat.Percent(m.DiskPercent)), R("Temperature", ValueFormat.Celsius(m.DiskTempC))],
            TaskbarItemId.GpuTemp => [R("Temperature", ValueFormat.Celsius(m.GpuTempC)), R("Thresholds", "70° / 85°")],
            TaskbarItemId.DiskTemp => [R("Temperature", ValueFormat.Celsius(m.DiskTempC)), R("Thresholds", "55° / 70°"), R("DiskNote", "NVMe")],
            TaskbarItemId.Ping =>
            [
                R("Server", c.PingHost), R("Last", ValueFormat.Milliseconds(c.PingLast)),
                R("MinAvgMax", $"{ValueFormat.Milliseconds(c.Ping.Min)} / {ValueFormat.Milliseconds(c.Ping.Avg)} / {ValueFormat.Milliseconds(c.Ping.Max)}"),
                R("Loss", string.Format(Tr, "% {0:0}", c.Ping.LossPercent)),
            ],
            TaskbarItemId.LocalIp or TaskbarItemId.PublicIp =>
            [
                R("LocalIp", c.LocalIp), R("PublicIp", c.PublicIp), R("Adapter", m.Adapters),
            ],
            TaskbarItemId.Keyboard =>
            [
                R("Caps", OnOff(c.Keys.Caps)), R("Num", OnOff(c.Keys.Num)), R("Scroll", OnOff(c.Keys.Scroll)),
            ],
            _ => Array.Empty<DetailRow>(),
        };
    }

    private static string Uptime()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return t.Days > 0
            ? string.Format(Tr, Strings.Get("Detail.UptimeDays"), t.Days, t.Hours, t.Minutes)
            : string.Format(Tr, Strings.Get("Detail.UptimeHours"), t.Hours, t.Minutes);
    }
}
