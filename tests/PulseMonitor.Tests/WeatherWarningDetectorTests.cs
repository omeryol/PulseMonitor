using PulseMonitor.Tests;
using PulseMonitor.Weather;
using PulseMonitor.Weather.Warnings;

namespace PulseMonitor.Tests;

/// <summary>Sert hava algılayıcısı: hangi olay hangi seviyede görünür, hangi koşullar sayılmaz.</summary>
public class WeatherWarningDetectorTests
{
    private static int Count(WeatherReport r, int level, WarningKind kind) =>
        WeatherWarningDetector.Detect(r, level).Count(w => w.Kind == kind);

    private static WeatherReport WithHours(Action<int, HourForecast> change) => ReportBuilder.Calm(hour: change);

    [Fact]
    public void Calm_weather_has_no_warnings_at_any_level()
    {
        for (int level = 0; level <= 3; level++)
            Assert.Empty(WeatherWarningDetector.Detect(ReportBuilder.Calm(), level));
    }

    [Fact]
    public void Level_zero_is_off_even_in_a_storm()
    {
        var storm = WithHours((i, h) => { h.WeatherCode = 95; h.PrecipitationProbability = 80; });
        Assert.Empty(WeatherWarningDetector.Detect(storm, 0));
    }

    [Fact]
    public void Thunderstorm_is_found_once_at_its_first_hour()
    {
        var r = WithHours((i, h) => { if (i is 4 or 5) { h.WeatherCode = 95; h.PrecipitationProbability = 70; } });
        var found = WeatherWarningDetector.Detect(r, 1);
        Assert.Single(found);
        Assert.Equal(WarningKind.Thunder, found[0].Kind);
        Assert.Equal(ReportBuilder.Now.AddHours(4), found[0].At);
    }

    [Fact]
    public void Low_probability_thunderstorm_is_model_noise_and_ignored()
    {
        var r = WithHours((i, h) => { if (i == 4) { h.WeatherCode = 95; h.PrecipitationProbability = 20; } });
        Assert.Equal(0, Count(r, 3, WarningKind.Thunder));
    }

    [Fact]
    public void Hail_and_freezing_rain_are_extreme()
    {
        var hail = WithHours((i, h) => { if (i == 3) { h.WeatherCode = 96; h.PrecipitationProbability = 60; } });
        var ice = WithHours((i, h) => { if (i == 3) { h.WeatherCode = 67; h.PrecipitationProbability = 60; } });
        Assert.Equal(1, Count(hail, 1, WarningKind.Hail));
        Assert.Equal(1, Count(ice, 1, WarningKind.FreezingRain));
    }

    [Theory]
    [InlineData(65, WarningKind.HeavyRain)]
    [InlineData(82, WarningKind.HeavyRain)]
    [InlineData(75, WarningKind.HeavySnow)]
    [InlineData(86, WarningKind.HeavySnow)]
    public void Heavy_precipitation_needs_balanced_level(int code, WarningKind kind)
    {
        var r = WithHours((i, h) => { if (i == 2) { h.WeatherCode = code; h.PrecipitationProbability = 90; } });
        Assert.Equal(0, Count(r, 1, kind));
        Assert.Equal(1, Count(r, 2, kind));
    }

    [Theory]
    [InlineData(100, 1, 1)] // aşırı: en düşük seviyede de görünür
    [InlineData(70, 1, 0)]  // güçlü: yalnızca dengeli ve üstünde
    [InlineData(70, 2, 1)]
    [InlineData(50, 3, 0)]  // eşik altı: hiçbir seviyede
    public void Wind_gusts_follow_their_thresholds(double kmh, int level, int expected)
    {
        var r = WithHours((i, h) => { if (i == 6) h.WindGustKmh = kmh; });
        Assert.Equal(expected, Count(r, level, WarningKind.Gust));
    }

    [Fact]
    public void Missing_gust_data_is_not_an_error()
    {
        var r = WithHours((i, h) => h.WindGustKmh = null);
        Assert.Empty(WeatherWarningDetector.Detect(r, 3));
    }

