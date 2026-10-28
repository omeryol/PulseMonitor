namespace PulseMonitor.ViewModels;

/// <summary>Görev çubuğunda bir sütun: üst üste iki satır ya da sütunu tek başına kaplayan öğe (hava, klavye).</summary>
public sealed record TaskbarColumn(object? Top, object? Bottom, object? Tall);

/// <summary>
/// Görünür öğeleri sırayla sütunlara yerleştirir (TrafficMonitor gibi): her sütun iki satır,
/// <see cref="ITallItem"/> (hava, klavye) bir sütunu tek başına kaplar. Bir öğe gizlenince sonrakiler boşluğu doldurur.
/// </summary>
internal static class TaskbarLayout
{
    public static List<TaskbarColumn> Build(IEnumerable<object> items)
    {
        var columns = new List<TaskbarColumn>();
        object? pending = null;

        foreach (object item in items)
        {
            if (item is ITallItem)
            {
                Flush();
                columns.Add(new TaskbarColumn(null, null, item));
            }
            else if (pending is null)
            {
                pending = item;
            }
            else
            {
                columns.Add(new TaskbarColumn(pending, item, null));
                pending = null;
            }
        }
        Flush();
        return columns;

        void Flush()
        {
            if (pending is null) return;
            columns.Add(new TaskbarColumn(pending, null, null));
            pending = null;
        }
    }
}
