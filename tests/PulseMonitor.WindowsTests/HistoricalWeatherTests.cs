using System.Text.Json;
using System.IO;
using PulseMonitor.Weather;

namespace PulseMonitor.WindowsTests;

public sealed class HistoricalWeatherTests
{
    [Fact]
    public void RangeRejectsFutureAndOversizedRequests()
    {
        DateTime yesterday = DateTime.Today.AddDays(-1);
        Assert.True(HistoricalWeatherRange.TryCreate(yesterday.AddDays(-29), yesterday, out var valid));
        Assert.Equal(30, valid.End.DayNumber - valid.Start.DayNumber + 1);
        Assert.False(HistoricalWeatherRange.TryCreate(yesterday.AddDays(-366), yesterday, out _));
        Assert.False(HistoricalWeatherRange.TryCreate(yesterday, DateTime.Today, out _));
    }

    [Fact]
    public void ParserRejectsMismatchedArraysAndSkipsInvalidRows()
    {
        using var bad = JsonDocument.Parse("""{"daily":{"time":["2026-01-01"],"temperature_2m_max":[10,11],"temperature_2m_min":[2],"precipitation_sum":[1],"weather_code":[0]}}""");
        Assert.Throws<InvalidDataException>(() => OpenMeteoClient.ParseHistory(bad.RootElement));

        using var valid = JsonDocument.Parse("""{"daily":{"time":["2026-01-01","bad"],"temperature_2m_max":[10,11],"temperature_2m_min":[2,3],"precipitation_sum":[1.5,-2],"weather_code":[0,3]}}""");
        HistoricalWeatherReport report = OpenMeteoClient.ParseHistory(valid.RootElement);
        var day = Assert.Single(report.Days);
        Assert.Equal(new DateOnly(2026, 1, 1), day.Date);
        Assert.Equal(1.5, day.PrecipitationMm);
    }
}
