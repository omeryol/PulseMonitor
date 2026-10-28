using System.Collections.ObjectModel;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>
/// Ayarlar: görev çubuğu öğelerinin sırası ve görünürlüğü. Listede üstteki öğe görev çubuğunda soldadır;
/// her değişiklik hemen uygulanır. Ayarlara her seferinde yeni liste atanır (arka plan döngüsü okurken bozulmasın).
/// </summary>
public sealed class ItemsSettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private ItemRow? _selected;

    internal ItemsSettingsViewModel(SettingsStore store)
    {
        _store = store;
        foreach (TaskbarItemSetting i in store.Current.Items)
            Rows.Add(new ItemRow(i.Id, Strings.Get($"Item.{i.Id}"), i.Visible, Save));
        MoveUpCommand = new RelayCommand(() => Move(-1));
        MoveDownCommand = new RelayCommand(() => Move(+1));
    }

    public ObservableCollection<ItemRow> Rows { get; } = new();
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    public ItemRow? SelectedRow { get => _selected; set => Set(ref _selected, value); }

    private void Move(int delta)
    {
        if (SelectedRow is null) return;
        int from = Rows.IndexOf(SelectedRow);
        int to = from + delta;
        if (from < 0 || to < 0 || to >= Rows.Count) return;
        Rows.Move(from, to);
        Save();
    }

    private void Save()
    {
        _store.Current.Items = Rows.Select(r => new TaskbarItemSetting { Id = r.Id, Visible = r.Visible }).ToList();
        _store.Save();
    }
}

public sealed class ItemRow : ObservableObject
{
    private readonly Action _changed;
    private bool _visible;

    internal ItemRow(string id, string name, bool visible, Action changed)
    {
        Id = id;
        Name = name;
        _visible = visible;
        _changed = changed;
    }

    public string Id { get; }
    public string Name { get; }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (Set(ref _visible, value)) _changed();
        }
    }
}
