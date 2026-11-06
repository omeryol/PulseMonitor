using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.WPF;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.Views;

/// <summary>Seçili konum için, yıllık ve mevsimsel karşılaştırma sunan günlük hava geçmişi.</summary>
internal sealed class WeatherHistoryWindow : Window
{
    private readonly WeatherLocation _location;
    private readonly bool _fahrenheit;
    private readonly DatePicker _start = new() { Width = 125 };
    private readonly DatePicker _end = new() { Width = 125 };
    private readonly CheckBox _compare = new() { VerticalAlignment = System.Windows.VerticalAlignment.Center };
    private readonly Button _load = new() { MinWidth = 90, Margin = new Thickness(8, 0, 0, 0) };
    private readonly TextBlock _summary = new() { Margin = new Thickness(0, 4, 0, 2), TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly TextBlock _comparison = new() { Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    // Her satır pencerenin tamamını paylaşır: tek mevsim bütün satırı, dört mevsim dört eşit sütunu kaplar.
    private readonly UniformGrid _insights = new() { Margin = new Thickness(0, 0, 0, 6), Rows = 1 };
    private readonly UniformGrid _seasons = new() { Margin = new Thickness(0, 0, 0, 5), Rows = 1 };
    private readonly StackPanel _cards = new() { Margin = new Thickness(0, 0, 0, 8) };
    private readonly WrapPanel _temperatureKey = new() { Margin = new Thickness(0, 0, 0, 6) };
    private readonly WrapPanel _rainKey = new() { Margin = new Thickness(0, 0, 0, 6) };
    private readonly WpfPlot _temperatureChart = new() { Height = 280, Margin = new Thickness(0, 0, 0, 8) };
    private readonly WpfPlot _rainChart = new() { Height = 190, Margin = new Thickness(0, 0, 0, 8) };
    private readonly TextBlock _hoverTitle = new() { FontWeight = FontWeights.SemiBold, FontSize = 12 };
    private readonly TextBlock _hoverContext = new() { FontSize = 10, Margin = new Thickness(0, 2, 0, 0) };
    private readonly TextBlock _hoverTemperature = new() { FontSize = 11, Margin = new Thickness(0, 3, 0, 0) };
    private readonly TextBlock _hoverRain = new() { FontSize = 11, Margin = new Thickness(0, 2, 0, 0) };
    private readonly Popup _hoverCard = new() { AllowsTransparency = true, IsHitTestVisible = false, StaysOpen = true };
    private Border? _hoverCardBorder;
    private readonly CancellationTokenSource _closing = new();
    private IReadOnlyList<ChartPoint> _chartPoints = [];
    private string? _lastHoverCaption;

    public WeatherHistoryWindow(WeatherLocation location, StyleSettings style)
    {
        _location = location;
        _fahrenheit = style.TemperatureUnit == StyleOptions.Fahrenheit;
        WindowTheme.Prepare(this);
        Title = $"{Strings.Get("Weather.History")} · {location.ShownName}";
        Width = 900; Height = 860; MinWidth = 620; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "Ui.Window");
        SetResourceReference(ForegroundProperty, "Ui.Text");
        DateTime yesterday = DateTime.Today.AddDays(-1);
        _start.SelectedDate = yesterday.AddDays(-29);
        _end.SelectedDate = yesterday;
        _compare.Content = Strings.Get("Weather.HistoryCompare");
        _compare.IsChecked = true;
        _load.Content = Strings.Get("Weather.HistoryLoad");
        _load.Click += async (_, _) => await LoadAsync();
        Closed += (_, _) => _closing.Cancel();
        ConfigurePlot(_temperatureChart);
        ConfigurePlot(_rainChart);
        BuildHoverCard();

        var rangePanel = BuildRangePanel();

        var content = new StackPanel();
        content.Children.Add(rangePanel);
        content.Children.Add(_summary);
        content.Children.Add(_comparison);
        content.Children.Add(_insights);
        content.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistorySeasons"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 3) });
        content.Children.Add(_seasons);
        content.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryTemperatureChart"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 3) });
        content.Children.Add(_temperatureKey);
        content.Children.Add(_temperatureChart);
        content.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryRainChart"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 3) });
        content.Children.Add(_rainKey);
        content.Children.Add(_rainChart);
        content.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryDetails"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
        content.Children.Add(_cards);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(16), Content = content };
        Loaded += async (_, _) => await LoadAsync();
    }

