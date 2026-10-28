using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlarda bir öğenin rengi: #RRGGBB kutusu ve renk önizlemesi.</summary>
public sealed class ColorRow : ObservableObject
{
    private readonly Action<string, string> _changed;
    private string _hex = "#FFFFFF";
    private Brush _preview = Brushes.White;
    private bool _showing;

    internal ColorRow(string item, string name, Action<string, string> changed)
    {
        Item = item;
        Name = name;
        _changed = changed;
    }

    public string Item { get; }
    public string Name { get; }
    public Brush Preview { get => _preview; private set => Set(ref _preview, value); }

    /// <summary>Geçerli bir #RRGGBB yazılınca hemen uygulanır; yarım yazım yok sayılır.</summary>
    public string Hex
    {
        get => _hex;
        set
        {
            string v = value.Trim().ToUpperInvariant();
            if (!v.StartsWith('#')) v = "#" + v;
            if (!Set(ref _hex, v) || _showing || !StyleOptions.TryParseColor(v, out Color c)) return;
            Preview = new SolidColorBrush(c);
            _changed(Item, v);
        }
    }

    /// <summary>Görev çubuğunda o an kullanılan rengi gösterir (palet/tema değişince).</summary>
    internal void ShowCurrent()
    {
        if (ThemeBrushes.Get($"Brush.Item.{Item}") is not SolidColorBrush b) return;
        _showing = true;
        Hex = $"#{b.Color.R:X2}{b.Color.G:X2}{b.Color.B:X2}";
        _showing = false;
        Preview = b;
    }
}
