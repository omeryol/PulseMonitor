using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

/// <summary>
/// D3DKMT: Windows'un ekran kartı çekirdek arayüzü (gdi32.dll). Görev Yöneticisi GPU sıcaklığını buradan okur;
/// sürücü kurmadan ve yönetici yetkisi olmadan çalışır (WDDM 2.4+).
/// </summary>
internal static class Gdi32
{
    public const int KMTQAITYPE_ADAPTERPERFDATA = 62;
    public const int MaxAdapters = 16;

    [DllImport("gdi32.dll")]
    public static extern int D3DKMTEnumAdapters(ref D3DKMT_ENUMADAPTERS adapters);

    [DllImport("gdi32.dll")]
    public static extern int D3DKMTQueryAdapterInfo(ref D3DKMT_QUERYADAPTERINFO info);
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_ADAPTERINFO
{
    public uint hAdapter;
    public uint LuidLow;
    public int LuidHigh;
    public uint NumOfSources;
    public int bPrecise;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_ENUMADAPTERS
{
    public uint NumAdapters;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Gdi32.MaxAdapters)] public D3DKMT_ADAPTERINFO[] Adapters;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_QUERYADAPTERINFO
{
    public uint hAdapter;
    public int Type;
    public IntPtr pPrivateDriverData;
    public uint PrivateDriverDataSize;
}

/// <summary>D3DKMT_ADAPTER_PERFDATA; <see cref="Temperature"/> derecenin onda biri (395 = 39,5 °C).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3DKMT_ADAPTER_PERFDATA
{
    public uint PhysicalAdapterIndex;
    public ulong MemoryFrequency;
    public ulong MaxMemoryFrequency;
    public ulong MaxMemoryFrequencyOC;
    public ulong MemoryBandwidth;
    public ulong PCIEBandwidth;
    public uint FanRPM;
    public uint Power;
    public uint Temperature;
    public byte PowerStateOverride;
}
