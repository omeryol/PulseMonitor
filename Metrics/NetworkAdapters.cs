using System.Runtime.InteropServices;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>Bir ağ bağdaştırıcısının o anki durumu ve toplam bayt sayaçları.</summary>
public sealed record NetworkAdapterInfo(
    Guid Id, string Name, string Description, bool IsUp, bool IsPhysical, ulong InOctets, ulong OutOctets);

/// <summary>
/// Bağdaştırıcıları GetIfTable2 ile okur. Geri döngü ve filtre arabirimleri (ör. "WFP LightWeight Filter")
/// atlanır: filtreler asıl bağdaştırıcının aynı trafiğini tekrar gösterdiği için çift sayıma yol açar.
/// </summary>
internal static class NetworkAdapters
{
    private static readonly int RowSize = Marshal.SizeOf<MIB_IF_ROW2>();

    public static List<NetworkAdapterInfo> Read()
    {
        var list = new List<NetworkAdapterInfo>();
        if (IpHlpApi.GetIfTable2(out IntPtr table) != 0) return list;

        try
        {
            int count = Marshal.ReadInt32(table);
            var seen = new HashSet<Guid>();
            for (int i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_IF_ROW2>(table + 8 + i * RowSize);
                if (row.Type == IpHlpApi.IF_TYPE_SOFTWARE_LOOPBACK) continue;
                if ((row.InterfaceAndOperStatusFlags & IpHlpApi.FLAG_FILTER_INTERFACE) != 0) continue;
                if (!seen.Add(row.InterfaceGuid)) continue;

                list.Add(new NetworkAdapterInfo(
                    row.InterfaceGuid,
                    row.Alias,
                    row.Description,
                    row.OperStatus == IpHlpApi.IF_OPER_STATUS_UP,
                    (row.InterfaceAndOperStatusFlags & IpHlpApi.FLAG_HARDWARE_INTERFACE) != 0,
                    row.InOctets,
                    row.OutOctets));
            }
        }
        finally
        {
            IpHlpApi.FreeMibTable(table);
        }
        return list;
    }

    /// <summary>MIB_IF_ROW2 tanımı Windows'unkiyle aynı boyutta mı? (1352 bayt olmalı)</summary>
    public static bool LayoutIsValid => RowSize == 1352;
}
