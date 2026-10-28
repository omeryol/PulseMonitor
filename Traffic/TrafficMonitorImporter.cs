using System.Globalization;
using System.IO;
using PulseMonitor.Core;

namespace PulseMonitor.Traffic;

/// <summary>
/// TrafficMonitor'ün <c>%APPDATA%\TrafficMonitor\history_traffic.dat</c> dosyasını içe aktarır (yalnızca okur).
/// Satır biçimi: <c>2026/10/28 365276/2150497</c> → yükleme/indirme, KB. Tek sayı varsa toplamdır.
/// </summary>
internal static class TrafficMonitorImporter
{
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrafficMonitor", "history_traffic.dat");

    /// <returns>Aktarılan gün sayısı.</returns>
    public static int Import(TrafficHistory history, string path)
    {
        if (!File.Exists(path)) return 0;

        int imported = 0;
        try
        {
            foreach (string line in File.ReadLines(path))
            {
                if (TryParse(line, out DateOnly day, out DayTraffic traffic))
                {
                    history.MergeMax(day, traffic);
                    imported++;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"TrafficMonitor geçmişi okunamadı: {path}", ex);
        }
        return imported;
    }

    private static bool TryParse(string line, out DateOnly day, out DayTraffic traffic)
    {
        traffic = default;
        string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        day = default;
        if (parts.Length != 2 ||
            !DateOnly.TryParseExact(parts[0], "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
            return false;

        string[] values = parts[1].Split('/');
        if (values.Length == 2 &&
            ulong.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong upKb) &&
            ulong.TryParse(values[1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong downKb))
        {
            traffic = new DayTraffic(downKb * 1024, upKb * 1024);
            return true;
        }
        if (values.Length == 1 && ulong.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong totalKb))
        {
            traffic = new DayTraffic(totalKb * 1024, 0);
            return true;
        }
        return false;
    }
}
