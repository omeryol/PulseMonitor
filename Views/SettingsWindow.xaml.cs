using System.Windows;
using Microsoft.Win32;
using PulseMonitor.Core;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        WindowTheme.Prepare(this);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ExportSettings_Click(object sender, RoutedEventArgs e) => Export(true);
    private void ExportHistory_Click(object sender, RoutedEventArgs e) => Export(false);
    private void ImportSettings_Click(object sender, RoutedEventArgs e) => Import(true);
    private void ImportHistory_Click(object sender, RoutedEventArgs e) => Import(false);

    private void PingHistory_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        new PingHistoryWindow(vm.PingMonitor.Outages) { Owner = this }.ShowDialog();
    }

    private void Export(bool settings)
    {
        if (DataContext is not SettingsViewModel vm) return;
        var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = settings ? "PulseMonitor-settings.json" : "PulseMonitor-traffic.json", AddExtension = true, DefaultExt = ".json" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (settings) BackupTransfer.ExportSettings(dialog.FileName, vm.Store.Current);
            else BackupTransfer.ExportHistory(dialog.FileName, vm.History);
            MessageBox.Show(this, Strings.Get("Backup.Exported"), Strings.Get("Backup.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Log.Error("Yedek dışa aktarılamadı.", ex); MessageBox.Show(this, Strings.Get("Backup.Failed"), Strings.Get("Backup.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Import(bool settings)
    {
        if (DataContext is not SettingsViewModel vm) return;
        var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (settings)
            {
                var imported = BackupTransfer.ReadSettings(dialog.FileName);
                if (MessageBox.Show(this, Strings.Get("Backup.ConfirmSettings"), Strings.Get("Backup.Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                vm.Store.Replace(imported);
                ((App)Application.Current).RestartAfterImport();
            }
            else
            {
                var imported = BackupTransfer.ReadHistory(dialog.FileName);
                if (MessageBox.Show(this, Strings.Get("Backup.ConfirmHistory"), Strings.Get("Backup.Title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                if (!TrafficHistoryStore.MergeAndSave(vm.History, imported)) throw new System.IO.IOException("Traffic history could not be saved.");
                MessageBox.Show(this, Strings.Get("Backup.Imported"), Strings.Get("Backup.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex) { Log.Error("Yedek içe aktarılamadı.", ex); MessageBox.Show(this, Strings.Get("Backup.Failed"), Strings.Get("Backup.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
