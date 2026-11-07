using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PulseMonitor.Core;
using PulseMonitor.ViewModels;

namespace PulseMonitor.Views.Cards;

/// <summary>
/// Görev çubuğu ayrıntı kartı (CPU, RAM, ağ, hava…): fareyle gezilebilir kalıcı Popup. Zamanlama
/// <see cref="CardHoverTracker"/>'da; burada yalnızca WPF bağlantısı var. Kart açıkken karta tıklamak o kartın ayar sekmesini,
/// başlıktaki ⚙ da aynısını açar (hava kartında yalnızca ⚙). Popup WS_EX_NOACTIVATE'tir: tıklama odak çalmaz.
/// </summary>
internal sealed class DetailCardPopup
{
    private readonly Window _owner;
    private readonly Popup _popup;
    private readonly ContentControl _chrome;
    private readonly CardHoverTracker _tracker = new();
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private FrameworkElement? _hovered;
    private object? _measured;
    private List<string> _measuredIds = [];

    /// <summary>Kartın ihtiyaç duyduğu ek ölçümler için (kart kimliği, gerekli mi): açılma beklemesi başlayınca ... kart kapanınca.</summary>
    public event Action<string, bool>? CardActive;

    /// <summary>Kart ya da ⚙ tıklandı.</summary>
    public event Action<SettingsTab>? SettingsRequested;

    public DetailCardPopup(Window owner)
    {
        _owner = owner;
        _chrome = new ContentControl { Focusable = false };
        _chrome.SetResourceReference(FrameworkElement.StyleProperty, "Ui.PopupCard");
        _popup = new Popup
        {
            Placement = PlacementMode.Top,
            StaysOpen = true,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None,
            Child = _chrome,
        };
        PopupPlacementFix.Attach(_popup);
        _chrome.MouseLeftButtonUp += OnCardClick;
        _chrome.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(OnGearClick));
        _timer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => Tick();
        _owner.Closed += (_, _) => Shutdown();
    }

    public void OnColumnEnter(FrameworkElement column)
    {
        if (!CanShow(column)) return;
        _hovered = column;
        _timer.Start();
    }

    public void OnColumnLeave(FrameworkElement column)
    {
        if (ReferenceEquals(_hovered, column)) _hovered = null;
    }

    private static bool Enabled => Application.Current.TryFindResource("DetailCards.Enabled") is true;

    private bool CanShow(FrameworkElement column) =>
        Enabled && _owner.IsVisible && column.DataContext is TaskbarColumn { HasCards: true } or TaskbarColumn { Weather.Tooltip: not null };

    private void Tick()
    {
        if (_hovered is { IsMouseOver: false } or { IsVisible: false }) _hovered = null;
        if (!Enabled || !_owner.IsVisible)
        {
            _hovered = null;
            if (_tracker.Reset()) _popup.IsOpen = false;
        }

        FrameworkElement? hovered = _hovered is not null && CanShow(_hovered) ? _hovered : null;
        switch (_tracker.Tick(hovered, _popup.IsOpen && _chrome.IsMouseOver, _clock.ElapsedMilliseconds))
        {
            case CardHoverAction.Open: Show((FrameworkElement)_tracker.Open!); break;
            case CardHoverAction.Close: _popup.IsOpen = false; break;
        }
        UpdateMeasurements();
        if (_tracker.Active is null && _hovered is null) _timer.Stop();
    }

    private void Show(FrameworkElement column)
    {
        var data = (TaskbarColumn)column.DataContext;
        _popup.IsOpen = false;
        if (data.Weather is { } weather)
        {
            _chrome.Cursor = null;
            _chrome.ContentTemplate = (DataTemplate)Application.Current.FindResource("Ui.WeatherCardTemplate");
            _chrome.SetBinding(ContentControl.ContentProperty, new Binding(nameof(WeatherViewModel.Tooltip)) { Source = weather });
        }
        else
        {
            _chrome.Cursor = Cursors.Hand;
            _chrome.ContentTemplate = (DataTemplate)Application.Current.FindResource("Ui.DetailCardsTemplate");
            BindingOperations.ClearBinding(_chrome, ContentControl.ContentProperty);
            _chrome.Content = data.Cards;
        }
        _popup.PlacementTarget = column;
        _popup.IsOpen = true;
    }

    /// <summary>Ek ölçümler bekleme başlayınca açılır (kart dolu gelsin), kart kapanınca kapanır; fare karta geçse de sürer.</summary>
    private void UpdateMeasurements()
    {
        object? active = _tracker.Active;
        if (ReferenceEquals(active, _measured)) return;
        foreach (string id in _measuredIds) CardActive?.Invoke(id, false);
        _measured = active;
        _measuredIds = active is FrameworkElement { DataContext: TaskbarColumn column } ? column.Cards.Select(c => c.Id).ToList() : [];
        foreach (string id in _measuredIds) CardActive?.Invoke(id, true);
    }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (FindCard(e.OriginalSource as DependencyObject) is { } card && CardTargets.Tab(card.Id) is { } tab)
            SettingsRequested?.Invoke(tab);
    }

    private void OnGearClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is Button { Tag: string id } && CardTargets.Tab(id) is { } tab)
        {
            e.Handled = true;
            SettingsRequested?.Invoke(tab);
        }
    }

    private static DetailCard? FindCard(DependencyObject? d)
    {
        while (d is not null)
        {
            if (d is FrameworkElement { DataContext: DetailCard card }) return card;
            d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return null;
    }

    private void Shutdown()
    {
        _timer.Stop();
        _popup.IsOpen = false;
        _hovered = null;
        _tracker.Reset();
        UpdateMeasurements();
    }
}
