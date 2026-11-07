namespace PulseMonitor.Metrics;

/// <summary>Bir saniyelik ölçümün tamamı. GPU/disk kapalıysa ya da okunamıyorsa null.</summary>
public sealed record MetricsSnapshot(
    double CpuPercent,
    double RamPercent,
    double? GpuPercent,
    double? DiskPercent,
    NetworkSample Network,
    double? GpuTempC = null,
    double? DiskTempC = null,
    ulong RamTotal = 0,
    ulong RamAvailable = 0,
    string Adapters = "--")
{
    public GpuReading? PrimaryGpu { get; init; }
    public GpuReading? SecondaryGpu { get; init; }

    /// <summary>Disk okuma/yazma hızı (bayt/sn); yalnızca Disk kartı açıkken ölçülür, aksi halde null.</summary>
    public double? DiskReadBps { get; init; }
    public double? DiskWriteBps { get; init; }

    /// <summary>Sabit disklerin boş/dolu alanı; yalnızca Disk kartı açıkken doldurulur.</summary>
    public IReadOnlyList<DiskSpace> Drives { get; init; } = [];

    /// <summary>GPU motor yükleri, video belleği ve en çok kullanan işlemler; yalnızca GPU kartı açıkken ölçülür, aksi halde null.</summary>
    public GpuDetails? GpuDetails { get; init; }
}

public sealed record GpuReading(string Name, double? Usage, double? Temperature);
