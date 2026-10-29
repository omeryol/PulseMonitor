using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Platform;

namespace PulseMonitor.ViewModels;

/// <summary>Güncelleme penceresi: yeni sürüm, seçili dildeki sürüm notları, indirme ve sürüm sayfası.</summary>
public sealed class UpdateViewModel
{
    internal UpdateViewModel(UpdateChecker updates)
    {
        Heading = string.Format(UiLanguage.Culture, Strings.Get("Update.Heading"), updates.LatestVersion);
        CurrentVersion = string.Format(UiLanguage.Culture, Strings.Get("Update.Current"), UpdateChecker.Current.ToString(3));
        string notes = ReleaseNotes.ForLanguage(updates.Notes, UiLanguage.Code);
        Notes = notes.Length > 0 ? notes : Strings.Get("Update.NoNotes");
        DownloadCommand = new RelayCommand(() => SystemActions.OpenUrl(updates.DownloadUrl));
        ReleasePageCommand = new RelayCommand(() => SystemActions.OpenUrl(updates.ReleaseUrl));
    }

    public string Heading { get; }
    public string CurrentVersion { get; }
    /// <summary>Markdown; pencerede MarkdownDocument ile çizilir.</summary>
    public string Notes { get; }
    public ICommand DownloadCommand { get; }
    public ICommand ReleasePageCommand { get; }
}
