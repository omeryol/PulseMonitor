namespace PulseMonitor.Metrics;

/// <summary>Bir saniyelik ölçümün tamamı. GPU/disk kapalıysa ya da okunamıyorsa null.</summary>
public sealed record MetricsSnapshot(
    double CpuPercent,
    double RamPercent,
    double? GpuPercent,
    double? DiskPercent,
    NetworkSample Network,
    double? GpuTempC = null,
    double? DiskTempC = null);
