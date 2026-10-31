using System.Text.Json;
using PulseMonitor.Weather.Warnings;

namespace PulseMonitor.Tests;

/// <summary>Bildirim kaydı: sessiz saat, günlük sınır, tekrar, gün dönümü ve dosyadan okuma.</summary>
public class WarningLedgerTests
{
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    [Theory]
    [InlineData(22, 59, false)]
    [InlineData(23, 0, true)]
    [InlineData(3, 0, true)]
    [InlineData(6, 59, true)]
    [InlineData(7, 0, false)]
    public void Quiet_hours_are_23_to_07(int hour, int minute, bool quiet) =>
        Assert.Equal(quiet, WarningLedger.IsQuiet(At(1, hour, minute), enabled: true));

    [Fact]
    public void Quiet_hours_can_be_turned_off() =>
        Assert.False(WarningLedger.IsQuiet(At(1, 3), enabled: false));

    [Fact]
    public void Same_event_is_shown_only_once()
    {
        var ledger = new WarningLedger();
        Assert.True(ledger.CanShow("a", At(1, 10)));
        ledger.Mark("a", At(1, 10));
        Assert.False(ledger.CanShow("a", At(1, 12)));
        Assert.True(ledger.CanShow("b", At(1, 12)));
    }

    [Fact]
    public void At_most_three_notifications_a_day_and_counter_resets_next_day()
    {
        var ledger = new WarningLedger();
        foreach (string key in new[] { "a", "b", "c" }) ledger.Mark(key, At(1, 10));
        Assert.False(ledger.CanShow("d", At(1, 14)));
        Assert.True(ledger.CanShow("d", At(2, 9)));
    }

    [Fact]
    public void Old_entries_are_forgotten_after_36_hours()
    {
        var ledger = new WarningLedger();
        ledger.Mark("x", At(1, 10));
        Assert.False(ledger.CanShow("x", At(2, 20))); // 34 saat
        Assert.True(ledger.CanShow("x", At(2, 23)));  // 37 saat
    }

    [Fact]
    public void Ledger_survives_a_write_and_read_round_trip()
    {
        var original = new WarningLedger();
        original.Mark("k1", At(5, 10));
        original.Mark("k2", At(5, 11));

        var back = JsonSerializer.Deserialize<WarningLedger>(JsonSerializer.Serialize(original))!;
        Assert.False(back.CanShow("k1", At(5, 15)));
        Assert.False(back.CanShow("k2", At(5, 15)));
        Assert.Equal(2, back.Today);

        back.Mark("k3", At(5, 16));
        Assert.False(back.CanShow("k4", At(5, 17))); // yüklenen kayıtta da günlük sınır geçerli
    }

    [Fact]
    public void Empty_or_old_file_behaves_like_a_fresh_ledger()
    {
        var ledger = JsonSerializer.Deserialize<WarningLedger>("{}")!;
        Assert.True(ledger.CanShow("a", At(1, 10)));
    }
}
