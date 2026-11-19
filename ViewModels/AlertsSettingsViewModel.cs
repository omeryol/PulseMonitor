using System.Runtime.CompilerServices;
using System.Windows.Input;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Ayarlar → Uyarılar. Her değişiklik hemen kaydedilir.</summary>
public sealed class AlertsSettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private AlertSettings A => _store.Current.Alerts;

    internal AlertsSettingsViewModel(SettingsStore store, Action testNotification)
    {
        _store = store;
        TestCommand = new RelayCommand(testNotification);
        WeatherLevel = new Choice("WeatherWarnings", () => A.WeatherLevel, v => Update(() => A.WeatherLevel = v));
        TrafficQuotaMode = new Choice("TrafficQuotaMode", () => A.TrafficQuotaMode, v =>
        {
            Update(() => A.TrafficQuotaMode = v);
            Raise(nameof(TrafficSplit));
            Raise(nameof(TrafficSingle));
        });
        TrafficWarningPercent = new Choice("TrafficWarning", () => A.TrafficWarningPercent.ToString(System.Globalization.CultureInfo.InvariantCulture),
            v => Update(() => A.TrafficWarningPercent = int.Parse(v, System.Globalization.CultureInfo.InvariantCulture)));
    }

    public ICommand TestCommand { get; }
    public Choice WeatherLevel { get; }
    public Choice TrafficWarningPercent { get; }
    public Choice TrafficQuotaMode { get; }
    public bool TrafficSplit => A.TrafficQuotaMode == StyleOptions.QuotaSplit;
    public bool TrafficSingle => !TrafficSplit;
    public bool WeatherQuietHours { get => A.WeatherQuietHours; set => Update(() => A.WeatherQuietHours = value); }

    public bool TrafficEnabled { get => A.TrafficEnabled; set => Update(() => A.TrafficEnabled = value); }
    public int TrafficLimitGb { get => A.TrafficLimitGb; set => Update(() => { A.TrafficLimitGb = value; Raise(nameof(TrafficSliderMax)); }); }
    /// <summary>Kaydırıcı en az 1000 GB; elle girilen değer daha büyükse kaydırıcı onu kapsar (aksi halde değer kırpılırdı).</summary>
    public int TrafficSliderMax => Math.Max(1000, A.TrafficLimitGb);
    public int TrafficUploadLimitGb { get => A.TrafficUploadLimitGb; set => Update(() => { A.TrafficUploadLimitGb = value; Raise(nameof(TrafficUploadSliderMax)); }); }
    public int TrafficUploadSliderMax => Math.Max(1000, A.TrafficUploadLimitGb);
    public int TrafficCycleStartDay { get => A.TrafficCycleStartDay; set => Update(() => A.TrafficCycleStartDay = value); }
    public bool CpuEnabled { get => A.CpuEnabled; set => Update(() => A.CpuEnabled = value); }
    public int CpuPercent { get => A.CpuPercent; set => Update(() => A.CpuPercent = value); }
    public bool RamEnabled { get => A.RamEnabled; set => Update(() => A.RamEnabled = value); }
    public int RamPercent { get => A.RamPercent; set => Update(() => A.RamPercent = value); }
    public bool GpuTempEnabled { get => A.GpuTempEnabled; set => Update(() => A.GpuTempEnabled = value); }
    public int GpuTempC { get => A.GpuTempC; set => Update(() => A.GpuTempC = value); }
    public bool DiskTempEnabled { get => A.DiskTempEnabled; set => Update(() => A.DiskTempEnabled = value); }
    public int DiskTempC { get => A.DiskTempC; set => Update(() => A.DiskTempC = value); }

    /// <summary>Değeri uygular, kaydeder ve özelliğin değiştiğini bildirir (kaydırıcı yanındaki değer metni güncellensin).</summary>
    private void Update(Action change, [CallerMemberName] string? property = null)
    {
        change();
        A.Normalize();
        _store.Save();
        Raise(property);
    }
}
