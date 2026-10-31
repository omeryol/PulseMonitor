namespace PulseMonitor.Weather.Warnings;

public enum WarningKind
{
    Thunder, Hail, FreezingRain, Gust, Heat, Cold,
    HeavyRain, HeavySnow, Drop, Rise, DayDrop, DayRise,
    AnomalyWarm, AnomalyCool,
}

/// <summary>
/// Tahmin modelinden çıkan bir uyarı. <see cref="MinLevel"/>: bu uyarıyı gösteren en düşük seviye
/// (1 yalnızca aşırı, 2 dengeli, 3 hassas). <see cref="At"/>: olayın saati (günlük olaylarda günün tarihi).
/// <see cref="Value"/>: rüzgâr hızı, sıcaklık ya da sıcaklık farkı (°C).
/// </summary>
public sealed record WeatherWarning(WarningKind Kind, int MinLevel, DateTime At, double Value = 0)
{
    /// <summary>"Son 30 güne göre" sıra dışı günler (hassas seviye) yalnızca kartta görünür, bildirim çıkarmaz.</summary>
    public bool Notify => Kind is not (WarningKind.AnomalyWarm or WarningKind.AnomalyCool);

    /// <summary>
    /// Uyarının yanındaki simge: seçili simge setindeki hava simgesi adı (Weather/WeatherCodes.cs → IconKey ile aynı adlar).
    /// Sıcaklık değişimleri için karşılığı olan simge seçilir; setlerde rüzgâr simgesi olmadığından rüzgâr "Cloudy" gösterir.
    /// </summary>
    public string IconKey => Kind switch
    {
        WarningKind.Thunder or WarningKind.Hail => "Thunder",
        WarningKind.FreezingRain or WarningKind.HeavyRain => "Rain",
        WarningKind.HeavySnow or WarningKind.Cold => "Snow",
        WarningKind.Heat or WarningKind.Rise or WarningKind.DayRise or WarningKind.AnomalyWarm => "ClearDay",
        _ => "Cloudy", // rüzgâr, sıcaklık düşüşü, serin gün
    };

    /// <summary>Aynı olay için tekrar bildirim çıkmasın diye: tür + olayın günü.</summary>
    public string Key => $"{Kind}|{At:yyyy-MM-dd}";
}
