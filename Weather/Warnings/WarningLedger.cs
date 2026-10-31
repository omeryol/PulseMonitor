namespace PulseMonitor.Weather.Warnings;

/// <summary>
/// Hangi hava olayının bildirildiğinin kaydı (saf mantık, diske yazmayı <see cref="WarningLedgerStore"/> yapar):
/// aynı olay bir kez, günde en çok <see cref="DailyLimit"/> bildirim, sessiz saatte bildirim yok.
/// Program yeniden başlayınca aynı olay tekrar bildirilmesin diye kayıt dosyada saklanır.
/// </summary>
public sealed class WarningLedger
{
    public const int DailyLimit = 3;
    public const int QuietFromHour = 23, QuietToHour = 7;
    private static readonly TimeSpan Remember = TimeSpan.FromHours(36);

    /// <summary>Olay anahtarı → bildirildiği an.</summary>
    public Dictionary<string, DateTime> Shown { get; set; } = new();
    public DateTime Day { get; set; }
    public int Today { get; set; }

    /// <summary>Sessiz saat (23:00–07:00); ayar kapalıysa hiç sessiz değildir.</summary>
    public static bool IsQuiet(DateTime now, bool enabled) =>
        enabled && (now.Hour >= QuietFromHour || now.Hour < QuietToHour);

    public bool CanShow(string key, DateTime now)
    {
        Roll(now);
        return Today < DailyLimit && !Shown.ContainsKey(key);
    }

    public void Mark(string key, DateTime now)
    {
        Roll(now);
        Shown[key] = now;
        Today++;
    }

    /// <summary>Yeni gün sayacı sıfırlar; eski kayıtları unutur.</summary>
    private void Roll(DateTime now)
    {
        if (Day.Date != now.Date) (Day, Today) = (now.Date, 0);
        foreach (string old in Shown.Where(kv => now - kv.Value > Remember).Select(kv => kv.Key).ToList()) Shown.Remove(old);
    }
}
