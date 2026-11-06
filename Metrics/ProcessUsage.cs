namespace PulseMonitor.Metrics;

/// <summary>Bir işlemin anlık örneği (PID başına).</summary>
internal sealed record ProcessSample(int Id, string Name, TimeSpan CpuTime, ulong WorkingSet);

/// <summary>Aynı adlı işlemlerin (ör. tüm chrome işlemleri) son aralıktaki toplam işlemci kullanımı.</summary>
public sealed record ProcessCpuUsage(string Name, double Percent, int Count = 1);

/// <summary>Aynı adlı işlemlerin bellekte tuttuğu toplam fiziksel RAM (çalışma kümesi).</summary>
public sealed record ProcessMemoryUsage(string Name, ulong WorkingSet, int Count = 1);

/// <summary>CPU/RAM ayrıntı kartı için geçici işlem listeleri.</summary>
public sealed record ProcessDetailsSnapshot(
    IReadOnlyList<ProcessCpuUsage> Cpu,
    IReadOnlyList<ProcessMemoryUsage> Memory,
    bool CpuLoading)
{
    public static ProcessDetailsSnapshot Empty { get; } = new([], [], false);
}

/// <summary>İşlem örneklerinden ada göre toplanmış ilk beş listeyi hesaplar (saf mantık, test edilir).</summary>
internal static class ProcessUsage
{
    public const int Top = 5;

    public static double CalculatePercent(TimeSpan previous, TimeSpan current, TimeSpan elapsed, int logicalProcessors)
    {
        if (elapsed <= TimeSpan.Zero || current < previous || logicalProcessors <= 0) return 0;
        double percent = 100 * (current - previous).TotalSeconds / (elapsed.TotalSeconds * logicalProcessors);
        return Math.Clamp(percent, 0, 100);
    }

    /// <summary>Önceki örnekte de bulunan işlemlerin CPU yüzdesi; aynı adlılar toplanır.</summary>
    public static IReadOnlyList<ProcessCpuUsage> TopCpu(IReadOnlyDictionary<int, ProcessSample> previous,
        IEnumerable<ProcessSample> current, TimeSpan elapsed, int logicalProcessors) =>
        current.Where(sample => previous.TryGetValue(sample.Id, out var old) && old.Name == sample.Name)
            .Select(sample => (sample.Name, Percent: CalculatePercent(previous[sample.Id].CpuTime, sample.CpuTime, elapsed, logicalProcessors)))
            .GroupBy(reading => reading.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProcessCpuUsage(group.First().Name, Math.Min(100, group.Sum(reading => reading.Percent)), group.Count()))
            .OrderByDescending(reading => reading.Percent).ThenBy(reading => reading.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Top).ToArray();

    /// <summary>Çalışma kümesi en büyük işlemler; aynı adlılar toplanır.</summary>
    public static IReadOnlyList<ProcessMemoryUsage> TopMemory(IEnumerable<ProcessSample> samples) =>
        samples.GroupBy(sample => sample.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProcessMemoryUsage(group.First().Name,
                group.Aggregate(0UL, (sum, sample) => sum + sample.WorkingSet), group.Count()))
            .OrderByDescending(usage => usage.WorkingSet).ThenBy(usage => usage.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Top).ToArray();
}
