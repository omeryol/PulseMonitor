using System.Windows;

namespace PulseMonitor.Core;

/// <summary>Kod içinden arayüz metni okuma (Resources/Strings.tr.xaml).</summary>
internal static class Strings
{
    public static string Get(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;
}
