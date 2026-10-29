using System.Windows;
using System.Windows.Navigation;
using PulseMonitor.Platform;

namespace PulseMonitor.Views;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Sürüm notlarındaki bağlantılar varsayılan tarayıcıda açılır.</summary>
    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        SystemActions.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
