using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Metrics;
using PulseMonitor.Platform;
using PulseMonitor.Settings;
using PulseMonitor.Taskbar;
using PulseMonitor.Traffic;
using PulseMonitor.Tray;
using PulseMonitor.ViewModels;
using PulseMonitor.Views;
using PulseMonitor.Weather;

namespace PulseMonitor;

/// <summary>Uygulamanın giriş noktası: parçaları oluşturur, birbirine bağlar ve kapanışta temizler.</summary>
public partial class App : Application
{
    private SingleInstance? _instance;
    private SettingsStore? _settings;
    private SystemTheme? _theme;
    private TrafficHistory? _history;
    private MetricsHub? _hub;
    private WeatherService? _weather;
    private IpAddressMonitor? _ip;
    private TaskbarViewModel? _viewModel;
    private TaskbarWindow? _window;
    private TaskbarDocker? _docker;
    private TrayIcon? _tray;
    private PingMonitor? _ping;
    private UpdateChecker? _updates;
    private bool _updateNotificationShown;
    private AppWindows? _windows;
    private readonly Alerts.AlertMonitor _alerts = new();
    private readonly Weather.Warnings.WeatherWarningNotifier _weatherWarnings = new();

    // Kaynak sözlükleri (App.xaml) OnStartup'tan önce yüklenir; oradaki bir hata da günlüğe düşsün.
    static App() => AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        Log.Error("Beklenmeyen hata.", a.ExceptionObject as Exception);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InstallErrorHandlers();

        // Küçük pencereler için işlemciyle çizim yeterli. Ekran kartıyla çizim özel belleği 89 → 51 MB,
        // iş parçacığını 38 → 19 artırıyordu. (GC ayarı ve ReadyToRun denendi, fark yaratmadı.)
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        if (e.Args.Contains("--exit"))
        {
            SingleInstance.SignalExit();
            Shutdown();
            return;
        }

        _instance = SingleInstance.TryAcquire();
        if (_instance is null)
        {
            Log.Info("Program zaten çalışıyor; ikinci kopya kapatılıyor.");
            Shutdown();
            return;
        }
        _instance.ExitRequested += () => Dispatcher.BeginInvoke(() => Shutdown());
        Log.Info($"Başladı. Sürüm {typeof(App).Assembly.GetName().Version}, {Environment.ProcessPath}");

        _settings = SettingsStore.Load();
        UiLanguage.Apply(this, _settings.Current.Language); // metinler her şeyden önce hazır olmalı
        _theme = new SystemTheme();
        _theme.Changed += () => Dispatcher.BeginInvoke(ApplyAppearance);
        ApplyAppearance(); // kaynaklar görünüm modellerinden önce hazır olmalı

        _history = TrafficHistoryStore.Load();
        _viewModel = new TaskbarViewModel();
        _viewModel.Rebuild(_settings.Current);
        _viewModel.ApplyStyle(_settings.Current.Style);
        _weather = new WeatherService(_settings.Current);
        _weather.Updated += () => Dispatcher.BeginInvoke(UpdateWeather);
        _viewModel.Weather.SwitchRequested += () => { if (_settings.Current.Weather.SelectNext()) _settings.Save(); };
        _ip = new IpAddressMonitor(_settings.Current);
        _ip.Changed += () => Dispatcher.BeginInvoke(() => _viewModel.UpdateIp(_ip.LocalIp, _ip.PublicIp));
        _ping = new PingMonitor(_settings.Current);
        _ping.Changed += () => Dispatcher.BeginInvoke(() => _viewModel.UpdatePing(_ping.LatencyMs));
        _alerts.Raised += (title, text) => _tray?.ShowNotification(title, text, warning: true);
        _weatherWarnings.Raised += (title, text, iconKey) =>
            _tray?.ShowNotification(title, text, warning: true, Resources[$"Wx.{iconKey}"] as System.Windows.Media.ImageSource);
        _updates = new UpdateChecker(_settings.Current);
        _windows = new AppWindows(_settings, _history, _updates,
            () => _tray?.ShowNotification(Strings.Get("Alert.Title"), Strings.Get("Alert.Test"), warning: false));
        _settings.Changed += OnSettingsChanged;

        var menu = new AppMenu(_settings, _windows.OpenSettings, _windows.OpenHistory, () => _weather.RefreshSoon(),
            _windows.OpenAbout, () => Shutdown());
        _updates.Changed += () => Dispatcher.BeginInvoke(() => OnUpdateChecked(menu));

        _window = new TaskbarWindow { DataContext = _viewModel };
        _window.MouseRightButtonUp += (_, _) => menu.Show();
        _window.MouseLeftButtonDown += OnWindowLeftButtonDown;
        _window.SetScale(_settings.Current.Style.Scale / 100.0);
        _docker = new TaskbarDocker(_window, _settings.Current);
        _window.Loaded += (_, _) => _docker.Start();
        _window.Show();

