using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PulseMonitor.Views.Controls;

/// <summary>
/// Lisans ve üçüncü taraf bildirimlerini okunur bir FlowDocument'e çevirir. Yalnızca bu dosyalarda geçen
/// Markdown parçaları desteklenir: başlık, paragraf, tablo, alıntı, [bağlantı](url) ve `kod`.
/// </summary>
internal static partial class MarkdownDocument
{
    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^)\s]+)\)|`([^`]+)`")]
    private static partial Regex InlinePattern();

    public static FlowDocument Build(string text)
    {
        var doc = new FlowDocument { PagePadding = new Thickness(14, 10, 14, 12), FontSize = 13, TextAlignment = TextAlignment.Left };
        doc.SetResourceReference(FlowDocument.FontFamilyProperty, "Font.Ui");
        doc.SetResourceReference(FlowDocument.ForegroundProperty, "Ui.Text");

        string[] lines = text.Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length;)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) { i++; continue; }
            if (line.StartsWith('#')) { doc.Blocks.Add(Heading(line)); i++; }
            else if (line.StartsWith('|')) doc.Blocks.Add(Table(Collect(lines, ref i, l => l.StartsWith('|'))));
            else if (line.StartsWith('>')) doc.Blocks.Add(Quote(Collect(lines, ref i, l => l.StartsWith('>'))));
            else doc.Blocks.Add(Paragraph(string.Join(" ", Collect(lines, ref i, IsPlain))));
        }
        return doc;
    }

    private static bool IsPlain(string l) => l.Length > 0 && !l.StartsWith('#') && !l.StartsWith('|') && !l.StartsWith('>');

    private static List<string> Collect(string[] lines, ref int i, Func<string, bool> match)
    {
        var block = new List<string>();
        while (i < lines.Length && match(lines[i].Trim())) block.Add(lines[i++].Trim());
        return block;
    }

    private static Paragraph Heading(string line)
    {
        int level = line.TakeWhile(c => c == '#').Count();
        var p = new Paragraph(new Run(line[level..].Trim()))
        {
            FontSize = level == 1 ? 18 : 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, level == 1 ? 0 : 14, 0, 6),
        };
        return p;
    }

    private static Paragraph Paragraph(string text)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 10), LineHeight = 20 };
        AddInlines(p.Inlines, text);
        return p;
    }

    private static Section Quote(List<string> lines)
    {
        var section = new Section { BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(12, 2, 0, 2), Margin = new Thickness(0, 0, 0, 10), FontSize = 12 };
        section.SetResourceReference(Section.BorderBrushProperty, "Ui.Border");
        section.SetResourceReference(Section.ForegroundProperty, "Ui.Muted");
        var current = new List<string>();
        foreach (string raw in lines.Append(">"))
        {
            string text = raw.TrimStart('>').Trim();
            if (text.Length > 0) { current.Add(text); continue; }
            if (current.Count > 0) section.Blocks.Add(Paragraph(string.Join(" ", current)));
            current.Clear();
        }
        return section;
    }

    private static Table Table(List<string> rows)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 2, 0, 12), FontSize = 12, BorderThickness = new Thickness(1) };
        table.SetResourceReference(System.Windows.Documents.Table.BorderBrushProperty, "Ui.Border");
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        bool header = true;
        foreach (string row in rows)
        {
            string[] cells = row.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.All(c => c.Trim('-', ':').Length == 0)) continue;   // |---|---| ayırıcı satırı
            while (table.Columns.Count < cells.Length)
                table.Columns.Add(new TableColumn { Width = new GridLength(table.Columns.Count == 0 ? 1.1 : 1.4, GridUnitType.Star) });

            var tr = new TableRow();
            if (header) { tr.FontWeight = FontWeights.SemiBold; tr.SetResourceReference(TableRow.BackgroundProperty, "Ui.AccentSoft"); }
            else if (group.Rows.Count % 2 == 0) tr.SetResourceReference(TableRow.BackgroundProperty, "Ui.Hover");
            foreach (string cell in cells)
            {
                var p = new Paragraph { Margin = new Thickness(0), LineHeight = 18 };
                AddInlines(p.Inlines, cell);
                var tc = new TableCell(p) { Padding = new Thickness(8, 6, 8, 6), BorderThickness = new Thickness(0, 0, 0, 1) };
                tc.SetResourceReference(TableCell.BorderBrushProperty, "Ui.Border");
                tr.Cells.Add(tc);
            }
            group.Rows.Add(tr);
            header = false;
        }
        return table;
    }

    private static void AddInlines(InlineCollection target, string text)
    {
        int pos = 0;
        foreach (Match m in InlinePattern().Matches(text))
        {
            if (m.Index > pos) target.Add(new Run(text[pos..m.Index]));
            if (m.Groups[1].Success)
            {
                var link = new Hyperlink(new Run(m.Groups[1].Value)) { NavigateUri = new Uri(m.Groups[2].Value), ToolTip = m.Groups[2].Value };
                link.SetResourceReference(Hyperlink.ForegroundProperty, "Ui.Accent");
                target.Add(link);
            }
            else
            {
                var code = new Run(m.Groups[3].Value) { FontFamily = new FontFamily("Consolas"), FontSize = 11.5 };
                code.SetResourceReference(Run.BackgroundProperty, "Ui.Hover");
                target.Add(code);
            }
            pos = m.Index + m.Length;
        }
        if (pos < text.Length) target.Add(new Run(text[pos..]));
    }
}
