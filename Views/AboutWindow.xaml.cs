using System.Windows;
using PulseMonitor.Core;

namespace PulseMonitor.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        WindowTheme.Prepare(this);
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
