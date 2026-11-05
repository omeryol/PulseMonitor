using System.Windows;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.WPF;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Weather;

namespace PulseMonitor.Views;

/// <summary>Seçili konum için istek üzerine indirilen, günlük tarihsel hava tablosu.</summary>
internal sealed class WeatherHistoryWindow : Window
{
    private readonly WeatherLocation _location;
    private readonly DatePicker _start = new() { Width = 125 };
    private readonly DatePicker _end = new() { Width = 125 };
    private readonly Button _load = new() { MinWidth = 90 };
    private readonly TextBlock _summary = new() { Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap };
    private readonly ListBox _rows = new() { MinHeight = 260 };
    private readonly WpfPlot _temperatureChart = new() { Height = 190, Margin = new Thickness(0, 0, 0, 10) };
    private readonly WpfPlot _rainChart = new() { Height = 130, Margin = new Thickness(0, 0, 0, 10) };
    private readonly CancellationTokenSource _closing = new();

    public WeatherHistoryWindow(WeatherLocation location)
    {
        _location = location;
        WindowTheme.Prepare(this);
        Title = Strings.Get("Weather.History");
        Width = 680; Height = 540; MinWidth = 520; MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "Ui.Window");
        SetResourceReference(ForegroundProperty, "Ui.Text");
        DateTime yesterday = DateTime.Today.AddDays(-1);
        _start.SelectedDate = yesterday.AddDays(-29);
        _end.SelectedDate = yesterday;
        _load.Content = Strings.Get("Weather.HistoryLoad");
        _load.Click += async (_, _) => await LoadAsync();
        Closed += (_, _) => _closing.Cancel();

        var top = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        top.Children.Add(new TextBlock { Text = location.ShownName, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 18, 0), VerticalAlignment = System.Windows.VerticalAlignment.Center });
        top.Children.Add(_start);
        top.Children.Add(new TextBlock { Text = " – ", VerticalAlignment = System.Windows.VerticalAlignment.Center });
        top.Children.Add(_end);
        top.Children.Add(_load);
        var panel = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(top, Dock.Top); panel.Children.Add(top);
        DockPanel.SetDock(_summary, Dock.Top); panel.Children.Add(_summary);
        var charts = new StackPanel();
        charts.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryTemperatureChart"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        charts.Children.Add(_temperatureChart);
        charts.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryRainChart"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        charts.Children.Add(_rainChart);
        charts.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryDetails"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        charts.Children.Add(_rows);
        panel.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = charts });
        Content = panel;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!HistoricalWeatherRange.TryCreate(_start.SelectedDate, _end.SelectedDate, out HistoricalWeatherRange range))
        {
            _summary.Text = Strings.Get("Weather.HistoryInvalidRange");
            return;
        }
        _load.IsEnabled = false;
        _summary.Text = Strings.Get("Weather.HistoryLoading");
        try
        {
            HistoricalWeatherReport report = await OpenMeteoClient.FetchHistoryAsync(_location.Latitude, _location.Longitude, range, _closing.Token);
            _summary.Text = report.Days.Count == 0
                ? Strings.Get("Weather.HistoryEmpty")
                : string.Format(UiLanguage.Culture, Strings.Get("Weather.HistorySummary"), report.Days.Count, ValueFormat.Celsius(report.AverageHigh), report.PrecipitationTotalMm.ToString("0.#", UiLanguage.Culture));
            _rows.ItemsSource = report.Days.OrderByDescending(day => day.Date).Select(day =>
                $"{day.Date.ToDateTime(TimeOnly.MinValue):d}  ·  {WeatherCodes.Describe(day.WeatherCode)}  ·  {ValueFormat.Celsius(day.Low)} – {ValueFormat.Celsius(day.High)}  ·  {day.PrecipitationMm:0.#} mm").ToList();
            DrawCharts(report.Days);
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log.Warn("Tarihsel hava verisi alınamadı.", ex);
            _summary.Text = Strings.Get("Weather.HistoryFailed");
        }
        finally { _load.IsEnabled = true; }
    }

    private void DrawCharts(IReadOnlyList<HistoricalWeatherDay> days)
    {
        double[] x = Enumerable.Range(0, days.Count).Select(i => (double)i).ToArray();
        _temperatureChart.Plot.Clear();
        var high = _temperatureChart.Plot.Add.ScatterLine(x, days.Select(day => day.High).ToArray(), Color.FromHtml("#E7773C"));
        var low = _temperatureChart.Plot.Add.ScatterLine(x, days.Select(day => day.Low).ToArray(), Color.FromHtml("#4CA7E8"));
        high.LineWidth = low.LineWidth = 2;
        high.LegendText = Strings.Get("Weather.HistoryHigh");
        low.LegendText = Strings.Get("Weather.HistoryLow");
        _temperatureChart.Plot.ShowLegend();
        _temperatureChart.Plot.Axes.AutoScale();
        _temperatureChart.Refresh();

        _rainChart.Plot.Clear();
        var bars = days.Select((day, index) => new Bar { Position = index, Value = day.PrecipitationMm, FillColor = Color.FromHtml("#4CA7E8"), LineWidth = 0 }).ToList();
        _rainChart.Plot.Add.Bars(bars);
        _rainChart.Plot.Axes.AutoScale();
        _rainChart.Refresh();
    }
}
