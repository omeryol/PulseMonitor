using System.Globalization;
using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Traffic;

/// <summary>
/// Trafik geçmişini <c>traffic-history.json</c> dosyasına yazar/okur.
/// Biçim: { "Version": 1, "Days": { "2026-10-28": [indirme, yükleme] } }
/// </summary>
internal static class TrafficHistoryStore
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly object FileSync = new();

    private sealed class FileModel
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, ulong[]> Days { get; set; } = new();
    }

    public static TrafficHistory Load()
    {
        var history = new TrafficHistory();
        string path = AppPaths.TrafficHistoryFile;
        if (!File.Exists(path)) return history;

        try
        {
            FileModel model;
            lock (FileSync) model = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(path)) ?? new FileModel();
            foreach (var (key, values) in model.Days)
            {
                if (values.Length < 2 ||
                    !DateOnly.TryParseExact(key, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day))
                    continue;
                history.MergeMax(day, new DayTraffic(values[0], values[1]));
            }
        }
        catch (Exception ex)
        {
            string backup = path + ".bad-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            Log.Error($"Trafik geçmişi okunamadı; yedeklendi: {backup}", ex);
            try { File.Copy(path, backup, overwrite: true); } catch (Exception copyEx) { Log.Warn("Yedek alınamadı.", copyEx); }
        }
        return history;
    }

    public static void Save(TrafficHistory history)
    {
        var model = new FileModel();
        foreach (var (day, t) in history.Snapshot())
            model.Days[day.ToString(DateFormat, CultureInfo.InvariantCulture)] = [t.Down, t.Up];

        try
        {
            lock (FileSync)
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                string temp = AppPaths.TrafficHistoryFile + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(model));
                File.Move(temp, AppPaths.TrafficHistoryFile, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Trafik geçmişi kaydedilemedi.", ex);
        }
    }
}
