using System.Globalization;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Metrics;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Her öğenin ayrıntı kartı satırları (metinler: lang/*.json → Detail.*).</summary>
internal static class DetailCards
{
    private static CultureInfo Tr => UiLanguage.Culture;

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

    public static IReadOnlyList<DetailRow> Rows(string id, DetailContext c, ProcessDetailsSnapshot processes)
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
            TaskbarItemId.Cpu => CpuRows(c, processes),
            TaskbarItemId.Ram => RamRows(c, processes),
            TaskbarItemId.Gpu when m.PrimaryGpu is { } gpu => GpuRows(gpu, m.SecondaryGpu, false),
            TaskbarItemId.GpuTemp when m.PrimaryGpu is { } gpu => GpuRows(gpu, m.SecondaryGpu, true),
            TaskbarItemId.Gpu => [R("Usage", ValueFormat.Percent(m.GpuPercent)), R("Temperature", ValueFormat.Celsius(m.GpuTempC))],
            TaskbarItemId.Disk => [R("Activity", ValueFormat.Percent(m.DiskPercent)), R("Temperature", ValueFormat.Celsius(m.DiskTempC))],
            TaskbarItemId.GpuTemp => [R("Temperature", ValueFormat.Celsius(m.GpuTempC)), R("Thresholds", "70° / 85°")],
            TaskbarItemId.DiskTemp => [R("Temperature", ValueFormat.Celsius(m.DiskTempC)), R("Thresholds", "55° / 70°"), R("DiskNote", "NVMe")],
            TaskbarItemId.Ping =>
            [
                R("Server", c.PingHost), R("Last", ValueFormat.Milliseconds(c.PingLast)),
                R("MinAvgMax", $"{ValueFormat.Milliseconds(c.Ping.Min)} / {ValueFormat.Milliseconds(c.Ping.Average)} / {ValueFormat.Milliseconds(c.Ping.Max)}"),
                R("Jitter", ValueFormat.Milliseconds(c.Ping.Jitter)),
                R("Loss", ValueFormat.Percent(c.Ping.LossPercent)),
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

    private static IReadOnlyList<DetailRow> GpuRows(Metrics.GpuReading primary, Metrics.GpuReading? secondary, bool temperatureOnly)
    {
        string Format(Metrics.GpuReading gpu) => temperatureOnly
            ? ValueFormat.Celsius(gpu.Temperature)
            : $"{ValueFormat.Percent(gpu.Usage)} / {ValueFormat.Celsius(gpu.Temperature)}";

        return secondary is null
            ? [new(primary.Name, Format(primary))]
            : [new(primary.Name, Format(primary)), new(secondary.Name, Format(secondary))];
    }

    private static IReadOnlyList<DetailRow> CpuRows(DetailContext context, ProcessDetailsSnapshot processes)
    {
        var rows = new List<DetailRow>
        {
            new(Strings.Get("Detail.Usage"), ValueFormat.Percent(context.Metrics.CpuPercent)),
            new(Strings.Get("Detail.Cores"), Environment.ProcessorCount.ToString(Tr)),
            new(Strings.Get("Detail.Uptime"), Uptime()),
        };
        rows.AddRange(processes.CpuLoading
            ? Enumerable.Range(0, 5).Select(_ => new DetailRow("", "", IsPlaceholder: true))
            : processes.Cpu.Select(process =>
                new DetailRow(ProcessLabel(process.Name, process.Count), ValueFormat.Percent(process.Percent), IsProcess: true)));
        return rows;
    }

    private static IReadOnlyList<DetailRow> RamRows(DetailContext context, ProcessDetailsSnapshot processes)
    {
        var m = context.Metrics;
        var rows = new List<DetailRow>
        {
            new(Strings.Get("Detail.Usage"), ValueFormat.Percent(m.RamPercent)),
            new(Strings.Get("Detail.Used"), ValueFormat.Size(m.RamTotal - m.RamAvailable)),
            new(Strings.Get("Detail.Free"), ValueFormat.Size(m.RamAvailable)),
            new(Strings.Get("Detail.Total"), ValueFormat.Size(m.RamTotal)),
        };
        rows.AddRange(processes.Memory.Select(process =>
            new DetailRow(ProcessLabel(process.Name, process.Count), ValueFormat.Size(process.WorkingSet), IsProcess: true)));
        return rows;
    }

    // Aynı adlı işlemler tek satırda toplanır; birden fazlaysa adın yanında kaç tane olduğu görünür (chrome ×12).
    private static string ProcessLabel(string name, int count) =>
        count > 1 ? string.Format(Tr, "{0} ×{1}", name, count) : name;

    private static string Uptime()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return t.Days > 0
            ? string.Format(Tr, Strings.Get("Detail.UptimeDays"), t.Days, t.Hours, t.Minutes)
            : string.Format(Tr, Strings.Get("Detail.UptimeHours"), t.Hours, t.Minutes);
    }
}
