using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PulseMonitor.Interop;
using PulseMonitor.Platform;
using PulseMonitor.Settings;

namespace PulseMonitor.Tray;

/// <summary>Hem tepsi simgesinden hem görev çubuğu penceresinden açılan sağ tık menüsü.</summary>
internal sealed class AppMenu
{
    private readonly ContextMenu _menu = new();
    private readonly SettingsStore _settings;
    private readonly MenuItem _startup;
    private readonly MenuItem _fullscreen;

    public AppMenu(SettingsStore settings, Action openSettings, Action openHistory, Action refreshWeather, Action openAbout, Action exit)
    {
        _settings = settings;

        // Yeni sürüm varsa en üstte görünür (ShowUpdate).
        _update.FontWeight = FontWeights.SemiBold;
        _update.Visibility = Visibility.Collapsed;
        _update.Click += (_, _) => _openUpdate?.Invoke();
        _menu.Items.Add(_update);
        _menu.Items.Add(_updateSeparator);
        _updateSeparator.Visibility = Visibility.Collapsed;

        Add("Menu.Settings", openSettings);
        Add("Menu.History", openHistory);
        Add("Menu.TaskManager", SystemActions.OpenTaskManager);
        Add("Menu.RefreshWeather", refreshWeather);
        _menu.Items.Add(new Separator());
        _startup = Add("Menu.StartWithWindows", () => StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled()));
        _fullscreen = Add("Menu.HideInFullscreen", ToggleFullscreen);
        _menu.Items.Add(new Separator());
        Add("Menu.About", openAbout);
        Add("Menu.Exit", exit);

        _menu.Opened += OnOpened;
    }

    private readonly MenuItem _update = new();
    private readonly Separator _updateSeparator = new();
    private Action? _openUpdate;

    /// <summary>Yeni sürüm satırını gösterir ("Güncelleme var: 0.9.0").</summary>
    public void ShowUpdate(string version, Action open)
    {
        _update.Header = string.Format(Core.Strings.Get("Menu.Update"), version);
        _openUpdate = open;
        _update.Visibility = Visibility.Visible;
        _updateSeparator.Visibility = Visibility.Visible;
    }

    /// <summary>Menü açılmadan önce öne alınacak kendi gizli penceremiz (tepsi penceresi).</summary>
    public IntPtr AnchorWindow { get; set; }

    public void Show()
    {
        // Menünün dışına tıklanınca kapanabilmesi için programın önde olması gerekir.
        // Menü açıldıktan sonra öne almak menüyü hemen kapatıyor; bu yüzden önce yapılır.
        if (AnchorWindow != IntPtr.Zero) User32.SetForegroundWindow(AnchorWindow);
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    private MenuItem Add(string headerKey, Action action)
    {
        var item = new MenuItem();
        item.SetResourceReference(HeaderedItemsControl.HeaderProperty, headerKey);
        item.Click += (_, _) => action();
        _menu.Items.Add(item);
        return item;
    }

    private void ToggleFullscreen()
    {
        _settings.Current.HideInFullscreen = !_settings.Current.HideInFullscreen;
        _settings.Save();
    }

    private void OnOpened(object sender, RoutedEventArgs e)
    {
        _startup.IsChecked = StartupRegistration.IsEnabled();
        _fullscreen.IsChecked = _settings.Current.HideInFullscreen;
    }
}
