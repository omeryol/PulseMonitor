using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PulseMonitor.Interop;

/// <summary>
/// Disk sıcaklığı: IOCTL_STORAGE_QUERY_PROPERTY + StorageDeviceTemperatureProperty. Disk erişim hakkı istemeden
/// (0) açılır; yönetici yetkisi gerekmez. NVMe diskler destekler, SATA diskler çoğunlukla desteklemez.
/// </summary>
internal static class StorageIo
{
    public const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
    public const int StorageDeviceTemperatureProperty = 52;
    public const int PropertyStandardQuery = 0;
    private const uint FILE_SHARE_READ_WRITE = 3;
    private const uint OPEN_EXISTING = 3;

    // STORAGE_TEMPERATURE_DATA_DESCRIPTOR: InfoCount 12. baytta, STORAGE_TEMPERATURE_INFO dizisi 24. bayttan
    // başlar (her biri 16 bayt; Temperature 2. baytta, °C).
    public const int InfoCountOffset = 12;
    public const int InfoArrayOffset = 24;
    public const int InfoSize = 16;
    public const int InfoTemperatureOffset = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_PROPERTY_QUERY
    {
        public int PropertyId;
        public int QueryType;
        public byte AdditionalParameters;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security,
        uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool DeviceIoControl(SafeFileHandle device, uint code, ref STORAGE_PROPERTY_QUERY input,
        int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);

    /// <summary>\\.\PhysicalDriveN'i yalnızca sorgu için açar (okuma/yazma hakkı yok).</summary>
    public static SafeFileHandle OpenForQuery(int index) =>
        CreateFile($@"\\.\PhysicalDrive{index}", 0, FILE_SHARE_READ_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
}
