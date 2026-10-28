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
    private AppWindows? _windows;
    private readonly Alerts.AlertMonitor _alerts = new();

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
        _theme = new SystemTheme();
        _theme.Changed += () => Dispatcher.BeginInvoke(ApplyAppearance);
        ApplyAppearance(); // kaynaklar görünüm modellerinden önce hazır olmalı

        _history = LoadHistory();
        _viewModel = new TaskbarViewModel();
        _viewModel.Rebuild(_settings.Current);
        _viewModel.ApplyStyle(_settings.Current.Style);
        _weather = new WeatherService(_settings.Current);
        _weather.Updated += () => Dispatcher.BeginInvoke(UpdateWeather);
        _ip = new IpAddressMonitor(_settings.Current);
        _ip.Changed += () => Dispatcher.BeginInvoke(() => _viewModel.UpdateIp(_ip.LocalIp, _ip.PublicIp));
        _ping = new PingMonitor(_settings.Current);
        _ping.Changed += () => Dispatcher.BeginInvoke(() => _viewModel.UpdatePing(_ping.LatencyMs));
        _alerts.Raised += (title, text) => _tray?.ShowNotification(title, text, warning: true);
        _windows = new AppWindows(_settings, _history,
            () => _tray?.ShowNotification(Strings.Get("Alert.Title"), Strings.Get("Alert.Test"), warning: false));
        _settings.Changed += OnSettingsChanged;

        var menu = new AppMenu(_settings, _windows.OpenSettings, _windows.OpenHistory, () => _weather.RefreshSoon(), () => Shutdown());

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

        _hub = new MetricsHub(_settings.Current, _history);
        _hub.Sampled += snapshot => Dispatcher.BeginInvoke(() =>
        {
            _viewModel.Update(snapshot, KeyboardState.Read());
            _alerts.Check(snapshot, _history, _settings.Current.Alerts);
        });
        _hub.Start();
        _weather.Start();
        _ip.Start();
        _ping.Start();

        SessionEnding += (_, _) => TrafficHistoryStore.Save(_history);
    }

    private void OnSettingsChanged()
    {
        if (_settings is null || _viewModel is null || _weather is null) return;
        _viewModel.Rebuild(_settings.Current);
        ApplyAppearance();
        _weather.SettingsChanged();
        _ip?.SettingsChanged();
    }

    private void UpdateWeather()
    {
        if (_settings is null || _viewModel is null || _weather is null) return;
        _viewModel.Weather.Update(_settings.Current, _weather.Current);
    }

    private static TrafficHistory LoadHistory()
    {
        TrafficHistory history = TrafficHistoryStore.Load();
        if (!history.ImportedTrafficMonitor)
        {
            int days = TrafficMonitorImporter.Import(history, TrafficMonitorImporter.DefaultPath);
            history.ImportedTrafficMonitor = true;
            TrafficHistoryStore.Save(history);
            Log.Info($"TrafficMonitor geçmişinden {days} gün içe aktarıldı.");
        }
        return history;
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
        _tray?.Dispose();
        _docker?.Dispose();
        _theme?.Dispose();
        if (_instance is not null)
        {
            Log.Info("Kapandı.");
            _instance.Dispose();
        }
        base.OnExit(e);
    }
}
