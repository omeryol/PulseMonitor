using System.Collections.ObjectModel;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Metrics;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Görev çubuğu penceresinin içeriği: öğeler ve ayarlardaki sıraya göre dizilmiş sütunlar.</summary>
public sealed class TaskbarViewModel
{
    private readonly Dictionary<string, object> _items;
    private readonly SpeedHistory _upHistory = new();
    private readonly SpeedHistory _downHistory = new();

    public TaskbarViewModel()
    {
        _items = new Dictionary<string, object>
        {
            [TaskbarItemId.Weather] = Weather,
            [TaskbarItemId.Keyboard] = Keyboard,
            [TaskbarItemId.Upload] = Upload,
            [TaskbarItemId.Download] = Download,
            [TaskbarItemId.Cpu] = Cpu,
            [TaskbarItemId.Ram] = Ram,
            [TaskbarItemId.Gpu] = Gpu,
            [TaskbarItemId.Disk] = Disk,
            [TaskbarItemId.LocalIp] = LocalIp,
            [TaskbarItemId.PublicIp] = PublicIp,
            [TaskbarItemId.GpuTemp] = GpuTemp,
            [TaskbarItemId.DiskTemp] = DiskTemp,
            [TaskbarItemId.Ping] = Ping,
        };

        // Ayrıntı kartları: hava hariç her öğe (havanın kendi renkli kartı var). Başlık rengi öğenin rengi.
        foreach (var (id, item) in _items.Where(kv => kv.Key != TaskbarItemId.Weather))
            _cards[id] = new DetailCard(id, Strings.Get($"Detail.Title.{id}"),
                item is MetricLine line ? line.BrushKey : "Brush.Item.Keyboard");
    }

    private readonly Dictionary<string, DetailCard> _cards = new();

    /// <summary>Ayrıntı kartlarını yeniler (her ölçümde; kart açık değilse yalnızca birkaç metin üretir).</summary>
    public void UpdateDetails(DetailContext context)
    {
        DetailCards.TrackPeaks(context.Metrics.Network.DownBps, context.Metrics.Network.UpBps);
        foreach (DetailCard card in _cards.Values) card.Rows = DetailCards.Rows(card.Id, context);
    }

    public WeatherViewModel Weather { get; } = new();
    public KeyboardViewModel Keyboard { get; } =
        new(Strings.Get("Key.Caps"), Strings.Get("Key.Num"), Strings.Get("Key.Scroll"));
    public MetricLine Upload { get; } = Line("Label.Up", ValueFormat.SpeedPlaceholder, "Upload");
    public MetricLine Download { get; } = Line("Label.Down", ValueFormat.SpeedPlaceholder, "Download");
    public MetricLine Cpu { get; } = Line("Label.Cpu", ValueFormat.PercentPlaceholder, "Cpu");
    public MetricLine Ram { get; } = Line("Label.Ram", ValueFormat.PercentPlaceholder, "Ram");
    public MetricLine Gpu { get; } = Line("Label.Gpu", ValueFormat.PercentPlaceholder, "Gpu");
    public MetricLine Disk { get; } = Line("Label.Disk", ValueFormat.PercentPlaceholder, "Disk");
    public MetricLine LocalIp { get; } = Line("Label.LocalIp", ValueFormat.IpPlaceholder, "Ip");
    public MetricLine PublicIp { get; } = Line("Label.PublicIp", ValueFormat.IpPlaceholder, "Ip");
    public MetricLine GpuTemp { get; } = Line("Label.GpuTemp", ValueFormat.TemperaturePlaceholder, "Gpu");
    public MetricLine DiskTemp { get; } = Line("Label.DiskTemp", ValueFormat.TemperaturePlaceholder, "Disk");
    public MetricLine Ping { get; } = Line("Label.Ping", ValueFormat.PingPlaceholder, "Ping");

    /// <summary>Ping: 100 ms üstü uyarı, 200 ms üstü yüksek.</summary>
    public void UpdatePing(double? ms)
    {
        Ping.Value = ValueFormat.Milliseconds(ms);
        Ping.SetLevel(ms, 100, 200);
    }

    public ObservableCollection<TaskbarColumn> Columns { get; } = new();

    private static MetricLine Line(string labelKey, string placeholder, string item) =>
        new(Strings.Get(labelKey), placeholder, $"Brush.Item.{item}");

