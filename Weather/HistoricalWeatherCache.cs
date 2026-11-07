using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// Kullanıcının özellikle açtığı tarihsel sorguları yerelde kısa süre saklar.
/// Aynı konumun büyük bir aralığı varsa, içindeki küçük dönemler ağ isteği yapmadan süzülür.
/// </summary>
internal static class HistoricalWeatherCache
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(2);
    private static readonly object Gate = new();
    private static List<Entry>? _entries;

    private sealed class Entry
    {
        public string LocationKey { get; set; } = "";
        public DateTimeOffset FetchedAtUtc { get; set; }
        public List<HistoricalWeatherDay> Days { get; set; } = [];
    }

    public static bool TryGet(double latitude, double longitude, HistoricalWeatherRange range, out HistoricalWeatherReport report)
    {
        lock (Gate)
        {
            List<Entry> entries = Entries();
            bool changed = RemoveExpired(entries);
            Entry? entry = entries.FirstOrDefault(item => item.LocationKey == Key(latitude, longitude));
            if (entry is null)
            {
                if (changed) Save(entries);
                report = default!;
                return false;
            }

            HistoricalWeatherDay[] days = entry.Days
                .Where(day => day.Date >= range.Start && day.Date <= range.End)
                .OrderBy(day => day.Date)
                .ToArray();
            if (days.Length != range.DayCount || days[0].Date != range.Start || days[^1].Date != range.End)
            {
                if (changed) Save(entries);
                report = default!;
                return false;
            }

            if (changed) Save(entries);
            report = new HistoricalWeatherReport(days);
            return true;
        }
    }

    public static void Store(double latitude, double longitude, HistoricalWeatherReport report)
    {
        if (report.Days.Count == 0) return;
        lock (Gate)
        {
            List<Entry> entries = Entries();
            RemoveExpired(entries);
            string key = Key(latitude, longitude);
            Entry entry = entries.FirstOrDefault(item => item.LocationKey == key) ?? new Entry { LocationKey = key };
            if (!entries.Contains(entry)) entries.Add(entry);
            entry.FetchedAtUtc = DateTimeOffset.UtcNow;
            entry.Days = entry.Days.Concat(report.Days)
                .GroupBy(day => day.Date).Select(group => group.Last()).OrderBy(day => day.Date)
                .TakeLast(HistoricalWeatherRange.MaxDays).ToList();
            Save(entries);
        }
    }

    private static List<Entry> Entries()
    {
        if (_entries is not null) return _entries;
        try
        {
            _entries = File.Exists(AppPaths.HistoricalWeatherCacheFile)
                ? JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(AppPaths.HistoricalWeatherCacheFile)) ?? []
                : [];
        }
        catch (Exception ex)
        {
            Log.Warn("Tarihsel hava önbelleği okunamadı; yok sayıldı.", ex);
            _entries = [];
        }
        return _entries;
    }

    private static bool RemoveExpired(List<Entry> entries) =>
        entries.RemoveAll(entry => entry.FetchedAtUtc < DateTimeOffset.UtcNow - Lifetime) > 0;

    private static void Save(List<Entry> entries)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string temp = AppPaths.HistoricalWeatherCacheFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(entries));
            File.Move(temp, AppPaths.HistoricalWeatherCacheFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warn("Tarihsel hava önbelleği yazılamadı.", ex);
        }
    }

    private static string Key(double latitude, double longitude) => FormattableString.Invariant($"{latitude:0.####},{longitude:0.####}");
}
