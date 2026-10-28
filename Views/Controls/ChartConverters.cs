using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PulseMonitor.Views.Controls;

/// <summary>Sayıyı ters çevirir (alttan uzaklığı yukarı kaydırmaya çevirmek için: TranslateTransform.Y = −değer).</summary>
public sealed class NegateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? -d : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Sayıyı sabit satır yüksekliğine (GridLength) çevirir.</summary>
public sealed class ToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? new GridLength(d) : GridLength.Auto;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
