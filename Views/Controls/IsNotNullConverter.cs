using System.Globalization;
using System.Windows.Data;

namespace PulseMonitor.Views.Controls;

/// <summary>Değer null değilse true (şablonlarda "varsa göster" tetikleyicisi için).</summary>
public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
