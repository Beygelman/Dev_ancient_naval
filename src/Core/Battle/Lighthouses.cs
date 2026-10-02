using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    public int LighthousePrice(Side side) => BuildPrice(side, ShipClass.Lighthouse);
    public string? LighthouseBlockReason(Side side, int producerId) => Find(producerId) is { } ship
        ? BuildBlockReason(side, ship.Id, ShipClass.Lighthouse)
        : VillageBuildBlockReason(side, producerId, ShipClass.Lighthouse);
    public IReadOnlyList<GridPosition> LighthouseBuildCells(int producerId) => Find(producerId) is { } ship
        ? SpawnCells(ship.Id) : VillageSpawnCells(producerId);
    public CommandResult BuildLighthouse(Side side, int producerId, GridPosition position)
    {
        if (PendingPresentation is not null) return CommandResult.Rejected("Wait for the current order.");
        return Find(producerId) is { } ship
            ? Build(side, ship.Id, ShipClass.Lighthouse, position)
            : BuildFromVillage(side, producerId, ShipClass.Lighthouse, position);
    }
}
