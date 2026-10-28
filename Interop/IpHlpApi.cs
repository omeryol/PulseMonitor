using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

internal static class IpHlpApi
{
    public const uint IF_TYPE_SOFTWARE_LOOPBACK = 24;
    public const byte FLAG_HARDWARE_INTERFACE = 0x01;
    public const byte FLAG_FILTER_INTERFACE = 0x02;
    public const int IF_OPER_STATUS_UP = 1;

    /// <summary>MIB_IF_TABLE2 döndürür; FreeMibTable ile serbest bırakılmalı.</summary>
    [DllImport("iphlpapi.dll")]
    public static extern int GetIfTable2(out IntPtr table);

    [DllImport("iphlpapi.dll")]
    public static extern void FreeMibTable(IntPtr memory);
}

/// <summary>netioapi.h MIB_IF_ROW2 (1352 bayt).</summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MIB_IF_ROW2
{
    public ulong InterfaceLuid;
    public uint InterfaceIndex;
    public Guid InterfaceGuid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Alias;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string Description;
    public uint PhysicalAddressLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PhysicalAddress;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] PermanentPhysicalAddress;
    public uint Mtu;
    public uint Type;
    public int TunnelType;
    public int MediaType;
    public int PhysicalMediumType;
    public int AccessType;
    public int DirectionType;
    public byte InterfaceAndOperStatusFlags;
    public int OperStatus;
    public int AdminStatus;
    public int MediaConnectState;
    public Guid NetworkGuid;
    public int ConnectionType;
    public ulong TransmitLinkSpeed;
    public ulong ReceiveLinkSpeed;
    public ulong InOctets;
    public ulong InUcastPkts;
    public ulong InNUcastPkts;
    public ulong InDiscards;
    public ulong InErrors;
    public ulong InUnknownProtos;
    public ulong InUcastOctets;
    public ulong InMulticastOctets;
    public ulong InBroadcastOctets;
    public ulong OutOctets;
    public ulong OutUcastPkts;
    public ulong OutNUcastPkts;
    public ulong OutDiscards;
    public ulong OutErrors;
    public ulong OutUcastOctets;
    public ulong OutMulticastOctets;
    public ulong OutBroadcastOctets;
    public ulong OutQLen;
}
