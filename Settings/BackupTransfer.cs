using System.Globalization;
using System.IO;
using System.Text.Json;
using PulseMonitor.Traffic;

namespace PulseMonitor.Settings;

/// <summary>Ayar ve günlük trafik yedekleri; içe aktarmadan önce tüm dosya doğrulanır.</summary>
internal static class BackupTransfer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private sealed class Backup
    {
        public int Version { get; set; }
        public string Kind { get; set; } = "";
        public AppSettings? Settings { get; set; }
        public Dictionary<string, ulong[]>? Days { get; set; }
    }

    public static void ExportSettings(string path, AppSettings settings) => Write(path, new Backup { Version = 1, Kind = "settings", Settings = settings });

    public static void ExportHistory(string path, TrafficHistory history) => Write(path, new Backup
    {
        Version = 1, Kind = "traffic",
        Days = history.Snapshot().ToDictionary(x => x.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x => new[] { x.Value.Down, x.Value.Up })
    });

    public static AppSettings ReadSettings(string path)
    {
        var backup = Read(path, "settings");
        var settings = backup.Settings ?? throw new InvalidDataException("Missing settings.");
        if (settings.SchemaVersion > 2) throw new InvalidDataException("Unsupported settings version.");
        TaskbarItems.Normalize(settings);
        StyleOptions.Normalize(settings);
        WeatherSettings.Normalize(settings);
        return settings;
    }

    public static List<KeyValuePair<DateOnly, DayTraffic>> ReadHistory(string path)
    {
        var backup = Read(path, "traffic");
        var days = backup.Days ?? throw new InvalidDataException("Missing traffic history.");
        var result = new List<KeyValuePair<DateOnly, DayTraffic>>();
        foreach (var (key, values) in days)
        {
            if (!DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || values is not { Length: 2 })
                throw new InvalidDataException("Invalid traffic record.");
            result.Add(new(date, new DayTraffic(values[0], values[1])));
        }
        return result;
    }

    private static Backup Read(string path, string kind)
    {
        if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("Backup is too large.");
        var backup = JsonSerializer.Deserialize<Backup>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Invalid backup.");
        if (backup.Version != 1 || backup.Kind != kind) throw new InvalidDataException("Unsupported backup type or version.");
        return backup;
    }

    private static void Write(string path, Backup backup)
    {
        string fullPath = Path.GetFullPath(path);
        string temp = Path.Combine(Path.GetDirectoryName(fullPath)!, $".{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(backup, Options));
            File.Move(temp, fullPath, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
