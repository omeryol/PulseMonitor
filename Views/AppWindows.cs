using System.Windows;
using PulseMonitor.Platform;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views;

/// <summary>Ayarlar, trafik geçmişi ve Hakkında pencereleri: her biri tek kopya açılır, açıksa öne getirilir.</summary>
internal sealed class AppWindows
{
    private readonly SettingsStore _settings;
    private readonly TrafficHistory _history;
    private readonly UpdateChecker _updates;
    private readonly Action _testNotification;
    private SettingsWindow? _settingsWindow;
    private HistoryWindow? _historyWindow;
    private AboutWindow? _aboutWindow;

    public AppWindows(SettingsStore settings, TrafficHistory history, UpdateChecker updates, Action testNotification)
    {
        _settings = settings;
        _history = history;
        _updates = updates;
        _testNotification = testNotification;
    }

    public void OpenSettings() => Open(ref _settingsWindow, () => new SettingsWindow
    {
        DataContext = new SettingsViewModel(_settings, _history, _testNotification, OpenHistory, new AboutViewModel(_updates)),
    });

    public void OpenHistory() => Open(ref _historyWindow, () => new HistoryWindow { DataContext = new HistoryViewModel(_history) });

    public void OpenAbout() => Open(ref _aboutWindow, () => new AboutWindow { DataContext = new AboutViewModel(_updates) });

    /// <summary>Pencere açıksa öne getirir; değilse oluşturur ve kapanınca alanı boşaltır.</summary>
    private void Open<T>(ref T? field, Func<T> create) where T : Window
    {
        if (field is not null)
        {
            field.Activate();
            return;
        }
        T window = create();
        field = window;
        window.Closed += (_, _) => Forget(window);
        window.Show();
        window.Activate();
    }

    private void Forget(Window window)
    {
        if (ReferenceEquals(window, _settingsWindow)) _settingsWindow = null;
        if (ReferenceEquals(window, _historyWindow)) _historyWindow = null;
        if (ReferenceEquals(window, _aboutWindow)) _aboutWindow = null;
    }
}
