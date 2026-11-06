using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.WPF;
using PulseMonitor.Core;
using PulseMonitor.Weather;

namespace PulseMonitor.Views;

internal sealed partial class WeatherHistoryWindow
{
    private sealed record ChartPoint(string Caption, double Low, double High, double RainMm, WeatherSeason? Season, int? WeatherCode);

    private void DrawCharts(IReadOnlyList<HistoricalWeatherDay> current, IReadOnlyList<HistoricalWeatherDay>? prior)
    {
        _chartPoints = BuildChartPoints(current);
        int count = _chartPoints.Count;
        double[] x = Enumerable.Range(0, count).Select(i => (double)i).ToArray();
        bool multiYear = current.Count > 366;
        _lastHoverCaption = null;
        _temperatureChart.Plot.Clear();
        ApplyChartTheme(_temperatureChart);
        if (!multiYear) AddSeasonBands(_temperatureChart, current);
        AddTemperatureLine(_temperatureChart, x, _chartPoints.Select(point => point.High).ToArray(), "#E7773C", Strings.Get("Weather.HistoryHigh"), 2.5f);
        AddTemperatureLine(_temperatureChart, x, _chartPoints.Select(point => point.Low).ToArray(), "#4CA7E8", Strings.Get("Weather.HistoryLow"), 2.5f);
        if (!multiYear && prior is { Count: > 0 })
        {
            int shared = Math.Min(count, prior.Count);
            double[] priorX = Enumerable.Range(0, shared).Select(i => (double)i).ToArray();
            AddTemperatureLine(_temperatureChart, priorX, prior.Take(shared).Select(day => day.High).ToArray(), "#BA8D72", Strings.Get("Weather.HistoryPriorHigh"), 1);
            AddTemperatureLine(_temperatureChart, priorX, prior.Take(shared).Select(day => day.Low).ToArray(), "#779DB6", Strings.Get("Weather.HistoryPriorLow"), 1);
        }
        _temperatureChart.Plot.HideLegend();
        _temperatureChart.Plot.Axes.AutoScale();
        SetXAxisLabels(_temperatureChart, _chartPoints);
        _temperatureChart.Refresh();

        _rainChart.Plot.Clear();
        ApplyChartTheme(_rainChart);
        if (!multiYear) AddSeasonBands(_rainChart, current);
        var currentBars = _chartPoints.Select((point, index) => new Bar { Position = index - (prior is null || multiYear ? 0 : .18), Value = point.RainMm, Size = prior is null || multiYear ? .72 : .32, FillColor = ScottPlot.Color.FromHtml("#4CA7E8"), LineWidth = 0 }).ToList();
        _rainChart.Plot.Add.Bars(currentBars).LegendText = Strings.Get("Weather.HistoryCurrent");
        if (!multiYear && prior is { Count: > 0 })
        {
            var priorBars = prior.Take(count).Select((day, index) => new Bar { Position = index + .18, Value = day.PrecipitationMm, Size = .32, FillColor = ScottPlot.Color.FromHtml("#9DA9B5"), LineWidth = 0 }).ToList();
            _rainChart.Plot.Add.Bars(priorBars).LegendText = Strings.Get("Weather.HistoryPrior");
            _rainChart.Plot.HideLegend();
        }
        _rainChart.Plot.Axes.AutoScale();
        SetXAxisLabels(_rainChart, _chartPoints);
        _rainChart.Refresh();
    }

    private IReadOnlyList<ChartPoint> BuildChartPoints(IReadOnlyList<HistoricalWeatherDay> days)
    {
        if (days.Count <= 366)
            return days.Select(day => new ChartPoint(day.Date.ToDateTime(TimeOnly.MinValue).ToString("d MMM", UiLanguage.Culture), day.Low, day.High, day.PrecipitationMm,
                HistoricalWeatherAnalytics.SeasonFor(day.Date.Month, _location.Latitude), day.WeatherCode)).ToArray();

        return days.GroupBy(day => day.Date.Year).OrderBy(group => group.Key).Select(group => new ChartPoint(
            group.Key.ToString(UiLanguage.Culture), group.Average(day => day.Low), group.Average(day => day.High), group.Sum(day => day.PrecipitationMm), null, null)).ToArray();
    }

    private static void SetXAxisLabels(WpfPlot plot, IReadOnlyList<ChartPoint> points)
    {
        if (points.Count == 0) return;
        int step = points.Count <= 10 ? 1 : Math.Max(1, (int)Math.Ceiling(points.Count / 8d));
        int[] indexes = Enumerable.Range(0, points.Count).Where(index => index % step == 0 || index == points.Count - 1).ToArray();
        plot.Plot.Axes.Bottom.SetTicks(indexes.Select(index => (double)index).ToArray(), indexes.Select(index => points[index].Caption).ToArray());
    }

