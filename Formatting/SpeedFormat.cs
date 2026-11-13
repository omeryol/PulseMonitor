using System.Globalization;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Formatting;

/// <summary>
/// Hız gösterimi: birim (bayt/bit + yazım) ve bölen (1000/1024). Ölçek her zaman otomatiktir (K/M/G).
/// Bit modunda değer = bayt/sn × 8. Bölen yalnızca hıza uygulanır; toplam trafik ve boyutlar 1024 bayttır.
/// </summary>
internal readonly record struct SpeedFormat(bool Bits, string Suffix, int Divisor)
{
    /// <summary>Eski davranış: bayt, 1024, "MB/s".</summary>
    public static readonly SpeedFormat Default = new(false, "/s", 1024);

    /// <summary>Görev çubuğu ve ayrıntı kartlarının kullandığı güncel biçim (Görünüm ayarlarından).</summary>
    public static SpeedFormat Current { get; set; } = Default;

    /// <summary>Ayar kimliklerinden biçim; geçersiz değer varsayılana düşer. Bölen "Auto": bayt 1024, bit 1000.</summary>
    public static SpeedFormat From(string? unit, string? divisor)
    {
        (bool bits, string suffix) = unit switch
        {
            StyleOptions.SpeedBitsSlash => (true, "/s"),
            StyleOptions.SpeedBitsPs => (true, "ps"),
            StyleOptions.SpeedBitsMbit => (true, "it/s"),
            _ => (false, "/s"),
        };
        int div = divisor switch
        {
            StyleOptions.Base1000 => 1000,
            StyleOptions.Base1024 => 1024,
            _ => bits ? 1000 : 1024,
        };
        return new SpeedFormat(bits, suffix, div);
    }

    /// <summary>En geniş hız metni (sütun genişliğini sabitler): "888,8 MB/s", "888,8 Mbit/s".</summary>
    public string Placeholder => string.Format(UiLanguage.Culture, "{0:0.0} M{1}{2}", 888.8, Bits ? "b" : "B", Suffix);

    /// <summary>12,3 KB/s · 4,5 Mb/s · 1,1 Gbps. Yuvarlamada 1000'e ulaşan değer bir üst birime geçer.</summary>
    public string Format(double bytesPerSecond, CultureInfo culture)
    {
        double v = Math.Max(0, bytesPerSecond) * (Bits ? 8 : 1) / Divisor;
        string[] prefixes = ["K", "M", "G"];
        int i = 0;
        while (i < prefixes.Length - 1 && Math.Round(v, 1) >= 1000) { v /= Divisor; i++; }
        string unit = Bits ? "b" : "B";
        return string.Format(culture, "{0:0.0} {1}{2}{3}", v, prefixes[i], unit, Suffix);
    }
}
