namespace PulseMonitor.Metrics;

/// <summary>Bir saniyelik disk ölçümü; ölçülmeyen ya da okunamayan değer null.</summary>
internal sealed record DiskReading(double? Percent, double? ReadBps, double? WriteBps);

/// <summary>
/// Tüm disklerin toplamı: etkinlik (Görev Yöneticisi'ndeki "etkin süre", 100 − boşta geçen süre) ve okuma/yazma hızı.
/// Sürücüsüz, Windows performans sayaçlarından. Yalnızca istenen sayaçlar açık tutulur: etkinlik görev çubuğundaki
/// Disk öğesi açıkken, hızlar yalnızca Disk ayrıntı kartı açıkken ölçülür.
/// </summary>
internal sealed class DiskSampler : IDisposable
{
    private readonly Source _idle = new(@"\PhysicalDisk(_Total)\% Idle Time");
    private readonly Source _read = new(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec");
    private readonly Source _write = new(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec");

    public DiskReading Sample(bool activity, bool speeds) => new(
        _idle.Read(activity, idle => Math.Clamp(100 - idle, 0, 100)),
        _read.Read(speeds, bytes => Math.Max(0, bytes)),
        _write.Read(speeds, bytes => Math.Max(0, bytes)));

    public void Dispose()
    {
        _idle.Dispose();
        _read.Dispose();
        _write.Dispose();
    }

    /// <summary>Tek sayaç: istenmediğinde kapanır, açılamazsa bir daha denenmez.</summary>
    private sealed class Source(string path) : IDisposable
    {
        private PdhCounter? _counter;
        private bool _unavailable;

        public double? Read(bool wanted, Func<double, double> convert)
        {
            if (!wanted)
            {
                Dispose();
                return null;
            }
            if (_unavailable) return null;
            _counter ??= PdhCounter.TryOpen(path);
            if (_counter is null)
            {
                _unavailable = true;
                return null;
            }
            if (!_counter.Collect()) return null;
            return _counter.ReadValue() is { } value ? convert(value) : null;
        }

        public void Dispose()
        {
            _counter?.Dispose();
            _counter = null;
        }
    }
}
