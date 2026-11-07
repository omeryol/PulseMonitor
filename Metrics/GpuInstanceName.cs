using System.Globalization;
using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

/// <summary>
/// GPU performans sayacı örnek adları:
/// <c>pid_1234_luid_0x00000000_0x0000D1A3_phys_0_eng_0_engtype_3D</c> (motor) ve
/// <c>luid_0x00000000_0x0000D1A3_phys_0</c> (adaptör belleği).
/// </summary>
internal static class GpuInstanceName
{
    private const string LuidMarker = "luid_", PhysMarker = "_phys", EngineTypeMarker = "_engtype_", PidPrefix = "pid_";

    /// <summary>Adaptörün LUID anahtarı (<see cref="GraphicsAdapter.LuidKey"/> biçiminde); bulunamazsa null.</summary>
    public static string? Luid(string instance)
    {
        int start = instance.IndexOf(LuidMarker, StringComparison.Ordinal);
        int end = start < 0 ? -1 : instance.IndexOf(PhysMarker, start, StringComparison.Ordinal);
        if (end <= start) return null;

        const string separator = "_0x";
        int highStart = start + "luid_0x".Length;
        int lowStart = instance.IndexOf(separator, highStart, StringComparison.OrdinalIgnoreCase);
        if (lowStart < highStart || lowStart + separator.Length >= end) return null;
        return uint.TryParse(instance.AsSpan(highStart, lowStart - highStart), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint high)
            && uint.TryParse(instance.AsSpan(lowStart + separator.Length, end - lowStart - separator.Length),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint low)
            ? GraphicsAdapter.LuidKey(unchecked((int)high), low)
            : null;
    }

    /// <summary>İşlem numarası (<c>pid_1234_…</c>); adaptör belleği örneklerinde yok.</summary>
    public static int? Pid(string instance)
    {
        if (!instance.StartsWith(PidPrefix, StringComparison.Ordinal)) return null;
        int end = instance.IndexOf('_', PidPrefix.Length);
        return end > PidPrefix.Length && int.TryParse(instance.AsSpan(PidPrefix.Length, end - PidPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int pid)
            ? pid : null;
    }

    /// <summary>Motor türü (<c>3D</c>, <c>VideoDecode</c>, <c>Compute_0</c>…); bulunamazsa null.</summary>
    public static string? EngineType(string instance)
    {
        int start = instance.IndexOf(EngineTypeMarker, StringComparison.Ordinal);
        return start < 0 ? null : instance[(start + EngineTypeMarker.Length)..];
    }

    /// <summary>Motorun kimliği (<c>phys_0_eng_1</c>): aynı motoru kullanan işlemler bu kimlikte toplanır.</summary>
    public static string? EngineId(string instance)
    {
        int start = instance.IndexOf(PhysMarker, StringComparison.Ordinal);
        int end = instance.IndexOf(EngineTypeMarker, StringComparison.Ordinal);
        return start < 0 || end <= start ? null : instance[(start + 1)..end];
    }
}
