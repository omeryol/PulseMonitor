namespace PulseMonitor.Weather;

/// <summary>Bir konumun anlık hava durumu ve önümüzdeki saatler.</summary>
public sealed class WeatherReport
{
    public DateTime FetchedAtUtc { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public int Humidity { get; set; }
    public double WindKmh { get; set; }
    public int WeatherCode { get; set; }
    public bool IsDay { get; set; } = true;

    public List<HourForecast> Hours { get; set; } = new();

    public TimeSpan Age => DateTime.UtcNow - FetchedAtUtc;
}

/// <summary>Saatlik tahmin; <see cref="Time"/> konumun yerel saatidir.</summary>
public sealed class HourForecast
{
    public DateTime Time { get; set; }
    public double Temperature { get; set; }
    public int WeatherCode { get; set; }
    public int? PrecipitationProbability { get; set; }
    public bool IsDay { get; set; } = true;
}
