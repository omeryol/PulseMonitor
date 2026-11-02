using System.Windows;
using System.Windows.Controls;
using PulseMonitor.Platform;

namespace PulseMonitor.Core;

/// <summary>
/// Ayarlar / Hakkında / Güncelleme pencerelerinin teması Windows'un uygulama temasını izler.
/// Renkler Resources/Theme.xaml → "Ui.&lt;anahtar&gt;.OnLight|OnDark"; geçerli hâli "Ui.&lt;anahtar&gt;" olarak atanır,
/// pencereler DynamicResource ile okuduğu için açık pencere de hemen değişir.
/// Standart denetimler (açılır liste, kaydırma çubuğu, başlık çubuğu…) WPF'in Fluent temasından gelir.
/// </summary>
internal static class WindowTheme
{
    private static readonly string[] Keys =
        ["Window", "Card", "Border", "Text", "Muted", "Hover", "Accent", "AccentHover", "AccentSoft", "AccentOnSoft", "OnAccent"];

    private static readonly Uri StylesUri = new("pack://application:,,,/Resources/WindowStyles.xaml");

    public static void Apply(Application app)
    {
        string sfx = SystemTheme.IsAppsLight() ? "OnLight" : "OnDark";
        foreach (string key in Keys)
            app.Resources[$"Ui.{key}"] = app.FindResource($"Ui.{key}.{sfx}");
    }

    /// <summary>
    /// InitializeComponent'ten önce çağrılır: Fluent teması (Windows'a göre açık/koyu) ve onun üstüne
    /// kendi düğme / sekme stillerimiz (sonra eklenen sözlük önce bulunur).
    /// </summary>
    public static void Prepare(Window window)
    {
#pragma warning disable WPF0001 // ThemeMode .NET 10'da hâlâ deneysel işaretli
        window.ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = StylesUri });
        window.Loaded += (_, _) => Compact(window);
    }

    /// <summary>
    /// Fluent'in 32 px'lik satırları ayarları gereksiz açıyor; denetimler biraz sıkılaştırılır. Fluent stilleri
    /// pencere açılınca eklendiği için XAML'deki BasedOn onları bulamaz; stil burada, bulunan Fluent stilinden türetilir.
    /// </summary>
    private static void Compact(Window window)
    {
        void Derive(Type type, params (DependencyProperty Property, object Value)[] setters)
        {
            if (window.TryFindResource(type) is not Style fluent) return;
            var style = new Style(type, fluent);
            foreach (var (property, value) in setters) style.Setters.Add(new Setter(property, value));
            window.Resources[type] = style;
        }

        Derive(typeof(ComboBox), (FrameworkElement.MinHeightProperty, 28.0), (Control.PaddingProperty, new Thickness(10, 3, 8, 3)));
        Derive(typeof(TextBox), (FrameworkElement.MinHeightProperty, 28.0), (Control.PaddingProperty, new Thickness(8, 3, 8, 3)));
        Derive(typeof(CheckBox), (FrameworkElement.MinHeightProperty, 24.0), (FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2)));
        Derive(typeof(ListBoxItem), (FrameworkElement.MinHeightProperty, 28.0), (Control.PaddingProperty, new Thickness(10, 3, 10, 3)));
    }
}
