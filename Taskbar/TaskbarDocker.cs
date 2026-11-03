using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PulseMonitor.Core;
using PulseMonitor.Interop;
using PulseMonitor.Settings;
using PulseMonitor.Platform;
using PulseMonitor.Views;

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
    private IntPtr _fgHook;
    private IntPtr _reorderHook;
    private readonly DispatcherTimer _burst;
    private DateTime _burstUntil;
    private User32.WinEventProc? _fgProc;
    private bool _wanted;
    private Rect _lastStartBounds = Rect.Empty;
    private readonly TaskbarAlignmentTransition _alignmentTransition = new();

    /// <summary>Simgeler sol hizadan ortaya geçince, sağdaki panel için sol yerleşim önerilebilir.</summary>
    public event Action? CenteredAfterLeftAlignment;

    public TaskbarDocker(Window window, AppSettings settings)
    {
        _window = window;
        _settings = settings;
        _taskbarCreatedMsg = (int)User32.RegisterWindowMessage("TaskbarCreated");
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        // Win+D sırasında görev çubuğu kısa süre pencerenin üstüne çıkar: birkaç yüz ms sık bakılır.
        _burst = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromMilliseconds(15) };
        _burst.Tick += (_, _) => BurstTick();
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
        // "Masaüstünü göster" masaüstü penceresini öne alır: olay gelir gelmez yeniden en üste al.
        _fgProc = (_, evt, hwnd, _, _, _, _) =>
        {
            // Görev çubuğu pencerenin üstüne çıkarsa (Win+D) hemen geri al; kendi sıra değişikliklerimizi yok say.
            if (evt == User32.EVENT_SYSTEM_FOREGROUND) StartBurst();
            if (evt == User32.EVENT_OBJECT_REORDER)
            {
                if (hwnd != _owner || _owner == IntPtr.Zero || !_wanted) return;
                User32.SetWindowPos(_hwnd, User32.HWND_TOPMOST, 0, 0, 0, 0,
                    User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
                return;
            }
            _window.Dispatcher.BeginInvoke(Refresh, DispatcherPriority.Send);
        };
        _reorderHook = User32.SetWinEventHook(User32.EVENT_OBJECT_REORDER, User32.EVENT_OBJECT_REORDER,
            IntPtr.Zero, _fgProc, 0, 0, User32.WINEVENT_OUTOFCONTEXT);
        _fgHook = User32.SetWinEventHook(User32.EVENT_SYSTEM_FOREGROUND, User32.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _fgProc, 0, 0, User32.WINEVENT_OUTOFCONTEXT);
        _timer.Start();
        Refresh();
    }

    public void Refresh()
    {
        if (_hwnd == IntPtr.Zero) return;

        TaskbarSnapshot? found = TaskbarLocator.Find(_settings.TaskbarMonitor);
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

        // Masaüstünü göster pencereyi küçültülmüş duruma alabilir: WPF konumu yanlış sanar,
        // kart ve menü ekranın sol üstünde açılır.
        if (_window.IsVisible && (User32.IsIconic(_hwnd) || _window.WindowState != WindowState.Normal))
        {
            Log.Info("Pencere küçültülmüştü, eski durumuna döndürüldü.");
            User32.ShowWindow(_hwnd, User32.SW_SHOWNOACTIVATE);
            _window.WindowState = WindowState.Normal;
        }

        bool fullscreen = _settings.HideInFullscreen && FullscreenDetector.IsActive(bar.Monitor, _hwnd);
        bool centered = TaskbarAlignment.IsCentered();
        if (_alignmentTransition.EnteredCentered(centered))
        {
            Log.Info("Görev çubuğu simgeleri sola hizadan ortaya alındı; panel konumu soruluyor.");
            CenteredAfterLeftAlignment?.Invoke();
        }
        bool left = _settings.TaskbarLeft && centered;
        if (!left && _window is TaskbarWindow taskbarWindow) taskbarWindow.RestoreRequestedScale();
        bool hasSpace = !left || FitLeftPlacement(bar);
        SetShown(bar.IsOnScreen && !fullscreen && hasSpace);
        if (_window.IsVisible) Place(bar);
    }

    private bool FitLeftPlacement(TaskbarSnapshot bar)
    {
        if (!TaskbarStartButtonLocator.TryGetBounds(bar, out Rect start))
        {
            _lastStartBounds = Rect.Empty;
            Log.Warn("Başlat düğmesinin konumu doğrulanamadı; sol görev çubuğu paneli geçici olarak gizlendi.");
            return false;
        }

        if (start != _lastStartBounds)
        {
            Log.Info($"Başlat düğmesi doğrulandı: {start.Left:0},{start.Top:0} {start.Width:0}x{start.Height:0}");
            _lastStartBounds = start;
        }

        double dpi = User32.GetDpiForWindow(_hwnd) / 96.0;
        // Düğme ile panel arasında bir fiziksel piksel bırakılır; kenara değme ve yuvarlama
        // nedeniyle oluşabilecek tek piksellik çakışmayı da engeller.
        int available = Math.Max(0, (int)Math.Floor(start.Left - bar.Bounds.Left) - 1);
        bool fits = _window is TaskbarWindow taskbarWindow && taskbarWindow.FitToTaskbarWidth(available, dpi);
        if (!fits)
            Log.Warn($"Başlat düğmesinin solunda yalnızca {available}px var; %55 altına inmeden panel sığmadığı için gizlendi.");
        return fits;
    }

    private void Place(TaskbarSnapshot bar)
    {
        double scale = User32.GetDpiForWindow(_hwnd) / 96.0;
        int w = (int)Math.Ceiling(_window.ActualWidth * scale);
        int h = (int)Math.Ceiling(_window.ActualHeight * scale);
        if (w <= 0 || h <= 0) return;

        int gap = (int)Math.Round(_settings.TrayGapPx * scale);
        bool left = _settings.TaskbarLeft && TaskbarAlignment.IsCentered();
        int x, y;
        if (bar.IsHorizontal)
        {
            x = TaskbarPosition.Calculate(bar.Bounds.Left, bar.Tray.Left, bar.Monitor.Left, bar.Monitor.Right, w, gap, left);
            y = bar.Bounds.Top + (bar.Bounds.Height - h) / 2;
        }
        else
        {
            x = bar.Bounds.Left + (bar.Bounds.Width - w) / 2;
            y = TaskbarPosition.Calculate(bar.Bounds.Top, bar.Tray.Top, bar.Monitor.Top, bar.Monitor.Bottom, h, gap, left);
        }

        bool topmost = (User32.GetWindowLongPtr(_hwnd, User32.GWL_EXSTYLE).ToInt64() & User32.WS_EX_TOPMOST) != 0;
        // Konum aynı olsa da sıra "Masaüstünü göster" ile bozulabilir; en üstte kalma her turda yenilenir.
        if (User32.GetWindowRect(_hwnd, out RECT now) && now.Left == x && now.Top == y && topmost)
        {
            User32.SetWindowPos(_hwnd, User32.HWND_TOPMOST, 0, 0, 0, 0,
                User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
            return;
        }

        User32.SetWindowPos(_hwnd, User32.HWND_TOPMOST, x, y, 0, 0, User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
    }

    private void StartBurst()
    {
        _burstUntil = DateTime.UtcNow.AddMilliseconds(1500);
        _burst.Start();
    }

    private void BurstTick()
    {
        if (DateTime.UtcNow > _burstUntil || !_wanted) { _burst.Stop(); return; }
        // Görev çubuğu hemen üstümüzdeyse geri en üste al.
        if (_owner != IntPtr.Zero && User32.GetWindow(_hwnd, User32.GW_HWNDPREV) == _owner)
            User32.SetWindowPos(_hwnd, User32.HWND_TOPMOST, 0, 0, 0, 0,
                User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
    }

    private void SetShown(bool shown)
    {
        _wanted = shown;
        if (shown == _window.IsVisible)
        {
            // "Masaüstünü göster" pencereyi Win32 düzeyinde gizler; WPF hâlâ görünür sanır.
            if (!shown) return;
            if (!User32.IsWindowVisible(_hwnd))
            {
                User32.ShowWindow(_hwnd, User32.SW_SHOWNOACTIVATE);
                Log.Info("Pencere kabuk tarafından gizlenmişti, yeniden gösterildi.");
            }
            else if (Dwmapi.IsCloaked(_hwnd))
            {
                Log.Info("Pencere örtülmüştü, yeniden gösteriliyor.");
                _window.Hide();
                _window.Show();
            }
            return;
        }
        if (shown) _window.Show();
        else _window.Hide();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Refresh();

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_WINDOWPOSCHANGING && _wanted)
        {
            // Kabuk (Masaüstünü göster) pencereyi gizlemeye ya da arkaya almaya kalkarsa engelle.
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            bool changed = false;
            if ((pos.flags & User32.SWP_HIDEWINDOW) != 0) { pos.flags &= ~User32.SWP_HIDEWINDOW; changed = true; }
            if ((pos.flags & User32.SWP_NOZORDER) == 0 && pos.hwndInsertAfter != User32.HWND_TOPMOST)
            {
                pos.hwndInsertAfter = User32.HWND_TOPMOST;
                changed = true;
            }
            if (changed) Marshal.StructureToPtr(pos, lParam, false);
        }
        else if (msg == User32.WM_SHOWWINDOW && wParam == IntPtr.Zero && _wanted)
        {
            QueueRefresh();
        }
        else if (msg == User32.WM_SIZE && wParam.ToInt64() == 1 && _wanted)
        {
            QueueRefresh();
        }
        else if (msg == _taskbarCreatedMsg)
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
        _burst.Stop();
        if (_fgHook != IntPtr.Zero) User32.UnhookWinEvent(_fgHook);
        if (_reorderHook != IntPtr.Zero) User32.UnhookWinEvent(_reorderHook);
        _window.SizeChanged -= OnSizeChanged;
        _source?.RemoveHook(WndProc);
    }
}
