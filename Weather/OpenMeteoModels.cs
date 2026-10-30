namespace PulseMonitor.Weather;

/// <summary>
/// Open-Meteo'nun sunduğu tahmin modelleri (aynı servis, aynı lisans: CC BY 4.0). Yalnızca dünya genelinde veri
/// veren modeller listelenir; bölgesel modeller (KMA, BOM…) kapsam dışında boş döndüğü için yok.
/// Adlar kurum/model adıdır, çevrilmez; yalnızca "Otomatik" dil dosyasından gelir.
/// </summary>
public static class OpenMeteoModels
{
    public const string Auto = "best_match";

    public static IReadOnlyList<(string Id, string Name)> All { get; } =
    [
        (Auto, ""),
        ("ecmwf_ifs", "ECMWF IFS"),
        ("icon_seamless", "DWD ICON"),
        ("gfs_seamless", "NOAA GFS"),
        ("meteofrance_seamless", "Météo-France"),
        ("ukmo_seamless", "UK Met Office"),
        ("gem_seamless", "Canada GEM"),
        ("jma_seamless", "JMA"),
    ];

    public static IReadOnlySet<string> Ids { get; } = All.Select(m => m.Id).ToHashSet();

    /// <summary>İpucunda kaynak satırı için kısa ad ("Open-Meteo · ECMWF IFS").</summary>
    public static string SourceLabel(string model)
    {
        string name = All.FirstOrDefault(m => m.Id == model).Name ?? "";
        return name.Length == 0 ? "Open-Meteo" : $"Open-Meteo · {name}";
    }
}
