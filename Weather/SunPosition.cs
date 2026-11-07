namespace PulseMonitor.Weather;

/// <summary>
/// Güneşin ufkun üstünde olup olmadığı (gündüz/gece). Yalnızca gündüz bilgisi vermeyen kaynaklar (MET Norway) için;
/// yaklaşık astronomik formül, birkaç dakikalık sapma simge seçimi için önemsizdir.
/// </summary>
public static class SunPosition
{
    private static readonly DateTime J2000 = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Güneşin merkezi ufuktan 0,833° aşağıdan yukarıdaysa gündüz (doğuş/batış tanımı).</summary>
    public static bool IsDaytime(double latitude, double longitude, DateTime utc)
    {
        double days = (DateTime.SpecifyKind(utc, DateTimeKind.Utc) - J2000).TotalDays;
        double meanAnomaly = Rad(357.529 + 0.98560028 * days);
        double meanLongitude = 280.459 + 0.98564736 * days;
        double eclipticLongitude = Rad(meanLongitude + 1.915 * Math.Sin(meanAnomaly) + 0.020 * Math.Sin(2 * meanAnomaly));
        double obliquity = Rad(23.439 - 0.00000036 * days);

        double declination = Math.Asin(Math.Sin(obliquity) * Math.Sin(eclipticLongitude));
        double rightAscension = Math.Atan2(Math.Cos(obliquity) * Math.Sin(eclipticLongitude), Math.Cos(eclipticLongitude));
        double siderealDegrees = (18.697374558 + 24.06570982441908 * days) % 24 * 15;
        double hourAngle = Rad(siderealDegrees + longitude) - rightAscension;

        double lat = Rad(latitude);
        double sinAltitude = Math.Sin(lat) * Math.Sin(declination) + Math.Cos(lat) * Math.Cos(declination) * Math.Cos(hourAngle);
        return Math.Asin(Math.Clamp(sinAltitude, -1, 1)) > Rad(-0.833);
    }

    private static double Rad(double degrees) => degrees * Math.PI / 180;
}
