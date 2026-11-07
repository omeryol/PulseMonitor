using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

internal sealed record GraphicsAdapter(string Id, string Name, string Luid, ulong DedicatedMemory = 0)
{
    private const uint DxgiSoftware = 1u << 1;
    private const uint RenderSupported = 1u << 0;
    private const uint SoftwareDevice = 1u << 2;
    private const uint IndirectDisplayDevice = 1u << 6;

    public static string LuidKey(int high, uint low) => $"luid_0x{(uint)high:X}_0x{low:X}";

    public static List<GraphicsAdapter> Read()
    {
        var result = new List<GraphicsAdapter>();
        var occurrences = new Dictionary<string, int>();
        var factoryId = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
        if (CreateDXGIFactory1(in factoryId, out IntPtr factory) < 0) return result;
        try
        {
            // IDXGIFactory1::EnumAdapters1 follows IDXGIFactory's nine methods and IUnknown.
            var enumerate = Method<EnumAdapters1>(factory, 12);
            for (uint index = 0; enumerate(factory, index, out IntPtr adapter) >= 0; index++)
            {
                try
                {
                    // IDXGIAdapter1::GetDesc1 follows IDXGIObject and IDXGIAdapter.
                    if (Method<GetDesc1>(adapter, 10)(adapter, out var desc) < 0 || (desc.Flags & DxgiSoftware) != 0
                        || IsDisplayOnlyOrSoftware(desc))
                        continue;

                    // LUID is session-local. Save hardware identifiers and an ordinal for identical cards.
                    string hardwareId = $"{desc.VendorId:X}:{desc.DeviceId:X}:{desc.SubSysId:X}:{desc.Revision:X}";
                    int ordinal = occurrences.GetValueOrDefault(hardwareId) + 1;
                    occurrences[hardwareId] = ordinal;
                    string name = ordinal == 1 ? desc.Description : $"{desc.Description} ({ordinal})";
                    result.Add(new GraphicsAdapter($"{hardwareId}:{ordinal}", name, LuidKey(desc.LuidHigh, desc.LuidLow), (ulong)desc.DedicatedVideoMemory));
                }
                finally { Marshal.Release(adapter); }
            }
        }
        finally { Marshal.Release(factory); }
        return result;
    }

    private static bool IsDisplayOnlyOrSoftware(AdapterDescription desc)
    {
        var adapter = new D3DKMT_OPENADAPTERFROMLUID { LuidLow = desc.LuidLow, LuidHigh = desc.LuidHigh };
        if (Gdi32.D3DKMTOpenAdapterFromLuid(ref adapter) != 0) return false;

        IntPtr buffer = IntPtr.Zero;
        try
        {
            buffer = Marshal.AllocHGlobal(sizeof(uint));
            var query = new D3DKMT_QUERYADAPTERINFO
            {
                hAdapter = adapter.hAdapter,
                Type = Gdi32.KMTQAITYPE_ADAPTERTYPE,
                pPrivateDriverData = buffer,
                PrivateDriverDataSize = sizeof(uint),
            };
            if (Gdi32.D3DKMTQueryAdapterInfo(ref query) != 0) return false;

            uint flags = unchecked((uint)Marshal.ReadInt32(buffer));
            return (flags & RenderSupported) == 0 || (flags & (SoftwareDevice | IndirectDisplayDevice)) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
            Gdi32.D3DKMTCloseAdapter(in adapter.hAdapter);
        }
    }

    private static T Method<T>(IntPtr instance, int slot) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));

    [DllImport("dxgi.dll", ExactSpelling = true)]
    private static extern int CreateDXGIFactory1(in Guid id, out IntPtr factory);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAdapters1(IntPtr self, uint index, out IntPtr adapter);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDesc1(IntPtr self, out AdapterDescription description);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct AdapterDescription
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
        public uint VendorId, DeviceId, SubSysId, Revision;
        public UIntPtr DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory;
        public uint LuidLow;
        public int LuidHigh;
        public uint Flags;
    }
}
