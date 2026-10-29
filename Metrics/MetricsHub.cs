using System.Diagnostics;
using PulseMonitor.Core;
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
    private static readonly TimeSpan TemperatureInterval = TimeSpan.FromSeconds(5);
    private readonly TemperatureSampler _temps = new();
    private readonly Stopwatch _tempClock = new();
    private double? _gpuTemp, _diskTemp;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private string? _lastError;

    public event Action<MetricsSnapshot>? Sampled;

    public MetricsHub(AppSettings settings, TrafficHistory history)
    {
        _settings = settings;
        _history = history;
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

            SampleTemperatures();
            Sampled?.Invoke(new MetricsSnapshot(cpu, ram, SampleGpu(), SampleDisk(), net, _gpuTemp, _diskTemp,
                _cpuRam.RamTotal, _cpuRam.RamAvailable, _network.AdapterNames));
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
    private void SampleTemperatures()
    {
        if (_tempClock.IsRunning && _tempClock.Elapsed < TemperatureInterval) return;
        _tempClock.Restart();
        _gpuTemp = _settings.IsShown(TaskbarItemId.GpuTemp) || _settings.Alerts.GpuTempEnabled ? TemperatureSampler.ReadGpu() : null;
        _diskTemp = _settings.IsShown(TaskbarItemId.DiskTemp) || _settings.Alerts.DiskTempEnabled ? _temps.ReadDisk() : null;
    }

    private double? SampleGpu()
    {
        if (_settings.IsShown(TaskbarItemId.Gpu)) return _gpu.Sample();
        _gpu.Dispose();
        return null;
    }

    private double? SampleDisk()
    {
        if (_settings.IsShown(TaskbarItemId.Disk)) return _disk.Sample();
        _disk.Dispose();
        return null;
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
        _cts.Dispose();
    }
}
