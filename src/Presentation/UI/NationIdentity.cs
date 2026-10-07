using DevAncientNaval.Core.Battle;

namespace DevAncientNaval.Presentation.UI;

internal static class NationIdentity
{
    // Display names are translated; persisted FleetColor identities never change.
    internal static string Name(FleetColor color) => Language.Translate(SourceName(color));

    internal static string SourceName(FleetColor color) => color switch
    {
        FleetColor.Blue => "Helian Covenant",
        FleetColor.Purple => "Argent Veil",
        FleetColor.Yellow => "Roselight Covenant",
        FleetColor.White => "Crimson Rune",
        FleetColor.Green => "Rootbound Circle",
        _ => "Embercrystal Clans"
    };
}
