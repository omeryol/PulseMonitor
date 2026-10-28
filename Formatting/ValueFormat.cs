using System.Globalization;

namespace PulseMonitor.Formatting;

/// <summary>Görev çubuğu ve pencerelerdeki sayı biçimleri (Türkçe: ondalık virgül).</summary>
internal static class ValueFormat
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>En geniş hız metni; sütun genişliğini sabitlemek için (rakamlar sabit genişlikte).</summary>
    public const string SpeedPlaceholder = "888,8 MB/s";
    public const string PercentPlaceholder = "% 100";
    public const string IpPlaceholder = "888.888.888.888";
    public const string TemperaturePlaceholder = "188°";
    public const string PingPlaceholder = "8888 ms";

    /// <summary>41° · okunamıyorsa --</summary>
    public static string Celsius(double? value) =>
        value is { } v ? string.Format(Tr, "{0:0}°", v) : "--";

    /// <summary>12 ms · yanıt yoksa --</summary>
    public static string Milliseconds(double? value) =>
        value is { } v ? string.Format(Tr, "{0:0} ms", v) : "--";

    /// <summary>12,3 KB/s · 4,5 MB/s · 1,1 GB/s. Yuvarlamada 1000'e ulaşan değer bir üst birime geçer.</summary>
    public static string Speed(double bytesPerSecond)
    {
        double kb = Math.Max(0, bytesPerSecond) / 1024;
        if (Math.Round(kb, 1) < 1000) return string.Format(Tr, "{0:0.0} KB/s", kb);
        double mb = kb / 1024;
        if (Math.Round(mb, 1) < 1000) return string.Format(Tr, "{0:0.0} MB/s", mb);
        return string.Format(Tr, "{0:0.0} GB/s", mb / 1024);
    }

    /// <summary>512 KB · 12,3 MB · 1,25 GB</summary>
    public static string Size(double bytes)
    {
        double kb = Math.Max(0, bytes) / 1024;
        if (kb < 1024) return string.Format(Tr, "{0:0} KB", kb);
        double mb = kb / 1024;
        if (mb < 1024) return string.Format(Tr, "{0:0.0} MB", mb);
        return string.Format(Tr, "{0:0.00} GB", mb / 1024);
    }

    /// <summary>15° (Celsius) ya da 59° (Fahrenheit); "-0°" yazılmaz.</summary>
    public static string Degrees(double celsius, bool fahrenheit)
    {
        double value = Math.Round(fahrenheit ? celsius * 9 / 5 + 32 : celsius);
        return string.Format(Tr, "{0:0}°", value == 0 ? 0 : value);
    }

    /// <summary>Grafik ekseni için kısa boyut: 8 GB · 1,5 GB · 512 MB (gereksiz ondalık yok).</summary>
    public static string SizeCompact(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = Math.Max(0, bytes);
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return string.Format(Tr, "{0:0.##} {1}", v, units[u]);
    }

    /// <summary>% 12 · okunamıyorsa % --</summary>
    public static string Percent(double? value) =>
        value is { } v ? string.Format(Tr, "% {0:0}", v) : "% --";
}
