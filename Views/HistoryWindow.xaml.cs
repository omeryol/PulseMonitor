using System.Windows;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using PulseMonitor.Core;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        WindowTheme.Prepare(this);
        InitializeComponent();
    }

    private void ApplyRange_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryViewModel vm) return;
        if (!vm.ApplyRange())
        {
            MessageBox.Show(this, Strings.Get("History.InvalidRange"), Strings.Get("History.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MonthOption.IsChecked = false;
        YearOption.IsChecked = false;
    }

    private void Month_Click(object sender, RoutedEventArgs e) { if (DataContext is HistoryViewModel vm) vm.SelectPeriodView(false); }
    private void Year_Click(object sender, RoutedEventArgs e) { if (DataContext is HistoryViewModel vm) vm.SelectPeriodView(true); }
    private void PeriodNavigation_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryViewModel vm) return;
        if (vm.YearView) YearOption.IsChecked = true;
        else MonthOption.IsChecked = true;
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryViewModel vm) return;
        var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "PulseMonitor-traffic.csv", DefaultExt = ".csv", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var lines = new List<string> { "date,download_bytes,upload_bytes" };
            lines.AddRange(vm.ExportRows().Select(row => $"{row.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)},{row.Value.Down},{row.Value.Up}"));
            File.WriteAllLines(dialog.FileName, lines, System.Text.Encoding.UTF8);
        }
        catch (Exception ex) { Log.Error("Trafik CSV dışa aktarılamadı.", ex); MessageBox.Show(this, Strings.Get("Backup.Failed"), Strings.Get("History.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
