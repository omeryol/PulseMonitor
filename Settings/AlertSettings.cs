namespace PulseMonitor.Settings;

/// <summary>Uyarı bildirimleri: eşik aşılınca Windows bildirimi. Aynı uyarı 30 dakikada bir tekrarlanır (trafik: günde bir).</summary>
public sealed class AlertSettings
{
    public bool TrafficEnabled { get; set; }
    /// <summary>Günlük toplam trafik sınırı (indirme + yükleme), GB.</summary>
    public int TrafficLimitGb { get; set; } = 20;

    public bool CpuEnabled { get; set; }
    /// <summary>CPU bu yüzdenin üstünde 30 saniye kalırsa.</summary>
    public int CpuPercent { get; set; } = 90;

    public bool RamEnabled { get; set; }
    public int RamPercent { get; set; } = 90;

    public bool GpuTempEnabled { get; set; }
    public int GpuTempC { get; set; } = 85;

    public bool DiskTempEnabled { get; set; }
    public int DiskTempC { get; set; } = 70;

    internal void Normalize()
    {
        TrafficLimitGb = Math.Clamp(TrafficLimitGb, 1, 10000);
        CpuPercent = Math.Clamp(CpuPercent, 10, 100);
        RamPercent = Math.Clamp(RamPercent, 10, 100);
        GpuTempC = Math.Clamp(GpuTempC, 30, 120);
        DiskTempC = Math.Clamp(DiskTempC, 30, 100);
    }
}
