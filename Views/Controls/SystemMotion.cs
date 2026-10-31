using System.ComponentModel;
using System.Windows;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Windows → Erişilebilirlik → Görsel efektler → "Animasyon efektleri" ayarı. Kapalıysa hava animasyonu
/// ve hareketli simgeler durur (sabit kare gösterilir). Ayar değişince <see cref="Changed"/> çalışır.
/// </summary>
public static class SystemMotion
{
    static SystemMotion() => SystemParameters.StaticPropertyChanged += OnSystemChanged;

    /// <summary>Windows animasyonlara izin veriyor mu.</summary>
    public static bool Allowed => SystemParameters.ClientAreaAnimation;

    public static event EventHandler? Changed;

    private static void OnSystemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SystemParameters.ClientAreaAnimation)) return;
        Core.Log.Info($"Windows animasyon efektleri: {(Allowed ? "açık" : "kapalı")}");
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
