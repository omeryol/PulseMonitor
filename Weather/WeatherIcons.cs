using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Weather;

public enum WeatherAnimationKind { None, Rain, Snow, Fog, Thunder }

/// <summary>Bir simge setinden bulunan simge: resim, şekil ya da animasyonlu film şeridi (+ sabit hâli).</summary>
public readonly record struct WeatherIcon(ImageSource? Image, Geometry? Shape, BitmapSource? Strip, BitmapSource? Still);

/// <summary>
/// Seçili simge setinden simgeyi bulur.
/// Renkli / Gerçekçi: resim. Çizgi / Dolu: şekil (temanın yazı rengiyle boyanır).
/// Meteocons / Makin-Things: Assets/WeatherAnim/&lt;set&gt;/&lt;simge&gt;.png film şeridi ve .static.png.
/// Kaynaklar: Resources/WeatherIcons*.xaml, Assets/WeatherAnim
/// </summary>
internal static class WeatherIcons
{
    private static readonly Dictionary<string, BitmapSource?> Bitmaps = new();

    public static WeatherIcon Resolve(string set, string iconKey)
    {
        if (StyleOptions.AnimatedIconSets.Contains(set))
            return new WeatherIcon(null, null, Load($"{set}/{iconKey}.png"), Load($"{set}/{iconKey}.static.png"));

        string key = set switch
        {
            StyleOptions.IconRealistic => $"WxImg.Realistic.{iconKey}",
            StyleOptions.IconLine => $"WxGeo.Line.{iconKey}",
            StyleOptions.IconMono => $"WxGeo.Mono.{iconKey}",
            StyleOptions.IconGlyph => string.Empty, // karakter: WeatherViewModel.IconGlyph
            _ => $"Wx.{iconKey}",
        };
        if (key.Length == 0) return default;
        object? resource = Application.Current?.TryFindResource(key);
        return new WeatherIcon(resource as ImageSource, resource as Geometry, null, null);
    }

    private static BitmapSource? Load(string path)
    {
        if (Bitmaps.TryGetValue(path, out BitmapSource? cached)) return cached;
        BitmapSource? bitmap = null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri($"pack://application:,,,/Assets/WeatherAnim/{path}");
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            bitmap = image;
        }
        catch (Exception ex)
        {
            Log.Warn($"Hava simgesi yüklenemedi: {path}", ex);
        }
        Bitmaps[path] = bitmap;
        return bitmap;
    }
}
