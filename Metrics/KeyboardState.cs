using PulseMonitor.Interop;

namespace PulseMonitor.Metrics;

public readonly record struct KeyboardSnapshot(bool Caps, bool Num, bool Scroll);

/// <summary>Kilit tuşlarının durumu. Arayüz iş parçacığında okunmalıdır.</summary>
internal static class KeyboardState
{
    private const int VK_CAPITAL = 0x14;
    private const int VK_NUMLOCK = 0x90;
    private const int VK_SCROLL = 0x91;

    public static KeyboardSnapshot Read() => new(IsToggled(VK_CAPITAL), IsToggled(VK_NUMLOCK), IsToggled(VK_SCROLL));

    private static bool IsToggled(int key) => (User32.GetKeyState(key) & 0x0001) != 0;
}
