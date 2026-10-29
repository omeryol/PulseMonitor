using System.Windows;
using PulseMonitor.Settings;

namespace PulseMonitor.Core;

/// <summary>
/// Renk dışındaki görünüm ayarlarını uygulama kaynaklarına koyar; görev çubuğu şablonları bunları
/// DynamicResource ile okur (Views/TaskbarWindow.xaml).
/// </summary>
internal static class StyleResources
{
    public static void Apply(Application app, StyleSettings s)
    {
        ResourceDictionary r = app.Resources;
        r["Font.Size"] = (double)s.FontSize;
        r["Font.ValueWeight"] = s.ValueWeight switch
        {
            StyleOptions.WeightNormal => FontWeights.Normal,
            StyleOptions.WeightBold => FontWeights.Bold,
            _ => FontWeights.SemiBold,
        };

        r["Thickness.LabelGap"] = new Thickness(0, 0, s.LabelGap, 0);
        r["Thickness.ColumnGap"] = new Thickness(s.ColumnGap, 0, 0, 0);
        r["Thickness.ItemsOffset"] = new Thickness(-s.ColumnGap, 0, 0, 0); // ilk sütunun boşluğunu geri al
        r["Thickness.ColumnPadding"] = s.Background == StyleOptions.BgCards ? new Thickness(8, 0, 8, 0) : new Thickness(2, 0, 2, 0);
        r["Thickness.ColumnSeparator"] = s.Separator ? new Thickness(1, 0, 0, 0) : new Thickness(0);

        r["DetailCards.Enabled"] = s.DetailCards;
        r["Visibility.SparkBehind"] = s.Sparkline == StyleOptions.SparkBehind ? Visibility.Visible : Visibility.Collapsed;
        r["Visibility.SparkSide"] = s.Sparkline == StyleOptions.SparkSide ? Visibility.Visible : Visibility.Collapsed;
    }
}
