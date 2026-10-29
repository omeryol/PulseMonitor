namespace PulseMonitor.Traffic;

public readonly record struct DayTraffic(ulong Down, ulong Up);

/// <summary>Günlük toplam trafik (bayt). İş parçacığı güvenlidir.</summary>
internal sealed class TrafficHistory
{
    private readonly object _sync = new();
    private readonly SortedDictionary<DateOnly, DayTraffic> _days = new();

    public void Add(DateOnly day, ulong down, ulong up)
    {
        if (down == 0 && up == 0) return;
        lock (_sync)
        {
            DayTraffic t = _days.GetValueOrDefault(day);
            _days[day] = new DayTraffic(t.Down + down, t.Up + up);
        }
    }

    /// <summary>Dosyadan yüklemede kullanılır: aynı gün iki kez geçerse büyük olan alınır (çift sayım olmaz).</summary>
    public void MergeMax(DateOnly day, DayTraffic other)
    {
        lock (_sync)
        {
            DayTraffic t = _days.GetValueOrDefault(day);
            _days[day] = new DayTraffic(Math.Max(t.Down, other.Down), Math.Max(t.Up, other.Up));
        }
    }

    public DayTraffic Get(DateOnly day)
    {
        lock (_sync) return _days.GetValueOrDefault(day);
    }

    public List<KeyValuePair<DateOnly, DayTraffic>> Snapshot()
    {
        lock (_sync) return _days.ToList();
    }

    public int DayCount
    {
        get { lock (_sync) return _days.Count; }
    }
}
