using System.Windows.Controls;
using System.Windows.Input;
using PulseMonitor.Weather;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views.Controls;

public partial class WeatherItem : UserControl
{
    private WeatherLocation? _firstClickLocation;

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
            if (vm.CanSwitch) vm.RequestSwitch();
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
}
