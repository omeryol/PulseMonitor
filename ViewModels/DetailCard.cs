using System.Windows.Media;
using PulseMonitor.Core;
using PulseMonitor.Metrics;
using PulseMonitor.Traffic;

namespace PulseMonitor.ViewModels;

/// <summary>Bir öğenin fare üzerine gelince açılan ayrıntı kartı: başlık (öğe renginde) ve satırlar. Her saniye yenilenir.</summary>
public sealed class DetailCard : ObservableObject
{
    private IReadOnlyList<DetailRow> _rows = Array.Empty<DetailRow>();
    private bool _isLoading;

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
    public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }

    /// <summary>Tema değişince başlık rengi yeniden okunur.</summary>
    public void RefreshAccent() => Raise(nameof(Accent));
}

/// <summary>Ayrıntı kartında bir satır: etiket ve değer; <see cref="IsOn"/> doluysa değer açık (yeşil) ya da kapalı (kırmızı) renginde gösterilir; <see cref="Bar"/> doluysa satırın altında doluluk çubuğu (0–100) çizilir.</summary>
public sealed record DetailRow(string Label, string Value, bool IsProcess = false, bool IsPlaceholder = false, double? Bar = null, bool? IsOn = null)
{
    public bool HasBar => Bar is not null;
    public double BarValue => Bar ?? 0;
}

/// <summary>Ayrıntı kartlarının o anki verileri (App her ölçümde doldurur).</summary>
public sealed record DetailContext(
    MetricsSnapshot Metrics,
    KeyboardSnapshot Keys,
    DayTraffic Today,
    string LocalIp,
    string PublicIp,
    string PingHost,
    PingQuality Ping,
    double? PingLast);
