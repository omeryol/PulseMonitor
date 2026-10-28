using System.Globalization;
using PulseMonitor.Traffic;

namespace PulseMonitor.ViewModels;

/// <summary>Geçmiş grafiğinin bir satırı (gün ya da ay).</summary>
internal readonly record struct HistoryRow(string Label, string Name, DayTraffic Traffic, bool IsCurrent, bool IsWeekend)
{
    public ulong Total => Traffic.Down + Traffic.Up;
}

/// <summary>Günlük kayıtlardan ay (günler) ve yıl (aylar) satırlarını üretir.</summary>
internal static class HistoryRows
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static List<HistoryRow> Days(Dictionary<DateOnly, DayTraffic> days, DateOnly month)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Enumerable.Range(1, DateTime.DaysInMonth(month.Year, month.Month))
            .Select(d => new DateOnly(month.Year, month.Month, d))
            .Select(d => new HistoryRow(d.Day.ToString(Tr), d.ToString("d MMMM yyyy, dddd", Tr), days.GetValueOrDefault(d),
                d == today, d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();
    }

    public static List<HistoryRow> Months(Dictionary<DateOnly, DayTraffic> days, int year)
    {
        var rows = new List<HistoryRow>();
        for (int m = 1; m <= 12; m++)
        {
            ulong down = 0, up = 0;
            foreach (var (_, t) in days.Where(kv => kv.Key.Year == year && kv.Key.Month == m))
            {
                down += t.Down;
                up += t.Up;
            }
            var first = new DateOnly(year, m, 1);
            bool current = year == DateTime.Today.Year && m == DateTime.Today.Month;
            rows.Add(new HistoryRow(first.ToString("MMM", Tr), first.ToString("MMMM yyyy", Tr), new DayTraffic(down, up), current, false));
        }
        return rows;
    }
}
