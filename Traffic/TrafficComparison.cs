namespace PulseMonitor.Traffic;

/// <summary>Seçilen dönemi, aynı uzunluktaki hemen önceki dönemle karşılaştırır.</summary>
internal readonly record struct TrafficComparison(ulong Current, ulong Previous)
{
    public double? PercentChange => Previous == 0 ? null : (Current - (double)Previous) * 100 / Previous;

    public static TrafficComparison For(TrafficHistory history, DateOnly start, DateOnly endInclusive)
    {
        int length = endInclusive.DayNumber - start.DayNumber + 1;
        if (length <= 0) return default;
        DateOnly previousEnd = start.AddDays(-1);
        DayTraffic current = TrafficPeriod.Total(history, new TrafficPeriod(start, endInclusive.AddDays(1)));
        DayTraffic previous = TrafficPeriod.Total(history, new TrafficPeriod(previousEnd.AddDays(-(length - 1)), previousEnd.AddDays(1)));
        return new TrafficComparison(TrafficPeriod.Add(current.Down, current.Up), TrafficPeriod.Add(previous.Down, previous.Up));
    }
}
