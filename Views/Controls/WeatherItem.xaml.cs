using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PulseMonitor.Weather;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views.Controls;

public partial class WeatherItem : UserControl
{
    private WeatherLocation? _firstClickLocation;
    private bool _openedByClick;

    public WeatherItem()
    {
        InitializeComponent();
    }

    /// <summary>Tek tık konumu gecikmeden değiştirir; çift tık ilk tıklanan konumun geçmişini açar.</summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not WeatherViewModel vm) return;
        if (e.ClickCount == 1)
        {
            // Geçmiş isteği ikinci tıkta geldiğinde ilk konum çoktan değişmiş olabilir.
            // Bu yüzden hedefi ilk tıklama anında saklarız; tek tık yine eski sürümdeki gibi anındadır.
            _firstClickLocation = vm.ActiveLocation;
            if (vm.CanSwitch)
            {
                vm.RequestSwitch();
                KeepCardOpen(); // konum değişse de ayrıntı kartı yeni veriyle görünür kalır
            }
            e.Handled = true;
            return;
        }
        if (e.ClickCount == 2)
        {
            vm.RequestHistory(_firstClickLocation ?? vm.ActiveLocation);
            _firstClickLocation = null;
        }
        e.Handled = true;
    }

    /// <summary>WPF tık sonrası ipucunu kapatır; aktif konum yenilendiğinde kart tekrar açılır.</summary>
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
