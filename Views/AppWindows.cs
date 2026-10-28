using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views;

/// <summary>Ayarlar ve trafik geçmişi pencereleri: her biri tek kopya açılır, açıksa öne getirilir.</summary>
internal sealed class AppWindows
{
    private readonly SettingsStore _settings;
    private readonly TrafficHistory _history;
    private readonly Action _testNotification;
    private SettingsWindow? _settingsWindow;
    private HistoryWindow? _historyWindow;

    public AppWindows(SettingsStore settings, TrafficHistory history, Action testNotification)
    {
        _settings = settings;
        _history = history;
        _testNotification = testNotification;
    }

    public void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel(_settings, _history, _testNotification, OpenHistory),
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void OpenHistory()
    {
        if (_historyWindow is not null)
        {
            _historyWindow.Activate();
            return;
        }
        _historyWindow = new HistoryWindow { DataContext = new HistoryViewModel(_history) };
        _historyWindow.Closed += (_, _) => _historyWindow = null;
        _historyWindow.Show();
        _historyWindow.Activate();
    }
}
