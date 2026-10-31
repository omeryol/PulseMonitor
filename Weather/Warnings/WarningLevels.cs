using PulseMonitor.Settings;

namespace PulseMonitor.Weather.Warnings;

/// <summary>Ayardaki seviye kimliği (Settings/StyleOptions.cs) → detektörün sayısal seviyesi.</summary>
internal static class WarningLevels
{
    public static int From(string? id) => id switch
    {
        StyleOptions.WarnExtreme => 1,
        StyleOptions.WarnBalanced => 2,
        StyleOptions.WarnSensitive => 3,
        _ => 0,
    };
}
