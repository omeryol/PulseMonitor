using System.Collections.ObjectModel;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Settings;
using PulseMonitor.Weather;

namespace PulseMonitor.ViewModels;

/// <summary>
/// Ayarlar → Hava durumu: takip edilen konumlar. Seçili konum görünen adıyla düzenlenir, görev çubuğunda
/// gösterilebilir, yukarı taşınabilir ya da kaldırılabilir. Her değişiklik hemen kaydedilir.
/// </summary>
public sealed class WeatherLocationsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private LocationRow? _selected;
    private (WeatherLocation Location, int Index, bool WasActive)? _removed; // "Geri al" için son kaldırılan

    internal WeatherLocationsViewModel(SettingsStore store)
    {
        _store = store;
        ShowCommand = new RelayCommand(() => Change(i => W.ActiveIndex = i));
        MoveUpCommand = new RelayCommand(MoveUp);
        RemoveCommand = new RelayCommand(Remove);
        UndoRemoveCommand = new RelayCommand(UndoRemove);
        Reload(W.Active);
    }

    private WeatherSettings W => _store.Current.Weather;

    public ObservableCollection<LocationRow> Rows { get; } = new();
    public ICommand ShowCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand UndoRemoveCommand { get; }

    public bool CanUndoRemove => _removed is not null;
    public string RemovedText => _removed is { } r ? string.Format(Strings.Get("Weather.Removed"), r.Location.ShownName) : string.Empty;

    public bool HasLocations => Rows.Count > 0;
    public bool IsEmpty => Rows.Count == 0;
    public bool HasSelection => _selected is not null;

    public LocationRow? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            Raise(nameof(HasSelection));
            Raise(nameof(DisplayName));
        }
    }

    public string DisplayName
    {
        get => _selected?.Location.DisplayName ?? string.Empty;
        set
        {
            if (_selected is null) return;
            _selected.Location.DisplayName = value.Trim();
            ForgetUndo();
            _store.Save();
            Reload(_selected.Location);
        }
    }

    /// <summary>Aramadan seçilen yeri ekler ve görev çubuğunda gösterir. Aynı yer zaten varsa ona geçilir.</summary>
    internal AddResult Add(WeatherLocation l)
    {
        List<WeatherLocation> list = W.Locations;
        int existing = list.FindIndex(x => x.SamePlace(l.Latitude, l.Longitude));
        if (existing < 0 && list.Count >= WeatherSettings.MaxLocations) return AddResult.Full;

        WeatherLocation target = existing >= 0 ? list[existing] : new WeatherLocation
        {
            Name = l.Name, DisplayName = l.Name, Context = l.Context,
            Latitude = l.Latitude, Longitude = l.Longitude, Source = l.Source,
        };
        if (existing < 0) W.Locations = [.. list, target];
        W.ActiveIndex = W.Locations.IndexOf(target);
        _store.Save();
        ForgetUndo();
        Log.Info($"Hava durumu konumu {(existing < 0 ? "eklendi" : "seçildi")}: {l.Name} ({l.Context}) {l.Latitude:0.####},{l.Longitude:0.####} [{l.Source}]");
        Reload(target);
        return existing < 0 ? AddResult.Added : AddResult.Existing;
    }

    private void MoveUp() => Change(i =>
    {
        if (i == 0) return;
        WeatherLocation? active = W.Active;
        var list = W.Locations.ToList();
        (list[i - 1], list[i]) = (list[i], list[i - 1]);
        W.Locations = list;
        W.ActiveIndex = active is null ? 0 : list.IndexOf(active);
    });

    private void Remove() => Change(i =>
    {
        WeatherLocation? active = W.Active;
        var list = W.Locations.ToList();
        Log.Info($"Hava durumu konumu kaldırıldı: {list[i].Name}");
        WeatherLocation removed = list[i];
        list.RemoveAt(i);
        W.Locations = list;
        W.ActiveIndex = active is null || !list.Contains(active) ? 0 : list.IndexOf(active);
        SetUndo((removed, i, ReferenceEquals(removed, active)));
    }, keepSelection: false);

    /// <summary>Son kaldırılan konumu eski yerine (ve gösteriliyorsa yine gösterilen olarak) geri koyar.</summary>
    private void UndoRemove()
    {
        if (_removed is not { } r) return;
        var list = W.Locations.ToList();
        if (list.Count >= WeatherSettings.MaxLocations || list.Any(x => x.SamePlace(r.Location.Latitude, r.Location.Longitude)))
        {
            SetUndo(null);
            return;
        }
        WeatherLocation? active = W.Active;
        list.Insert(Math.Min(r.Index, list.Count), r.Location);
        W.Locations = list;
        W.ActiveIndex = r.WasActive || active is null ? list.IndexOf(r.Location) : list.IndexOf(active);
        _store.Save();
        Log.Info($"Hava durumu konumu geri alındı: {r.Location.Name}");
        SetUndo(null);
        Reload(r.Location);
    }

    private void ForgetUndo()
    {
        if (_removed is not null) SetUndo(null);
    }

    private void SetUndo((WeatherLocation, int, bool)? removed)
    {
        _removed = removed;
        Raise(nameof(CanUndoRemove));
        Raise(nameof(RemovedText));
    }

    private void Change(Action<int> change, bool keepSelection = true)
    {
        if (_selected is null) return;
        WeatherLocation loc = _selected.Location;
        int index = W.Locations.IndexOf(loc);
        if (index < 0) return;
        change(index);
        _store.Save();
        Reload(keepSelection ? loc : null);
    }

    private void Reload(WeatherLocation? select)
    {
        WeatherLocation? active = W.Active;
        Rows.Clear();
        foreach (WeatherLocation l in W.Locations) Rows.Add(new LocationRow(l, ReferenceEquals(l, active)));
        Selected = Rows.FirstOrDefault(r => ReferenceEquals(r.Location, select));
        Raise(nameof(HasLocations));
        Raise(nameof(IsEmpty));
    }
}

internal enum AddResult { Added, Existing, Full }

/// <summary>Listede bir konum: ad (bağlam) ve görev çubuğunda gösteriliyorsa işaret.</summary>
public sealed record LocationRow(WeatherLocation Location, bool IsActive)
{
    public string Display => Location.Context.Length > 0 ? $"{Location.ShownName} — {Location.Context}" : Location.ShownName;
    public string Marker => IsActive ? "●" : "";
}
