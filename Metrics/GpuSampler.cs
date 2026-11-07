
namespace PulseMonitor.Metrics;

/// <summary>
/// GPU yükü: her GPU için tüm süreçlerin 3D motor kullanımı toplanır, adaptör kimliğine göre döndürülür. Yalnızca gösterge açıkken çalışır.
/// </summary>
internal sealed class GpuSampler : IDisposable
{
    private const string CounterPath = @"\GPU Engine(*engtype_3D)\Utilization Percentage";
    private PdhCounter? _counter;
    private bool _unavailable;

    public Dictionary<string, double>? Sample()
    {
        if (_unavailable) return null;
        _counter ??= PdhCounter.TryOpen(CounterPath);
        if (_counter is null)
        {
            _unavailable = true;
            return null;
        }
        if (!_counter.Collect()) return null;

        var perGpu = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in _counter.ReadArray())
        {
            if (GpuInstanceName.Luid(name) is not { } key) continue;
            perGpu[key] = perGpu.GetValueOrDefault(key) + value;
        }
        return perGpu;
    }

    public void Dispose()
    {
        _counter?.Dispose();
        _counter = null;
    }
}
