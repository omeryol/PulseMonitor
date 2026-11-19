using PulseMonitor.Settings;

namespace PulseMonitor.Traffic;

/// <summary>Kotanın bir parçası: ne sayıldığı, kullanılan bayt, sınır (GB), bildirim metni anahtarı.</summary>
/// <param name="Id">Total, Down, Up, SplitDown, SplitUp (bildirim tekrarını ayırt etmek için).</param>
/// <param name="Arrow">Ayarlar penceresinde gösterilen yön işareti (birlikte için boş).</param>
internal readonly record struct QuotaPart(string Id, string Arrow, ulong Used, int LimitGb, string AlertKey);

/// <summary>
/// Kota hesabı: seçili kipe göre indirme ve yükleme birlikte, yalnızca biri ya da ayrı ayrı (iki sınır) sayılır.
/// </summary>
internal static class TrafficQuota
{
    public const string TotalId = "Total";

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
