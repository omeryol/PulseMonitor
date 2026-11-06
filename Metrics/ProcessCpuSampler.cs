using System.ComponentModel;
using System.Diagnostics;
using PulseMonitor.Core;

namespace PulseMonitor.Metrics;

/// <summary>
/// CPU/RAM ayrıntı kartı açıkken işlemleri saniyede bir ölçer. Kart kapanınca ölçüm durur ve
/// kapanıştan sonra biten bir ölçüm listeye yazılamaz (her açılış ayrı nesil numarası alır).
/// </summary>
internal sealed class ProcessCpuSampler : IDisposable
{
    // İlk CPU değeri kısa bir ısınmadan sonra gelir; sonra Görev Yöneticisi gibi saniyede bir.
    private static readonly TimeSpan FirstInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private readonly object _gate = new();
    private CancellationTokenSource? _cancellation;
    private int _generation;

    public event Action<ProcessDetailsSnapshot>? Updated;

    public void SetActive(bool active)
    {
        CancellationTokenSource? cancelled;
        lock (_gate)
        {
            if (active)
            {
                if (_cancellation is not null) return;
                var started = new CancellationTokenSource();
                _cancellation = started;
                int generation = ++_generation;
                _ = Task.Run(() => RunAsync(generation, started.Token));
                return;
            }

            cancelled = _cancellation;
            _cancellation = null;
            _generation++;
            // Kilit içinde: boş liste, kapanıştan önce başlamış bir ölçümün ardından gelir ve son sözü söyler.
            Updated?.Invoke(ProcessDetailsSnapshot.Empty);
        }

        cancelled?.Cancel();
        cancelled?.Dispose();
    }

    private async Task RunAsync(int generation, CancellationToken token)
    {
        try
        {
            var previous = Capture();
            var clock = Stopwatch.StartNew();
            Publish(generation, new ProcessDetailsSnapshot([], ProcessUsage.TopMemory(previous.Values), CpuLoading: true));

            TimeSpan delay = FirstInterval;
            while (true)
            {
                await Task.Delay(delay, token);
                delay = Interval;
                var current = Capture();
                TimeSpan elapsed = clock.Elapsed;
                clock.Restart();
                Publish(generation, new ProcessDetailsSnapshot(
                    ProcessUsage.TopCpu(previous, current.Values, elapsed, Environment.ProcessorCount),
                    ProcessUsage.TopMemory(current.Values), CpuLoading: false));
                previous = current;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Warn($"İşlem listesi ölçülemedi: {ex.Message}");
        }
    }

    private void Publish(int generation, ProcessDetailsSnapshot snapshot)
    {
        lock (_gate)
        {
            if (generation != _generation || _cancellation is null) return;
            Updated?.Invoke(snapshot);
        }
    }

    private static Dictionary<int, ProcessSample> Capture()
    {
        Process[] processes;
        try { processes = Process.GetProcesses(); }
        catch (Win32Exception ex) { Log.Warn($"İşlemler listelenemedi: {ex.Message}"); return []; }

        var result = new Dictionary<int, ProcessSample>();
        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    result[process.Id] = new ProcessSample(process.Id, process.ProcessName, process.TotalProcessorTime,
                        (ulong)Math.Max(0, process.WorkingSet64));
                }
                // İşlem bu anda kapanmış ya da erişim izni vermiyor olabilir; kart bunları atlar (olağan, günlüğe yazılmaz).
                catch (InvalidOperationException) { }
                catch (Win32Exception) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        return result;
    }

    public void Dispose()
    {
        CancellationTokenSource? cancelled;
        lock (_gate)
        {
            cancelled = _cancellation;
            _cancellation = null;
            _generation++;
        }
        cancelled?.Cancel();
        cancelled?.Dispose();
    }
}
