using System.Runtime.InteropServices;
using PulseMonitor.Core;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>Tek sayaçlı bir PDH sorgusu. Joker karakterli (*) yollarda örnekler her toplamada yeniden taranır.</summary>
internal sealed class PdhCounter : IDisposable
{
    private const uint Format = Pdh.PDH_FMT_DOUBLE | Pdh.PDH_FMT_NOCAP100;
    private IntPtr _query;
    private readonly IntPtr _counter;

    private PdhCounter(IntPtr query, IntPtr counter)
    {
        _query = query;
        _counter = counter;
    }

    public static PdhCounter? TryOpen(string englishPath)
    {
        if (Pdh.PdhOpenQuery(null, IntPtr.Zero, out IntPtr query) != 0) return null;
        int status = Pdh.PdhAddEnglishCounter(query, englishPath, IntPtr.Zero, out IntPtr counter);
        if (status != 0)
        {
            Log.Warn($"Performans sayacı açılamadı: {englishPath} (0x{status:X8})");
            Pdh.PdhCloseQuery(query);
            return null;
        }
        var result = new PdhCounter(query, counter);
        result.Collect(); // oran sayaçları için ilk taban ölçümü
        return result;
    }

    public bool Collect() => Pdh.PdhCollectQueryData(_query) == 0;

    public double? ReadValue()
    {
        int status = Pdh.PdhGetFormattedCounterValue(_counter, Format, out _, out PDH_FMT_COUNTERVALUE value);
        if (status != 0 || value.CStatus > Pdh.PDH_CSTATUS_NEW_DATA) return null;
        return value.DoubleValue;
    }

    public List<(string Name, double Value)> ReadArray()
    {
        var result = new List<(string, double)>();
        uint size = 0;
        int status = Pdh.PdhGetFormattedCounterArray(_counter, Format, ref size, out _, IntPtr.Zero);
        if (status != Pdh.PDH_MORE_DATA || size == 0) return result;

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            status = Pdh.PdhGetFormattedCounterArray(_counter, Format, ref size, out uint count, buffer);
            if (status != 0) return result;

            for (int i = 0; i < count; i++)
            {
                IntPtr item = buffer + i * Pdh.ItemSize;
                uint itemStatus = (uint)Marshal.ReadInt32(item + Pdh.ItemStatusOffset);
                if (itemStatus > Pdh.PDH_CSTATUS_NEW_DATA) continue;
                string name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(item)) ?? string.Empty;
                double value = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(item + Pdh.ItemValueOffset));
                result.Add((name, value));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return result;
    }

    public void Dispose()
    {
        if (_query == IntPtr.Zero) return;
        Pdh.PdhCloseQuery(_query);
        _query = IntPtr.Zero;
    }
}