    private void AddSeasonBands(WpfPlot plot, IReadOnlyList<HistoricalWeatherDay> days)
    {
        if (days.Count == 0) return;
        int start = 0;
        WeatherSeason previous = HistoricalWeatherAnalytics.SeasonFor(days[0].Date.Month, _location.Latitude);
        for (int index = 1; index <= days.Count; index++)
        {
            WeatherSeason? season = index == days.Count ? null : HistoricalWeatherAnalytics.SeasonFor(days[index].Date.Month, _location.Latitude);
            if (season == previous) continue;
            var span = plot.Plot.Add.HorizontalSpan(start - .5, index - .5);
            span.FillStyle.Color = SeasonColor(previous).WithOpacity(.13);
            span.LineStyle.Width = 0;
            start = index;
            if (season is { } next) previous = next;
        }
    }

    private static ScottPlot.Color SeasonColor(WeatherSeason season) => season switch
    {
        WeatherSeason.Winter => ScottPlot.Color.FromHtml("#4CA7E8"),
        WeatherSeason.Spring => ScottPlot.Color.FromHtml("#65B784"),
        WeatherSeason.Summer => ScottPlot.Color.FromHtml("#E7B34A"),
        _ => ScottPlot.Color.FromHtml("#C98362"),
    };

    private static void AddTemperatureLine(WpfPlot plot, double[] x, double[] y, string color, string legend, float width)
    {
        var line = plot.Plot.Add.ScatterLine(x, y, ScottPlot.Color.FromHtml(color));
        line.LineWidth = width;
        line.LegendText = legend;
    }

    private void BuildHoverCard()
    {
        _hoverTitle.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Text");
        _hoverContext.SetResourceReference(TextBlock.ForegroundProperty, "Ui.Muted");
        var content = new StackPanel { MinWidth = 168 };
        content.Children.Add(_hoverTitle);
        content.Children.Add(_hoverContext);
        content.Children.Add(_hoverTemperature);
        content.Children.Add(_hoverRain);
        var card = new Border { Padding = new Thickness(9, 7, 10, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Child = content };
        card.SetResourceReference(Border.BackgroundProperty, "Ui.Card");
        card.SetResourceReference(Border.BorderBrushProperty, "Ui.Accent");
        _hoverCardBorder = card;
        _hoverCard.Child = card;
    }

    private void ConfigurePlot(WpfPlot plot)
    {
        plot.UserInputProcessor.Disable();
        plot.MouseMove += (_, eventArgs) => ShowHover(plot, eventArgs.GetPosition(plot));
        plot.MouseLeave += (_, _) => { _lastHoverCaption = null; _hoverCard.IsOpen = false; };
    }

    private void ShowHover(WpfPlot plot, System.Windows.Point point)
    {
        if (_chartPoints.Count == 0) return;
        Coordinates coordinates = plot.Plot.GetCoordinates((float)point.X, (float)point.Y, plot.Plot.Axes.Bottom, plot.Plot.Axes.Left);
        int index = Math.Clamp((int)Math.Round(coordinates.X), 0, _chartPoints.Count - 1);
        ChartPoint selected = _chartPoints[index];
        if (_lastHoverCaption != selected.Caption)
        {
            _lastHoverCaption = selected.Caption;
            _hoverTitle.Text = selected.Caption;
            _hoverContext.Text = selected.Season is { } season && selected.WeatherCode is { } code ? $"{SeasonName(season)} · {WeatherCodes.Describe(code)}" : Strings.Get("Weather.HistoryHoverYearly");
            _hoverTemperature.Text = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryHoverCardTemperature"), Degrees(selected.Low), Degrees(selected.High));
            _hoverRain.Text = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryHoverCardRain"), selected.RainMm.ToString("0.#", UiLanguage.Culture));
            _hoverTemperature.Foreground = TemperatureBrush((selected.Low + selected.High) / 2);
            _hoverRain.Foreground = selected.RainMm > 0 ? Brushes.DodgerBlue : ThemeBrushes.Get("Brush.Muted");
            _hoverCardBorder!.BorderBrush = selected.WeatherCode is { } weatherCode ? WeatherBrush(weatherCode) : ThemeBrushes.Get("Ui.Accent");
        }
        _hoverCard.PlacementTarget = plot;
        _hoverCard.Placement = PlacementMode.Relative;
        _hoverCard.HorizontalOffset = Math.Min(point.X + 14, Math.Max(0, plot.ActualWidth - 170));
        _hoverCard.VerticalOffset = Math.Min(point.Y + 14, Math.Max(0, plot.ActualHeight - 76));
        _hoverCard.IsOpen = true;
    }

    private static void ApplyChartTheme(WpfPlot plot)
    {
        plot.Plot.FigureBackground.Color = ToPlotColor("Ui.Card");
        plot.Plot.DataBackground.Color = ToPlotColor("Ui.Window");
        plot.Plot.Axes.Color(ToPlotColor("Ui.Text"));
        plot.Plot.Grid.MajorLineColor = ToPlotColor("Ui.Border");
    }

    private static ScottPlot.Color ToPlotColor(string key)
    {
        System.Windows.Media.Color color = (System.Windows.Application.Current?.TryFindResource(key) as SolidColorBrush)?.Color ?? System.Windows.Media.Colors.Gray;
        return ScottPlot.Color.FromHtml($"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }
}