    /// <summary>
    /// Sık dönemler tek eylemde yüklenir; takvim alanları da nadir/özel aralıklar için görünür kalır.
    /// Böylece tarih seçimi mouse kullanımına veya gizli bir kısayola bağlı değildir.
    /// </summary>
    private Border BuildRangePanel()
    {
        var panel = new StackPanel();
        var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 4), LastChildFill = false };
        heading.Children.Add(new TextBlock
        {
            Text = _location.ShownName, FontWeight = FontWeights.SemiBold, FontSize = 16,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        heading.Children.Add(new TextBlock
        {
            Text = Strings.Get("Weather.HistoryRange"), Foreground = ThemeBrushes.Get("Brush.Muted"),
            VerticalAlignment = System.Windows.VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        });
        DockPanel.SetDock(heading.Children[^1], Dock.Right);
        panel.Children.Add(heading);

        var quickRanges = new WrapPanel { Margin = new Thickness(0, 0, 0, 5) };
        quickRanges.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryQuick"), Foreground = ThemeBrushes.Get("Brush.Muted"), VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        quickRanges.Children.Add(QuickRangeButton("Weather.HistoryLast7", 7));
        quickRanges.Children.Add(QuickRangeButton("Weather.HistoryLast30", 30));
        quickRanges.Children.Add(QuickRangeButton("Weather.HistoryLast90", 90));
        quickRanges.Children.Add(QuickRangeButton("Weather.HistoryLast365", 365));
        var thisYear = new Button { Content = Strings.Get("Weather.HistoryThisYear"), Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(10, 3, 10, 3) };
        thisYear.Click += async (_, _) => { SetThisYearRange(); await LoadAsync(); };
        quickRanges.Children.Add(thisYear);
        quickRanges.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryLongRange"), Foreground = ThemeBrushes.Get("Brush.Muted"), VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
        foreach (int years in new[] { 5, 10, 15, 20 }) quickRanges.Children.Add(YearRangeButton(years));
        panel.Children.Add(quickRanges);

        var customRange = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        customRange.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryStart"), Foreground = ThemeBrushes.Get("Brush.Muted"), VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        customRange.Children.Add(_start);
        customRange.Children.Add(new TextBlock { Text = Strings.Get("Weather.HistoryEnd"), Foreground = ThemeBrushes.Get("Brush.Muted"), VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(12, 0, 6, 0) });
        customRange.Children.Add(_end);
        panel.Children.Add(customRange);

        customRange.Children.Add(_compare);
        customRange.Children.Add(_load);

        return new Border
        {
            Background = ThemeBrushes.Get("Ui.Card"), BorderBrush = ThemeBrushes.Get("Ui.Border"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 6), Child = panel,
        };
    }

    private Button QuickRangeButton(string resourceKey, int days)
    {
        var button = new Button { Content = Strings.Get(resourceKey), Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(10, 3, 10, 3) };
        button.Click += async (_, _) => { SetRecentRange(days); await LoadAsync(); };
        return button;
    }

    private Button YearRangeButton(int years)
    {
        var button = new Button { Content = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryLastYears"), years), Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(10, 3, 10, 3) };
        button.Click += async (_, _) => { SetRecentYears(years); await LoadAsync(); };
        return button;
    }

    private void SetRecentRange(int days)
    {
        DateTime end = DateTime.Today.AddDays(-1);
        _start.SelectedDate = end.AddDays(1 - days);
        _end.SelectedDate = end;
    }

    private void SetThisYearRange()
    {
        DateTime end = DateTime.Today.AddDays(-1);
        _start.SelectedDate = new DateTime(end.Year, 1, 1);
        _end.SelectedDate = end;
    }

    private void SetRecentYears(int years)
    {
        DateTime end = DateTime.Today.AddDays(-1);
        _start.SelectedDate = end.AddYears(-years).AddDays(1);
        _end.SelectedDate = end;
        // Çok yıllı görünüm, aynı dönemin bir yıl öncesiyle ikinci bir 20 yıllık isteği yapmaz.
        _compare.IsChecked = false;
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
        _comparison.Text = "";
        try
        {
            Task<HistoricalWeatherReport> currentTask = OpenMeteoClient.FetchHistoryAsync(_location.Latitude, _location.Longitude, range, _closing.Token);
            Task<HistoricalWeatherReport>? priorTask = null;
            if (_compare.IsChecked == true && !range.IsMultiYear)
            {
                var prior = new HistoricalWeatherRange(range.Start.AddYears(-1), range.End.AddYears(-1));
                priorTask = OpenMeteoClient.FetchHistoryAsync(_location.Latitude, _location.Longitude, prior, _closing.Token);
            }
            HistoricalWeatherReport current = await currentTask;
            HistoricalWeatherReport? priorYear = priorTask is null ? null : await priorTask;
            ShowReport(current, priorYear);
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log.Warn("Tarihsel hava verisi alınamadı.", ex);
            _summary.Text = Strings.Get("Weather.HistoryFailed");
        }
        finally { _load.IsEnabled = true; }
    }

    private void ShowReport(HistoricalWeatherReport report, HistoricalWeatherReport? priorYear)
    {
        _summary.Text = report.Days.Count == 0
            ? Strings.Get("Weather.HistoryEmpty")
            : string.Format(UiLanguage.Culture, Strings.Get("Weather.HistorySummary"), report.Days.Count, Degrees(report.AverageHigh), report.PrecipitationTotalMm.ToString("0.#", UiLanguage.Culture));
        HistoricalWeatherAnalysis analysis = HistoricalWeatherAnalytics.Analyze(report, priorYear, _location.Latitude);
        _comparison.Text = priorYear is null ? "" : string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryComparison"), SignedDegrees(analysis.AverageHighDifference), SignedNumber(analysis.PrecipitationDifference));
        DrawInsightCards(report, analysis, priorYear is not null);
        DrawKeys(priorYear is not null);
        DrawSeasonCards(analysis.Seasons);
        DrawDayCards(report.Days);
        DrawCharts(report.Days, priorYear?.Days);
    }

    private void DrawKeys(bool hasPriorYear)
    {
        _temperatureKey.Children.Clear();
        _rainKey.Children.Clear();
        _temperatureKey.Children.Add(Key("—", Strings.Get("Weather.HistoryHigh"), Brushes.OrangeRed));
        _temperatureKey.Children.Add(Key("—", Strings.Get("Weather.HistoryLow"), Brushes.DodgerBlue));
        if (hasPriorYear) _temperatureKey.Children.Add(Key("┄", Strings.Get("Weather.HistoryPrior"), ThemeBrushes.Get("Brush.Muted")));
        _rainKey.Children.Add(Key("■", Strings.Get("Weather.HistoryCurrent"), Brushes.DodgerBlue));
        if (hasPriorYear) _rainKey.Children.Add(Key("■", Strings.Get("Weather.HistoryPrior"), ThemeBrushes.Get("Brush.Muted")));
    }

    private static TextBlock Key(string mark, string label, Brush color) => new()
    {
        Text = $"{mark}  {label}", Foreground = color, FontSize = 11, FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 0, 18, 0),
    };

    private void DrawInsightCards(HistoricalWeatherReport report, HistoricalWeatherAnalysis analysis, bool hasComparison)
    {
        _insights.Children.Clear();
        _insights.Columns = hasComparison ? 3 : 2;
        _insights.Children.Add(Insight(Strings.Get("Weather.HistoryInsightAverageHigh"), Degrees(report.AverageHigh), ThemeBrushes.Get("Brush.Temp.Warm")));
        _insights.Children.Add(Insight(Strings.Get("Weather.HistoryInsightRain"), $"{report.PrecipitationTotalMm:0.#} mm", Brushes.DodgerBlue));
        if (hasComparison)
            _insights.Children.Add(Insight(Strings.Get("Weather.HistoryInsightComparison"), SignedDegrees(analysis.AverageHighDifference), analysis.AverageHighDifference >= 0 ? ThemeBrushes.Get("Brush.Temp.Warm") : ThemeBrushes.Get("Brush.Temp.Cold")));
    }

    private Border Insight(string label, string value, Brush accent)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = label, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = ThemeBrushes.Get("Brush.Muted") });
        stack.Children.Add(new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = accent, Margin = new Thickness(0, 2, 0, 0) });
        return Card(stack, accent);
    }

    private void DrawSeasonCards(IReadOnlyList<HistoricalSeasonSummary> seasons)
    {
        _seasons.Children.Clear();
        _seasons.Columns = Math.Max(1, seasons.Count);
        foreach (HistoricalSeasonSummary season in seasons)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = SeasonName(season.Season), FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistorySeasonStats"), season.Days, Degrees(season.AverageHigh), season.PrecipitationMm.ToString("0.#", UiLanguage.Culture)), FontSize = 11, TextWrapping = TextWrapping.Wrap });
            _seasons.Children.Add(Card(panel));
        }
    }

    private void DrawDayCards(IReadOnlyList<HistoricalWeatherDay> days)
    {
        _cards.Children.Clear();
        foreach (HistoricalWeatherDay day in days.OrderByDescending(day => day.Date))
        {
            Brush accent = WeatherBrush(day.WeatherCode);
            var row = new DockPanel { LastChildFill = true };
            row.Children.Add(new TextBlock { Text = day.Date.ToDateTime(TimeOnly.MinValue).ToString("ddd\nd MMM", UiLanguage.Culture), Width = 58, FontWeight = FontWeights.SemiBold, Foreground = ThemeBrushes.Get("Brush.Muted"), TextAlignment = TextAlignment.Center });
            row.Children.Add(new TextBlock { Text = WeatherCodes.Glyph(day.WeatherCode, true), Width = 34, FontSize = 21, Foreground = accent, TextAlignment = TextAlignment.Center });
            row.Children.Add(new TextBlock { Text = $"☂ {day.PrecipitationMm:0.#} mm", Width = 78, Foreground = BrushForRain(day.PrecipitationMm), VerticalAlignment = System.Windows.VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
            row.Children.Add(new TextBlock { Text = $"{Degrees(day.Low)}  —  {Degrees(day.High)}", Width = 108, Foreground = TemperatureBrush((day.Low + day.High) / 2), FontWeight = FontWeights.SemiBold, VerticalAlignment = System.Windows.VerticalAlignment.Center, TextAlignment = TextAlignment.Right });
            row.Children.Add(new TextBlock { Text = WeatherCodes.Describe(day.WeatherCode), Foreground = ThemeBrushes.Get("Brush.Text"), VerticalAlignment = System.Windows.VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) });
            _cards.Children.Add(new Border { Child = row, Padding = new Thickness(10, 8, 12, 8), Margin = new Thickness(0, 0, 0, 4), Background = ThemeBrushes.Get("Ui.Card"), BorderBrush = accent, BorderThickness = new Thickness(2, 1, 1, 1), CornerRadius = new CornerRadius(5) });
        }
    }

    private Border Card(UIElement content, Brush? accent = null)
    {
        Brush background = ThemeBrushes.Get("Ui.Card");
        if (accent is SolidColorBrush solid)
            background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(78, solid.Color.R, solid.Color.G, solid.Color.B));
        return new Border
        {
            Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(9), CornerRadius = new CornerRadius(6),
            Background = background, BorderBrush = accent ?? ThemeBrushes.Get("Ui.Border"), BorderThickness = new Thickness(1), Child = content,
        };
    }

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
            // HorizontalSpan x eksenini böler ve grafikte dikey mevsim kesiti oluşturur.
            // VerticalSpan ise y eksenini böler; onun kullanılması sıcaklık ölçeğini 300+ değerlerine taşırdı.
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
        var card = new Border
        {
            Padding = new Thickness(9, 7, 10, 8), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Child = content,
        };
        card.SetResourceReference(Border.BackgroundProperty, "Ui.Card");
        card.SetResourceReference(Border.BorderBrushProperty, "Ui.Accent");
        _hoverCardBorder = card;
        _hoverCard.Child = card;
    }

    private void ConfigurePlot(WpfPlot plot)
    {
        plot.UserInputProcessor.Disable();
        plot.MouseMove += (_, eventArgs) => ShowHover(plot, eventArgs.GetPosition(plot));
        plot.MouseLeave += (_, _) =>
        {
            _lastHoverCaption = null;
            _hoverCard.IsOpen = false;
        };
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
            _hoverContext.Text = selected.Season is { } season && selected.WeatherCode is { } code
                ? $"{SeasonName(season)} · {WeatherCodes.Describe(code)}"
                : Strings.Get("Weather.HistoryHoverYearly");
            _hoverTemperature.Text = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryHoverCardTemperature"), Degrees(selected.Low), Degrees(selected.High));
            _hoverRain.Text = string.Format(UiLanguage.Culture, Strings.Get("Weather.HistoryHoverCardRain"), selected.RainMm.ToString("0.#", UiLanguage.Culture));
            _hoverTemperature.Foreground = TemperatureBrush((selected.Low + selected.High) / 2);
            _hoverRain.Foreground = selected.RainMm > 0 ? Brushes.DodgerBlue : ThemeBrushes.Get("Brush.Muted");
            _hoverCardBorder!.BorderBrush = selected.WeatherCode is { } weatherCode ? WeatherBrush(weatherCode) : ThemeBrushes.Get("Ui.Accent");
        }

        // Kart fare olaylarını yakalamaz; bu nedenle grafik üzerinde kesintisiz hareket eder.
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

    private string Degrees(double? celsius) => celsius is null ? "–" : ValueFormat.Degrees(celsius.Value, _fahrenheit);
    private string SignedDegrees(double? celsius) => celsius is null ? "–" : $"{(celsius >= 0 ? "+" : "")}{ValueFormat.Degrees(celsius.Value, _fahrenheit)}";
    private static string SignedNumber(double? value) => value is null ? "–" : $"{(value >= 0 ? "+" : "")}{value.Value:0.#} mm";
    private static Brush BrushForRain(double amount) => amount >= 10 ? Brushes.DodgerBlue : amount > 0 ? Brushes.SteelBlue : ThemeBrushes.Get("Brush.Muted");
    private static Brush TemperatureBrush(double celsius) => ThemeBrushes.Get(Math.Round(celsius) switch
    {
        <= 0 => "Brush.Temp.Freezing",
        < 10 => "Brush.Temp.Cold",
        < 20 => "Brush.Temp.Mild",
        < 28 => "Brush.Temp.Warm",
        _ => "Brush.Temp.Hot",
    });
    private static Brush WeatherBrush(int code) => code switch
    {
        0 or 1 or 2 => ThemeBrushes.Get("Brush.Temp.Warm"),
        51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => Brushes.DodgerBlue,
        71 or 73 or 75 or 77 or 85 or 86 => ThemeBrushes.Get("Brush.Temp.Freezing"),
        95 or 96 or 99 => Brushes.MediumOrchid,
        _ => ThemeBrushes.Get("Brush.Muted"),
    };
    private static string SeasonName(WeatherSeason season) => Strings.Get($"Weather.Season.{season}");
}
