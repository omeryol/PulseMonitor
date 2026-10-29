namespace PulseMonitor.ViewModels;

/// <summary>
/// Görev çubuğunda bir sütun: üst üste iki satır ya da sütunu tek başına kaplayan öğe (hava, klavye).
/// <see cref="Cards"/>: fare üzerine gelince açılan ayrıntı kartları (sütundaki öğelerin). Eşitlik yalnızca
/// öğelere bakar; böylece aynı dizilimde sütunlar yeniden kurulmaz (titreme olmaz).
/// </summary>
public sealed record TaskbarColumn(object? Top, object? Bottom, object? Tall)
{
    public IReadOnlyList<DetailCard> Cards { get; init; } = Array.Empty<DetailCard>();
    public bool HasCards => Cards.Count > 0;

    public bool Equals(TaskbarColumn? other) =>
        other is not null && Equals(Top, other.Top) && Equals(Bottom, other.Bottom) && Equals(Tall, other.Tall);

    public override int GetHashCode() => HashCode.Combine(Top, Bottom, Tall);
}

/// <summary>
/// Görünür öğeleri sırayla sütunlara yerleştirir (TrafficMonitor gibi): her sütun iki satır,
/// <see cref="ITallItem"/> (hava, klavye) bir sütunu tek başına kaplar. Bir öğe gizlenince sonrakiler boşluğu doldurur.
/// </summary>
internal static class TaskbarLayout
{
    public static List<TaskbarColumn> Build(IEnumerable<(object Item, DetailCard? Card)> items)
    {
        var columns = new List<TaskbarColumn>();
        (object Item, DetailCard? Card)? pending = null;

        foreach (var entry in items)
        {
            if (entry.Item is ITallItem)
            {
                Flush();
                columns.Add(new TaskbarColumn(null, null, entry.Item) { Cards = CardsOf(entry) });
            }
            else if (pending is null)
            {
                pending = entry;
            }
            else
            {
                columns.Add(new TaskbarColumn(pending.Value.Item, entry.Item, null) { Cards = CardsOf(pending.Value, entry) });
                pending = null;
            }
        }
        Flush();
        return columns;

        void Flush()
        {
            if (pending is not { } p) return;
            columns.Add(new TaskbarColumn(p.Item, null, null) { Cards = CardsOf(p) });
            pending = null;
        }
    }

    private static IReadOnlyList<DetailCard> CardsOf(params (object Item, DetailCard? Card)[] entries) =>
        entries.Select(e => e.Card).OfType<DetailCard>().ToList();
}
