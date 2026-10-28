using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>
/// Görev çubuğunda tek satırlık ölçüm: etiket (yazı ya da simge) + değer, isteğe bağlı mini grafik.
/// Renk: öğe rengi (yüke göre değişebilir), görünüm ayarına göre etikete, değere ya da ikisine uygulanır.
/// </summary>
public sealed class MetricLine : ObservableObject
{
    private string _value = "--";
    private string _labelText;
    private Geometry? _labelIcon;
    private int _level;
    private bool _loadColors = true;
    private string _colorTarget = StyleOptions.TargetBoth;
    private Brush _itemBrush, _labelBrush, _valueBrush;
    private IReadOnlyList<double>? _history;

    /// <param name="brushKey">Öğenin temel rengi, ör. "Brush.Item.Cpu" (Core/ThemeBrushes.cs).</param>
    public MetricLine(string label, string placeholder, string brushKey)
    {
        DefaultLabel = label;
        _labelText = label;
        Placeholder = placeholder;
        BrushKey = brushKey;
        _itemBrush = _labelBrush = _valueBrush = ThemeBrushes.Get(brushKey);
    }

    public string DefaultLabel { get; }
    public string Placeholder { get; }
    public string BrushKey { get; }
    public string Value { get => _value; set => Set(ref _value, value); }

    public string LabelText { get => _labelText; private set => Set(ref _labelText, value); }
    public Geometry? LabelIcon { get => _labelIcon; private set => Set(ref _labelIcon, value); }

    public Brush ItemBrush { get => _itemBrush; private set => Set(ref _itemBrush, value); }
    public Brush LabelBrush { get => _labelBrush; private set => Set(ref _labelBrush, value); }
    public Brush ValueBrush { get => _valueBrush; private set => Set(ref _valueBrush, value); }

    /// <summary>Mini grafik değerleri (0–1); yalnızca ağ satırlarında dolu.</summary>
    public IReadOnlyList<double>? History { get => _history; set => Set(ref _history, value); }

    /// <summary>Etiket (yazı ya da simge; ikisi de boşsa etiketsiz) ve renk kuralları.</summary>
    internal void ApplyStyle(string labelText, Geometry? labelIcon, string colorTarget, bool loadColors)
    {
        LabelText = labelIcon is null ? labelText : string.Empty;
        LabelIcon = labelIcon;
        _colorTarget = colorTarget;
        _loadColors = loadColors;
        RefreshBrush();
    }

    /// <summary>Yük düzeyi: 0 normal, 1 uyarı (% 50+), 2 yüksek (% 80+).</summary>
    public void SetLoad(double? percent) => SetLevel(percent, 50, 80);

    /// <summary>Değer uyarı / yüksek eşiğini geçince renk turuncu / kırmızı olur (yüke göre renk açıksa).</summary>
    public void SetLevel(double? value, double warn, double hot)
    {
        int level = value >= hot ? 2 : value >= warn ? 1 : 0;
        if (level == _level) return;
        _level = level;
        RefreshBrush();
    }

    /// <summary>Tema, palet, yük düzeyi ya da renk kuralı değişince çağrılır.</summary>
    public void RefreshBrush()
    {
        string key = !_loadColors ? BrushKey : _level switch { 2 => "Brush.Load.Hot", 1 => "Brush.Load.Warn", _ => BrushKey };
        ItemBrush = ThemeBrushes.Get(key);
        LabelBrush = _colorTarget is StyleOptions.TargetLabel or StyleOptions.TargetBoth ? ItemBrush : ThemeBrushes.Get("Brush.Muted");
        ValueBrush = _colorTarget is StyleOptions.TargetValue or StyleOptions.TargetBoth ? ItemBrush : ThemeBrushes.Get("Brush.Text");
    }
}

/// <summary>İki satırı birden kaplayan, kendi sütununda tek başına duran öğe (hava durumu, klavye).</summary>
public interface ITallItem
{
}

/// <summary>Caps / Num / Scroll Lock: tek öğe, tek sütunda üç satır.</summary>
public sealed class KeyboardViewModel : ITallItem
{
    public KeyboardViewModel(string caps, string num, string scroll)
    {
        Caps = new KeyIndicator(caps);
        Num = new KeyIndicator(num);
        Scroll = new KeyIndicator(scroll);
    }

    public KeyIndicator Caps { get; }
    public KeyIndicator Num { get; }
    public KeyIndicator Scroll { get; }
}

/// <summary>Kilit tuşu göstergesi (CAPS / NUM / SCRL): açıkken vurgulu, kapalıyken soluk.</summary>
public sealed class KeyIndicator : ObservableObject
{
    private bool _isOn;

    public KeyIndicator(string label) => Label = label;

    public string Label { get; }
    public bool IsOn { get => _isOn; set => Set(ref _isOn, value); }
}
