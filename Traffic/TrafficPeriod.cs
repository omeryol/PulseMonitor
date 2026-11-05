namespace PulseMonitor.Traffic;

/// <summary>Aylık kota dönemi. Başlangıç günü 1–28 ile sınırlıdır; kısa aylarda sürpriz oluşmaz.</summary>
public readonly record struct TrafficPeriod(DateOnly Start, DateOnly EndExclusive)
{
    public static TrafficPeriod For(DateOnly date, int startDay)
    {
        int day = Math.Clamp(startDay, 1, 28);
        DateOnly start = date.Day >= day ? new DateOnly(date.Year, date.Month, day) : new DateOnly(date.Year, date.Month, day).AddMonths(-1);
        return new TrafficPeriod(start, start.AddMonths(1));
    }

    internal static DayTraffic Total(TrafficHistory history, TrafficPeriod period) => Total(history.Snapshot(), period);

    public static DayTraffic Total(IEnumerable<KeyValuePair<DateOnly, DayTraffic>> days, TrafficPeriod period)
    {
        ulong down = 0, up = 0;
        foreach (var (day, traffic) in days.Where(x => x.Key >= period.Start && x.Key < period.EndExclusive))
        {
            down = Add(down, traffic.Down);
            up = Add(up, traffic.Up);
        }
        return new DayTraffic(down, up);
    }

    public static ulong Add(ulong first, ulong second) => ulong.MaxValue - first < second ? ulong.MaxValue : first + second;
}
