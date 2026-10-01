using System.Collections.Generic;
using System.Linq;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Plain content shared by the information card and its accessibility/debug text.</summary>
internal sealed record LorePage(string Summary, IReadOnlyList<LoreSection> Sections)
{
    public static LorePage Empty { get; } = new("", System.Array.Empty<LoreSection>());
    public string PlainText => string.Join("\n\n", new[] { Summary }.Concat(
        Sections.Select(section => section.Title + "\n" + string.Join("\n",
            section.Rows.Select(row => row.Label + ": " + row.Value)))));
}

internal sealed record LoreSection(string Title, IReadOnlyList<LoreRow> Rows);
internal sealed record LoreRow(string Label, string Value);
