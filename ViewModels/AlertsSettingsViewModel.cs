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
    }

    public ICommand TestCommand { get; }

    public bool TrafficEnabled { get => A.TrafficEnabled; set => Update(() => A.TrafficEnabled = value); }
    public int TrafficLimitGb { get => A.TrafficLimitGb; set => Update(() => A.TrafficLimitGb = value); }
    public bool CpuEnabled { get => A.CpuEnabled; set => Update(() => A.CpuEnabled = value); }
    public int CpuPercent { get => A.CpuPercent; set => Update(() => A.CpuPercent = value); }
    public bool RamEnabled { get => A.RamEnabled; set => Update(() => A.RamEnabled = value); }
    public int RamPercent { get => A.RamPercent; set => Update(() => A.RamPercent = value); }
    public bool GpuTempEnabled { get => A.GpuTempEnabled; set => Update(() => A.GpuTempEnabled = value); }
    public int GpuTempC { get => A.GpuTempC; set => Update(() => A.GpuTempC = value); }
    public bool DiskTempEnabled { get => A.DiskTempEnabled; set => Update(() => A.DiskTempEnabled = value); }
    public int DiskTempC { get => A.DiskTempC; set => Update(() => A.DiskTempC = value); }

    private void Update(Action change)
    {
        change();
        A.Normalize();
        _store.Save();
    }
}
