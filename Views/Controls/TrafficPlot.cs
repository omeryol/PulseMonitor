using System.Collections;
using System.Windows;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.WPF;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views.Controls;

/// <summary>MIT lisanslı ScottPlot yüzeyiyle trafik yönlerini aynı zaman ekseninde gösterir.</summary>
public sealed class TrafficPlot : WpfPlot
{
    public TrafficPlot()
    {
        Loaded += (_, _) =>
        {
            UserInputProcessor.Disable();
            RenderTraffic();
        };
    }

    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(TrafficPlot), new FrameworkPropertyMetadata(null, Redraw));
    public int Version { get => (int)GetValue(VersionProperty); set => SetValue(VersionProperty, value); }
    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(nameof(Version), typeof(int), typeof(TrafficPlot), new FrameworkPropertyMetadata(0, Redraw));
    public HistoryChartType ChartType { get => (HistoryChartType)GetValue(ChartTypeProperty); set => SetValue(ChartTypeProperty, value); }
    public static readonly DependencyProperty ChartTypeProperty = DependencyProperty.Register(nameof(ChartType), typeof(HistoryChartType), typeof(TrafficPlot), new FrameworkPropertyMetadata(HistoryChartType.StackedColumns, Redraw));

    private static void Redraw(DependencyObject source, DependencyPropertyChangedEventArgs _) => ((TrafficPlot)source).RenderTraffic();

    private void RenderTraffic()
    {
        if (!IsLoaded) return;
        IReadOnlyList<HistoryBar> rows = Items?.Cast<HistoryBar>().ToArray() ?? [];
        Plot.Clear();
        ApplyTheme();
        if (rows.Count == 0) { Refresh(); return; }

        double[] x = Enumerable.Range(0, rows.Count).Select(i => (double)i).ToArray();
        double[] down = rows.Select(row => row.DownHeight).ToArray();
        double[] up = rows.Select(row => row.UpHeight).ToArray();
        switch (ChartType)
        {
            case HistoryChartType.GroupedColumns: AddGrouped(rows); break;
            case HistoryChartType.Lines: AddLines(x, down, up, false, false); break;
            case HistoryChartType.Areas: AddLines(x, down, up, true, false); break;
            case HistoryChartType.Dots: AddLines(x, down, up, false, true); break;
            case HistoryChartType.Heatmap: AddHeatmap(rows); break;
            default: AddStacked(rows); break;
        }
        if (ChartType != HistoryChartType.Heatmap) Plot.ShowLegend();
        Plot.Axes.AutoScale();
        Refresh();
    }

    private void AddStacked(IReadOnlyList<HistoryBar> rows)
    {
        ScottPlot.Color down = ToPlotColor("Brush.History.DownFill"), up = ToPlotColor("Brush.History.UpFill");
        var downBars = rows.Select((row, i) => new Bar { Position = i, Value = row.DownHeight, FillColor = down, LineWidth = 0 }).ToList();
        var upBars = rows.Select((row, i) => new Bar { Position = i, Value = row.UpHeight, ValueBase = row.DownHeight, FillColor = up, LineWidth = 0 }).ToList();
        Plot.Add.Bars(downBars).LegendText = PulseMonitor.Core.Strings.Get("History.Down");
        Plot.Add.Bars(upBars).LegendText = PulseMonitor.Core.Strings.Get("History.Up");
    }

    private void AddGrouped(IReadOnlyList<HistoryBar> rows)
    {
        ScottPlot.Color down = ToPlotColor("Brush.History.DownFill"), up = ToPlotColor("Brush.History.UpFill");
        var downBars = rows.Select((row, i) => new Bar { Position = i - .2, Value = row.DownHeight, Size = .34, FillColor = down, LineWidth = 0 }).ToList();
        var upBars = rows.Select((row, i) => new Bar { Position = i + .2, Value = row.UpHeight, Size = .34, FillColor = up, LineWidth = 0 }).ToList();
        Plot.Add.Bars(downBars).LegendText = PulseMonitor.Core.Strings.Get("History.Down");
        Plot.Add.Bars(upBars).LegendText = PulseMonitor.Core.Strings.Get("History.Up");
    }

    private void AddLines(double[] x, double[] down, double[] up, bool filled, bool dots)
    {
        var downLine = Plot.Add.ScatterLine(x, down, ToPlotColor("Brush.History.DownFill"));
        var upLine = Plot.Add.ScatterLine(x, up, ToPlotColor("Brush.History.UpFill"));
        foreach (var line in new[] { downLine, upLine })
        {
            line.LineWidth = filled ? 2 : 3;
            line.MarkerSize = dots ? 7 : 0;
            line.FillY = filled;
        }
        downLine.LegendText = PulseMonitor.Core.Strings.Get("History.Down");
        upLine.LegendText = PulseMonitor.Core.Strings.Get("History.Up");
    }

    private void AddHeatmap(IReadOnlyList<HistoryBar> rows)
    {
        double maximum = Math.Max(rows.Max(row => Math.Max(row.DownHeight, row.UpHeight)), 1);
        var cells = new double[2, rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            cells[0, i] = rows[i].DownHeight <= 0 ? double.NaN : rows[i].DownHeight / maximum;
            cells[1, i] = rows[i].UpHeight <= 0 ? double.NaN : rows[i].UpHeight / maximum;
        }
        var heatmap = Plot.Add.Heatmap(cells);
        heatmap.NaNCellColor = ScottPlot.Color.FromARGB(0);
        heatmap.Colormap = new ScottPlot.Colormaps.Viridis();
    }

    private void ApplyTheme()
    {
        Plot.FigureBackground.Color = ToPlotColor("Brush.History.Card");
        Plot.DataBackground.Color = ToPlotColor("Brush.History.Window");
        Plot.Axes.Color(ToPlotColor("Brush.History.Muted"));
        Plot.Grid.MajorLineColor = ToPlotColor("Brush.History.Grid");
    }

    private static ScottPlot.Color ToPlotColor(string key)
    {
        System.Windows.Media.Color color = (Application.Current?.TryFindResource(key) as SolidColorBrush)?.Color ?? System.Windows.Media.Colors.Gray;
        return ScottPlot.Color.FromHtml($"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }
}
