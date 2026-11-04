using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Interop;
using PulseMonitor.Metrics;
using PulseMonitor.Platform;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.Taskbar;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar penceresi. Her değişiklik hemen uygulanır ve kaydedilir.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    internal SettingsStore Store => _store;
    internal TrafficHistory History { get; }
    internal PingMonitor PingMonitor { get; }
    private AppSettings S => _store.Current;

    internal SettingsViewModel(SettingsStore store, TrafficHistory history, PingMonitor ping, Action testNotification, Action openHistory, AboutViewModel about)
    {
        _store = store;
        History = history;
        PingMonitor = ping;
        About = about;
        Alerts = new AlertsSettingsViewModel(store, testNotification);
        OpenHistoryCommand = new RelayCommand(openHistory);
        Weather = new WeatherSettingsViewModel(store);
        Items = new ItemsSettingsViewModel(store);
        Style = new StyleSettingsViewModel(store);
        var gpus = GraphicsAdapter.Read().OrderBy(gpu => gpu.Id, StringComparer.Ordinal).ToList();
        var gpuOptions = new List<AdapterOption> { new("", Strings.Get("Settings.GpuAuto")) };
        for (int i = 0; i < gpus.Count; i++)
        {
            var gpu = gpus[i];
            gpuOptions.Add(new AdapterOption(gpu.Id, gpu.Name));
            for (int j = i + 1; j < gpus.Count; j++)
                gpuOptions.Add(new AdapterOption($"{gpu.Id}|{gpus[j].Id}", $"{gpu.Name} + {gpus[j].Name}"));
        }
        if (!string.IsNullOrEmpty(S.GpuSelection) && gpuOptions.All(o => o.Id != S.GpuSelection))
            gpuOptions.Add(new AdapterOption(S.GpuSelection, Strings.Get("Settings.GpuMissing")));
        GpuOptions = gpuOptions;
        HasGpuOptions = gpus.Count > 1;
        Adapters = BuildAdapterOptions(S.NetworkAdapterId);
        var monitors = new List<MonitorOption> { new("", Strings.Get("Taskbar.MonitorAuto")) };
        int number = 0;
        foreach (var (device, primary) in TaskbarLocator.Displays())
            monitors.Add(new MonitorOption(device, $"{Strings.Get("Taskbar.Monitor")} {++number}{(primary ? " ★" : "")}"));
        if (!string.IsNullOrEmpty(S.TaskbarMonitor) && monitors.All(x => x.Id != S.TaskbarMonitor))
            monitors.Add(new MonitorOption(S.TaskbarMonitor, Strings.Get("Taskbar.MonitorMissing")));
        Monitors = monitors;
        DayTraffic today = history.Get(DateOnly.FromDateTime(DateTime.Now));
        TodayText = $"↓ {ValueFormat.Size(today.Down)}    ↑ {ValueFormat.Size(today.Up)}";
    }

    public ItemsSettingsViewModel Items { get; }
    public StyleSettingsViewModel Style { get; }
    public AlertsSettingsViewModel Alerts { get; }
    public AboutViewModel About { get; }

    public bool CheckForUpdates { get => S.CheckForUpdates; set => Update(() => S.CheckForUpdates = value); }
    public System.Windows.Input.ICommand OpenHistoryCommand { get; }

    /// <summary>Ping sunucusu; boş bırakılırsa 1.1.1.1.</summary>
    public string PingHost
    {
        get => S.PingHost;
        set => Update(() => S.PingHost = string.IsNullOrWhiteSpace(value) ? "1.1.1.1" : value.Trim());
    }

    public bool HideInFullscreen { get => S.HideInFullscreen; set => Update(() => S.HideInFullscreen = value); }

    public int TrayGapPx
    {
        get => S.TrayGapPx;
        set
        {
            int gap = Math.Clamp(value, 0, 200);
            if (gap == S.TrayGapPx) return;
            S.TrayGapPx = gap;
            Raise(nameof(TrayGapPx));
            _store.Save();
        }
    }

    public bool StartWithWindows
    {
        get => StartupRegistration.IsEnabled();
        set => StartupRegistration.SetEnabled(value);
    }

    /// <summary>Arayüz dili; değişince program yeniden başlar (App.OnSettingsChanged).</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } = UiLanguage.Options();

    public LanguageOption? SelectedLanguage
    {
        get => Languages.FirstOrDefault(l => l.Code == S.Language) ?? Languages[0];
        set
        {
            if (value is null || value.Code == S.Language) return;
            Update(() => S.Language = value.Code);
        }
    }

    public IReadOnlyList<AdapterOption> GpuOptions { get; }
    public bool HasGpuOptions { get; }
    public AdapterOption SelectedGpu
    {
        get => GpuOptions.FirstOrDefault(o => o.Id == S.GpuSelection) ?? GpuOptions[0];
        set
        {
            if (value is not null && value.Id != (S.GpuSelection ?? ""))
                Update(() => S.GpuSelection = value.Id.Length == 0 ? null : value.Id);
        }
    }

    public IReadOnlyList<AdapterOption> Adapters { get; }
    public IReadOnlyList<MonitorOption> Monitors { get; }
    public MonitorOption? SelectedMonitor
    {
        get => Monitors.FirstOrDefault(x => x.Id == (S.TaskbarMonitor ?? "")) ?? Monitors[0];
        set { if (value is not null && value.Id != (S.TaskbarMonitor ?? "")) Update(() => S.TaskbarMonitor = value.Id.Length == 0 ? null : value.Id); }
    }
    public bool TaskbarLeft { get => S.TaskbarLeft; set => Update(() => S.TaskbarLeft = value); }

    public AdapterOption? SelectedAdapter
    {
        get => Adapters.FirstOrDefault(a => a.Id == (S.NetworkAdapterId ?? string.Empty));
        set
        {
            if (value is null) return;
            Update(() => S.NetworkAdapterId = value.Id.Length == 0 ? null : value.Id);
        }
    }

    public string TodayText { get; }

    public WeatherSettingsViewModel Weather { get; }

    private void Update(Action change)
    {
        change();
        _store.Save();
    }

    private static List<AdapterOption> BuildAdapterOptions(string? savedId)
    {
        var options = new List<AdapterOption> { new(string.Empty, Strings.Get("Settings.AdapterAuto")) };
        foreach (NetworkAdapterInfo a in NetworkAdapters.Read()
                     .OrderByDescending(a => a.IsUp).ThenByDescending(a => a.IsPhysical).ThenBy(a => a.Name))
        {
            string state = Strings.Get(a.IsUp ? "Settings.AdapterUp" : "Settings.AdapterDown");
            options.Add(new AdapterOption(a.Id.ToString(), $"{a.Name} — {a.Description} ({state})"));
        }

        // Kayıtlı bağdaştırıcı şu an yoksa seçim kaybolmasın.
        if (!string.IsNullOrEmpty(savedId) && options.All(o => o.Id != savedId))
            options.Add(new AdapterOption(savedId, Strings.Get("Settings.AdapterMissing")));
        return options;
    }
}

public sealed record AdapterOption(string Id, string Display);
public sealed record MonitorOption(string Id, string Display);
