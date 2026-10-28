using System.Diagnostics;

namespace PulseMonitor.Metrics;

/// <summary>Bir ölçüm aralığındaki ağ trafiği: hız (bayt/sn) ve aralıkta geçen bayt.</summary>
public readonly record struct NetworkSample(double DownBps, double UpBps, ulong DownBytes, ulong UpBytes);

/// <summary>
/// Ağ hızını bağdaştırıcı başına sayaç farkından hesaplar. Her bağdaştırıcı ayrı izlendiği için
/// bağdaştırıcı takılıp çıkarılınca ya da sayaç sıfırlanınca toplam bozulmaz.
/// </summary>
internal sealed class NetworkSampler
{
    private Dictionary<Guid, (ulong In, ulong Out)> _previous = new();
    private long _previousTicks;

    /// <param name="selectedId">Seçili bağdaştırıcı GUID'i; null/boş ise tüm bağlı fiziksel bağdaştırıcılar.</param>
    public NetworkSample Sample(string? selectedId)
    {
        List<NetworkAdapterInfo> adapters = NetworkAdapters.Read();
        long now = Stopwatch.GetTimestamp();
        double seconds = _previousTicks == 0 ? 0 : (now - _previousTicks) / (double)Stopwatch.Frequency;
        _previousTicks = now;

        Guid? selected = Guid.TryParse(selectedId, out Guid id) ? id : null;
        ulong down = 0, up = 0;
        var current = new Dictionary<Guid, (ulong In, ulong Out)>(adapters.Count);

        foreach (NetworkAdapterInfo a in adapters)
        {
            current[a.Id] = (a.InOctets, a.OutOctets);
            bool counted = selected is { } sel ? a.Id == sel : a.IsPhysical && a.IsUp;
            if (!counted || !_previous.TryGetValue(a.Id, out var prev)) continue;

            // Sayaç geriye gittiyse (bağdaştırıcı yeniden başladı) bu aralık sayılmaz.
            if (a.InOctets >= prev.In) down += a.InOctets - prev.In;
            if (a.OutOctets >= prev.Out) up += a.OutOctets - prev.Out;
        }

        _previous = current;
        return seconds > 0
            ? new NetworkSample(down / seconds, up / seconds, down, up)
            : new NetworkSample(0, 0, 0, 0);
    }
}
