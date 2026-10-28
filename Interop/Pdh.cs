using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

/// <summary>Performans sayaçları (pdh.dll). Sayaç yolları İngilizce verilir; Türkçe Windows'ta da çalışır.</summary>
internal static class Pdh
{
    public const uint PDH_FMT_DOUBLE = 0x00000200;
    public const uint PDH_FMT_NOCAP100 = 0x00008000;
    public const int PDH_MORE_DATA = unchecked((int)0x800007D2);

    /// <summary>CStatus 0 (VALID_DATA) ve 1 (NEW_DATA) geçerli değerdir.</summary>
    public const uint PDH_CSTATUS_NEW_DATA = 1;

    // PDH_FMT_COUNTERVALUE_ITEM düzeni: szName(0), CStatus(8), doubleValue(16), boyut 24.
    public const int ItemSize = 24;
    public const int ItemStatusOffset = 8;
    public const int ItemValueOffset = 16;

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    public static extern int PdhOpenQuery(string? dataSource, IntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    public static extern int PdhAddEnglishCounter(IntPtr query, string path, IntPtr userData, out IntPtr counter);

    [DllImport("pdh.dll")]
    public static extern int PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll")]
    public static extern int PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out PDH_FMT_COUNTERVALUE value);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhGetFormattedCounterArrayW")]
    public static extern int PdhGetFormattedCounterArray(IntPtr counter, uint format, ref uint bufferSize, out uint itemCount, IntPtr buffer);

    [DllImport("pdh.dll")]
    public static extern int PdhCloseQuery(IntPtr query);
}

[StructLayout(LayoutKind.Explicit)]
internal struct PDH_FMT_COUNTERVALUE
{
    [FieldOffset(0)] public uint CStatus;
    [FieldOffset(8)] public double DoubleValue;
}
