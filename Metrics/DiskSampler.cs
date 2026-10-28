namespace PulseMonitor.Metrics;

/// <summary>Disk etkinliği (Görev Yöneticisi'ndeki "etkin süre"): 100 − boşta geçen süre yüzdesi.</summary>
internal sealed class DiskSampler : IDisposable
{
    private const string CounterPath = @"\PhysicalDisk(_Total)\% Idle Time";
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
        return _counter.ReadValue() is { } idle ? Math.Clamp(100 - idle, 0, 100) : null;
    }

    public void Dispose()
    {
        _counter?.Dispose();
        _counter = null;
    }
}
