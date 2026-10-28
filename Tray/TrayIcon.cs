using System.Runtime.InteropServices;
using System.Windows.Interop;
using PulseMonitor.Core;
using PulseMonitor.Interop;

namespace PulseMonitor.Tray;

/// <summary>
/// Sistem tepsisi simgesi (Shell_NotifyIcon). Gizli bir üst düzey pencere kullanır;
/// böylece Explorer yeniden başlayınca gelen TaskbarCreated mesajını alıp simgeyi geri ekler.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const int CallbackMessage = User32.WM_APP + 1;
    private const int IconId = 1;

    private readonly HwndSource _source;
    private readonly int _taskbarCreatedMsg;
    private readonly IntPtr _icon;
    private readonly string _tooltip;

    /// <summary>Gizli tepsi penceresi; menü açılmadan önce öne alınır.</summary>
    public IntPtr Handle => _source.Handle;

    public event Action? RightClick;
    public event Action? LeftDoubleClick;

    public TrayIcon(string iconPath, string tooltip)
    {
        _tooltip = tooltip;
        _taskbarCreatedMsg = (int)User32.RegisterWindowMessage("TaskbarCreated");
        _source = new HwndSource(new HwndSourceParameters("PulseMonitor.Tray") { WindowStyle = 0 });
        _source.AddHook(WndProc);

        int size = User32.GetSystemMetrics(User32.SM_CXSMICON);
        _icon = User32.LoadImage(IntPtr.Zero, iconPath, User32.IMAGE_ICON, size, size, User32.LR_LOADFROMFILE);
        if (_icon == IntPtr.Zero) Log.Warn($"Tepsi simgesi yüklenemedi: {iconPath}");

        Add();
    }

    private void Add()
    {
        var data = CreateData();
        data.uFlags = Shell32.NIF_MESSAGE | Shell32.NIF_ICON | Shell32.NIF_TIP;
        if (!Shell32.Shell_NotifyIcon(Shell32.NIM_ADD, ref data))
            Log.Warn("Tepsi simgesi eklenemedi.");
    }

    /// <summary>Windows bildirimi (Windows 10/11'de bildirim merkezine düşer). Odaklanma yardımı açıksa Windows erteler.</summary>
    public void ShowNotification(string title, string text, bool warning)
    {
        var data = CreateData();
        data.uFlags = Shell32.NIF_INFO;
        data.szInfoTitle = title.Length > 63 ? title[..63] : title;
        data.szInfo = text.Length > 255 ? text[..255] : text;
        data.dwInfoFlags = warning ? Shell32.NIIF_WARNING : Shell32.NIIF_INFO;
        if (!Shell32.Shell_NotifyIcon(Shell32.NIM_MODIFY, ref data))
            Log.Warn($"Bildirim gösterilemedi: {title}");
    }

    private NOTIFYICONDATA CreateData() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = _source.Handle,
        uID = IconId,
        uCallbackMessage = CallbackMessage,
        hIcon = _icon,
        szTip = _tooltip,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == CallbackMessage)
        {
            int mouse = (int)(lParam.ToInt64() & 0xFFFF);
            if (mouse == User32.WM_RBUTTONUP) RightClick?.Invoke();
            else if (mouse == User32.WM_LBUTTONDBLCLK) LeftDoubleClick?.Invoke();
            handled = true;
        }
        else if (msg == _taskbarCreatedMsg)
        {
            Add();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        var data = CreateData();
        Shell32.Shell_NotifyIcon(Shell32.NIM_DELETE, ref data);
        if (_icon != IntPtr.Zero) User32.DestroyIcon(_icon);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
