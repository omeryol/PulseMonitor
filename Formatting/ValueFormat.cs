using System.Globalization;
using PulseMonitor.Core;

namespace PulseMonitor.Formatting;

/// <summary>Görev çubuğu ve pencerelerdeki sayı biçimleri (dilin kültürüne göre; Türkçede ondalık virgül).</summary>
internal static class ValueFormat
{
    private static CultureInfo Tr => UiLanguage.Culture;

    /// <summary>En geniş hız metni (güncel birime göre); sütun genişliğini sabitlemek için.</summary>
    public static string SpeedPlaceholder => SpeedFormat.Current.Placeholder;
    public const string PercentPlaceholder = "% 100";
    public const string IpPlaceholder = "888.888.888.888";
    public const string TemperaturePlaceholder = "188°";
    public const string PingPlaceholder = "8888 ms";

    /// <summary>Kapasite (sürücü, video belleği): 512 MB · 93,5 GB · 465 GB · 1,82 TB (büyüdükçe ondalık azalır).</summary>
    public static string Capacity(double bytes)
    {
        const double gb = 1024d * 1024 * 1024;
        double b = Math.Max(0, bytes);
        if (b >= 1024 * gb) return string.Format(Tr, "{0:0.00} TB", b / (1024 * gb));
        if (b >= 100 * gb) return string.Format(Tr, "{0:0} GB", b / gb);
        if (b >= gb) return string.Format(Tr, "{0:0.0} GB", b / gb);
        return string.Format(Tr, "{0:0} MB", b / (1024 * 1024));
    }

    /// <summary>41° · okunamıyorsa --</summary>
    public static string Celsius(double? value) =>
        value is { } v ? string.Format(Tr, "{0:0}°", v) : "--";

    /// <summary>12 ms · yanıt yoksa --</summary>
    public static string Milliseconds(double? value) =>
        value is { } v ? string.Format(Tr, "{0:0} ms", v) : "--";

    /// <summary>Hız, ayarlardaki birim ve bölenle (<see cref="SpeedFormat.Current"/>).</summary>
    public static string Speed(double bytesPerSecond) => SpeedFormat.Current.Format(bytesPerSecond, Tr);

    /// <summary>Hız, verilen biçimle (disk gibi her zaman bayt gösterilenler için <see cref="SpeedFormat.Default"/>).</summary>
    public static string Speed(double bytesPerSecond, SpeedFormat format) => format.Format(bytesPerSecond, Tr);

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

    /// <summary>Sıcaklık farkı (konum yok): 8° · Fahrenheit'ta farklar da 9/5 ile çarpılır.</summary>
    public static string DegreeDelta(double celsiusDelta, bool fahrenheit) =>
        string.Format(Tr, "{0:0}°", Math.Round(Math.Abs(fahrenheit ? celsiusDelta * 9 / 5 : celsiusDelta)));

    /// <summary>Grafik ekseni için kısa boyut: 8 GB · 1,5 GB · 512 MB (gereksiz ondalık yok).</summary>
    public static string SizeCompact(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = Math.Max(0, bytes);
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return string.Format(Tr, "{0:0.##} {1}", v, units[u]);
    }

    /// <summary>% 12 (İngilizcede 12%) · okunamıyorsa % --. Yazım dil dosyasından: "Format.Percent".</summary>
    public static string Percent(double? value) =>
        string.Format(Tr, Strings.Get("Format.Percent"), value is { } v ? v.ToString("0", Tr) : "--");
}
