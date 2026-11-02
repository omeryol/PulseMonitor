using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using PulseMonitor.Interop;

namespace PulseMonitor.Core;

/// <summary>
/// "Masaüstünü göster" sonrası WPF açılır pencerelerin (kart, sağ tık menüsü) konumunu yanlış hesaplayıp
/// ekranın sol üstüne koyuyor; tıklayınca düzeliyor. Açıldıktan sonra sol üstteyse Windows’un gerçek
/// konumlarıyla yeniden yerleştirilir; doğru yerdeyse dokunulmaz.
/// </summary>
internal static class PopupPlacementFix
{
    private const int Gap = 4;

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(PopupPlacementFix), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

    public static readonly DependencyProperty ComboBoxEnabledProperty = DependencyProperty.RegisterAttached(
        "ComboBoxEnabled", typeof(bool), typeof(PopupPlacementFix), new PropertyMetadata(false, OnComboBoxEnabledChanged));

    public static bool GetComboBoxEnabled(DependencyObject d) => (bool)d.GetValue(ComboBoxEnabledProperty);
    public static void SetComboBoxEnabled(DependencyObject d, bool value) => d.SetValue(ComboBoxEnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolTip tip && (bool)e.NewValue) tip.Opened += (_, _) => FixCard(tip);
    }

    private static void OnComboBoxEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ComboBox combo && (bool)e.NewValue)
        {
            combo.PreviewMouseLeftButtonDown += (_, _) => PrepareDropDown(combo);
            combo.PreviewKeyDown += (_, _) => PrepareDropDown(combo);
            combo.DropDownOpened += (_, _) => combo.Dispatcher.BeginInvoke(
                () => FixDropDown(combo), DispatcherPriority.Loaded);
        }
    }

    /// <summary>Sağ tık menüsü için: imlecin yanına (görev çubuğunda yukarı doğru) yerleştirir.</summary>
    public static void AttachMenu(ContextMenu menu) => menu.Opened += (_, _) => FixMenu(menu);

    private static void FixCard(ToolTip tip)
    {
        if (!TryGetMisplaced(tip, out IntPtr hwnd, out RECT rect)) return;
        if (tip.PlacementTarget is not FrameworkElement target || Window.GetWindow(target) is not { } window) return;

        IntPtr ownerHwnd = new WindowInteropHelper(window).Handle;
        if (ownerHwnd == IntPtr.Zero || !User32.GetWindowRect(ownerHwnd, out RECT bar)) return;

        double scale = User32.GetDpiForWindow(ownerHwnd) / 96.0;
        Point origin = target.TransformToAncestor(window).Transform(new Point(0, 0));
        int center = bar.Left + (int)Math.Round((origin.X + target.ActualWidth / 2) * scale);

        int x = center - rect.Width / 2;
        int y = bar.Top - rect.Height - Gap;
        Move(hwnd, rect, x, y, below: bar.Bottom + Gap, new POINT { X = center, Y = bar.Top });
    }

    private static void FixMenu(ContextMenu menu)
    {
        if (!TryGetMisplaced(menu, out IntPtr hwnd, out RECT rect)) return;
        if (!User32.GetCursorPos(out POINT cursor)) return;
        Move(hwnd, rect, cursor.X, cursor.Y - rect.Height, below: cursor.Y, cursor);
    }

    private static void FixDropDown(ComboBox combo)
    {
        if (combo.Template.FindName("PART_Popup", combo) is not Popup { Child: FrameworkElement child }) return;
        if (!TryGetMisplaced(child, out IntPtr hwnd, out RECT rect)) return;
        if (Window.GetWindow(combo) is not { } window) return;

        IntPtr ownerHwnd = new WindowInteropHelper(window).Handle;
        if (ownerHwnd == IntPtr.Zero || !User32.GetWindowRect(ownerHwnd, out RECT owner)) return;

        double scale = User32.GetDpiForWindow(ownerHwnd) / 96.0;
        Point origin = combo.TransformToAncestor(window).Transform(new Point(0, 0));
        int x = owner.Left + (int)Math.Round(origin.X * scale);
        int y = owner.Top + (int)Math.Round((origin.Y + combo.ActualHeight) * scale);
        Point anchor = new(x + rect.Width / 2, y);
        MoveDropDown(hwnd, rect, x, y, above: owner.Top + (int)Math.Round(origin.Y * scale) - rect.Height, anchor);
    }

    /// <summary>Popup görünmeden önce WPF'e göreli konumu açıkça verir; Win+D sonrası yanlış ekran kökenini kullanmaz.</summary>
    private static void PrepareDropDown(ComboBox combo)
    {
        if (combo.Template.FindName("PART_Popup", combo) is not Popup popup) return;
        popup.Placement = PlacementMode.Custom;
        popup.CustomPopupPlacementCallback = PlaceDropDown;
    }

    private static CustomPopupPlacement[] PlaceDropDown(Size popupSize, Size targetSize, Point offset) =>
    [
        new CustomPopupPlacement(new Point(0, targetSize.Height), PopupPrimaryAxis.Vertical),
        new CustomPopupPlacement(new Point(0, -popupSize.Height), PopupPrimaryAxis.Vertical)
    ];

    /// <summary>Açılır pencerenin gerçek konumu sol üst köşedeyse (yanlış yerleşim) true.</summary>
    private static bool TryGetMisplaced(FrameworkElement popupRoot, out IntPtr hwnd, out RECT rect)
    {
        hwnd = IntPtr.Zero;
        rect = default;
        if (PresentationSource.FromVisual(popupRoot) is not HwndSource source) return false;
        hwnd = source.Handle;
        return User32.GetWindowRect(hwnd, out rect) && rect.Left <= 2 && rect.Top <= 2;
    }

    private static void Move(IntPtr hwnd, RECT rect, int x, int y, int below, POINT onScreen)
    {
        var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        IntPtr monitor = User32.MonitorFromPoint(onScreen, User32.MONITOR_DEFAULTTOPRIMARY);
        if (User32.GetMonitorInfo(monitor, ref info))
        {
            RECT area = info.rcWork;
            if (y < area.Top) y = below;
            x = Math.Max(area.Left, Math.Min(x, area.Right - rect.Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - rect.Height));
        }
        User32.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            User32.SWP_NOSIZE | User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
        Log.Info($"Açılır pencere sol üstte açılmıştı, {x},{y} konumuna taşındı.");
    }

    private static void MoveDropDown(IntPtr hwnd, RECT rect, int x, int y, int above, Point anchor)
    {
        var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        IntPtr monitor = User32.MonitorFromPoint(new POINT { X = (int)anchor.X, Y = (int)anchor.Y }, User32.MONITOR_DEFAULTTOPRIMARY);
        if (User32.GetMonitorInfo(monitor, ref info))
        {
            RECT area = info.rcWork;
            if (y + rect.Height > area.Bottom) y = above;
            x = Math.Max(area.Left, Math.Min(x, area.Right - rect.Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - rect.Height));
        }
        User32.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            User32.SWP_NOSIZE | User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
        Log.Info($"Açılır liste sol üstte açılmıştı, {x},{y} konumuna taşındı.");
    }
}
