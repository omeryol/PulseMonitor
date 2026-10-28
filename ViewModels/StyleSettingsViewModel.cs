using System.Collections.ObjectModel;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar → Görünüm ve hava durumu görünümü. Her değişiklik hemen kaydedilir ve görev çubuğuna uygulanır.</summary>
public sealed class StyleSettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private StyleSettings S => _store.Current.Style;

    internal StyleSettingsViewModel(SettingsStore store)
    {
        _store = store;
        Choice C(string group, Func<StyleSettings, string> get, Action<StyleSettings, string> set) =>
            new(group, () => get(S), v => Update(() => set(S, v)));

        Theme = C("Theme", s => s.Theme, (s, v) => s.Theme = v);
        Palette = C("Palette", s => s.Palette, (s, v) => s.Palette = v);
        ColorTarget = C("ColorTarget", s => s.ColorTarget, (s, v) => s.ColorTarget = v);
        ValueWeight = C("ValueWeight", s => s.ValueWeight, (s, v) => s.ValueWeight = v);
        Arrows = C("Arrows", s => s.Arrows, (s, v) => s.Arrows = v);
        Labels = C("Labels", s => s.Labels, (s, v) => s.Labels = v);
        Sparkline = C("Sparkline", s => s.Sparkline, (s, v) => s.Sparkline = v);
        KeyboardStyle = C("KeyboardStyle", s => s.KeyboardStyle, (s, v) => s.KeyboardStyle = v);
        Background = C("Background", s => s.Background, (s, v) => s.Background = v);
        WeatherIcons = C("WeatherIcons", s => s.WeatherIcons, (s, v) => s.WeatherIcons = v);
        TemperatureUnit = C("TemperatureUnit", s => s.TemperatureUnit, (s, v) => s.TemperatureUnit = v);
        WeatherLabel = C("WeatherLabel", s => s.WeatherLabel, (s, v) => s.WeatherLabel = v);

        foreach (string item in StyleOptions.ColorItems)
            CustomColors.Add(new ColorRow(item, Strings.Get($"Style.ColorItem.{item}"), SetCustomColor));
        RefreshColorRows();
        ResetCommand = new RelayCommand(Reset);
    }

    public Choice Theme { get; }
    public Choice Palette { get; }
    public Choice ColorTarget { get; }
    public Choice ValueWeight { get; }
    public Choice Arrows { get; }
    public Choice Labels { get; }
    public Choice Sparkline { get; }
    public Choice KeyboardStyle { get; }
    public Choice Background { get; }
    public Choice WeatherIcons { get; }
    public Choice TemperatureUnit { get; }
    public Choice WeatherLabel { get; }

    public bool LoadColors { get => S.LoadColors; set => Update(() => S.LoadColors = value); }
    public bool Separator { get => S.Separator; set => Update(() => S.Separator = value); }
    public bool TemperatureColors { get => S.TemperatureColors; set => Update(() => S.TemperatureColors = value); }
    public bool ShowFeelsLike { get => S.ShowFeelsLike; set => Update(() => S.ShowFeelsLike = value); }
    public bool WeatherAnimation { get => S.WeatherAnimation; set => Update(() => S.WeatherAnimation = value); }

    public int FontSize { get => S.FontSize; set => Update(() => S.FontSize = Math.Clamp(value, StyleOptions.MinFont, StyleOptions.MaxFont)); }
    public int BackgroundOpacity { get => S.BackgroundOpacity; set => Update(() => S.BackgroundOpacity = Math.Clamp(value, StyleOptions.MinOpacity, StyleOptions.MaxOpacity)); }
    public int ColumnGap { get => S.ColumnGap; set => Update(() => S.ColumnGap = Math.Clamp(value, StyleOptions.MinColumnGap, StyleOptions.MaxColumnGap)); }
    public int LabelGap { get => S.LabelGap; set => Update(() => S.LabelGap = Math.Clamp(value, StyleOptions.MinLabelGap, StyleOptions.MaxLabelGap)); }
    public int Scale { get => S.Scale; set => Update(() => S.Scale = Math.Clamp(value, StyleOptions.MinScale, StyleOptions.MaxScale)); }

    public int MinFont => StyleOptions.MinFont;
    public int MaxFont => StyleOptions.MaxFont;
    public int MinColumnGap => StyleOptions.MinColumnGap;
    public int MaxColumnGap => StyleOptions.MaxColumnGap;
    public int MinLabelGap => StyleOptions.MinLabelGap;
    public int MaxLabelGap => StyleOptions.MaxLabelGap;
    public int MinScale => StyleOptions.MinScale;
    public int MaxScale => StyleOptions.MaxScale;

    /// <summary>Öğe renkleri (#RRGGBB). Birini değiştirmek paleti "Özel"e geçirir.</summary>
    public ObservableCollection<ColorRow> CustomColors { get; } = new();
    public ICommand ResetCommand { get; }

    private void SetCustomColor(string item, string hex)
    {
        if (S.Palette != StyleOptions.PaletteCustom)
        {
            // Özel palete geçerken diğer öğeler o an görünen renklerini korusun.
            foreach (ColorRow row in CustomColors) S.CustomColors[row.Item] = row.Hex;
            S.Palette = StyleOptions.PaletteCustom;
        }
        S.CustomColors[item] = hex;
        _store.Save();
        Palette.Refresh();
    }

    private void Reset()
    {
        _store.Current.Style = new StyleSettings();
        _store.Save();
        Raise(null); // tüm özellikler yeniden okunur
        foreach (Choice c in new[] { Theme, Palette, ColorTarget, ValueWeight, Arrows, Labels, Sparkline, KeyboardStyle, Background, WeatherIcons, TemperatureUnit, WeatherLabel })
            c.Refresh();
        RefreshColorRows();
    }

    private void Update(Action change)
    {
        change();
        _store.Save();      // görev çubuğu hemen güncellenir (App.OnSettingsChanged)
        RefreshColorRows(); // palet ya da tema değiştiyse renk kutuları yeni renkleri göstersin
    }

    private void RefreshColorRows()
    {
        foreach (ColorRow row in CustomColors) row.ShowCurrent();
    }
}
