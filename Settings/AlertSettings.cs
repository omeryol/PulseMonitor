namespace PulseMonitor.Settings;

/// <summary>Uyarı bildirimleri: eşik aşılınca Windows bildirimi. Aynı uyarı 30 dakikada bir tekrarlanır (trafik: günde bir).</summary>
public sealed class AlertSettings
{
    public bool TrafficEnabled { get; set; }
    /// <summary>Aylık kota (indirme + yükleme), GB.</summary>
    public int TrafficLimitGb { get; set; } = 20;
    /// <summary>Kota döneminin her ay başladığı gün; 1–28 arası tutulur.</summary>
    public int TrafficCycleStartDay { get; set; } = 1;
    /// <summary>Bildirim eşiği: 0 kapalı, aksi halde kota yüzdesi.</summary>
    public int TrafficWarningPercent { get; set; } = 80;
    /// <summary>Aynı dönem için tekrar bildirim göndermemek üzere kalıcı dönem anahtarı.</summary>
    public string? LastTrafficQuotaNotificationPeriod { get; set; }

    public bool CpuEnabled { get; set; }
    /// <summary>CPU bu yüzdenin üstünde 30 saniye kalırsa.</summary>
    public int CpuPercent { get; set; } = 90;

    public bool RamEnabled { get; set; }
    public int RamPercent { get; set; } = 90;

    public bool GpuTempEnabled { get; set; }
    public int GpuTempC { get; set; } = 85;

    public bool DiskTempEnabled { get; set; }
    public int DiskTempC { get; set; } = 70;

    /// <summary>
    /// Hava durumu uyarıları (Weather/Warnings): Kapalı / yalnızca aşırı olaylar / dengeli / hassas
    /// (<see cref="StyleOptions.Groups"/> → "WeatherWarnings"). Varsayılan yalnızca aşırı: nadir ve önemli olaylar.
    /// </summary>
    public string WeatherLevel { get; set; } = StyleOptions.WarnExtreme;

    /// <summary>23:00–07:00 arasında hava bildirimi gösterme.</summary>
    public bool WeatherQuietHours { get; set; } = true;

    internal void Normalize()
    {
        if (!StyleOptions.Groups["WeatherWarnings"].Contains(WeatherLevel)) WeatherLevel = StyleOptions.WarnExtreme;
        TrafficLimitGb = Math.Clamp(TrafficLimitGb, 1, 10000);
        TrafficCycleStartDay = Math.Clamp(TrafficCycleStartDay, 1, 28);
        TrafficWarningPercent = TrafficWarningPercent is 0 or 50 or 80 or 90 or 100 ? TrafficWarningPercent : 80;
        CpuPercent = Math.Clamp(CpuPercent, 10, 100);
        RamPercent = Math.Clamp(RamPercent, 10, 100);
        GpuTempC = Math.Clamp(GpuTempC, 30, 120);
        DiskTempC = Math.Clamp(DiskTempC, 30, 100);
    }
}
