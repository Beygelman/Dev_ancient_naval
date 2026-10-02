using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Immutable observed artwork inputs. Deferred texture painting cannot
/// accidentally pick up a capture or upgrade which happened behind the fog.</summary>
internal readonly record struct TownArtState(int Id, GridPosition Position, Side? Owner, int Level,
    bool IsFortified, bool HasPort, GridPosition PortBerth, Color Accent, FleetColor Monument);

public partial class BoardView
{
    private TownArtState ObserveTownArt(Village town) => new(town.Id, town.Position, town.Owner, town.Level,
        town.IsFortified, town.HasPort, Battle.PortBerth(town), FleetPalette.For(Battle, town.Owner),
        town.Owner is null or Side.Pirates ? FleetColor.Blue : Battle.ColorFor(town.Owner.Value));
}
