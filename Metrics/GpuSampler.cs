namespace PulseMonitor.Metrics;

/// <summary>
/// GPU yükü: her GPU için tüm süreçlerin 3D motor kullanımı toplanır, en yüklü GPU gösterilir
/// (Görev Yöneticisi ile aynı yaklaşım). Yalnızca gösterge açıkken çalışır.
/// </summary>
internal sealed class GpuSampler : IDisposable
{
    private const string CounterPath = @"\GPU Engine(*engtype_3D)\Utilization Percentage";
    private PdhCounter? _counter;
    private bool _unavailable;

    public double? Sample()
    {
        if (_unavailable) return null;
        _counter ??= PdhCounter.TryOpen(CounterPath);
        if (_counter is null)
        {
            _unavailable = true;
            return null;
        }
        if (!_counter.Collect()) return null;

        var perGpu = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (name, value) in _counter.ReadArray())
        {
            string key = GpuKey(name);
            perGpu[key] = perGpu.GetValueOrDefault(key) + value;
        }
        return perGpu.Count == 0 ? 0 : Math.Clamp(perGpu.Values.Max(), 0, 100);
    }

    /// <summary>"pid_12_luid_0x0_0xD1A3_phys_0_eng_0_engtype_3D" → "luid_0x0_0xD1A3"</summary>
    private static string GpuKey(string instance)
    {
        int start = instance.IndexOf("luid_", StringComparison.Ordinal);
        int end = start < 0 ? -1 : instance.IndexOf("_phys", start, StringComparison.Ordinal);
        return end > start ? instance[start..end] : string.Empty;
    }

    public void Dispose()
    {
        _counter?.Dispose();
        _counter = null;
    }
}
