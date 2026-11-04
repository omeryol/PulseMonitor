using System.Globalization;
using PulseMonitor.Interop;

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
            string key = GpuKey(name);
            if (key.Length == 0) continue;
            perGpu[key] = perGpu.GetValueOrDefault(key) + value;
        }
        return perGpu;
    }

    /// <summary>"pid_12_luid_0x0_0xD1A3_phys_0_eng_0_engtype_3D" → "luid_0x0_0xD1A3"</summary>
    private static string GpuKey(string instance)
    {
        int start = instance.IndexOf("luid_", StringComparison.Ordinal);
        int end = start < 0 ? -1 : instance.IndexOf("_phys", start, StringComparison.Ordinal);
        if (end <= start) return string.Empty;
        const string separator = "_0x";
        int lowStart = instance.IndexOf(separator, start + "luid_0x".Length, StringComparison.OrdinalIgnoreCase);
        int highStart = start + "luid_0x".Length;
        if (lowStart < highStart || lowStart + separator.Length >= end) return string.Empty;
        return uint.TryParse(instance.AsSpan(highStart, lowStart - highStart), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint high)
            && uint.TryParse(instance.AsSpan(lowStart + separator.Length, end - lowStart - separator.Length),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint low)
            ? GraphicsAdapter.LuidKey(unchecked((int)high), low)
            : string.Empty;
    }

    public void Dispose()
    {
        _counter?.Dispose();
        _counter = null;
    }
}
