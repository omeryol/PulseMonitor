using System.Runtime.InteropServices;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>CPU yükünü GetSystemTimes farkından, bellek yükünü GlobalMemoryStatusEx'ten okur.</summary>
internal sealed class CpuRamSampler
{
    private long _prevIdle;
    private long _prevTotal;

    /// <summary>Son okumada toplam ve kullanılabilir fiziksel bellek (bayt); ayrıntı kartı için.</summary>
    public ulong RamTotal { get; private set; }
    public ulong RamAvailable { get; private set; }

    public (double Cpu, double Ram) Sample()
    {
        double cpu = 0;
        if (Kernel32.GetSystemTimes(out long idle, out long kernel, out long user))
        {
            long total = kernel + user; // kernel süresi idle'ı da içerir
            if (_prevTotal != 0 && total > _prevTotal)
                cpu = 100.0 * (1.0 - (double)(idle - _prevIdle) / (total - _prevTotal));
            _prevIdle = idle;
            _prevTotal = total;
        }

        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        double ram = 0;
        if (Kernel32.GlobalMemoryStatusEx(ref mem))
        {
            ram = mem.dwMemoryLoad;
            RamTotal = mem.ullTotalPhys;
            RamAvailable = mem.ullAvailPhys;
        }

        return (Math.Clamp(cpu, 0, 100), ram);
    }
}
