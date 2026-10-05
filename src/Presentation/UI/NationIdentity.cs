using DevAncientNaval.Core.Battle;

namespace DevAncientNaval.Presentation.UI;

internal static class NationIdentity
{
    internal static string Name(FleetColor color) => color switch
    {
        FleetColor.Blue => "Helian Covenant",
        FleetColor.Purple => "Argent Veil",
        FleetColor.Yellow => "Roselight Covenant",
        FleetColor.White => "Crimson Rune",
        FleetColor.Green => "Rootbound Circle",
        _ => "Embercrystal Clans"
    };
}
