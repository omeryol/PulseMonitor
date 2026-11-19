using PulseMonitor.Core;

namespace PulseMonitor.Weather;

/// <summary>
/// WMO hava kodu → açıklama, görev çubuğu simgesi (Resources/WeatherIcons.xaml anahtarı)
/// ve ipucundaki saatlik listede kullanılan karakter.
/// </summary>
internal static class WeatherCodes
{
    public static string Describe(int code) => Strings.Get($"Weather.Code.{Group(code)}");

    private static readonly int[] SampleCodes = [0, 1, 2, 3, 45, 51, 56, 61, 71, 80, 85, 95, 96, -1];

    /// <summary>Ortalama uzunluktaki hava açıklaması (uzunluğa göre sıralıyken dörtte üçüncü sıradaki; yaygın açıklamalar sığar, çok uzun olan küçülür); yer adı satırı genişliği bunu baz alır.</summary>
    public static string TypicalDescription()
    {
        List<string> all = SampleCodes.Select(Describe).OrderBy(d => d.Length).ToList();
        return all[all.Count * 3 / 4];
    }

    /// <summary>Simge adı (ClearDay, Rain…); her sette aynı adlar kullanılır (Weather/WeatherIcons.cs).</summary>
    public static string IconKey(int code, bool isDay) => Group(code) switch
    {
        "Clear" => isDay ? "ClearDay" : "ClearNight",
        "MainlyClear" or "PartlyCloudy" => isDay ? "PartlyDay" : "PartlyNight",
        "Fog" => "Fog",
        "Drizzle" or "Rain" or "FreezingRain" or "Showers" => "Rain",
        "Snow" or "SnowShowers" => "Snow",
        "Thunderstorm" or "Hail" => "Thunder",
        _ => "Cloudy",
    };

    /// <summary>Hava kartındaki animasyon türü (Views/Controls/WeatherAnimation.cs).</summary>
    public static WeatherAnimationKind Animation(int code, bool isDay) => Group(code) switch
    {
        "Clear" => isDay ? WeatherAnimationKind.Sun : WeatherAnimationKind.Stars,
        "MainlyClear" => WeatherAnimationKind.FewClouds,
        "PartlyCloudy" => WeatherAnimationKind.Clouds,
        "Overcast" => WeatherAnimationKind.Overcast,
        "Drizzle" or "Rain" or "FreezingRain" or "Showers" => code switch
        {
            80 => WeatherAnimationKind.LightShowers,
            81 => WeatherAnimationKind.Showers,
            51 => WeatherAnimationKind.LightDrizzle,
            53 => WeatherAnimationKind.Drizzle,
            55 => WeatherAnimationKind.DenseDrizzle,
            56 => WeatherAnimationKind.FreezingDrizzle,
            57 => WeatherAnimationKind.DenseFreezingDrizzle,
            61 => WeatherAnimationKind.LightRain,
            65 => WeatherAnimationKind.SteadyHeavyRain,
            66 => WeatherAnimationKind.FreezingRain,
            67 => WeatherAnimationKind.HeavyFreezingRain,
            82 => WeatherAnimationKind.HeavyRain,
            _ => WeatherAnimationKind.Rain,
        },
        "Snow" or "SnowShowers" => code switch
        {
            71 => WeatherAnimationKind.LightSnow,
            75 => WeatherAnimationKind.HeavySnow,
            77 => WeatherAnimationKind.SnowGrains,
            85 => WeatherAnimationKind.SnowShower,
            86 => WeatherAnimationKind.HeavySnowShower,
            _ => WeatherAnimationKind.Snow,
        },
        "Fog" => code == 48 ? WeatherAnimationKind.RimeFog : WeatherAnimationKind.Fog,
        "Thunderstorm" => code == 97 ? WeatherAnimationKind.HeavyThunder : WeatherAnimationKind.Thunder,
        "Hail" => code == 99 ? WeatherAnimationKind.HeavyHail : WeatherAnimationKind.Hail,
        _ => WeatherAnimationKind.None,
    };

    public static string Glyph(int code, bool isDay) => Group(code) switch
    {
        "Clear" => isDay ? "☀" : "☾",
        "MainlyClear" or "PartlyCloudy" => isDay ? "⛅" : "☁",
        "Overcast" => "☁",
        "Fog" => "≋",
        "Drizzle" or "Rain" or "FreezingRain" or "Showers" => "☂",
        "Snow" or "SnowShowers" => "❄",
        "Thunderstorm" or "Hail" => "⚡",
        _ => "?",
    };

    private static string Group(int code) => code switch
    {
        0 => "Clear",
        1 => "MainlyClear",
        2 => "PartlyCloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 or 66 or 67 => "FreezingRain",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Showers",
        85 or 86 => "SnowShowers",
        95 or 97 => "Thunderstorm",
        96 or 99 => "Hail",
        _ => "Unknown",
    };
}
