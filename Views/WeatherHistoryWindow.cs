using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScottPlot.WPF;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.Views;

/// <summary>Seçili konum için, yıllık ve mevsimsel karşılaştırma sunan günlük hava geçmişi.</summary>
internal sealed partial class WeatherHistoryWindow : Window
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
