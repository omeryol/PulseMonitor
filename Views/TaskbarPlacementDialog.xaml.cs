using System.Windows;
using System.Windows.Interop;
using PulseMonitor.Core;
using PulseMonitor.Interop;

namespace PulseMonitor.Views;

/// <summary>Görev çubuğu hizası değişince panelin sol konuma alınmasını Pulse temasında sorar.</summary>
public partial class TaskbarPlacementDialog : Window
{
    public TaskbarPlacementDialog()
    {
        WindowTheme.Prepare(this);
        InitializeComponent();
        Loaded += (_, _) => BringToFront();
    }

    private void BringToFront()
    {
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        User32.SetWindowPos(hwnd, User32.HWND_TOPMOST, 0, 0, 0, 0,
            User32.SWP_NOMOVE | User32.SWP_NOSIZE);
        User32.SetForegroundWindow(hwnd);
        Activate();
    }

    private void MoveLeft_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void KeepRight_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
