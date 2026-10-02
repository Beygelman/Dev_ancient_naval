using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public IReadOnlyList<GridPosition> SpawnCells(int mothershipId)
    {
        var ship = Find(mothershipId);
        return ship is null || ship.Definition.Class != ShipClass.Mothership ? Array.Empty<GridPosition>() : Board.GetNeighbors(ship.Position).Where(IsFreeWater).ToArray();
    }

    public static int RequiredLevel(ShipClass kind) => kind switch
    {
        ShipClass.FishingDock or ShipClass.CannonTower => 2,
        ShipClass.Invader => 3,
        ShipClass.Kolonel => 4,
        ShipClass.Togus => 5,
        _ => 1
    };
    public string? BuildBlockReason(Side requester, int mothershipId, ShipClass shipClass)
    {
        var error = ValidateActor(requester, mothershipId, out var mother);
        if (error is not null)
            return error;
        if (!Enum.IsDefined(shipClass) || shipClass is ShipClass.Mothership or ShipClass.Balloon or ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.PirateSchooner)
            return "This class cannot be built.";
        if (shipClass == ShipClass.Lighthouse && !Rules.LighthousesEnabled)
            return "Lighthouse construction is unavailable in this voyage.";
        if (mother!.Definition.Class != ShipClass.Mothership)
            return "Select a Mothership to build ships.";
        if (mother.Level < RequiredLevel(shipClass))
            return $"Available at Mothership level {RequiredLevel(shipClass)}.";
        if (mother.HasProduced)
            return "This Mothership has already built a ship this turn.";
        if (shipClass != ShipClass.CannonTower && Rules.Get(shipClass).Damage > 0 && _ships.Count(s => s.Owner == requester && s.CountsTowardFleet) >= Rules.FleetLimit)
            return $"Fleet limit: {Rules.FleetLimit}.";
        if (Credits(requester) < BuildPrice(requester, shipClass))
            return "Not enough Thors.";
        if (SpawnCells(mothershipId).Count == 0)
            return "No adjacent water tile is free.";
        return null;
    }

    public CommandResult Build(Side requester, int mothershipId, ShipClass shipClass, GridPosition spawn)
    {
        var error = BuildBlockReason(requester, mothershipId, shipClass);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!SpawnCells(mothershipId).Contains(spawn))
            return CommandResult.Rejected("Choose a free water tile beside the Mothership.");
        var definition = Rules.Get(shipClass);
        var ship = new Ship(_nextId++, requester, definition, spawn);
        bool first = !_everProduced[(int)requester];
        ship.IsExhausted = !first;
        _ships.Add(ship);
        RecordShipConstruction(ship);
        RegisterShipIncome(ship);
        _credits[(int)requester] -= BuildPrice(requester, shipClass);
        _everProduced[(int)requester] = true;
        var mother = Find(mothershipId)!;
        mother.HasProduced = true;
        mother.MovementLocked = true;
        UpdateVision();
        return new(true, $"{definition.Name} built. " + (first ? "Ready for action now." : "Ready next turn."), CommandKind.Build, mothershipId, ship.Id);
    }
}
