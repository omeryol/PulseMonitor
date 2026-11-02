using PulseMonitor.Taskbar;

namespace PulseMonitor.WindowsTests;

public sealed class TaskbarPositionTests
{
    [Fact]
    public void RightPlacementRespectsTrayAndGap() =>
        Assert.Equal(1490, TaskbarPosition.Calculate(0, 1700, 0, 1920, 200, 10, false, 1));

    [Fact]
    public void LeftPlacementSupportsNegativeMonitorCoordinates() =>
        Assert.Equal(-1850, TaskbarPosition.Calculate(-1920, -300, -1920, 0, 200, 0, true, 1.25));

    [Fact]
    public void PlacementIsClampedWhenThereIsNotEnoughSpace() =>
        Assert.Equal(0, TaskbarPosition.Calculate(0, 20, 0, 100, 200, 0, false, 1));
}
