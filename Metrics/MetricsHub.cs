using System.Diagnostics;
using PulseMonitor.Core;
using PulseMonitor.Interop;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;

namespace PulseMonitor.Metrics;

/// <summary>
/// Tüm ölçümleri arka planda saniyede bir yapar, trafiği geçmişe ekler ve dakikada bir diske yazar.
/// <see cref="Sampled"/> arka plan iş parçacığında çağrılır.
/// </summary>
internal sealed class MetricsHub : IDisposable
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(1);

    private readonly AppSettings _settings;
    private readonly TrafficHistory _history;
    private readonly CpuRamSampler _cpuRam = new();
    private readonly NetworkSampler _network = new();
    private readonly GpuSampler _gpu = new();
    private readonly DiskSampler _disk = new();
    private readonly ProcessCpuSampler _processes = new();
    private static readonly TimeSpan TemperatureInterval = TimeSpan.FromSeconds(5);
    private readonly TemperatureSampler _temps = new();
    private readonly Stopwatch _tempClock = new();
    private Dictionary<string, double> _gpuTemperatures = new(StringComparer.OrdinalIgnoreCase);
    private List<GraphicsAdapter> _graphicsAdapters = [];
    private string? _lastGpuSelection;
    private double? _diskTemp;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private string? _lastError;
    private int _processDetailsHovered;
    private int _diskDetailsHovered;
    private readonly DiskSpaceReader _space = new();
    private int _gpuDetailsHovered;
    private readonly GpuDetailsSampler _gpuDetails = new();

    /// <summary>CPU/RAM ayrıntı kartı görünürken işlem listesi kısa aralıklarla ölçülür.</summary>
    private bool ProcessDetailsHovered
    {
        get => Volatile.Read(ref _processDetailsHovered) != 0;
        set
        {
            if (Interlocked.Exchange(ref _processDetailsHovered, value ? 1 : 0) == (value ? 1 : 0)) return;
            _processes.SetActive(value);
        }
    }

    /// <summary>Disk ayrıntı kartı görünürken disk hızları ve sürücü doluluğu ölçülür; kart kapanınca ölçüm durur.</summary>
    private bool DiskDetailsHovered
    {
        get => Volatile.Read(ref _diskDetailsHovered) != 0;
        set => Volatile.Write(ref _diskDetailsHovered, value ? 1 : 0);
    }

    /// <summary>GPU ayrıntı kartı görünürken motor yükleri, video belleği ve işlem listesi ölçülür.</summary>
    private bool GpuDetailsHovered
    {
        get => Volatile.Read(ref _gpuDetailsHovered) != 0;
        set => Volatile.Write(ref _gpuDetailsHovered, value ? 1 : 0);
    }

    /// <summary>Bir ayrıntı kartı açıldı/kapandı: kartın ihtiyaç duyduğu ek ölçümler (işlem, disk, GPU) açılır/kapanır.</summary>
    public void SetCardHovered(string cardId, bool hovered)
    {
        switch (cardId)
        {
            case TaskbarItemId.Cpu or TaskbarItemId.Ram: ProcessDetailsHovered = hovered; break;
            case TaskbarItemId.Disk: DiskDetailsHovered = hovered; break;
            case TaskbarItemId.Gpu: GpuDetailsHovered = hovered; break;
        }
    }

    public event Action<MetricsSnapshot>? Sampled;
    public event Action<ProcessDetailsSnapshot>? ProcessDetailsUpdated;

    public MetricsHub(AppSettings settings, TrafficHistory history)
    {
        _settings = settings;
        _history = history;
        _processes.Updated += details => ProcessDetailsUpdated?.Invoke(details);
        if (!NetworkAdapters.LayoutIsValid) Log.Error("MIB_IF_ROW2 boyutu beklenenden farklı; ağ ölçümü hatalı olabilir.");
    }

    public void Start() => _loop = Task.Run(() => RunAsync(_cts.Token));

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var sinceSave = Stopwatch.StartNew();
        try
        {
            do
            {
                SampleOnce();
                if (sinceSave.Elapsed >= SaveInterval)
                {
                    TrafficHistoryStore.Save(_history);
                    sinceSave.Restart();
                }
            }
            while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SampleOnce()
    {
        try
        {
            var (cpu, ram) = _cpuRam.Sample();
            NetworkSample net = _network.Sample(_settings.NetworkAdapterId);
            _history.Add(DateOnly.FromDateTime(DateTime.Now), net.DownBytes, net.UpBytes);

            var selection = _settings.GpuSelection;
            SampleTemperatures(selection);
            var usage = SampleGpu();
            double? gpu = usage is null ? null : Math.Clamp(usage.Values.DefaultIfEmpty().Max(), 0, 100);
            double? gpuTemp = _gpuTemperatures.Values.Select(t => (double?)t).Max();
            GpuReading? primary = null, secondary = null;
            if (!string.IsNullOrEmpty(selection) && _graphicsAdapters.Count > 1)
            {
                string[] ids = selection.Split('|', 2);
                primary = ReadSelectedGpu(ids[0], usage);
                if (ids.Length == 2) secondary = ReadSelectedGpu(ids[1], usage);
                gpu = primary.Usage;
                gpuTemp = new[] { primary.Temperature, secondary?.Temperature }.Max();
            }

            bool diskCard = DiskDetailsHovered;
            DiskReading disk = SampleDisk(diskCard);
            Sampled?.Invoke(new MetricsSnapshot(cpu, ram, gpu, disk.Percent, net, gpuTemp, _diskTemp,
                _cpuRam.RamTotal, _cpuRam.RamAvailable, _network.AdapterNames)
            {
                PrimaryGpu = primary,
                SecondaryGpu = secondary,
                DiskReadBps = disk.ReadBps,
                DiskWriteBps = disk.WriteBps,
                Drives = diskCard ? _space.ReadAll() : [],
                GpuDetails = SampleGpuDetails(selection),
            });
            _lastError = null;
        }
        catch (Exception ex)
        {
            // Aynı hata her saniye günlüğü doldurmasın.
            if (ex.Message != _lastError) Log.Error("Ölçüm hatası.", ex);
            _lastError = ex.Message;
        }
    }

    /// <summary>Sıcaklıklar yavaş değişir: 5 saniyede bir ve yalnızca öğe açıkken okunur.</summary>
    private void SampleTemperatures(string? gpuSelection)
    {
        if (gpuSelection == _lastGpuSelection && _tempClock.IsRunning && _tempClock.Elapsed < TemperatureInterval) return;
        _lastGpuSelection = gpuSelection;
        _tempClock.Restart();
        if (!string.IsNullOrEmpty(gpuSelection))
            _graphicsAdapters = GraphicsAdapter.Read();
        _gpuTemperatures = _settings.IsShown(TaskbarItemId.GpuTemp) || _settings.Alerts.GpuTempEnabled
            ? TemperatureSampler.ReadGpu()
            : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        _diskTemp = _settings.IsShown(TaskbarItemId.DiskTemp) || _settings.Alerts.DiskTempEnabled ? _temps.ReadDisk() : null;
    }

    private GpuReading ReadSelectedGpu(string id, Dictionary<string, double>? usage)
    {
        var adapter = _graphicsAdapters.Find(a => a.Id == id);
        if (adapter is null) return new GpuReading(Strings.Get("Settings.GpuMissing"), null, null);

        double? load = usage is null ? null : Math.Clamp(usage.GetValueOrDefault(adapter.Luid), 0, 100);
        double? temperature = _gpuTemperatures.TryGetValue(adapter.Luid, out double value) ? value : null;
        return new GpuReading(adapter.Name, load, temperature);
    }

    private Dictionary<string, double>? SampleGpu()
    {
        if (_settings.IsShown(TaskbarItemId.Gpu)) return _gpu.Sample();
        _gpu.Dispose();
        return null;
    }

    private GpuDetails? SampleGpuDetails(string? selection)
    {
        if (GpuDetailsHovered) return _gpuDetails.Sample(selection);
        _gpuDetails.Dispose();
        return null;
    }

    /// <summary>Etkinlik Disk öğesi açıkken, okuma/yazma hızı yalnızca Disk kartı açıkken ölçülür.</summary>
    private DiskReading SampleDisk(bool card)
    {
        bool activity = _settings.IsShown(TaskbarItemId.Disk);
        if (activity || card) return _disk.Sample(activity, card);
        _disk.Dispose();
        _gpuDetails.Dispose();
        return new DiskReading(null, null, null);
    }

    /// <summary>Döngüyü durdurur ve trafiği son kez diske yazar.</summary>
    public void Dispose()
    {
        _cts.Cancel();
        try { _loop?.Wait(TimeSpan.FromSeconds(2)); }
        catch (AggregateException ex) { Log.Warn("Ölçüm döngüsü düzgün kapanmadı.", ex); }
        TrafficHistoryStore.Save(_history);
        _gpu.Dispose();
        _disk.Dispose();
        _gpuDetails.Dispose();
        _processes.Dispose();
        _cts.Dispose();
    }
}
