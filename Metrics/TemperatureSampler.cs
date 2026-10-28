using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>
/// Sürücüsüz sıcaklıklar (antivirüsün tepki vereceği bir çekirdek sürücüsü kullanılmaz):
/// GPU — D3DKMT (Görev Yöneticisi ile aynı kaynak), disk — NVMe'nin kendi sıcaklık raporu.
/// CPU sıcaklığı masaüstü Intel/AMD'de sürücüsüz okunamadığı için yoktur.
/// </summary>
internal sealed class TemperatureSampler
{
    private const int MaxDrives = 8;
    private static readonly TimeSpan DriveRescan = TimeSpan.FromMinutes(5);

    private int _driveIndex = -1;
    private DateTime _lastDriveScan = DateTime.MinValue;

    /// <summary>En sıcak GPU (°C); okunamazsa null.</summary>
    public static double? ReadGpu()
    {
        var adapters = new D3DKMT_ENUMADAPTERS { Adapters = new D3DKMT_ADAPTERINFO[Gdi32.MaxAdapters] };
        if (Gdi32.D3DKMTEnumAdapters(ref adapters) != 0) return null;

        double? hottest = null;
        int size = Marshal.SizeOf<D3DKMT_ADAPTER_PERFDATA>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            for (int i = 0; i < Math.Min(adapters.NumAdapters, (uint)Gdi32.MaxAdapters); i++)
            {
                Marshal.StructureToPtr(new D3DKMT_ADAPTER_PERFDATA(), buffer, false);
                var query = new D3DKMT_QUERYADAPTERINFO
                {
                    hAdapter = adapters.Adapters[i].hAdapter,
                    Type = Gdi32.KMTQAITYPE_ADAPTERPERFDATA,
                    pPrivateDriverData = buffer,
                    PrivateDriverDataSize = (uint)size,
                };
                if (Gdi32.D3DKMTQueryAdapterInfo(ref query) != 0) continue;
                uint tenths = Marshal.PtrToStructure<D3DKMT_ADAPTER_PERFDATA>(buffer).Temperature;
                if (tenths is > 0 and < 1500) hottest = Math.Max(hottest ?? 0, tenths / 10.0);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return hottest;
    }

    /// <summary>Sıcaklık bildiren ilk diskin sıcaklığı (°C); bulunan disk hatırlanır, okunamazsa 5 dk sonra yeniden aranır.</summary>
    public double? ReadDisk()
    {
        if (_driveIndex >= 0 && ReadDrive(_driveIndex) is { } t) return t;
        if (DateTime.UtcNow - _lastDriveScan < DriveRescan) return null;

        _lastDriveScan = DateTime.UtcNow;
        _driveIndex = -1;
        for (int i = 0; i < MaxDrives; i++)
        {
            if (ReadDrive(i) is { } found)
            {
                _driveIndex = i;
                return found;
            }
        }
        return null;
    }

    private static double? ReadDrive(int index)
    {
        using SafeFileHandle handle = StorageIo.OpenForQuery(index);
        if (handle.IsInvalid) return null;

        var query = new StorageIo.STORAGE_PROPERTY_QUERY
        {
            PropertyId = StorageIo.StorageDeviceTemperatureProperty,
            QueryType = StorageIo.PropertyStandardQuery,
        };
        var output = new byte[512];
        if (!StorageIo.DeviceIoControl(handle, StorageIo.IOCTL_STORAGE_QUERY_PROPERTY, ref query,
                Marshal.SizeOf(query), output, output.Length, out _, IntPtr.Zero))
            return null;

        if (BitConverter.ToUInt16(output, StorageIo.InfoCountOffset) == 0) return null;
        short celsius = BitConverter.ToInt16(output, StorageIo.InfoArrayOffset + StorageIo.InfoTemperatureOffset);
        return celsius is > 0 and < 150 ? celsius : null;
    }
}
