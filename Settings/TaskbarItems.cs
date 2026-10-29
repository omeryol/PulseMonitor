namespace PulseMonitor.Settings;

/// <summary>Görev çubuğundaki bir öğenin sırası ve görünürlüğü (ayarlarda liste sırası = soldan sağa).</summary>
public sealed class TaskbarItemSetting
{
    public string Id { get; set; } = string.Empty;
    public bool Visible { get; set; }
}

/// <summary>Öğe kimlikleri. Yeni öğe eklenince <see cref="DefaultOrder"/>'a da eklenir; eski ayarlara sona eklenir.</summary>
public static class TaskbarItemId
{
    public const string Weather = "Weather";
    public const string Keyboard = "Keyboard"; // Caps / Num / Scroll Lock tek sütunda
    public const string Upload = "Upload";
    public const string Download = "Download";
    public const string Cpu = "Cpu";
    public const string Ram = "Ram";
    public const string Gpu = "Gpu";
    public const string Disk = "Disk";
    public const string LocalIp = "LocalIp";
    public const string PublicIp = "PublicIp";
    public const string GpuTemp = "GpuTemp";
    public const string DiskTemp = "DiskTemp";
    public const string Ping = "Ping";

    public static readonly string[] DefaultOrder =
        [Upload, Download, Cpu, Ram, Gpu, Disk, Keyboard, Weather, LocalIp, PublicIp, GpuTemp, DiskTemp, Ping];

    /// <summary>0.4.0'daki ayrı kilit tuşu öğeleri; <see cref="Keyboard"/>'a taşınır.</summary>
    public static readonly string[] LegacyLockKeys = ["CapsLock", "NumLock", "ScrollLock"];

    public static bool DefaultVisible(string id) => id is Upload or Download or Cpu or Ram or Gpu or Disk or Keyboard or Weather;
}

internal static class TaskbarItems
{
    public static List<TaskbarItemSetting> Defaults() =>
        TaskbarItemId.DefaultOrder.Select(id => new TaskbarItemSetting { Id = id, Visible = TaskbarItemId.DefaultVisible(id) }).ToList();

    /// <summary>
    /// Ayar dosyası okunduktan sonra çağrılır: bilinmeyen/tekrarlanan öğeleri atar, eksikleri sona ekler,
    /// 0.2–0.3 sürümlerinin ShowX alanlarını yeni listeye taşır.
    /// </summary>
    public static void Normalize(AppSettings s)
    {
        var items = MergeLegacyLockKeys(s.Items ?? new List<TaskbarItemSetting>())
            .Where(i => TaskbarItemId.DefaultOrder.Contains(i.Id))
            .GroupBy(i => i.Id).Select(g => g.First())
            .ToList();
        foreach (string id in TaskbarItemId.DefaultOrder.Where(id => items.All(i => i.Id != id)))
            items.Add(new TaskbarItemSetting { Id = id, Visible = TaskbarItemId.DefaultVisible(id) });

        void Legacy(bool? value, params string[] ids)
        {
            if (value is not { } v) return;
            foreach (TaskbarItemSetting i in items.Where(i => ids.Contains(i.Id))) i.Visible = v;
        }
        Legacy(s.ShowNetwork, TaskbarItemId.Upload, TaskbarItemId.Download);
        Legacy(s.ShowCpu, TaskbarItemId.Cpu);
        Legacy(s.ShowRam, TaskbarItemId.Ram);
        Legacy(s.ShowGpu, TaskbarItemId.Gpu);
        Legacy(s.ShowDisk, TaskbarItemId.Disk);
        Legacy(s.ShowKeyboard, TaskbarItemId.Keyboard);
        Legacy(s.ShowWeather, TaskbarItemId.Weather);
        s.ShowNetwork = s.ShowCpu = s.ShowRam = s.ShowGpu = s.ShowDisk = s.ShowKeyboard = s.ShowWeather = null;

        s.Items = items;
    }

    /// <summary>Ayrı Caps/Num/Scroll öğeleri → ilkinin yerinde tek Klavye öğesi; biri açıksa açık.</summary>
    private static List<TaskbarItemSetting> MergeLegacyLockKeys(List<TaskbarItemSetting> items)
    {
        int first = items.FindIndex(i => TaskbarItemId.LegacyLockKeys.Contains(i.Id));
        if (first < 0) return items;

        var result = items.ToList();
        if (result.All(i => i.Id != TaskbarItemId.Keyboard))
        {
            bool visible = items.Any(i => TaskbarItemId.LegacyLockKeys.Contains(i.Id) && i.Visible);
            result.Insert(first, new TaskbarItemSetting { Id = TaskbarItemId.Keyboard, Visible = visible });
        }
        result.RemoveAll(i => TaskbarItemId.LegacyLockKeys.Contains(i.Id));
        return result;
    }
}