    [Fact]
    public void Extreme_heat_and_cold_only_look_at_today_and_tomorrow()
    {
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[1].High = 41), 1, WarningKind.Heat));
        Assert.Equal(0, Count(ReportBuilder.Calm(r => r.Days[3].High = 41), 3, WarningKind.Heat));
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[0].Low = -12), 1, WarningKind.Cold));
    }

    [Fact]
    public void Sudden_temperature_swing_is_balanced_level_and_direction_aware()
    {
        var drop = WithHours((i, h) => h.Temperature = 20 - i);
        var rise = WithHours((i, h) => h.Temperature = 5 + i);
        var gentle = WithHours((i, h) => h.Temperature = 20 - i * 0.3);
        Assert.Equal(1, Count(drop, 2, WarningKind.Drop));
        Assert.Equal(0, Count(drop, 1, WarningKind.Drop));
        Assert.Equal(1, Count(rise, 2, WarningKind.Rise));
        Assert.Equal(0, Count(gentle, 3, WarningKind.Drop));
    }

    [Fact]
    public void Tomorrow_much_colder_or_warmer_than_today_is_found()
    {
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[1].High = 8), 2, WarningKind.DayDrop));
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[1].High = 31), 2, WarningKind.DayRise));
    }

    [Fact]
    public void Unusual_day_needs_both_six_degrees_and_one_and_a_half_deviations()
    {
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[1].High = 27), 3, WarningKind.AnomalyWarm));
        Assert.Equal(0, Count(ReportBuilder.Calm(r => r.Days[1].High = 27), 2, WarningKind.AnomalyWarm)); // seviye 2'de yok
        Assert.Equal(0, Count(ReportBuilder.Calm(r => r.Days[1].High = 24), 3, WarningKind.AnomalyWarm)); // 4 °C
        Assert.Equal(0, Count(ReportBuilder.Calm(r => { r.RecentHighSd = 6; r.Days[1].High = 27; }), 3, WarningKind.AnomalyWarm)); // 1,5 sapma = 9 °C
        Assert.Equal(1, Count(ReportBuilder.Calm(r => r.Days[1].High = 12), 3, WarningKind.AnomalyCool));
    }

    [Fact]
    public void Without_thirty_day_data_there_is_no_unusual_day()
    {
        var r = ReportBuilder.Calm(x => { x.RecentHighMean = null; x.RecentHighSd = null; x.Days[1].High = 30; });
        Assert.Equal(0, Count(r, 3, WarningKind.AnomalyWarm));
    }

    [Fact]
    public void Unusual_day_never_notifies_but_storm_does()
    {
        var unusual = WeatherWarningDetector.Detect(ReportBuilder.Calm(r => r.Days[1].High = 27), 3).Single();
        Assert.False(unusual.Notify);
        var storm = WithHours((i, h) => { if (i == 2) { h.WeatherCode = 95; h.PrecipitationProbability = 90; } });
        Assert.True(WeatherWarningDetector.Detect(storm, 1).Single().Notify);
    }

    [Fact]
    public void Most_severe_comes_first_and_unusual_day_last()
    {
        var r = ReportBuilder.Calm(x => x.Days[1].High = 27, (i, h) => { if (i == 5) { h.WeatherCode = 95; h.PrecipitationProbability = 90; h.WindGustKmh = 95; } });
        var list = WeatherWarningDetector.Detect(r, 3);
        Assert.Equal(1, list[0].MinLevel);
        Assert.Equal(WarningKind.AnomalyWarm, list[^1].Kind);
    }

    [Fact]
    public void Past_hours_are_ignored()
    {
        var old = ReportBuilder.Calm(r => r.Hours.ForEach(h => { h.Time = h.Time.AddDays(-2); h.WeatherCode = 95; h.PrecipitationProbability = 90; }));
        Assert.Equal(0, Count(old, 3, WarningKind.Thunder));
    }

    [Theory]
    [InlineData(WarningKind.Thunder, "Thunder")]
    [InlineData(WarningKind.HeavyRain, "Rain")]
    [InlineData(WarningKind.HeavySnow, "Snow")]
    [InlineData(WarningKind.Heat, "ClearDay")]
    [InlineData(WarningKind.Gust, "Cloudy")]
    public void Each_kind_maps_to_a_weather_icon(WarningKind kind, string icon) =>
        Assert.Equal(icon, new WeatherWarning(kind, 1, DateTime.Now).IconKey);
}
