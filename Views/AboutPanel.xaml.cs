using System.Windows.Controls;
using System.Windows.Navigation;
using PulseMonitor.Platform;

namespace PulseMonitor.Views;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
    }

    /// <summary>Bağlantılar varsayılan tarayıcıda açılır.</summary>
    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        SystemActions.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