        _tray = new TrayIcon(AppPaths.TrayIconFile, Strings.Get("Tray.Tooltip"));
        menu.AnchorWindow = _tray.Handle;
        _tray.RightClick += menu.Show;
        _tray.LeftDoubleClick += _windows.OpenSettings;
        _tray.NotificationClicked += () =>
        {
            if (_updateNotificationShown) _windows.OpenUpdate();
        };

        _hub = new MetricsHub(_settings.Current, _history);
        _hub.Sampled += snapshot => Dispatcher.BeginInvoke(() =>
        {
            KeyboardSnapshot keys = KeyboardState.Read();
            _viewModel.Update(snapshot, keys);
            _viewModel.UpdateDetails(new DetailContext(snapshot, keys, _history.Get(DateOnly.FromDateTime(DateTime.Now)),
                _ip.LocalIp, _ip.PublicIp, _settings.Current.PingHost, _ping.Stats, _ping.LatencyMs));
            _alerts.Check(snapshot, _history, _settings.Current.Alerts);
        });
        _hub.Start();
        _weather.Start();
        if (e.Args.Contains("--settings")) _windows.OpenSettings(); // dil değişince yeniden başlatıldı
        _ip.Start();
        _ping.Start();
        _updates.Start();

        SessionEnding += (_, _) => TrafficHistoryStore.Save(_history);
    }

    private void OnSettingsChanged()
    {
        if (_settings is null || _viewModel is null || _weather is null) return;
        if (_settings.Current.Language != UiLanguage.Setting)
        {
            RestartForLanguage();
            return;
        }
        _viewModel.Rebuild(_settings.Current);
        ApplyAppearance();
        _weather.SettingsChanged();
        _ip?.SettingsChanged();
    }

    private string? _restartArgs;

    /// <summary>
    /// Dil değişince program kendini yeniden başlatır: metinlerin bir kısmı (etiketler, kart başlıkları, listeler)
    /// açılışta kurulur. Ayarlar penceresi açıksa yeni kopyada yeniden açılır.
    /// </summary>
    private void RestartForLanguage()
    {
        if (_restartArgs is not null) return;
        _restartArgs = _windows?.IsSettingsOpen == true ? "--settings" : "";
        Log.Info($"Dil değişti ({UiLanguage.Setting} → {_settings?.Current.Language}); yeniden başlatılıyor.");
        Dispatcher.BeginInvoke(() => Shutdown());
    }

    /// <summary>Yeni sürüm varsa menüde satır gösterir; o sürüm için ilk kez ise bildirim verir.</summary>
    private void OnUpdateChecked(AppMenu menu)
    {
        if (_updates is null || _settings is null || _updates.State != UpdateState.Available || _updates.LatestVersion is null) return;
        menu.ShowUpdate(_updates.LatestVersion, () => _windows?.OpenUpdate());
        if (!_updates.ShouldNotify(_settings)) return;
        _updateNotificationShown = true;
        _tray?.ShowNotification(Strings.Get("Update.NotifyTitle"),
            string.Format(Strings.Get("Update.NotifyText"), _updates.LatestVersion), warning: false);
    }

    private void UpdateWeather()
    {
        if (_settings is null || _viewModel is null || _weather is null) return;
        _viewModel.Weather.Update(_settings.Current, _weather.ReportFor);
        _weatherWarnings.Check(_settings.Current, _weather.ReportFor);
    }

    private static void OnWindowLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) SystemActions.OpenTaskManager();
    }

    /// <summary>Görünüm ayarlarını ve temayı uygular: kaynaklar, satır renkleri, hava öğesi, ölçek.</summary>
    private void ApplyAppearance()
    {
        if (_settings is null) return;
        StyleSettings style = _settings.Current.Style;
        StyleResources.Apply(this, style);
        WindowTheme.Apply(this);
        ThemeBrushes.Apply(this, ThemeBrushes.IsLight(style, SystemTheme.IsTaskbarLight()), style);
        _viewModel?.ApplyStyle(style);
        UpdateWeather();
        _window?.SetScale(style.Scale / 100.0);
    }

    private void InstallErrorHandlers()
    {
        DispatcherUnhandledException += (_, a) =>
        {
            Log.Error("Arayüzde beklenmeyen hata.", a.Exception);
            a.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Log.Error("Arka plan görevinde hata.", a.Exception);
            a.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hub?.Dispose();
        _weather?.Dispose();
        _ip?.Dispose();
        _ping?.Dispose();
        _updates?.Dispose();
        _tray?.Dispose();
        _docker?.Dispose();
        _theme?.Dispose();
        if (_instance is not null)
        {
            Log.Info("Kapandı.");
            _instance.Dispose();
            if (_restartArgs is not null) SystemActions.StartSelf(_restartArgs); // kilit bırakıldı, yeni kopya açılabilir
        }
        base.OnExit(e);
    }
}
