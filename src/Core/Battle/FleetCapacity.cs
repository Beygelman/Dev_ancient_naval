using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public int FleetUsed(Side side) => OwnShips(side).Count(s => s.CountsTowardFleet || Rules.DynamicFleetCapacity && s.Definition.Class == ShipClass.Fishing);
    public int FleetCapacity(Side side) => !Rules.DynamicFleetCapacity ? Rules.FleetLimit :
        OwnShips(side).Where(s => s.IsMothership).Sum(s => 4 + 2 * (s.Level - 1)) +
        _villages.Where(v => v.Owner == side && v.Health > 0).Sum(v => 2 + v.Level - 1);
    private bool UsesFleetSlot(ShipClass kind) => kind is not (ShipClass.Balloon or ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)
        && (Rules.Get(kind).Damage > 0 || Rules.DynamicFleetCapacity && kind == ShipClass.Fishing);
    public string? ScuttleBlockReason(Side side, int id)
    {
        if (PendingPresentation is not null || IsOver || ActiveSide != side || PendingUpgrade(side) is not null)
            return "Cannot scuttle a ship now.";
        var ship = Find(id);
        return ship is null || ship.Owner != side || ship.IsMothership ? "Select one of your ships other than the Mothership." : null;
    }
    public bool CanScuttle(Side side, int id) => ScuttleBlockReason(side, id) is null;
    public CommandResult Scuttle(Side side, int id)
    {
        if (ScuttleBlockReason(side, id) is { } error) return CommandResult.Rejected(error);
        var ship = Find(id)!;
        var position = ship.Position;
        RemoveDestroyedShip(ship);
        UpdateVision();
        return new(true, "Ship dismantled. No refund.", CommandKind.Scuttle, id, Path: new[] { position });
    }
}
