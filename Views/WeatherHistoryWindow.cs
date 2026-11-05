using System.Windows;
using System.Windows.Controls;
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
        top.Children.Add(new TextBlock { Text = location.ShownName, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 18, 0), VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(_start);
        top.Children.Add(new TextBlock { Text = " – ", VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(_end);
        top.Children.Add(_load);
        var panel = new DockPanel { Margin = new Thickness(16) };
        DockPanel.SetDock(top, Dock.Top); panel.Children.Add(top);
        DockPanel.SetDock(_summary, Dock.Top); panel.Children.Add(_summary);
        panel.Children.Add(_rows);
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
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log.Warn("Tarihsel hava verisi alınamadı.", ex);
            _summary.Text = Strings.Get("Weather.HistoryFailed");
        }
        finally { _load.IsEnabled = true; }
    }
}
