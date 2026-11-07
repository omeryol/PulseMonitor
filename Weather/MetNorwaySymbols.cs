namespace PulseMonitor.Weather;

/// <summary>MET Norway simge kodu (ör. "lightrain_day", "heavysnowshowersandthunder_night") → WMO hava kodu.</summary>
internal static class MetNorwaySymbols
{
    /// <summary>Tanınmayan kod için null. Gündüz/gece eki yok sayılır (gündüz bilgisi güneş konumundan hesaplanır).</summary>
    public static int? ToWmo(string? symbolCode)
    {
        if (string.IsNullOrWhiteSpace(symbolCode)) return null;
        string s = symbolCode;
        int underscore = s.IndexOf('_');
        if (underscore > 0) s = s[..underscore];
        if (s.Contains("thunder", StringComparison.Ordinal)) return 95;

        return s switch
        {
            "clearsky" => 0,
            "fair" => 1,
            "partlycloudy" => 2,
            "cloudy" => 3,
            "fog" => 45,
            "lightrain" => 61,
            "rain" => 63,
            "heavyrain" => 65,
            "lightrainshowers" => 80,
            "rainshowers" => 81,
            "heavyrainshowers" => 82,
            "lightsleet" => 66,
            "sleet" or "heavysleet" or "lightsleetshowers" or "sleetshowers" or "heavysleetshowers" => 67,
            "lightsnow" => 71,
            "snow" => 73,
            "heavysnow" => 75,
            "lightsnowshowers" or "snowshowers" => 85,
            "heavysnowshowers" => 86,
            _ => null,
        };
    }
}
