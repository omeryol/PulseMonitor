namespace PulseMonitor.Weather;

/// <summary>
/// Seçilebilir hava kaynakları: Open-Meteo'nun modelleri (<see cref="OpenMeteoModels"/>) ve MET Norway.
/// Ayardaki <c>Model</c> kimliği hangi kaynaktan alınacağını belirler; raporun <c>Model</c> alanı da aynı kimliği taşır.
/// </summary>
internal static class WeatherSources
{
    public const string MetNorway = "met_norway";
    private const string MetNorwayName = "MET Norway";

    public static IReadOnlyList<(string Id, string Name)> All { get; } = [.. OpenMeteoModels.All, (MetNorway, MetNorwayName)];

    public static IReadOnlySet<string> Ids { get; } = All.Select(m => m.Id).ToHashSet();

    /// <summary>İpucunda kaynak satırı için kısa ad ("Open-Meteo · ECMWF IFS", "MET Norway").</summary>
    public static string SourceLabel(string model) => model == MetNorway ? MetNorwayName : OpenMeteoModels.SourceLabel(model);

    public static Task<WeatherReport> FetchAsync(double lat, double lon, string model, CancellationToken token) =>
        model == MetNorway ? MetNorwayClient.FetchAsync(lat, lon, token) : OpenMeteoClient.FetchAsync(lat, lon, model, token);
}
