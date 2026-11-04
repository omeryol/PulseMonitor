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
    private DateTime? _rangeStart = DateTime.Today.AddDays(-29), _rangeEnd = DateTime.Today;
    private DateOnly _appliedStart, _appliedEnd;
    private bool _rangeView;
    private string _title = "", _down = "", _up = "", _average = "", _peak = "", _peakName = "", _averageLabel = "";
    private double _averageOffset;
    private HistoryChartOption? _selectedChart;
    private int _trafficVersion;

    internal HistoryViewModel(TrafficHistory history)
    {
        _history = history;
        PreviousCommand = new RelayCommand(() => Move(-1));
        NextCommand = new RelayCommand(() => Move(+1));
        TodayCommand = new RelayCommand(() =>
        {
            _rangeView = false;
            _period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            Refresh();
        });
        ChartTypes =
        [
            new(HistoryChartType.StackedColumns, "History.Chart.Stacked"),
            new(HistoryChartType.GroupedColumns, "History.Chart.Grouped"),
            new(HistoryChartType.Lines, "History.Chart.Line"),
            new(HistoryChartType.Areas, "History.Chart.Area"),
            new(HistoryChartType.Dots, "History.Chart.Dot"),
            new(HistoryChartType.Heatmap, "History.Chart.Heatmap"),
        ];
        _selectedChart = ChartTypes[0];
        Refresh();
    }

    public ObservableCollection<HistoryBar> Bars { get; } = new();
    public ObservableCollection<GridLine> GridLines { get; } = new();
    public IReadOnlyList<HistoryChartOption> ChartTypes { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand TodayCommand { get; }
    public DateTime? RangeStart { get => _rangeStart; set => Set(ref _rangeStart, value); }
    public DateTime? RangeEnd { get => _rangeEnd; set => Set(ref _rangeEnd, value); }
    public HistoryChartOption? SelectedChart
    {
        get => _selectedChart;
        set
        {
            if (value is null || !Set(ref _selectedChart, value)) return;
            Raise(nameof(ChartType));
        }
    }
    public HistoryChartType ChartType => SelectedChart?.Type ?? HistoryChartType.StackedColumns;
    public int TrafficVersion { get => _trafficVersion; private set => Set(ref _trafficVersion, value); }

    internal bool ApplyRange()
    {
        if (RangeStart is not { } start || RangeEnd is not { } end || end.Date < start.Date || (end.Date - start.Date).TotalDays > 365) return false;
        _appliedStart = DateOnly.FromDateTime(start);
        _appliedEnd = DateOnly.FromDateTime(end);
        _rangeView = true;
        Refresh();
        return true;
    }

    internal void SelectPeriodView(bool year)
    {
        _rangeView = false;
        _yearView = year;
        Raise(nameof(YearView));
        Refresh();
    }

    internal List<KeyValuePair<DateOnly, DayTraffic>> ExportRows()
    {
        var start = _rangeView ? _appliedStart : YearView ? new DateOnly(_period.Year, 1, 1) : _period;
        var end = _rangeView ? _appliedEnd : YearView ? new DateOnly(_period.Year, 12, 31) : _period.AddMonths(1).AddDays(-1);
        return _history.Snapshot().Where(x => x.Key >= start && x.Key <= end).ToList();
    }
    public double Height => ChartHeight;

    public bool YearView
    {
        get => _yearView;
        set
        {
            if (Set(ref _yearView, value)) { _rangeView = false; Refresh(); }
        }
    }

    public string Title { get => _title; private set => Set(ref _title, value); }
    public string DownTotal { get => _down; private set => Set(ref _down, value); }
    public string UpTotal { get => _up; private set => Set(ref _up, value); }
    public string Average { get => _average; private set => Set(ref _average, value); }
    public string AverageCaption => Strings.Get(YearView && !_rangeView ? "History.AveragePerMonth" : "History.AveragePerDay");
    public string Peak { get => _peak; private set => Set(ref _peak, value); }
    public string PeakName { get => _peakName; private set => Set(ref _peakName, value); }
    public double AverageOffset { get => _averageOffset; private set => Set(ref _averageOffset, value); }
    public string AverageLabel { get => _averageLabel; private set => Set(ref _averageLabel, value); }

    private void Move(int step)
    {
        _rangeView = false;
        _period = YearView ? _period.AddYears(step) : _period.AddMonths(step);
        Refresh();
    }

    private void Refresh()
    {
        var days = _history.Snapshot().ToDictionary(kv => kv.Key, kv => kv.Value);
        List<HistoryRow> rows = _rangeView ? HistoryRows.Range(days, _appliedStart, _appliedEnd) : YearView ? HistoryRows.Months(days, _period.Year) : HistoryRows.Days(days, _period);
        Title = _rangeView ? $"{_appliedStart.ToString("d", Tr)} – {_appliedEnd.ToString("d", Tr)}" : YearView ? _period.Year.ToString(Tr) : _period.ToString("MMMM yyyy", Tr);

        ulong down = 0, up = 0;
        foreach (HistoryRow r in rows) { down += r.Traffic.Down; up += r.Traffic.Up; }
        int used = _rangeView ? rows.Count : rows.Count(r => r.Total > 0);
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
        TrafficVersion++;
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

public enum HistoryChartType { StackedColumns, GroupedColumns, Lines, Areas, Dots, Heatmap }

public sealed class HistoryChartOption
{
    public HistoryChartOption(HistoryChartType type, string resourceKey) { Type = type; ResourceKey = resourceKey; }
    public HistoryChartType Type { get; }
    public string ResourceKey { get; }
    public string Display => Strings.Get(ResourceKey);
    public override string ToString() => Display;
}
