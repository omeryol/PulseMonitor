using PulseMonitor.Settings;
using PulseMonitor.ViewModels;

namespace PulseMonitor.WindowsTests;

public sealed class TaskbarLayoutTests
{
    private static (object, DetailCard?) E(string id) => (id, new DetailCard(id, id, "Brush.Text"));

    private static List<string> Shape(params string[] ids) =>
        TaskbarLayout.Build(ids.Select(E)).Select(c => $"{c.Top}+{c.Bottom}").ToList();

    [Fact]
    public void OddItemBeforeNetworkDoesNotSplitUploadDownloadPair()
    {
        Assert.Equal(["Quota+", "Upload+Download", "Cpu+Ram"],
            Shape(TaskbarItemId.Quota, TaskbarItemId.Upload, TaskbarItemId.Download, TaskbarItemId.Cpu, TaskbarItemId.Ram));
    }

    [Fact]
    public void NetworkPairStaysTogetherWhenDownloadComesFirst()
    {
        Assert.Equal(["Quota+", "Download+Upload"], Shape(TaskbarItemId.Quota, TaskbarItemId.Download, TaskbarItemId.Upload));
    }

    [Fact]
    public void OrdinaryItemsStillPairTwoByTwo()
    {
        Assert.Equal(["Upload+Download", "Cpu+Ram", "Gpu+"], Shape(TaskbarItemId.Upload, TaskbarItemId.Download, TaskbarItemId.Cpu, TaskbarItemId.Ram, TaskbarItemId.Gpu));
    }
}
