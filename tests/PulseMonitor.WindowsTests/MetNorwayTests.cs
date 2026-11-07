using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.WindowsTests;

public sealed class MetNorwayTests
{
    private static readonly TimeZoneInfo Istanbul = WeatherTimeZone.FromName("Europe/Istanbul", 3 * 3600);
    private static readonly DateTime NowUtc = new(2026, 11, 8, 9, 20, 0, DateTimeKind.Utc);
    private const double Lat = 41.0, Lon = 29.0;

    [Theory]
    [InlineData("clearsky_day", 0)]
    [InlineData("clearsky_night", 0)]
    [InlineData("fair_polartwilight", 1)]
    [InlineData("partlycloudy_day", 2)]
    [InlineData("cloudy", 3)]
    [InlineData("fog", 45)]
    [InlineData("lightrain", 61)]
    [InlineData("heavyrain", 65)]
    [InlineData("rainshowers_night", 81)]
    [InlineData("sleet", 67)]
    [InlineData("lightsnow", 71)]
    [InlineData("heavysnowshowers_day", 86)]
    [InlineData("rainandthunder", 95)]
    [InlineData("heavysnowshowersandthunder_night", 95)]
    public void SymbolCodesMapToWmo(string symbol, int wmo) => Assert.Equal(wmo, MetNorwaySymbols.ToWmo(symbol));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tornado")]
    public void UnknownSymbolsAreNull(string? symbol) => Assert.Null(MetNorwaySymbols.ToWmo(symbol));

    [Theory]
    [InlineData(9, true)]   // 12:00 yerel
    [InlineData(2, false)]  // 05:00 yerel
    [InlineData(18, false)] // 21:00 yerel
    public void SunPositionFollowsLocalDaylight(int utcHour, bool day) =>
        Assert.Equal(day, SunPosition.IsDaytime(Lat, Lon, new DateTime(2026, 11, 8, utcHour, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public void SunPositionHandlesPolarDayAndNight()
    {
        Assert.True(SunPosition.IsDaytime(80, 15, new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc)));
        Assert.False(SunPosition.IsDaytime(80, 15, new DateTime(2026, 12, 21, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void TimeZoneUsesIanaNameThenFixedOffsetThenLongitude()
    {
        Assert.Equal(TimeSpan.FromHours(3), Istanbul.GetUtcOffset(NowUtc));
        Assert.Equal(TimeSpan.FromHours(5), WeatherTimeZone.FromName("Nowhere/Unknown", 5 * 3600).GetUtcOffset(NowUtc));
        Assert.Equal(TimeSpan.FromHours(2), WeatherTimeZone.Approximate(29).GetUtcOffset(NowUtc));
    }

    [Fact]
    public void ParseBuildsCurrentHoursAndDays()
    {
        WeatherReport r = Parse(Series());

        Assert.Equal(WeatherSources.MetNorway, r.Model);
        Assert.Equal(3 * 3600, r.UtcOffsetSeconds);
        Assert.Equal(10.0, r.Temperature);                    // 09:00Z noktası (şimdi 09:20Z)
        Assert.Equal(8.0, r.ApparentTemperature);
        Assert.Equal(60, r.Humidity);
        Assert.Equal(5.0 * 3.6, r.WindKmh, 6);
        Assert.Equal(61, r.WeatherCode);
        Assert.True(r.IsDay);
        Assert.Null(r.RecentHighMean);

        Assert.Equal(13, r.Hours.Count);
        Assert.Equal(new DateTime(2026, 11, 8, 12, 0, 0), r.Hours[0].Time); // UTC+3
        Assert.Null(r.Hours[0].PrecipitationProbability);
        Assert.Equal(7.0 * 3.6, r.Hours[0].WindGustKmh!.Value, 6);

        Assert.Equal(5, r.Days.Count);
        Assert.Equal(new DateTime(2026, 11, 8), r.Days[0].Date);
        Assert.Equal(new DateTime(2026, 11, 9), r.Days[1].Date);
    }

    [Fact]
    public void DayRangeUsesMeasurementsAndSixHourExtremes()
    {
        WeatherReport r = Parse(Series());
        DayForecast tomorrow = r.Days[1];
        // 9 Kasım yerel günü: ölçümler 10 + saat*0 + ... ; 6 saatlik uç değerler sınırı genişletir.
        Assert.Equal(25.0, tomorrow.High);
        Assert.Equal(-3.0, tomorrow.Low);
    }

    [Fact]
    public void EmptyOrUnrecognizedSeriesIsRejected()
    {
        Assert.Throws<InvalidDataException>(() => Parse("""{"properties":{"timeseries":[]}}"""));
        Assert.Throws<InvalidDataException>(() => Parse("""{"properties":{}}"""));
        Assert.Throws<InvalidDataException>(() => Parse(Series(symbol: "tornado")));
    }

    [Fact]
    public void SettingsKeepMetNorwayAndResetUnknownModels()
    {
        var settings = new AppSettings { Weather = new WeatherSettings { Model = WeatherSources.MetNorway } };
        WeatherSettings.Normalize(settings);
        Assert.Equal(WeatherSources.MetNorway, settings.Weather.Model);

        settings.Weather.Model = "bogus";
        WeatherSettings.Normalize(settings);
        Assert.Equal(OpenMeteoModels.Auto, settings.Weather.Model);
    }

    private static WeatherReport Parse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        return MetNorwayClient.Parse(doc.RootElement, Lat, Lon, Istanbul, NowUtc);
    }

    /// <summary>Saatlik (ilk 40 saat) sonra 6 saatlik noktalardan oluşan örnek yanıt; 08.11.2026 08:00Z'den başlar.</summary>
    private static string Series(string symbol = "lightrain_day")
    {
        var sb = new StringBuilder("{\"properties\":{\"timeseries\":[");
        DateTime t = new(2026, 11, 8, 8, 0, 0, DateTimeKind.Utc);
        DateTime extremes = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc); // yerel 9 Kasım: uç değerler burada
        string sym = "\"summary\":{\"symbol_code\":\"" + symbol + "\"}";
        for (int i = 0; i < 70; i++)
        {
            bool hourly = i < 40;
            sb.Append("{\"time\":\"").Append(t.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)).Append('"')
              .Append(",\"data\":{\"instant\":{\"details\":{\"air_temperature\":10,\"apparent_air_temperature\":8,")
              .Append("\"relative_humidity\":60,\"wind_speed\":5,\"wind_speed_of_gust\":7}}");
            if (hourly) sb.Append(",\"next_1_hours\":{").Append(sym).Append(",\"details\":{\"precipitation_amount\":0.1}}");
            if (!hourly || i % 6 == 0 || t == extremes)
            {
                (int max, int min) = t == extremes ? (25, -3) : (11, 9);
                sb.Append(",\"next_6_hours\":{").Append(sym).Append(",\"details\":{\"air_temperature_max\":").Append(max)
                  .Append(",\"air_temperature_min\":").Append(min).Append("}}");
            }
            sb.Append("}}");
            if (i < 69) sb.Append(',');
            t = t.AddHours(hourly ? 1 : 6);
        }
        return sb.Append("]}}").ToString();
    }
}
