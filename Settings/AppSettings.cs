using System.Text.Json.Serialization;
using PulseMonitor.Weather;

namespace PulseMonitor.Settings;

/// <summary>Kalıcı kullanıcı ayarları. Yeni alanlar eklenince eski dosyalar varsayılanla açılır.</summary>
public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;

    /// <summary>Arayüz dili: "auto" (Windows diline göre) ya da lang klasöründeki bir dosyanın kodu ("tr", "en"…).</summary>
    public string Language { get; set; } = "auto";

    /// <summary>Tam ekran uygulama (oyun, video, sunum) açıkken gizlen.</summary>
    public bool HideInFullscreen { get; set; } = true;

    /// <summary>Pencere ile sistem tepsisi arasındaki boşluk (96 DPI'da piksel).</summary>
    public int TrayGapPx { get; set; } = 0;

    /// <summary>Ölçülen ağ bağdaştırıcısının GUID'i; boşsa tüm bağlı fiziksel bağdaştırıcılar.</summary>
    public string? NetworkAdapterId { get; set; }

    /// <summary>
    /// Görev çubuğu öğeleri: sıra (soldan sağa) ve görünürlük. Değiştirirken liste yerinde değiştirilmez,
    /// yenisi atanır; arka plan ölçüm döngüsü bu listeyi okur.
    /// </summary>
    public List<TaskbarItemSetting> Items { get; set; } = TaskbarItems.Defaults();

    /// <summary>Hava durumu: kaynak, model, konumlar; konum yoksa hava durumu gösterilmez.</summary>
    public WeatherSettings Weather { get; set; } = new();

    /// <summary>Görünüm (renk, yazı, işaretler, zemin, hava simgeleri…).</summary>
    public StyleSettings Style { get; set; } = new();

    /// <summary>Ping öğesinin sunucusu (ad ya da IP).</summary>
    public string PingHost { get; set; } = "1.1.1.1";

    /// <summary>Uyarı bildirimleri.</summary>
    public AlertSettings Alerts { get; set; } = new();

    /// <summary>GitHub'da yeni sürüm denetimi (günde bir).</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Bildirimi gösterilmiş son sürüm (aynı sürüm için tekrar bildirim yok).</summary>
    public string? LastNotifiedVersion { get; set; }

    public bool IsShown(string id)
    {
        List<TaskbarItemSetting> items = Items;
        return items.Any(i => i.Id == id && i.Visible);
    }

    // 0.2–0.3 sürümlerinin alanları: yalnızca okunur, TaskbarItems.Normalize ile Items'a taşınıp silinir.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowNetwork { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowCpu { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowRam { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowGpu { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowDisk { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowKeyboard { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowWeather { get; set; }

    // 0.3–0.12 sürümlerinin tek konumu: WeatherSettings.Normalize ile Weather.Locations'a taşınıp silinir.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public WeatherLocation? WeatherLocation { get; set; }
}
