using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar penceresindeki hava durumu bölümü: konum arama, seçme, görünen adı değiştirme.</summary>
public sealed class WeatherSettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private string _query = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;
    private CancellationTokenSource? _search;

    internal WeatherSettingsViewModel(SettingsStore store)
    {
        _store = store;
        SearchCommand = new RelayCommand(Search);
    }

    private WeatherLocation? Location => _store.Current.WeatherLocation;

    public ObservableCollection<LocationOption> Results { get; } = new();
    public ICommand SearchCommand { get; }

    public string Query { get => _query; set => Set(ref _query, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }

    public bool HasLocation => Location is not null;

    public string CurrentText => Location is { } l
        ? (l.Context.Length > 0 ? $"{l.Name} ({l.Context})" : l.Name)
        : Strings.Get("Weather.NoLocation");

    public string DisplayName
    {
        get => Location?.DisplayName ?? string.Empty;
        set
        {
            if (Location is null) return;
            Location.DisplayName = value.Trim();
            _store.Save();
        }
    }

    /// <summary>Listeden seçilen sonuç hemen konum olarak kaydedilir.</summary>
    public LocationOption? SelectedResult
    {
        get => null;
        set
        {
            if (value is null) return;
            WeatherLocation l = value.Location;
            _store.Current.WeatherLocation = new WeatherLocation
            {
                Name = l.Name, DisplayName = l.Name, Context = l.Context,
                Latitude = l.Latitude, Longitude = l.Longitude, Source = l.Source,
            };
            _store.Save();
            Log.Info($"Hava durumu konumu: {l.Name} ({l.Context}) {l.Latitude:0.####},{l.Longitude:0.####} [{l.Source}]");
            OnLocationChanged();
        }
    }

    private async void Search()
    {
        string query = Query.Trim();
        if (query.Length < 2)
        {
            Status = Strings.Get("Weather.Search.TooShort");
            return;
        }

        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();
        IsBusy = true;
        Status = Strings.Get("Weather.Search.Busy");
        Results.Clear();
        try
        {
            List<WeatherLocation> found = await LocationSearch.SearchAsync(query, cts.Token);
            if (cts.IsCancellationRequested) return;
            foreach (WeatherLocation l in found) Results.Add(new LocationOption(l));
            Status = found.Count == 0
                ? Strings.Get("Weather.Search.None")
                : string.Format(Strings.Get("Weather.Search.Count"), found.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (cts.IsCancellationRequested) return;
            Log.Warn($"Konum araması başarısız: {query}", ex);
            Status = Strings.Get("Weather.Search.Failed");
        }
        finally
        {
            if (ReferenceEquals(_search, cts)) IsBusy = false;
        }
    }

    private void OnLocationChanged()
    {
        Raise(nameof(HasLocation));
        Raise(nameof(CurrentText));
        Raise(nameof(DisplayName));
    }
}

public sealed record LocationOption(WeatherLocation Location)
{
    public string Display => Location.Context.Length > 0
        ? $"{Location.Name} — {Location.Context}   · {Location.Source}"
        : $"{Location.Name}   · {Location.Source}";
}
