using PulseMonitor.Taskbar;

namespace PulseMonitor.WindowsTests;

public sealed class TaskbarPositionTests
{
    [Fact]
    public void AlignmentTransitionOnlyReportsLeftToCenter()
    {
        var transition = new TaskbarAlignmentTransition();
        Assert.False(transition.EnteredCentered(false));
        Assert.False(transition.EnteredCentered(false));
        Assert.True(transition.EnteredCentered(true));
        Assert.False(transition.EnteredCentered(true));
    }

    [Fact]
    public void AlignmentTransitionDoesNotPromptOnInitialCenteredState() =>
        Assert.False(new TaskbarAlignmentTransition().EnteredCentered(true));

    [Fact]
    public void ScaleShrinksToMeasuredStartButtonSpace() =>
        Assert.Equal(0.8, TaskbarScale.Fit(1, 200, 160, 1));

    [Fact]
    public void ScaleKeepsRequestedValueWhenThereIsSpace() =>
        Assert.Equal(0.7, TaskbarScale.Fit(0.7, 200, 500, 1));

    [Fact]
    public void ScaleRefusesSpaceBelowReadableMinimum() =>
        Assert.Null(TaskbarScale.Fit(1, 200, 100, 1));

    [Fact]
    public void RightPlacementRespectsTrayAndGap() =>
        Assert.Equal(1490, TaskbarPosition.Calculate(0, 1700, 0, 1920, 200, 10, false));

    [Fact]
    public void LeftPlacementSupportsNegativeMonitorCoordinates() =>
        Assert.Equal(-1920, TaskbarPosition.Calculate(-1920, -300, -1920, 0, 200, 10, true));

    [Fact]
    public void PlacementIsClampedWhenThereIsNotEnoughSpace() =>
        Assert.Equal(0, TaskbarPosition.Calculate(0, 20, 0, 100, 200, 0, false));
}
