namespace PulseMonitor.Views.Cards;

public enum CardHoverAction { None, Open, Close }

/// <summary>
/// Kalıcı ayrıntı kartının açılma/kapanma zamanlaması (saf mantık; saat dışarıdan verilir, testli).
/// Açılış: fare aynı sütunda <see cref="OpenDelayMs"/> kalınca. Kapanış: fare hem sütunun hem kartın dışına çıkınca,
/// <see cref="CloseGraceMs"/> tolerans sonrası (sütundan karta geçerken kart titremesin). Başka sütuna geçilirse
/// kart gecikmesiz o sütuna taşınır.
/// </summary>
public sealed class CardHoverTracker
{
    public const int OpenDelayMs = 400;
    public const int CloseGraceMs = 250;

    private object? _pending;
    private long _pendingSince;
    private long _lastOver;

    /// <summary>Şu an açık kartın sütunu (yoksa null).</summary>
    public object? Open { get; private set; }

    /// <summary>Ek ölçümler için: açık ya da açılması beklenen sütun.</summary>
    public object? Active => Open ?? _pending;

    public CardHoverAction Tick(object? hoveredColumn, bool overCard, long nowMs)
    {
        if (Open is not null)
        {
            if (hoveredColumn is not null && !ReferenceEquals(hoveredColumn, Open))
            {
                Open = hoveredColumn;
                _lastOver = nowMs;
                return CardHoverAction.Open;
            }
            if (hoveredColumn is not null || overCard)
            {
                _lastOver = nowMs;
                return CardHoverAction.None;
            }
            if (nowMs - _lastOver < CloseGraceMs) return CardHoverAction.None;
            Open = null;
            return CardHoverAction.Close;
        }

        if (hoveredColumn is null)
        {
            _pending = null;
            return CardHoverAction.None;
        }
        if (!ReferenceEquals(_pending, hoveredColumn))
        {
            _pending = hoveredColumn;
            _pendingSince = nowMs;
            return CardHoverAction.None;
        }
        if (nowMs - _pendingSince < OpenDelayMs) return CardHoverAction.None;
        Open = hoveredColumn;
        _pending = null;
        _lastOver = nowMs;
        return CardHoverAction.Open;
    }

    /// <summary>Kartı hemen kapatır (tam ekran, ayar kapalı, pencere gizli).</summary>
    public bool Reset()
    {
        bool wasOpen = Open is not null;
        Open = null;
        _pending = null;
        return wasOpen;
    }
}
