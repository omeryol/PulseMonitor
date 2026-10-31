namespace PulseMonitor.Weather;

/// <summary>Bir konumun anlık hava durumu ve önümüzdeki saatler.</summary>
public sealed class WeatherReport
{
    public DateTime FetchedAtUtc { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Raporu üreten Open-Meteo modeli; 0.12 ve öncesinin önbelleğinde boş (= otomatik).</summary>
    public string Model { get; set; } = OpenMeteoModels.Auto;

    /// <summary>Konumun UTC farkı; saatlik listede "şimdi" konumun saatine göre bulunur. Eski önbellekte yok.</summary>
    public int? UtcOffsetSeconds { get; set; }

    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public int Humidity { get; set; }
    public double WindKmh { get; set; }
    public int WeatherCode { get; set; }
    public bool IsDay { get; set; } = true;

    public List<HourForecast> Hours { get; set; } = new();

    /// <summary>Günlük tahmin (bugünden başlar); 0.13 ve öncesinin önbelleğinde boş, sonraki yenilemede dolar.</summary>
    public List<DayForecast> Days { get; set; } = new();

    /// <summary>
    /// Son 30 günün günlük en yüksek sıcaklık ortalaması ve standart sapması (Open-Meteo past_days); "son 30 güne göre"
    /// sıra dışı günleri bulmak için. İklim normali değildir. Eski önbellekte ve veri azsa yok.
    /// </summary>
    public double? RecentHighMean { get; set; }
    public double? RecentHighSd { get; set; }

    public TimeSpan Age => DateTime.UtcNow - FetchedAtUtc;

    /// <summary>Konumdaki şu anki saat (<see cref="HourForecast.Time"/> ile karşılaştırmak için).</summary>
    public DateTime LocalNow => UtcOffsetSeconds is { } s ? DateTime.UtcNow.AddSeconds(s) : DateTime.Now;
}

/// <summary>Günlük tahmin; <see cref="Date"/> konumun yerel günüdür.</summary>
public sealed class DayForecast
{
    public DateTime Date { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public int WeatherCode { get; set; }
    public int? PrecipitationProbability { get; set; }
}

/// <summary>Saatlik tahmin; <see cref="Time"/> konumun yerel saatidir.</summary>
public sealed class HourForecast
{
    public DateTime Time { get; set; }
    public double Temperature { get; set; }
    public int WeatherCode { get; set; }
    public int? PrecipitationProbability { get; set; }
    public bool IsDay { get; set; } = true;
    /// <summary>Rüzgâr hamlesi, km/sa; eski önbellekte yok.</summary>
    public double? WindGustKmh { get; set; }
}
