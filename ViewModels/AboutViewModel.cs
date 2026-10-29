using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PulseMonitor.Core;
using PulseMonitor.Platform;

namespace PulseMonitor.ViewModels;

/// <summary>Hakkında: sürüm, güncelleme durumu ve programa gömülü lisans metinleri (LICENSE, THIRD-PARTY-NOTICES.md).</summary>
public sealed class AboutViewModel : ObservableObject
{
    private static CultureInfo Tr => UiLanguage.Culture;
    private readonly UpdateChecker _updates;
    private string _updateText = "";
    private bool _updateAvailable;

    internal AboutViewModel(UpdateChecker updates, Action openUpdate)
    {
        _updates = updates;
        CheckCommand = new RelayCommand(async () =>
        {
            UpdateText = Strings.Get("Update.Checking");
            await _updates.CheckAsync();
            Application.Current?.Dispatcher.BeginInvoke(Refresh);
        });
        DownloadCommand = new RelayCommand(openUpdate); // yenilikler penceresi, oradan indirilir
        Refresh();
    }

    public string Version { get; } = UpdateChecker.Current.ToString(3);
    public string LicenseText { get; } = ReadEmbedded("LICENSE");
    public string NoticesText { get; } = ReadEmbedded("THIRD-PARTY-NOTICES.md");
    public ICommand CheckCommand { get; }
    public ICommand DownloadCommand { get; }

    public string UpdateText { get => _updateText; private set => Set(ref _updateText, value); }
    public bool UpdateAvailable { get => _updateAvailable; private set => Set(ref _updateAvailable, value); }

    private void Refresh()
    {
        UpdateAvailable = _updates.State == UpdateState.Available;
        string when = _updates.LastChecked is { } t ? t.ToString("HH:mm", Tr) : "";
        UpdateText = _updates.State switch
        {
            UpdateState.Available => string.Format(Tr, Strings.Get("Update.Available"), _updates.LatestVersion),
            UpdateState.UpToDate => string.Format(Tr, Strings.Get("Update.UpToDate"), when),
            UpdateState.Failed => Strings.Get("Update.Failed"),
            _ => Strings.Get("Update.NotChecked"),
        };
    }

    private static string ReadEmbedded(string name)
    {
        try
        {
            var info = Application.GetResourceStream(new Uri($"pack://application:,,,/{name}"));
            if (info is null) return string.Empty;
            using var reader = new StreamReader(info.Stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Log.Warn($"Gömülü metin okunamadı: {name}", ex);
            return string.Empty;
        }
    }
}
