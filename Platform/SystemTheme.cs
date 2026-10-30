using Microsoft.Win32;

namespace PulseMonitor.Platform;

/// <summary>Görev çubuğunun açık/koyu temasını okur ve değişince haber verir.</summary>
internal sealed class SystemTheme : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public event Action? Changed;

    public SystemTheme() => SystemEvents.UserPreferenceChanged += OnPreferenceChanged;

    /// <summary>Görev çubuğu açık renkliyse true (SystemUsesLightTheme).</summary>
    public static bool IsTaskbarLight()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value && value == 1;
    }

    /// <summary>Uygulamalar açık temadaysa true (AppsUseLightTheme); ayarlar penceresi bunu izler.</summary>
    public static bool IsAppsLight()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is not int value || value == 1;
    }

    private void OnPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General) Changed?.Invoke();
    }

    public void Dispose() => SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
}
