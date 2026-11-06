using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PulseMonitor.Settings;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views;

public partial class TaskbarWindow : Window
{
    private double _requestedScale = 1;
    private double _effectiveScale = 1;

    public event Action<bool>? ProcessDetailsHoverChanged;

    public TaskbarWindow()
    {
        InitializeComponent();
    }

    /// <summary>Tüm içeriği ölçekler (Ayarlar → Görünüm → Pencere boyutu). 1 = % 100.</summary>
    public void SetScale(double scale)
    {
        _requestedScale = scale;
        SetEffectiveScale(scale);
    }

    /// <summary>
    /// Başlat düğmesinin solundaki gerçek kullanılabilir alan için ölçeği geçici olarak azaltır.
    /// Ayar değeri değişmez; alan açılınca SetScale ile istenen değere geri dönülür.
    /// </summary>
    public bool FitToTaskbarWidth(int availablePhysicalWidth, double dpiScale)
    {
        if (ActualWidth <= 0) return false;
        double naturalWidthDip = ActualWidth / _effectiveScale;
        double? scale = Taskbar.TaskbarScale.Fit(_requestedScale, naturalWidthDip, availablePhysicalWidth, dpiScale);
        if (scale is null) return false;
        SetEffectiveScale(scale.Value);
        return true;
    }

    public void RestoreRequestedScale() => SetEffectiveScale(_requestedScale);

    private void OnColumnMouseEnter(object sender, MouseEventArgs e) => SetProcessDetailsHovered(sender, true);

    private void OnColumnMouseLeave(object sender, MouseEventArgs e) => SetProcessDetailsHovered(sender, false);

    private void SetProcessDetailsHovered(object sender, bool hovered)
    {
        if (Application.Current.TryFindResource("DetailCards.Enabled") is true &&
            sender is FrameworkElement { DataContext: TaskbarColumn column } &&
            column.Cards.Any(card => card.Id is TaskbarItemId.Cpu or TaskbarItemId.Ram))
            ProcessDetailsHoverChanged?.Invoke(hovered);
    }

    private void SetEffectiveScale(double scale)
    {
        if (Math.Abs(_effectiveScale - scale) < 0.001) return;
        _effectiveScale = scale;
        Root.LayoutTransform = Math.Abs(scale - 1) < 0.001 ? Transform.Identity : new ScaleTransform(scale, scale);
    }
}
