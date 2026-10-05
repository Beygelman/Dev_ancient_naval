using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public int FleetUsed(Side side) => OwnShips(side).Sum(s => FleetSlotCost(s.Definition.Class));
    public int FleetCapacity(Side side) => !Rules.DynamicFleetCapacity ? Rules.FleetLimit :
        OwnShips(side).Where(s => s.IsMothership).Sum(s => 4 + 2 * (s.Level - 1)) +
        _villages.Where(v => v.Owner == side && v.Health > 0).Sum(v => 2 + v.Level - 1);
    public int FleetSlotCost(ShipClass kind) => Rules.WeightedFleetCapacity ? kind switch
    {
        ShipClass.Garrison or ShipClass.Fishing or ShipClass.PirateSchooner => 1,
        ShipClass.Invader => 2,
        ShipClass.Kolonel => 4,
        ShipClass.Togus => 3,
        _ => 0
    } : kind is not (ShipClass.Balloon or ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)
        && (Rules.Get(kind).Damage > 0 || Rules.DynamicFleetCapacity && kind == ShipClass.Fishing) ? 1 : 0;
    public bool CanFitFleet(Side side, ShipClass kind) => FleetSlotCost(kind) == 0
        || FleetUsed(side) + FleetSlotCost(kind) <= FleetCapacity(side);
    public int ScuttleRefund(Side side, int id) => Find(id) is { } ship && ship.Owner == side && !ship.IsMothership
        ? Ship.Whole((ship.ConstructionPrice ?? BuildPrice(side, ship.Definition.Class)) * Rules.ScuttleRefundFraction) : 0;
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
        int refund = ScuttleRefund(side, id);
        ClearBalloonCrashes();
        QueueBalloonCrash(ship, side);
        RemoveDestroyedShip(ship);
        _credits[(int)side] = checked(_credits[(int)side] + refund);
        RecordCurrencyReceipt(side, refund);
        RecordImpact("scuttle");
        ResolveBalloonCrashes();
        UpdateVision();
        return new(true, refund > 0 ? $"Ship dismantled. Returned {refund} Thors." : "Ship dismantled. No refund.",
            CommandKind.Scuttle, id, Amount: refund, Path: new[] { position })
            { BalloonCrashes = _balloonCrashes.ToArray() };
    }
}
