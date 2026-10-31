using System.Text.RegularExpressions;

namespace PulseMonitor.Tests;

/// <summary>
/// Sürüm adı iki yerde yazılıdır (csproj ReleaseName → Hakkında; sürüm notunun başındaki ad satırı → GitHub başlığı).
/// İkisi ayrışırsa Hakkında ile GitHub farklı ad gösterir; bu test mevcut sürüm için eşitliği denetler.
/// </summary>
public class ReleaseNamingTests
{
    private static string Root()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "PulseMonitor.csproj"))) dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("PulseMonitor.csproj bulunamadı");
    }

    private static string Csproj => File.ReadAllText(Path.Combine(Root(), "PulseMonitor.csproj"));

    [Fact]
    public void Current_version_has_release_notes_with_the_same_name_as_the_project()
    {
        string version = Regex.Match(Csproj, @"<Version>([^<]+)</Version>").Groups[1].Value;
        string name = Regex.Match(Csproj, @"<ReleaseName>([^<]+)</ReleaseName>").Groups[1].Value;
        Assert.NotEmpty(version);
        Assert.NotEmpty(name);

        string notesFile = Path.Combine(Root(), "release-notes", $"v{version}.md");
        Assert.True(File.Exists(notesFile), $"release-notes/v{version}.md yok");

        Match fromNotes = Regex.Match(File.ReadAllText(notesFile), @"(?m)^<!--\s*name:\s*(.+?)\s*-->");
        Assert.True(fromNotes.Success, "sürüm notunun başında <!-- name: Ad --> satırı yok");
        Assert.Equal(name, fromNotes.Groups[1].Value);
    }

    [Fact]
    public void Release_notes_keep_both_language_sections()
    {
        string version = Regex.Match(Csproj, @"<Version>([^<]+)</Version>").Groups[1].Value;
        string text = File.ReadAllText(Path.Combine(Root(), "release-notes", $"v{version}.md"));
        Assert.Contains("<!-- lang:en -->", text);
        Assert.Contains("<!-- lang:tr -->", text);
    }
}
