using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public const int CollectionPrice = 2;
    private readonly HashSet<GridPosition> _fish = new();
    public IReadOnlyCollection<GridPosition> FishSpots => _fish.ToArray();

    public IEnumerable<GridPosition> KnownFish(Side side) => _fish.Where(p => Vision.IsVisible(side, p));
    public Ship? Mothership(Side side) => OwnShips(side).FirstOrDefault(s => s.IsMothership);
    public Ship? PendingUpgrade(Side side) => OwnShips(side).FirstOrDefault(s => s.PendingUpgradeLevel > 0);
    private void InitializeFishing(IEnumerable<GridPosition>? supplied, int seed)
    {
        if (supplied is not null)
        {
            foreach (var cell in supplied)
            {
                if (!Board.Contains(cell) || Board.GetTile(cell).Terrain == TerrainType.Land)
                    throw new ArgumentException("Fish must be at sea.");
                _fish.Add(cell);
            }

            InitializeShoals(seed);
            return;
        }

        InitializeShoals(seed);
        var candidates = Board.Tiles.Where(t => t.Terrain != TerrainType.Land && At(t.Position)is null && (!Rules.EmptyOuterRim || !Board.IsOuterCell(t.Position))).Select(t => t.Position).ToList();
        int resourceCount = Math.Max(Rules.Economy.MinimumResourceSpots, Rules.Economy.ResourceTileInterval > 0 ? Board.Tiles.Count / Rules.Economy.ResourceTileInterval : 0);
        resourceCount = Math.Max(1, (int)Math.Round(resourceCount * Rules.Economy.ResourceDensityMultiplier));
        var resources = WorldResourcePlacement.Order(Board, candidates.Where(p => !_shoals.Contains(p)), seed);
        // One guaranteed first catch per fleet; further resources require exploration.
        foreach (var mother in Ships.Where(s => s.IsMothership))
            foreach (var cell in resources.Where(p => Board.InRadius(mother.Position, p, mother.Definition.CollectionRange)).Take(1))
                _fish.Add(cell);
        if (Board.Kind == WorldKind.Pangaea)
        {
            int interiorTarget = resourceCount / 2 + 1;
            foreach (var cell in resources.Where(p => PangaeaWaters.IsInterior(Board, p)))
            {
                if (_fish.Count >= resourceCount || _fish.Count(p => PangaeaWaters.IsInterior(Board, p)) >= interiorTarget) break;
                if (_fish.All(p => !Board.GetNeighbors(p).Contains(cell))) _fish.Add(cell);
            }
        }
        foreach (var cell in resources)
        {
            if (_fish.Count >= Math.Min(resourceCount, resources.Count)) break;
            if (_fish.All(p => !Board.GetNeighbors(p).Contains(cell))) _fish.Add(cell);
        }
    }

    public string? RadarBlockReason(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return error;
        if (ship!.Definition.Class is not (ShipClass.Mothership or ShipClass.Kolonel or ShipClass.CannonTower or ShipClass.Lighthouse))
            return "This class cannot equip radar.";
        if (ship.HasRadar)
            return "Radar is already installed.";
        if (Credits(requester) < ship.Definition.RadarPrice)
            return "Not enough Thors.";
        return null;
    }

    public CommandResult BuyRadar(Side requester, int id)
    {
        var error = RadarBlockReason(requester, id);
        if (error is not null)
            return CommandResult.Rejected(error);
        var ship = Find(id)!;
        _credits[(int)requester] -= ship.Definition.RadarPrice;
        ship.HasRadar = true;
        UpdateVision();
        return new(true, "Radar installed.", CommandKind.Radar, id);
    }

    private bool WithinCollectionReach(Ship ship, GridPosition cell) => Rules.Economy.AdjacentCollectionOnly ? cell == ship.Position || Board.GetSurrounding(ship.Position).Contains(cell) : Board.InRadius(cell, ship.Position, ship.Definition.CollectionRange);
    public IReadOnlyCollection<GridPosition> CollectionCells(int id)
    {
        var ship = Find(id);
        if (ship is null || ship.IsExhausted || ship.HasRepaired || ship.Definition.CollectionRange <= 0 || IsOver || ship.Owner != ActiveSide || PendingUpgrade(ship.Owner)is not null || Mothership(ship.Owner)is not { Level: < 5 })
            return Array.Empty<GridPosition>();
        return _fish.Where(p => Vision.IsVisible(ship.Owner, p) && WithinCollectionReach(ship, p)).ToArray();
    }

    public IReadOnlyCollection<GridPosition> CollectionCells(Side side) => OwnShips(side).Where(s => s.Definition.CollectionRange > 0).SelectMany(s => CollectionCells(s.Id)).Distinct().ToArray();
    public CommandResult Collect(Side requester, GridPosition cell)
    {
        var collector = OwnShips(requester).FirstOrDefault(s => CollectionCells(s.Id).Contains(cell));
        return collector is null ? CommandResult.Rejected("A collection ship must be within range.") : Collect(requester, collector.Id, cell);
    }

    public CommandResult Collect(Side requester, int id, GridPosition cell)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!CollectionCells(id).Contains(cell))
            return CommandResult.Rejected("Fish must be visible and within collection range.");
        if (Credits(requester) < CollectionCost(requester))
            return CommandResult.Rejected($"Collection costs {CollectionCost(requester)} Thors.");
        var mother = Mothership(requester)!;
        _credits[(int)requester] -= CollectionCost(requester);
        _fish.Remove(cell);
        int oldLevel = mother.Level;
        GrantResources(mother, 1);
        bool advanced = mother.Level > oldLevel;
        return new(true, advanced ? $"Mothership: level {mother.Level}!" : $"+1 Mothership resource · −{CollectionCost(requester)} Thors", CommandKind.Collect, id, mother.Id, 1);
    }

    public IReadOnlyList<UpgradeChoice> UpgradeOptions(int motherId) => Rules.Upgrades.FirstOrDefault(u => u.Level == Find(motherId)?.PendingUpgradeLevel)?.Choices ?? Array.Empty<UpgradeChoice>();
    public CommandResult ChooseUpgrade(Side requester, int id, UpgradeChoice choice)
    {
        var mother = Find(id);
        if (PendingPresentation is not null || IsOver || requester != ActiveSide || mother?.Owner != requester || !UpgradeOptions(id).Contains(choice))
            return CommandResult.Rejected("This upgrade is not available now.");
        if (mother.HasRepaired && choice is UpgradeChoice.FishingBoat or UpgradeChoice.Balloon)
            return CommandResult.Rejected("A repaired ship cannot build again this turn.");
        switch (choice)
        {
            case UpgradeChoice.Restoration:
                mother.RestorationUpgrade = true;
                mother.Health += 5;
                break;
            case UpgradeChoice.Vision:
                mother.VisionUpgrade = true;
                break;
            case UpgradeChoice.Firepower:
                mother.FirepowerUpgrade = true;
                break;
            case UpgradeChoice.Shipwright:
                mother.ShipwrightUpgrade = true;
                break;
            case UpgradeChoice.Mobility:
                mother.MobilityUpgrade = true;
                break;
            case UpgradeChoice.SecondAttack:
                mother.SecondAttackUpgrade = true;
                break;
            case UpgradeChoice.FishingBoat:
                if (!CanFitFleet(requester, ShipClass.Fishing))
                    return CommandResult.Rejected($"Fishing expedition requires a free fleet slot. Fleet limit: {FleetCapacity(requester)}.");
                // Occupied adjacent berths do not block delivery to other reachable water.
                var berth = FishingRewardBerth(mother);
                if (berth is null)
                    return CommandResult.Rejected("There is no reachable free water for the fishing boat.");
                var fishing = new Ship(_nextId++, requester, Rules.Get(ShipClass.Fishing), berth.Value)
                {
                    IsExhausted = true
                };
                _ships.Add(fishing);
                RegisterShipIncome(fishing);
                break;
            case UpgradeChoice.Balloon:
                var balloonBerth = BalloonRewardBerth(mother);
                if (balloonBerth is null)
                    return CommandResult.Rejected("No free interior tile remains for the Balloon.");
                _ships.Add(new Ship(_nextId++, requester, Rules.Get(ShipClass.Balloon), balloonBerth.Value));
                break;
        }

        mother.PendingUpgradeLevel = 0;
        UpdateVision();
        return new(true, "Upgrade installed.", CommandKind.Upgrade, id);
    }

    private GridPosition? FishingRewardBerth(Ship mother)
    {
        var scout = new Ship(0, mother.Owner, Rules.Get(ShipClass.Fishing), mother.Position);
        var visited = new HashSet<GridPosition>
        {
            mother.Position
        };
        var pending = new Queue<GridPosition>();
        pending.Enqueue(mother.Position);
        while (pending.TryDequeue(out var cell))
        {
            if (IsFreeWater(cell) && (!Rules.EmptyOuterRim || !Board.IsOuterCell(cell)))
                return cell;
            foreach (var next in Board.GetSurrounding(cell))
                if (!visited.Contains(next) && StepCost(scout, cell, next, false)is not null)
                {
                    visited.Add(next);
                    pending.Enqueue(next);
                }
        }

        return null;
    }

    private GridPosition? BalloonRewardBerth(Ship mother)
    {
        // Old voyages retain delivery directly above the flagship. Surface
        // ships and land are valid for the new airborne berth as well.
        if (!Rules.EmptyOuterRim || !Board.IsOuterCell(mother.Position))
            return mother.Position;
        var occupiedAir = _ships.Where(s => s.IsAirborne).Select(s => s.Position).ToHashSet();
        return Board.Tiles.Where(t => !Board.IsOuterCell(t.Position) && !occupiedAir.Contains(t.Position)
                && !_forbidden.Contains(t.Position))
            .OrderBy(t => Board.Distance(mother.Position, t.Position))
            .ThenBy(t => System.Numerics.Vector2.DistanceSquared(Board.Center(mother.Position), Board.Center(t.Position)))
            .ThenBy(t => t.Position.Y).ThenBy(t => t.Position.X)
            .Select(t => (GridPosition?)t.Position).FirstOrDefault();
    }

    private (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) FlightRoutes(Ship ship)
    {
        var costs = new Dictionary<GridPosition, int>
        {
            {
                ship.Position,
                0
            }
        };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new Queue<GridPosition>();
        queue.Enqueue(ship.Position);
        while (queue.TryDequeue(out var p))
            foreach (var n in Board.GetSurrounding(p))
            {
                int cost = costs[p] + 10;
                if (cost > ship.MovementRemainingUnits || _forbidden.Contains(n) || !costs.TryAdd(n, cost))
                    continue;
                previous[n] = p;
                queue.Enqueue(n);
            }

        return (costs, previous);
    }

    private CommandResult Fly(Ship ship, GridPosition destination)
    {
        var path = PathTo(ship.Id, destination);
        if (path.Count < 2)
            return CommandResult.Rejected("Choose an accessible tile within flight range.");
        var frames = new List<MovementFrame>
        {
            MovementFrame(ship)
        };
        foreach (var cell in path.Skip(1))
        {
            ship.Position = cell;
            ship.MovementSpentUnits += 10;
            UpdateVision();
            frames.Add(MovementFrame(ship));
        }

        ship.HasMoved = true;
        return new(true, "Balloon: flight complete.", CommandKind.Move, ship.Id, Path: path, Movement: frames);
    }
}
