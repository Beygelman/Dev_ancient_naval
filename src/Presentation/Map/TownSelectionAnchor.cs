using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    internal Vector2 VillageWorldAnchor(Village town) => Projection.GridToWorld(town.Position) + VillagePlacement(town).Offset;
    internal Vector2 TownHealthAnchor(Village town) => TownHealthAnchor(VillageWorldAnchor(town));
}
