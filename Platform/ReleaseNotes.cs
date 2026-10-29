using System.Text.RegularExpressions;

namespace PulseMonitor.Platform;

/// <summary>
/// GitHub sürüm metninden (kaynağı depodaki <c>release-notes/v&lt;sürüm&gt;.md</c>) seçili dilin bölümünü alır.
/// Her bölüm <c>&lt;!-- lang:xx --&gt;</c> satırıyla başlar, ardından GitHub'da görünen bir başlık gelir (pencerede gösterilmez).
/// Dil yoksa İngilizce, işaret hiç yoksa metnin tamamı döner.
/// </summary>
internal static partial class ReleaseNotes
{
    [GeneratedRegex(@"^[ \t]*<!--\s*lang:([A-Za-z-]+)\s*-->[ \t]*\r?$", RegexOptions.Multiline)]
    private static partial Regex Marker();

    public static string ForLanguage(string body, string code)
    {
        MatchCollection marks = Marker().Matches(body);
        if (marks.Count == 0) return body.Trim();

        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < marks.Count; i++)
        {
            int start = marks[i].Index + marks[i].Length;
            int end = i + 1 < marks.Count ? marks[i + 1].Index : body.Length;
            sections[marks[i].Groups[1].Value] = WithoutHeading(body[start..end]);
        }
        return sections.GetValueOrDefault(code) ?? sections.GetValueOrDefault("en") ?? sections.Values.First();
    }

    /// <summary>Bölümün ilk satırı başlıksa ("## English") atılır; pencerenin kendi başlığı var.</summary>
    private static string WithoutHeading(string section)
    {
        string text = section.Trim();
        if (!text.StartsWith('#')) return text;
        int newline = text.IndexOf('\n');
        return newline < 0 ? "" : text[(newline + 1)..].Trim();
    }
}
