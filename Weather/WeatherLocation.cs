using System.Text.Json.Serialization;

namespace PulseMonitor.Weather;

/// <summary>Hava durumu için seçilen nokta (köy, mahalle, ilçe...). Ayarlarda saklanır.</summary>
public sealed class WeatherLocation
{
    /// <summary>Kaynaktaki ad (ör. "Kayaköy").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Görev çubuğunda gösterilen ad; kullanıcı değiştirebilir.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>İlçe, il (ör. "Fethiye, Muğla").</summary>
    public string Context { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>"Open-Meteo" ya da "OpenStreetMap".</summary>
    public string Source { get; set; } = string.Empty;

    [JsonIgnore]
    public string ShownName => string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    /// <summary>Önbelleğin bu konuma ait olup olmadığını anlamak için.</summary>
    public bool SamePlace(double lat, double lon) =>
        Math.Abs(Latitude - lat) < 0.0005 && Math.Abs(Longitude - lon) < 0.0005;
}
