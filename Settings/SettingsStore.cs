using System.Globalization;
using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Settings;

/// <summary>Ayarları JSON olarak okur/yazar. Yazma atomiktir; bozuk dosya yedeklenip varsayılana dönülür.</summary>
internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    /// <summary>Ayarlar her kaydedildiğinde (arayüz iş parçacığında) tetiklenir.</summary>
    public event Action? Changed;

    public static SettingsStore Load()
    {
        var store = new SettingsStore();
        string path = AppPaths.SettingsFile;
        if (!File.Exists(path))
        {
            TaskbarItems.Normalize(store.Current);
            StyleOptions.Normalize(store.Current);
            return store;
        }

        try
        {
            store.Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string backup = Path.Combine(AppPaths.DataDir, $"settings.bad-{stamp}.json");
            Log.Warn($"Ayar dosyası okunamadı, varsayılanlar kullanılıyor. Yedek: {backup}", ex);
            TryCopy(path, backup);
        }
        TaskbarItems.Normalize(store.Current);
        StyleOptions.Normalize(store.Current);
        return store;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string temp = AppPaths.SettingsFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Current, JsonOptions));
            File.Move(temp, AppPaths.SettingsFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error("Ayarlar kaydedilemedi.", ex);
        }
        Changed?.Invoke();
    }

    private static void TryCopy(string from, string to)
    {
        try { File.Copy(from, to, overwrite: true); }
        catch (Exception ex) { Log.Warn("Bozuk ayar dosyası yedeklenemedi.", ex); }
    }
}
