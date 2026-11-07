namespace PulseMonitor.Metrics;

/// <summary>Bir motor türünün yükü (0–100).</summary>
public sealed record GpuEngineLoad(string Type, double Percent);

/// <summary>Aynı adlı işlemlerin GPU kullanımı (en yoğun motor türüne göre).</summary>
public sealed record GpuProcessUsage(string Name, double Percent, int Count = 1);

/// <summary>GPU ayrıntı kartı verisi: motor yükleri, video belleği (özel/paylaşılan) ve en çok kullanan işlemler.</summary>
public sealed record GpuDetails(
    IReadOnlyList<GpuEngineLoad> Engines,
    ulong DedicatedUsed,
    ulong DedicatedTotal,
    ulong SharedUsed,
    IReadOnlyList<GpuProcessUsage> Processes);

/// <summary>GPU performans sayaçlarından kart verisini hesaplar (saf mantık, test edilir).</summary>
internal static class GpuUsage
{
    public const string Engine3D = "3D", VideoDecode = "VideoDecode", VideoEncode = "VideoEncode",
        VideoProcessing = "VideoProcessing", Copy = "Copy", Compute = "Compute";

    /// <summary>Karttaki satır sırası.</summary>
    public static readonly string[] EngineOrder = [Engine3D, VideoDecode, VideoEncode, VideoProcessing, Copy, Compute];

    private const double MinProcessPercent = 0.1;

    /// <summary>Sayaç motor türünü kart türüne çevirir; kartta gösterilmeyen türler için null.</summary>
    public static string? NormalizeEngine(string? type) => type switch
    {
        null => null,
        "3D" => Engine3D,
        "VideoDecode" => VideoDecode,
        "VideoEncode" => VideoEncode,
        "VideoProcessing" => VideoProcessing,
        "Copy" => Copy,
        _ when type.StartsWith("Compute", StringComparison.Ordinal) || type.StartsWith("Cuda", StringComparison.Ordinal) => Compute,
        _ => null,
    };

    /// <param name="engines">"GPU Engine" sayacı: (örnek adı, yüzde).</param>
    /// <param name="dedicated">"GPU Adapter Memory / Dedicated Usage": (örnek adı, bayt).</param>
    /// <param name="shared">"GPU Adapter Memory / Shared Usage": (örnek adı, bayt).</param>
    /// <param name="luids">Dikkate alınan adaptörler; boşsa hepsi.</param>
    /// <param name="processName">İşlem numarasından ad; bulunamazsa null (satır atlanır).</param>
    /// <param name="top">Listelenecek işlem sayısı.</param>
    public static GpuDetails Summarize(
        IEnumerable<(string Name, double Value)> engines,
        IEnumerable<(string Name, double Value)> dedicated,
        IEnumerable<(string Name, double Value)> shared,
        ISet<string> luids, ulong dedicatedTotal, Func<int, string?> processName, int top)
    {
        bool Wanted(string instance) => luids.Count == 0 || (GpuInstanceName.Luid(instance) is { } luid && luids.Contains(luid));

        // Aynı motoru kullanan tüm işlemlerin yükü toplanır; motor türünün yükü, o türün en yoğun motorudur.
        var perEngine = new Dictionary<(string Luid, string Engine, string Type), double>();
        var perProcess = new Dictionary<(int Pid, string Type), double>();
        foreach (var (name, value) in engines)
        {
            if (NormalizeEngine(GpuInstanceName.EngineType(name)) is not { } type ||
                GpuInstanceName.Luid(name) is not { } luid || GpuInstanceName.EngineId(name) is not { } engine || !Wanted(name))
                continue;
            perEngine[(luid, engine, type)] = perEngine.GetValueOrDefault((luid, engine, type)) + value;
            if (GpuInstanceName.Pid(name) is { } pid) perProcess[(pid, type)] = perProcess.GetValueOrDefault((pid, type)) + value;
        }

        var loads = new List<GpuEngineLoad>();
        foreach (string type in EngineOrder)
        {
            double max = perEngine.Where(kv => kv.Key.Type == type).Select(kv => kv.Value).DefaultIfEmpty(0).Max();
            loads.Add(new GpuEngineLoad(type, Math.Clamp(max, 0, 100)));
        }

        // İşlem yükü: en yoğun motor türü (Görev Yöneticisi'ndeki GPU sütunu gibi); aynı adlılar toplanır.
        var processes = perProcess
            .GroupBy(kv => kv.Key.Pid)
            .Select(g => (Pid: g.Key, Percent: Math.Clamp(g.Max(kv => kv.Value), 0, 100)))
            .Where(p => p.Percent >= MinProcessPercent)
            .OrderByDescending(p => p.Percent)
            .Select(p => (Name: processName(p.Pid), p.Percent))
            .Where(p => p.Name is not null)
            .GroupBy(p => p.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GpuProcessUsage(g.First().Name!, Math.Min(100, g.Sum(p => p.Percent)), g.Count()))
            .OrderByDescending(p => p.Percent).ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Take(top).ToArray();

        return new GpuDetails(loads, Sum(dedicated.Where(m => Wanted(m.Name))), dedicatedTotal,
            Sum(shared.Where(m => Wanted(m.Name))), processes);
    }

    private static ulong Sum(IEnumerable<(string Name, double Value)> values) =>
        (ulong)Math.Max(0, values.Sum(v => v.Value));
}
