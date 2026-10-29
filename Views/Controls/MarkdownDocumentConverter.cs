using System.Globalization;
using System.Windows.Data;

namespace PulseMonitor.Views.Controls;

/// <summary>Metni (Markdown) FlowDocument'e çevirir; her bağlama yeni belge üretir (belge iki görüntüleyicide paylaşılamaz).</summary>
public sealed class MarkdownDocumentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? MarkdownDocument.Build(text) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
