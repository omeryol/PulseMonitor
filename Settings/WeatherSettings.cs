using System.Text.Json.Serialization;
using PulseMonitor.Weather;

namespace PulseMonitor.Settings;

/// <summary>Hava durumu: tahmin modeli ve takip edilen konumlar (görev çubuğunda biri gösterilir).</summary>
public sealed class WeatherSettings
{
    public const int MaxLocations = 8;

    /// <summary>Open-Meteo tahmin modeli (<see cref="OpenMeteoModels"/>); "best_match" konuma en uygun modeli seçer.</summary>
    public string Model { get; set; } = OpenMeteoModels.Auto;

    /// <summary>Takip edilen konumlar. Değiştirirken liste yerinde değiştirilmez, yenisi atanır (servis arka planda okur).</summary>
    public List<WeatherLocation> Locations { get; set; } = new();

    /// <summary>Görev çubuğunda gösterilen konumun sırası.</summary>
    public int ActiveIndex { get; set; }

    [JsonIgnore]
    public WeatherLocation? Active
    {
        get
        {
            List<WeatherLocation> list = Locations;
            return list.Count == 0 ? null : list[Math.Clamp(ActiveIndex, 0, list.Count - 1)];
        }
    }

    /// <summary>Sıradaki konuma geçer; tek konum varsa bir şey yapmaz.</summary>
    public bool SelectNext()
    {
        if (Locations.Count < 2) return false;
        ActiveIndex = (Math.Clamp(ActiveIndex, 0, Locations.Count - 1) + 1) % Locations.Count;
        return true;
    }

    /// <summary>
    /// Açılışta: 0.12 ve öncesindeki tek konum (<see cref="AppSettings.WeatherLocation"/>) listeye taşınır;
    /// bilinmeyen model varsayılana döner.
    /// </summary>
    public static void Normalize(AppSettings s)
    {
        WeatherSettings w = s.Weather ??= new WeatherSettings();
        w.Locations ??= new List<WeatherLocation>();
        if (s.WeatherLocation is { } legacy)
        {
            if (w.Locations.Count == 0) w.Locations = [legacy];
            s.WeatherLocation = null;
        }
        if (w.Locations.Count > MaxLocations) w.Locations = w.Locations.Take(MaxLocations).ToList();
        w.ActiveIndex = w.Locations.Count == 0 ? 0 : Math.Clamp(w.ActiveIndex, 0, w.Locations.Count - 1);
        if (!OpenMeteoModels.Ids.Contains(w.Model)) w.Model = OpenMeteoModels.Auto;
    }
}
