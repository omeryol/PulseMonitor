using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views.Controls;

public partial class WeatherItem : UserControl
{
    /// <summary>Kart tıklamadan sonra elle açıldı; fare çıkınca elle kapatılır (ToolTipService kapatmaz).</summary>
    private bool _openedByClick;

    public WeatherItem()
    {
        InitializeComponent();
    }

    /// <summary>Tıklayınca sıradaki konum; çift tık burada görev yöneticisini açmaz (pencereye iletilmez).</summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not WeatherViewModel vm || !vm.CanSwitch) return;
        if (e.ClickCount == 1)
        {
            vm.RequestSwitch();
            KeepCardOpen();
        }
        e.Handled = true;
    }

    /// <summary>WPF fareye basılınca ipucunu kapatır; kart yeni konumla hemen yeniden açılır.</summary>
    private void KeepCardOpen()
    {
        if (ToolTip is not ToolTip card || !ToolTipService.GetIsEnabled(this)) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsMouseOver) return;
            card.PlacementTarget = this;
            card.IsOpen = true;
            _openedByClick = true;
        }, DispatcherPriority.Input);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_openedByClick || ToolTip is not ToolTip card) return;
        _openedByClick = false;
        card.IsOpen = false;
    }
}
