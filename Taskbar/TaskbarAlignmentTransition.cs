namespace PulseMonitor.Taskbar;

/// <summary>Görev çubuğu simgelerinin hizasındaki anlamlı geçişleri ayırır.</summary>
internal sealed class TaskbarAlignmentTransition
{
    private bool? _wasCentered;

    /// <summary>İlk okuma sessizdir; yalnızca sola hizalı durumdan ortaya geçişte true döner.</summary>
    public bool EnteredCentered(bool centered)
    {
        bool entered = _wasCentered is false && centered;
        _wasCentered = centered;
        return entered;
    }
}