    /// <summary>Sıra/görünürlük değişince sütunları yeniden kurar; değişiklik yoksa dokunmaz (titreme olmaz).</summary>
    internal void Rebuild(AppSettings s)
    {
        var visible = s.Items
            .Where(i => i.Visible && _items.ContainsKey(i.Id))
            .Where(i => i.Id != TaskbarItemId.Weather || s.Weather.Active is not null)
            .Select(i => (_items[i.Id], _cards.GetValueOrDefault(i.Id)));

        List<TaskbarColumn> columns = TaskbarLayout.Build(visible);
        if (columns.SequenceEqual(Columns)) return;

        Columns.Clear();
        foreach (TaskbarColumn c in columns) Columns.Add(c);
    }

    /// <summary>Tema ya da görünüm değişti: etiketler, renk kuralları ve renkler yeniden uygulanır.</summary>
    internal void ApplyStyle(StyleSettings s)
    {
        foreach (var (id, line) in _items.Where(kv => kv.Value is MetricLine).Select(kv => (kv.Key, (MetricLine)kv.Value)))
        {
            var (text, icon) = LabelFor(id, line.DefaultLabel, s);
            line.ApplyStyle(text, icon, s.ColorTarget, s.LoadColors);
        }
        foreach (DetailCard card in _cards.Values) card.RefreshAccent();
        Weather.RefreshBrush();
    }

    private static readonly Dictionary<string, (string Up, string Down)> ArrowText = new()
    {
        [StyleOptions.ArrowThin] = ("↑", "↓"),
        [StyleOptions.ArrowTriangle] = ("▲", "▼"),
        [StyleOptions.ArrowDashed] = ("⇡", "⇣"),
        [StyleOptions.ArrowText] = ("UP", "DN"),
    };

    /// <summary>Ağ satırlarında ok ayarı, diğerlerinde etiket ayarı (yazı / simge / yok) geçerlidir.</summary>
    private static (string Text, System.Windows.Media.Geometry? Icon) LabelFor(string id, string defaultLabel, StyleSettings s)
    {
        bool isNet = id is TaskbarItemId.Upload or TaskbarItemId.Download;
        string iconName = id switch
        {
            TaskbarItemId.LocalIp or TaskbarItemId.PublicIp => "Ip",
            TaskbarItemId.GpuTemp or TaskbarItemId.DiskTemp => "Temp",
            _ => id,
        };
        var icon = System.Windows.Application.Current?.TryFindResource($"LabelIcon.{iconName}") as System.Windows.Media.Geometry;

        if (isNet)
        {
            if (s.Arrows == StyleOptions.ArrowIcon) return ("", icon);
            var (up, down) = ArrowText.GetValueOrDefault(s.Arrows, ArrowText[StyleOptions.ArrowTriangle]);
            return (id == TaskbarItemId.Upload ? up : down, null);
        }
        return s.Labels switch
        {
            StyleOptions.LabelsIcon when id != TaskbarItemId.PublicIp => ("", icon),
            StyleOptions.LabelsNone => ("", null),
            _ => (defaultLabel, null), // Dış IP simge modunda da yazıyla kalır (yerel IP ile karışmasın)
        };
    }

    public void Update(MetricsSnapshot m, KeyboardSnapshot keys)
    {
        Upload.Value = ValueFormat.Speed(m.Network.UpBps);
        Download.Value = ValueFormat.Speed(m.Network.DownBps);
        Upload.History = _upHistory.Add(m.Network.UpBps);
        Download.History = _downHistory.Add(m.Network.DownBps);

        SetPercent(Cpu, m.CpuPercent);
        SetPercent(Ram, m.RamPercent);
        SetPercent(Gpu, m.GpuPercent);
        SetPercent(Disk, m.DiskPercent);
        GpuTemp.Value = ValueFormat.Celsius(m.GpuTempC);
        GpuTemp.SetLevel(m.GpuTempC, 70, 85);
        DiskTemp.Value = ValueFormat.Celsius(m.DiskTempC);
        DiskTemp.SetLevel(m.DiskTempC, 55, 70);

        Keyboard.Caps.IsOn = keys.Caps;
        Keyboard.Num.IsOn = keys.Num;
        Keyboard.Scroll.IsOn = keys.Scroll;
    }

    private static void SetPercent(MetricLine line, double? percent)
    {
        line.Value = ValueFormat.Percent(percent);
        line.SetLoad(percent);
    }

    public void UpdateIp(string local, string @public)
    {
        LocalIp.Value = local;
        PublicIp.Value = @public;
    }
}
