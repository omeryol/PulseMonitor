using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar penceresindeki hava durumu bölümü: tahmin modeli, konum arama ve takip edilen konumlar.</summary>
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
        Results.CollectionChanged += (_, _) => Raise(nameof(HasResults));
        Locations = new WeatherLocationsViewModel(store);
        Models = OpenMeteoModels.All
            .Select(m => new Option(m.Id, m.Id == OpenMeteoModels.Auto ? Strings.Get("Weather.Model.Auto") : m.Name))
            .ToList();
    }

    public WeatherLocationsViewModel Locations { get; }
    public ObservableCollection<LocationOption> Results { get; } = new();
    public ICommand SearchCommand { get; }
    public bool HasResults => Results.Count > 0;

    public string Query { get => _query; set => Set(ref _query, value); }
    public string Status
    {
        get => _status;
        private set
        {
            if (Set(ref _status, value)) Raise(nameof(HasStatus));
        }
    }

    public bool HasStatus => _status.Length > 0;
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }

    public IReadOnlyList<Option> Models { get; }

    public Option? SelectedModel
    {
        get => Models.FirstOrDefault(m => m.Id == _store.Current.Weather.Model);
        set
        {
            if (value is null || value.Id == _store.Current.Weather.Model) return;
            _store.Current.Weather.Model = value.Id;
            _store.Save();
            Log.Info($"Hava durumu modeli: {value.Id}");
            Raise(nameof(SelectedModel));
        }
    }

    /// <summary>Listeden seçilen sonuç konumlara eklenir ve görev çubuğunda gösterilir.</summary>
    public LocationOption? SelectedResult
    {
        get => null;
        set
        {
            if (value is null) return;
            Status = Locations.Add(value.Location) switch
            {
                AddResult.Full => string.Format(Strings.Get("Weather.Limit"), WeatherSettings.MaxLocations),
                _ => string.Format(Strings.Get("Weather.Added"), value.Location.Name),
            };
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
}

public sealed record LocationOption(WeatherLocation Location)
{
    public string Display => Location.Context.Length > 0
        ? $"{Location.Name} — {Location.Context}   · {Location.Source}"
        : $"{Location.Name}   · {Location.Source}";
}
