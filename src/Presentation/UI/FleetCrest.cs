using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class FleetCrest : Control
{
    public FleetColor Faction { get; set; }
    public override void _Draw()
    {
        var center = new Vector2(Size.X / 2, Size.Y - 2);
        DrawSetTransform(center, 0, new Vector2(1, .42f));
        DrawCircle(Vector2.Zero, 13, new Color(FleetPalette.Color(Faction), .25f));
        DrawSetTransform(Vector2.Zero);
        FactionSanctuaryArt.Draw(this, (x,y,z) => center + new Vector2((x-y)*.85f, (x+y)*.42f-z), Faction);
    }
}
