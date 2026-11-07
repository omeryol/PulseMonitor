using PulseMonitor.Settings;
using PulseMonitor.Views.Cards;

namespace PulseMonitor.WindowsTests;

public sealed class DetailCardClickTests
{
    [Theory]
    [InlineData(TaskbarItemId.Upload, SettingsTab.Traffic)]
    [InlineData(TaskbarItemId.Download, SettingsTab.Traffic)]
    [InlineData(TaskbarItemId.Weather, SettingsTab.Weather)]
    [InlineData(TaskbarItemId.Gpu, SettingsTab.General)]
    [InlineData(TaskbarItemId.Cpu, SettingsTab.Items)]
    [InlineData(TaskbarItemId.Ram, SettingsTab.Items)]
    [InlineData(TaskbarItemId.Disk, SettingsTab.Items)]
    [InlineData(TaskbarItemId.Keyboard, SettingsTab.Items)]
    [InlineData(TaskbarItemId.Ping, SettingsTab.Alerts)]
    [InlineData(TaskbarItemId.GpuTemp, SettingsTab.Alerts)]
    public void Card_maps_to_settings_tab(string id, SettingsTab tab) => Assert.Equal(tab, CardTargets.Tab(id));

    [Fact]
    public void Every_default_item_has_a_settings_tab_and_unknown_has_none()
    {
        foreach (string id in TaskbarItemId.DefaultOrder) Assert.NotNull(CardTargets.Tab(id));
        Assert.Null(CardTargets.Tab("Nope"));
    }

    [Theory]
    [InlineData(TaskbarItemId.Cpu, WindowsPage.TaskManager)]
    [InlineData(TaskbarItemId.Ram, WindowsPage.TaskManager)]
    [InlineData(TaskbarItemId.Gpu, WindowsPage.TaskManager)]
    [InlineData(TaskbarItemId.Disk, WindowsPage.Storage)]
    [InlineData(TaskbarItemId.Upload, WindowsPage.Network)]
    [InlineData(TaskbarItemId.Ping, WindowsPage.Network)]
    [InlineData(TaskbarItemId.PublicIp, WindowsPage.Network)]
    [InlineData(TaskbarItemId.Keyboard, WindowsPage.None)]
    [InlineData(TaskbarItemId.Weather, WindowsPage.None)]
    public void Card_maps_to_windows_page(string id, WindowsPage page) => Assert.Equal(page, CardTargets.Page(id));

    [Fact]
    public void Windows_page_uris()
    {
        Assert.Equal("ms-settings:storagesense", CardTargets.SettingsUri(WindowsPage.Storage));
        Assert.Equal("ms-settings:network-status", CardTargets.SettingsUri(WindowsPage.Network));
        Assert.Null(CardTargets.SettingsUri(WindowsPage.TaskManager));
        Assert.Null(CardTargets.SettingsUri(WindowsPage.None));
    }

    private static readonly object A = new(), B = new();

    [Fact]
    public void Opens_only_after_delay_on_same_column()
    {
        var t = new CardHoverTracker();
        Assert.Equal(CardHoverAction.None, t.Tick(A, false, 0));
        Assert.Equal(CardHoverAction.None, t.Tick(A, false, 399));
        Assert.Equal(CardHoverAction.Open, t.Tick(A, false, 400));
        Assert.Same(A, t.Open);
    }

    [Fact]
    public void Leaving_before_delay_cancels_open()
    {
        var t = new CardHoverTracker();
        t.Tick(A, false, 0);
        t.Tick(null, false, 300);
        Assert.Equal(CardHoverAction.None, t.Tick(A, false, 500)); // sayaç yeniden başlar
        Assert.Equal(CardHoverAction.Open, t.Tick(A, false, 900));
    }

    [Fact]
    public void Moving_from_column_to_card_keeps_open_and_leaving_both_closes_after_grace()
    {
        var t = Opened(A);
        t.Tick(A, false, 950);
        Assert.Equal(CardHoverAction.None, t.Tick(null, false, 1000)); // sütundan çıktı, kart henüz değil
        Assert.Equal(CardHoverAction.None, t.Tick(null, true, 1050)); // karta geçti
        Assert.Equal(CardHoverAction.None, t.Tick(null, true, 5000)); // kartta kaldıkça açık
        Assert.Equal(CardHoverAction.None, t.Tick(null, false, 5050));
        Assert.Equal(CardHoverAction.None, t.Tick(null, false, 5249));
        Assert.Equal(CardHoverAction.Close, t.Tick(null, false, 5250));
        Assert.Null(t.Open);
    }

    [Fact]
    public void Gap_crossing_within_grace_does_not_close()
    {
        var t = Opened(A);
        t.Tick(A, false, 950);
        t.Tick(null, false, 1000);
        Assert.Equal(CardHoverAction.None, t.Tick(null, false, 1100));
        Assert.Equal(CardHoverAction.None, t.Tick(null, true, 1180));
        Assert.Same(A, t.Open);
    }

    [Fact]
    public void Other_column_moves_card_without_delay()
    {
        var t = Opened(A);
        Assert.Equal(CardHoverAction.Open, t.Tick(B, false, 1000));
        Assert.Same(B, t.Open);
    }

    [Fact]
    public void Active_covers_pending_and_open_and_reset_closes()
    {
        var t = new CardHoverTracker();
        t.Tick(A, false, 0);
        Assert.Same(A, t.Active); // ölçümler bekleme sırasında başlar
        t.Tick(A, false, 400);
        Assert.True(t.Reset());
        Assert.Null(t.Active);
        Assert.False(t.Reset());
    }

    private static CardHoverTracker Opened(object column)
    {
        var t = new CardHoverTracker();
        t.Tick(column, false, 0);
        t.Tick(column, false, 400);
        return t;
    }
}
