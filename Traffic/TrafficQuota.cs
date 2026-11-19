using PulseMonitor.Settings;

namespace PulseMonitor.Traffic;

/// <summary>Kotanın bir parçası: ne sayıldığı, kullanılan bayt, sınır (GB), bildirim metni anahtarı.</summary>
/// <param name="Id">Total, Down, Up, SplitDown, SplitUp (bildirim tekrarını ayırt etmek için).</param>
/// <param name="Arrow">Ayarlar penceresinde gösterilen yön işareti (birlikte için boş).</param>
internal readonly record struct QuotaPart(string Id, string Arrow, ulong Used, int LimitGb, string AlertKey);

/// <summary>Kotanın bir parçasının kullanımı: yön işareti, kullanılan bayt, sınır (GB), yüzde.</summary>
public readonly record struct QuotaUsage(string Arrow, ulong Used, int LimitGb, double Percent);

/// <summary>
/// Kota hesabı: seçili kipe göre indirme ve yükleme birlikte, yalnızca biri ya da ayrı ayrı (iki sınır) sayılır.
/// </summary>
internal static class TrafficQuota
{
    public const string TotalId = "Total";

    /// <summary>Bu fatura dönemindeki kullanım (kipe göre bir ya da iki parça).</summary>
    public static IReadOnlyList<QuotaUsage> Usage(AlertSettings a, TrafficHistory history, DateOnly today)
    {
        DayTraffic t = TrafficPeriod.Total(history, TrafficPeriod.For(today, a.TrafficCycleStartDay));
        return Parts(a, t.Down, t.Up)
            .Select(p => new QuotaUsage(p.Arrow, p.Used, p.LimitGb, p.Used * 100d / (p.LimitGb * 1024d * 1024 * 1024)))
            .ToList();
    }

    public static IReadOnlyList<QuotaPart> Parts(AlertSettings a, ulong down, ulong up) => a.TrafficQuotaMode switch
    {
        StyleOptions.QuotaDown => [new("Down", "↓", down, a.TrafficLimitGb, "Alert.TrafficDown")],
        StyleOptions.QuotaUp => [new("Up", "↑", up, a.TrafficLimitGb, "Alert.TrafficUp")],
        StyleOptions.QuotaSplit =>
        [
            new("SplitDown", "↓", down, a.TrafficLimitGb, "Alert.TrafficDown"),
            new("SplitUp", "↑", up, a.TrafficUploadLimitGb, "Alert.TrafficUp"),
        ],
        _ => [new(TotalId, "", TrafficPeriod.Add(down, up), a.TrafficLimitGb, "Alert.Traffic")],
    };
}
