using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Traffic;

namespace PulseMonitor.ViewModels;

/// <summary>
/// Trafik geçmişi penceresi: ay görünümünde günler, yıl görünümünde aylar. Çubuk yükseklikleri, değer çizgileri ve
/// ortalama çizgisi piksel olarak hesaplanır (grafik yüksekliği <see cref="ChartHeight"/>).
/// </summary>
public sealed class HistoryViewModel : ObservableObject
{
    private static CultureInfo Tr => UiLanguage.Culture;
    public const double ChartHeight = 220;
    private static readonly CornerRadius TopRounded = new(4, 4, 0, 0);

    private readonly TrafficHistory _history;
    private DateOnly _period = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _yearView;
    private string _title = "", _down = "", _up = "", _average = "", _peak = "", _peakName = "", _averageLabel = "";
    private double _averageOffset;

    internal HistoryViewModel(TrafficHistory history)
    {
        _history = history;
        PreviousCommand = new RelayCommand(() => Move(-1));
        NextCommand = new RelayCommand(() => Move(+1));
        TodayCommand = new RelayCommand(() =>
        {
            _period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            Refresh();
        });
        Refresh();
    }

    public ObservableCollection<HistoryBar> Bars { get; } = new();
    public ObservableCollection<GridLine> GridLines { get; } = new();
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand TodayCommand { get; }
    public double Height => ChartHeight;

    public bool YearView
    {
        get => _yearView;
        set
        {
            if (Set(ref _yearView, value)) Refresh();
        }
    }

    public string Title { get => _title; private set => Set(ref _title, value); }
    public string DownTotal { get => _down; private set => Set(ref _down, value); }
    public string UpTotal { get => _up; private set => Set(ref _up, value); }
    public string Average { get => _average; private set => Set(ref _average, value); }
    public string AverageCaption => Strings.Get(YearView ? "History.AveragePerMonth" : "History.AveragePerDay");
    public string Peak { get => _peak; private set => Set(ref _peak, value); }
    public string PeakName { get => _peakName; private set => Set(ref _peakName, value); }
    public double AverageOffset { get => _averageOffset; private set => Set(ref _averageOffset, value); }
    public string AverageLabel { get => _averageLabel; private set => Set(ref _averageLabel, value); }

    private void Move(int step)
    {
        _period = YearView ? _period.AddYears(step) : _period.AddMonths(step);
        Refresh();
    }

    private void Refresh()
    {
        var days = _history.Snapshot().ToDictionary(kv => kv.Key, kv => kv.Value);
        List<HistoryRow> rows = YearView ? HistoryRows.Months(days, _period.Year) : HistoryRows.Days(days, _period);
        Title = YearView ? _period.Year.ToString(Tr) : _period.ToString("MMMM yyyy", Tr);

        ulong down = 0, up = 0;
        foreach (HistoryRow r in rows) { down += r.Traffic.Down; up += r.Traffic.Up; }
        int used = rows.Count(r => r.Total > 0);
        double average = used == 0 ? 0 : (down + up) / (double)used;
        double scale = NiceMax(Math.Max(rows.Max(r => (double)r.Total), 1));

        Bars.Clear();
        foreach (HistoryRow r in rows)
        {
            double upH = r.Traffic.Up * ChartHeight / scale, downH = r.Traffic.Down * ChartHeight / scale;
            Bars.Add(new HistoryBar(r.Label, downH, upH, upH > 0.5 ? new CornerRadius(0) : TopRounded,
                r.IsCurrent, r.IsWeekend,
                $"{r.Name}\n↓ {ValueFormat.Size(r.Traffic.Down)}   ↑ {ValueFormat.Size(r.Traffic.Up)}\n{Strings.Get("History.Sum")} {ValueFormat.Size(r.Total)}"));
        }

        GridLines.Clear();
        for (int i = 1; i <= 4; i++)
            GridLines.Add(new GridLine(ChartHeight * i / 4, ValueFormat.SizeCompact(scale * i / 4)));

        DownTotal = ValueFormat.Size(down);
        UpTotal = ValueFormat.Size(up);
        Average = used == 0 ? "—" : ValueFormat.Size(average);
        AverageOffset = average * ChartHeight / scale;
        AverageLabel = used == 0 ? "" : string.Format(Tr, Strings.Get("History.AverageLine"), ValueFormat.Size(average));
        HistoryRow peak = rows.MaxBy(r => r.Total);
        Peak = peak.Total == 0 ? "—" : ValueFormat.Size(peak.Total);
        PeakName = peak.Total == 0 ? Strings.Get("History.Empty") : peak.Name;
        Raise(nameof(AverageCaption));
    }

    /// <summary>Ekseni okunur bir üst değere yuvarlar (ör. 7,6 GB → 8 GB), değer çizgileri düzgün sayılar olsun.</summary>
    private static double NiceMax(double value)
    {
        double magnitude = Math.Pow(2, Math.Floor(Math.Log2(value)));
        foreach (double f in new[] { 1, 1.25, 1.5, 2 })
            if (magnitude * f >= value) return magnitude * f;
        return magnitude * 2;
    }
}

/// <summary>Grafikte bir çubuk: indirme (altta) ve yükleme (üstte) yükseklikleri piksel olarak.</summary>
public sealed record HistoryBar(string Label, double DownHeight, double UpHeight, CornerRadius DownCorners,
    bool IsCurrent, bool IsWeekend, string Tooltip);

/// <summary>Yatay değer çizgisi: alttan uzaklık (piksel) ve etiketi.</summary>
public sealed record GridLine(double Offset, string Label);
