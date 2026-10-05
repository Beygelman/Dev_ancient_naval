using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Both menus read the checked-in issue identity, never today's runtime date.</summary>
internal static class GameIdentity
{
    internal static string Version => ProjectSettings.GetSetting("application/config/version", "v020.7b").AsString();
    internal static string IssueDate => ProjectSettings.GetSetting("application/config/release_date", "06.10.2026").AsString();
    internal static string Signature => $"GitHub: Beygelman  @Ancient_Naval_{Version} {IssueDate}";
    internal static Label Footer(string name) => new()
    {
        Name = name, Text = Signature, MouseFilter = Control.MouseFilterEnum.Ignore,
        Modulate = new Color(1, 1, 1, .55f),
        HorizontalAlignment = HorizontalAlignment.Left
    };
}
