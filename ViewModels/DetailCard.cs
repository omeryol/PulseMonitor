using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Metrics;
using PulseMonitor.Traffic;

namespace PulseMonitor.ViewModels;

/// <summary>Bir öğenin fare üzerine gelince açılan ayrıntı kartı: başlık (öğe renginde) ve satırlar. Her saniye yenilenir.</summary>
public sealed class DetailCard : ObservableObject
{
    private IReadOnlyList<DetailRow> _rows = Array.Empty<DetailRow>();

    public DetailCard(string id, string title, string brushKey)
    {
        Id = id;
        Title = title;
        BrushKey = brushKey;
    }

    public string Id { get; }
    public string Title { get; }
    public string BrushKey { get; }
    public Brush Accent => ThemeBrushes.Get(BrushKey);
    public IReadOnlyList<DetailRow> Rows { get => _rows; set => Set(ref _rows, value); }

    /// <summary>Tema değişince başlık rengi yeniden okunur.</summary>
    public void RefreshAccent() => Raise(nameof(Accent));
}

/// <summary>Ayrıntı kartında bir satır: etiket ve değer.</summary>
public sealed record DetailRow(string Label, string Value);

/// <summary>Ayrıntı kartlarının o anki verileri (App her ölçümde doldurur).</summary>
public sealed record DetailContext(
    MetricsSnapshot Metrics,
    KeyboardSnapshot Keys,
    DayTraffic Today,
    string LocalIp,
    string PublicIp,
    string PingHost,
    (double? Min, double? Avg, double? Max, double LossPercent) Ping,
    double? PingLast);
