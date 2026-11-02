using System.Windows;
using System.Windows.Controls;
using PulseMonitor.Core;
using PulseMonitor.Metrics;

namespace PulseMonitor.Views;

/// <summary>Ping hedefine erişilemeyen kayıtlı dönemleri seçilen aralığa göre gösterir.</summary>
internal sealed class PingHistoryWindow : Window
{
    private readonly List<PingOutages.Outage> _history;
    private readonly ComboBox _range = new() { MinWidth = 160, Margin = new Thickness(0, 0, 0, 10) };
    private readonly TextBlock _summary = new() { Margin = new Thickness(0, 0, 0, 8) };
    private readonly ListBox _items = new() { MinHeight = 200 };

    public PingHistoryWindow(PingOutages history)
    {
        _history = history.Snapshot();
        WindowTheme.Prepare(this);
        Title = Strings.Get("Ping.History");
        Width = 620; Height = 440; MinWidth = 450; MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "Ui.Window");
        SetResourceReference(ForegroundProperty, "Ui.Text");
        var panel = new DockPanel { Margin = new Thickness(16) };
        var top = new StackPanel();
        top.Children.Add(new TextBlock { Text = Strings.Get("Ping.HistoryHint"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        _range.Items.Add(Strings.Get("Ping.RangeDay"));
        _range.Items.Add(Strings.Get("Ping.RangeWeek"));
        _range.Items.Add(Strings.Get("Ping.RangeMonth"));
        _range.Items.Add(Strings.Get("Ping.RangeAll"));
        _range.SelectionChanged += (_, _) => Refresh();
        top.Children.Add(_range);
        top.Children.Add(_summary);
        DockPanel.SetDock(top, Dock.Top);
        panel.Children.Add(top);
        panel.Children.Add(_items);
        Content = panel;
        _range.SelectedIndex = 1;
    }

    private void Refresh()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset? start = _range.SelectedIndex switch { 0 => now.AddDays(-1), 1 => now.AddDays(-7), 2 => now.AddDays(-30), _ => null };
        var entries = PingOutages.InRange(_history, start);
        _summary.Text = string.Format(Strings.Get("Ping.RangeCount"), entries.Count, _history.Count);
        _items.ItemsSource = entries.Count == 0
            ? new[] { Strings.Get("Ping.NoHistory") }
            : entries.Select(x => $"{x.Started.ToLocalTime():g} — {(x.Recovered is { } end ? end.ToLocalTime().ToString("g") : Strings.Get("Ping.EndUnknown"))}  ({x.Host})").ToArray();
    }
}
