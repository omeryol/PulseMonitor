using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using PulseMonitor.Interop;

namespace PulseMonitor.Core;

/// <summary>
/// "Masaüstünü göster" sonrası WPF açılır pencerelerin (kart, sağ tık menüsü) konumunu yanlış hesaplayıp
/// ekranın sol üstüne koyuyor. Konum düzeltilir ve menü görev çubuğunun üstünde tutulur.
/// </summary>
internal static class PopupPlacementFix
{
    private const int Gap = 4;

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(PopupPlacementFix), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToolTip tip && (bool)e.NewValue) tip.Opened += (_, _) => FixCard(tip);
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
        if (PresentationSource.FromVisual(menu) is not HwndSource source) return;

        uint flags = User32.SWP_NOSIZE | User32.SWP_NOACTIVATE;
        if (User32.GetWindowRect(source.Handle, out RECT rect) && User32.GetCursorPos(out POINT cursor))
        {
            var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            IntPtr monitor = User32.MonitorFromPoint(cursor, User32.MONITOR_DEFAULTTOPRIMARY);
            if (User32.GetMonitorInfo(monitor, ref info))
            {
                RECT area = info.rcWork;
                bool misplaced = Math.Abs(rect.Left) <= 2 && Math.Abs(rect.Top) <= 2;
                int x = misplaced ? cursor.X : rect.Left;
                int y = misplaced ? cursor.Y - rect.Height : rect.Top;
                x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - rect.Width));
                y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - rect.Height));
                User32.SetWindowPos(source.Handle, User32.HWND_TOPMOST, x, y, 0, 0, flags);
                return;
            }
        }
        User32.SetWindowPos(source.Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, flags | User32.SWP_NOMOVE);
    }

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
            RECT area = info.rcMonitor;
            if (y < area.Top) y = below;
            x = Math.Max(area.Left, Math.Min(x, area.Right - rect.Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - rect.Height));
        }
        User32.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            User32.SWP_NOSIZE | User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
        Log.Info($"Açılır pencere sol üstte açılmıştı, {x},{y} konumuna taşındı.");
    }
}
