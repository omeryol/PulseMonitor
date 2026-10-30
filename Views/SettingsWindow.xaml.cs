using System.Windows;
using PulseMonitor.Core;

namespace PulseMonitor.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        WindowTheme.Prepare(this);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
