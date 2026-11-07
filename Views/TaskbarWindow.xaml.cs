using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PulseMonitor.Platform;
using PulseMonitor.ViewModels;
using PulseMonitor.Views.Cards;

namespace PulseMonitor.Views;

public partial class TaskbarWindow : Window
{
    private double _requestedScale = 1;
    private double _effectiveScale = 1;

    /// <summary>Bir ayrıntı kartı için ek ölçümler gerekiyor mu (kart kimliği, gerekli mi): kart açılmayı beklerken ve açıkken true, kapanınca false.</summary>
    public event Action<string, bool>? DetailCardHoverChanged;

    /// <summary>Ayrıntı kartına ya da kart başlığındaki ⚙ düğmesine tıklandı: ilgili ayar sekmesi açılsın.</summary>
    public event Action<SettingsTab>? SettingsRequested;

    private readonly DetailCardPopup _cards;

    public TaskbarWindow()
    {
        InitializeComponent();
        _cards = new DetailCardPopup(this);
        _cards.CardActive += (id, on) => DetailCardHoverChanged?.Invoke(id, on);
        _cards.SettingsRequested += tab => SettingsRequested?.Invoke(tab);
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


    private void OnColumnMouseEnter(object sender, MouseEventArgs e) => _cards.OnColumnEnter((FrameworkElement)sender);

    private void OnColumnMouseLeave(object sender, MouseEventArgs e) => _cards.OnColumnLeave((FrameworkElement)sender);

    /// <summary>
    /// Sütuna çift tık: ilgili Windows sayfası (CPU/RAM/GPU → Görev Yöneticisi, disk → depolama, ağ → ağ durumu).
    /// Kartı olan sütunlar olayı işler (pencerenin genel "Görev Yöneticisi" çift tıkına düşmez); hava sütununu WeatherItem işler.
    /// İki kartlı sütunda üst yarı ilk, alt yarı ikinci karta gider.
    /// </summary>
    private void OnColumnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TaskbarColumn { HasCards: true } column } element) return;
        e.Handled = true;
        if (e.ClickCount != 2) return;
        bool lower = column.Cards.Count > 1 && e.GetPosition(element).Y > element.ActualHeight / 2;
        WindowsPages.Open(CardTargets.Page(column.Cards[lower ? 1 : 0].Id));
    }


    private void SetEffectiveScale(double scale)
    {
        if (Math.Abs(_effectiveScale - scale) < 0.001) return;
        _effectiveScale = scale;
        Root.LayoutTransform = Math.Abs(scale - 1) < 0.001 ? Transform.Identity : new ScaleTransform(scale, scale);
    }
}
