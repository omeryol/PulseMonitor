using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PulseMonitor.Core;
using PulseMonitor.Interop;
using PulseMonitor.Settings;

namespace PulseMonitor.Taskbar;

/// <summary>
/// Pencereyi görev çubuğuna bağlar (sahibi Shell_TrayWnd olur, böylece hep üstünde kalır),
/// sistem tepsisinin hemen soluna yerleştirir ve tam ekranda gizler.
/// </summary>
internal sealed class TaskbarDocker : IDisposable
{
    private readonly Window _window;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly int _taskbarCreatedMsg;
    private HwndSource? _source;
    private IntPtr _hwnd;
    private IntPtr _owner;

    public TaskbarDocker(Window window, AppSettings settings)
    {
        _window = window;
        _settings = settings;
        _taskbarCreatedMsg = (int)User32.RegisterWindowMessage("TaskbarCreated");
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    public void Start()
    {
        _hwnd = new WindowInteropHelper(_window).Handle;
        long ex = User32.GetWindowLongPtr(_hwnd, User32.GWL_EXSTYLE).ToInt64();
        User32.SetWindowLongPtr(_hwnd, User32.GWL_EXSTYLE,
            new IntPtr(ex | User32.WS_EX_TOOLWINDOW | User32.WS_EX_NOACTIVATE));

        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
        _window.SizeChanged += OnSizeChanged;
        _timer.Start();
        Refresh();
    }

    public void Refresh()
    {
        if (_hwnd == IntPtr.Zero) return;

        TaskbarSnapshot? found = TaskbarLocator.Find();
        if (found is not { } bar)
        {
            SetShown(false);
            return;
        }

        if (bar.Handle != _owner)
        {
            User32.SetWindowLongPtr(_hwnd, User32.GWLP_HWNDPARENT, bar.Handle);
            _owner = bar.Handle;
            Log.Info($"Görev çubuğuna bağlandı. Çubuk {bar.Bounds}, tepsi {bar.Tray}");
        }

        bool fullscreen = _settings.HideInFullscreen && FullscreenDetector.IsActive(bar.Monitor, _hwnd);
        SetShown(bar.IsOnScreen && !fullscreen);
        if (_window.IsVisible) Place(bar);
    }

    private void Place(TaskbarSnapshot bar)
    {
        double scale = User32.GetDpiForWindow(_hwnd) / 96.0;
        int w = (int)Math.Ceiling(_window.ActualWidth * scale);
        int h = (int)Math.Ceiling(_window.ActualHeight * scale);
        if (w <= 0 || h <= 0) return;

        int gap = (int)Math.Round(_settings.TrayGapPx * scale);
        int x, y;
        if (bar.IsHorizontal)
        {
            x = bar.Tray.Left - w - gap;
            y = bar.Bounds.Top + (bar.Bounds.Height - h) / 2;
        }
        else
        {
            x = bar.Bounds.Left + (bar.Bounds.Width - w) / 2;
            y = bar.Tray.Top - h - gap;
        }

        bool topmost = (User32.GetWindowLongPtr(_hwnd, User32.GWL_EXSTYLE).ToInt64() & User32.WS_EX_TOPMOST) != 0;
        if (User32.GetWindowRect(_hwnd, out RECT now) && now.Left == x && now.Top == y && topmost) return;

        User32.SetWindowPos(_hwnd, User32.HWND_TOPMOST, x, y, 0, 0, User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
    }

    private void SetShown(bool shown)
    {
        if (shown == _window.IsVisible) return;
        if (shown) _window.Show();
        else _window.Hide();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Refresh();

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _taskbarCreatedMsg)
        {
            // Explorer yeniden başladı: yeni görev çubuğuna tekrar bağlan.
            _owner = IntPtr.Zero;
            QueueRefresh();
        }
        else if (msg == User32.WM_SYSCOMMAND && (wParam.ToInt64() & 0xFFF0) == User32.SC_MINIMIZE)
        {
            handled = true;
        }
        else if (msg is User32.WM_DISPLAYCHANGE or User32.WM_SETTINGCHANGE or User32.WM_DPICHANGED)
        {
            QueueRefresh();
        }
        return IntPtr.Zero;
    }

    private void QueueRefresh() => _window.Dispatcher.BeginInvoke(Refresh, DispatcherPriority.Background);

    public void Dispose()
    {
        _timer.Stop();
        _window.SizeChanged -= OnSizeChanged;
        _source?.RemoveHook(WndProc);
    }
}
