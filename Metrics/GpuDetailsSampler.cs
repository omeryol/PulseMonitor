using System.ComponentModel;
using System.Diagnostics;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>
/// GPU ayrıntı kartı açıkken (<see cref="MetricsHub.GpuDetailsHovered"/>) motor yüklerini, video belleğini ve en çok
/// kullanan işlemleri ölçer; kart kapanınca sayaçlar kapatılır. Ayarlardaki ekran kartı seçimine uyar
/// (seçim yoksa tüm kartlar). Sürücüsüz, yalnızca Windows performans sayaçları ve DXGI.
/// </summary>
internal sealed class GpuDetailsSampler : IDisposable
{
    private const int TopProcesses = 5;
    private static readonly TimeSpan AdapterRefresh = TimeSpan.FromSeconds(30);

    private readonly Source _engines = new(@"\GPU Engine(*)\Utilization Percentage");
    private readonly Source _dedicated = new(@"\GPU Adapter Memory(*)\Dedicated Usage");
    private readonly Source _shared = new(@"\GPU Adapter Memory(*)\Shared Usage");
    private List<GraphicsAdapter> _adapters = [];
    private DateTime _adaptersReadAt;

    /// <param name="selection">Ayardaki ekran kartı seçimi: boş, "kimlik" ya da "kimlik|kimlik".</param>
    public GpuDetails Sample(string? selection)
    {
        if (_adapters.Count == 0 || DateTime.UtcNow - _adaptersReadAt > AdapterRefresh)
        {
            _adapters = GraphicsAdapter.Read();
            _adaptersReadAt = DateTime.UtcNow;
        }

        string[] ids = string.IsNullOrEmpty(selection) ? [] : selection.Split('|');
        List<GraphicsAdapter> chosen = ids.Length == 0 ? _adapters : _adapters.Where(a => ids.Contains(a.Id)).ToList();
        if (chosen.Count == 0) chosen = _adapters;
        HashSet<string> luids = ids.Length == 0 ? [] : chosen.Select(a => a.Luid).ToHashSet();
        ulong dedicatedTotal = chosen.Aggregate(0UL, (sum, a) => sum + a.DedicatedMemory);

        var names = new Dictionary<int, string?>();
        return GpuUsage.Summarize(_engines.Read(), _dedicated.Read(), _shared.Read(), luids, dedicatedTotal,
            pid => names.TryGetValue(pid, out string? name) ? name : names[pid] = ProcessName(pid), TopProcesses);
    }

    private static string? ProcessName(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        // İşlem bu arada kapanmış ya da erişim vermiyor olabilir; satır atlanır (olağan, günlüğe yazılmaz).
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _engines.Dispose();
        _dedicated.Dispose();
        _shared.Dispose();
    }

    /// <summary>Joker karakterli tek sayaç: açılamazsa bir daha denenmez, boş liste döner.</summary>
    private sealed class Source(string path) : IDisposable
    {
        private PdhCounter? _counter;
        private bool _unavailable;

        public List<(string Name, double Value)> Read()
        {
            if (_unavailable) return [];
            _counter ??= PdhCounter.TryOpen(path);
            if (_counter is null)
            {
                _unavailable = true;
                return [];
            }
            return _counter.Collect() ? _counter.ReadArray() : [];
        }

        public void Dispose()
        {
            _counter?.Dispose();
            _counter = null;
        }
    }
}
