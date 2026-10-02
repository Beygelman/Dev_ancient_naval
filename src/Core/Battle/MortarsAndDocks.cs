using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public int MortarPrice => Rules.Mortar.PurchasePrice;

    private readonly HashSet<GridPosition> _shoals = new();
    public IReadOnlyCollection<GridPosition> Shoals => _shoals.ToArray();

    public IEnumerable<GridPosition> KnownShoals(Side side) => _shoals.Where(p => Vision.IsVisible(side, p));
    private void InitializeShoals(int seed)
    {
        var water = WorldResourcePlacement.Order(Board, Board.Tiles.Where(t => t.Terrain != TerrainType.Land &&
            At(t.Position) is null && !_fish.Contains(t.Position)).Select(t => t.Position), seed ^ 0x5367);
        foreach (var mother in Ships.Where(s => s.IsMothership))
        {
            var near = water.Where(p => Board.InRadius(mother.Position, p, 2)).ToArray();
            if (near.Length > 0)
                _shoals.Add(near[0]);
        }

        foreach (var position in water)
        {
            if (_shoals.Count >= 6)
                break;
            if (_shoals.All(other => !Board.InRadius(position, other, 2)))
                _shoals.Add(position);
        }
    }

    private void GrantResources(Ship mother, int amount)
    {
        if (mother.Level >= 5)
            return;
        mother.Resources += amount;
        if (mother.Resources < mother.ResourcesRequired)
            return;
        int needed = mother.ResourcesRequired;
        double oldMax = mother.MaxHealth;
        mother.Resources -= needed;
        mother.Level++;
        int reward = Rules.LevelCurrencyRewards[mother.Level - 2];
        _credits[(int)mother.Owner] += reward;
        RecordCurrencyReceipt(mother.Owner, reward);
        mother.Health += mother.MaxHealth - oldMax;
        mother.PendingUpgradeLevel = mother.Level;
        if (mother.Level == 5)
            mother.Resources = 0;
        RegisterShipIncome(mother);
    }

    public string? MortarBlockReason(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return error;
        if (!ship!.IsMothership)
            return "Only a Mothership can equip this mortar.";
        if (ship.Level < 5)
            return "The mortar unlocks at Mothership level 5.";
        if (ship.HasMortar)
            return "A mortar is already installed.";
        if (!ship.HasRadar)
            return "Install radar first.";
        if (Credits(requester) < MortarPrice)
            return $"The mortar costs {MortarPrice} Thors.";
        return null;
    }

    public CommandResult BuyMortar(Side requester, int id)
    {
        var error = MortarBlockReason(requester, id);
        if (error is not null)
            return CommandResult.Rejected(error);
        _credits[(int)requester] -= MortarPrice;
        Find(id)!.HasMortar = true;
        return new(true, "Mortar installed · fires beyond 3 tiles", CommandKind.Upgrade, id);
    }

    public int DockPrice(Side side) => Creative && side == Side.Player ? 0 : Rules.Get(ShipClass.FishingDock).Price;
    public IReadOnlyCollection<GridPosition> DockCells(int id)
    {
        var ship = Find(id);
        if (ship is null || ship.IsExhausted || IsOver || ship.Owner != ActiveSide || ship.Definition.CollectionRange == 0 || Mothership(ship.Owner) is not { Level: >= 2 } || PendingUpgrade(ship.Owner) is not null)
            return Array.Empty<GridPosition>();
        return _shoals.Where(p => Vision.IsVisible(ship.Owner, p) && IsFreeWater(p) && WithinCollectionReach(ship, p)).ToArray();
    }

    public IReadOnlyCollection<GridPosition> DockCells(Side side) => OwnShips(side).Where(s => s.Definition.CollectionRange > 0).SelectMany(s => DockCells(s.Id)).Distinct().ToArray();
    public CommandResult BuildDock(Side requester, GridPosition cell)
    {
        var collector = OwnShips(requester).FirstOrDefault(s => DockCells(s.Id).Contains(cell));
        return collector is null ? CommandResult.Rejected("A collection ship must be within range.") : BuildDock(requester, collector.Id, cell);
    }

    public CommandResult BuildDock(Side requester, int id, GridPosition cell)
    {
        var error = ValidateActor(requester, id, out _);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!DockCells(id).Contains(cell))
            return CommandResult.Rejected("Choose an empty shoal within collection range.");
        if (Credits(requester) < DockPrice(requester))
            return CommandResult.Rejected("Not enough Thors for a dock.");
        var mother = Mothership(requester)!;
        _credits[(int)requester] -= DockPrice(requester);
        _shoals.Remove(cell);
        _fish.Remove(cell);
        var dock = new Ship(_nextId++, requester, Rules.Get(ShipClass.FishingDock), cell);
        _ships.Add(dock);
        RegisterShipIncome(dock);
        GrantResources(mother, Rules.DockResourceReward);
        UpdateVision();
        return new(true, $"Fishing dock: +{Rules.DockResourceReward} resources and +{dock.Definition.IncomePerTurn} income", CommandKind.Dock, id, dock.Id, Rules.DockResourceReward);
    }
}
