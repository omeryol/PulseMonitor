using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Metrics;
using PulseMonitor.Platform;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar penceresi. Her değişiklik hemen uygulanır ve kaydedilir.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private AppSettings S => _store.Current;

    internal SettingsViewModel(SettingsStore store, TrafficHistory history, Action testNotification, Action openHistory, AboutViewModel about)
    {
        _store = store;
        About = about;
        Alerts = new AlertsSettingsViewModel(store, testNotification);
        OpenHistoryCommand = new RelayCommand(openHistory);
        Weather = new WeatherSettingsViewModel(store);
        Items = new ItemsSettingsViewModel(store);
        Style = new StyleSettingsViewModel(store);
        Adapters = BuildAdapterOptions(S.NetworkAdapterId);
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
        set => Update(() => S.TrayGapPx = Math.Clamp(value, 0, 200));
    }

    public bool StartWithWindows
    {
        get => StartupRegistration.IsEnabled();
        set => StartupRegistration.SetEnabled(value);
    }

    public IReadOnlyList<AdapterOption> Adapters { get; }

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
