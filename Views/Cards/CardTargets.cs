using PulseMonitor.Settings;

namespace PulseMonitor.Views.Cards;

/// <summary>Ayarlar penceresi sekmeleri (Views/SettingsWindow.xaml sırasıyla).</summary>
public enum SettingsTab { Items, Traffic, Style, Weather, Alerts, General, About }

/// <summary>Sütuna çift tıklanınca açılan Windows sayfası.</summary>
public enum WindowsPage { None, TaskManager, Storage, Network }

/// <summary>Hangi ayrıntı kartı hangi ayar sekmesine / Windows sayfasına gider (saf eşleme; testli).</summary>
public static class CardTargets
{
    public static SettingsTab? Tab(string cardId) => cardId switch
    {
        TaskbarItemId.Upload or TaskbarItemId.Download => SettingsTab.Traffic,
        TaskbarItemId.Weather => SettingsTab.Weather,
        TaskbarItemId.Gpu => SettingsTab.General, // ekran kartı seçimi
        TaskbarItemId.GpuTemp or TaskbarItemId.DiskTemp or TaskbarItemId.Ping => SettingsTab.Alerts,
        TaskbarItemId.Cpu or TaskbarItemId.Ram or TaskbarItemId.Disk or TaskbarItemId.Keyboard
            or TaskbarItemId.LocalIp or TaskbarItemId.PublicIp => SettingsTab.Items,
        _ => null,
    };

    public static WindowsPage Page(string cardId) => cardId switch
    {
        TaskbarItemId.Cpu or TaskbarItemId.Ram or TaskbarItemId.Gpu or TaskbarItemId.GpuTemp => WindowsPage.TaskManager,
        TaskbarItemId.Disk or TaskbarItemId.DiskTemp => WindowsPage.Storage,
        TaskbarItemId.Upload or TaskbarItemId.Download or TaskbarItemId.Ping
            or TaskbarItemId.LocalIp or TaskbarItemId.PublicIp => WindowsPage.Network,
        _ => WindowsPage.None, // klavye: bir şey yapılmaz; hava: kendi çift tık davranışı var
    };

    /// <summary>Windows ayar sayfası adresi; Görev Yöneticisi ve boş için null.</summary>
    public static string? SettingsUri(WindowsPage page) => page switch
    {
        WindowsPage.Storage => "ms-settings:storagesense",
        WindowsPage.Network => "ms-settings:network-status",
        _ => null,
    };
}
