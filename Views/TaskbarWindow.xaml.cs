using System.Windows;
using System.Windows.Media;

namespace PulseMonitor.Views;

public partial class TaskbarWindow : Window
{
    public TaskbarWindow()
    {
        InitializeComponent();
    }

    /// <summary>Tüm içeriği ölçekler (Ayarlar → Görünüm → Pencere boyutu). 1 = % 100.</summary>
    public void SetScale(double scale)
    {
        Root.LayoutTransform = Math.Abs(scale - 1) < 0.001 ? Transform.Identity : new ScaleTransform(scale, scale);
    }
}
