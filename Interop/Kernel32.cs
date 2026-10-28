using System.Runtime.InteropServices;

namespace PulseMonitor.Interop;

internal static class Kernel32
{
    // FILETIME değerleri 64 bit tamsayı olarak okunur.
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);
}
