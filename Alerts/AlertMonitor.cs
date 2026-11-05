using System.Globalization;
using PulseMonitor.Core;
using PulseMonitor.Formatting;
using PulseMonitor.Metrics;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;

namespace PulseMonitor.Alerts;

/// <summary>
/// Her ölçümde eşikleri denetler, aşılınca <see cref="Raised"/> (başlık, metin) tetikler.
/// CPU eşiği 30 saniye sürmeli; aynı uyarı 30 dakikada bir, trafik uyarısı günde bir kez.
/// Arayüz iş parçacığında çağrılır.
/// </summary>
internal sealed class AlertMonitor
{
    private static CultureInfo Tr => UiLanguage.Culture;
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CpuSustain = TimeSpan.FromSeconds(30);
    private const double BytesPerGb = 1024.0 * 1024 * 1024;

    private readonly Dictionary<string, DateTime> _lastFired = new();
    private DateTime? _cpuHighSince;

    public event Action<string, string>? Raised;
    /// <summary>Kota bildirimi kalıcı işaretini ayarlara yazmak için çağrılır.</summary>
    public event Action? QuotaNotificationRecorded;

    public void Check(MetricsSnapshot m, TrafficHistory history, AlertSettings a)
    {
        DateTime now = DateTime.Now;

        if (a.TrafficEnabled)
        {
            var period = TrafficPeriod.For(DateOnly.FromDateTime(now), a.TrafficCycleStartDay);
            DayTraffic t = TrafficPeriod.Total(history, period);
            ulong total = TrafficPeriod.Add(t.Down, t.Up);
            double gb = total / BytesPerGb;
            string periodKey = period.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (a.TrafficWarningPercent > 0 && gb * 100 >= a.TrafficLimitGb * a.TrafficWarningPercent && a.LastTrafficQuotaNotificationPeriod != periodKey)
            {
                a.LastTrafficQuotaNotificationPeriod = periodKey;
                QuotaNotificationRecorded?.Invoke();
                Raise("Alert.Traffic", ValueFormat.Size(total), a.TrafficLimitGb, a.TrafficWarningPercent);
            }
        }

        if (a.CpuEnabled && m.CpuPercent >= a.CpuPercent)
        {
            _cpuHighSince ??= now;
            if (now - _cpuHighSince >= CpuSustain) Fire("cpu", now, "Alert.Cpu", Math.Round(m.CpuPercent), a.CpuPercent);
        }
        else
        {
            _cpuHighSince = null;
        }

        if (a.RamEnabled && m.RamPercent >= a.RamPercent)
            Fire("ram", now, "Alert.Ram", Math.Round(m.RamPercent), a.RamPercent);
        if (a.GpuTempEnabled && m.GpuTempC >= a.GpuTempC)
            Fire("gpuTemp", now, "Alert.GpuTemp", Math.Round(m.GpuTempC.Value), a.GpuTempC);
        if (a.DiskTempEnabled && m.DiskTempC >= a.DiskTempC)
            Fire("diskTemp", now, "Alert.DiskTemp", Math.Round(m.DiskTempC.Value), a.DiskTempC);
    }

    private void Fire(string key, DateTime now, string textKey, params object[] args)
    {
        if (_lastFired.TryGetValue(key, out DateTime last) && now - last < Cooldown) return;
        _lastFired[key] = now;
        Raise(textKey, args);
    }

    private void Raise(string textKey, params object[] args)
    {
        string text = string.Format(Tr, Strings.Get(textKey), args);
        Log.Info($"Uyarı: {text}");
        Raised?.Invoke(Strings.Get("Alert.Title"), text);
    }
}
