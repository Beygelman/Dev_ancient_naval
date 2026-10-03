using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
<<<<<<< Updated upstream
    private TradeNavigation? TradePolicy(Ship ship)
    {
        if (ship.IsAirborne || ship.Owner == Side.Pirates) return null;
        var network = TradeRoutes(ship.Owner);
        return network.IsEmpty ? null : new TradeNavigation(network, ship, Rules.Ports);
    }
    private bool KnownOccupied(Side side, GridPosition cell) => ObservedShips(side).Any(s => !s.IsAirborne && s.Position == cell) || Vision.State(side, cell) == VisibilityState.RadarContact;
    private NavalNavigationQuery NavigationQuery(Ship ship, bool knowledge = true) => new(Board, ship, Vision, Ships, _forbidden, ship.Owner == Side.Pirates && _pirateHomes.TryGetValue(ship.Id, out var home) ? home : null, knowledge, Rules.FreeCoastalNavigation);
=======
    private bool KnownOccupied(Side side, GridPosition cell) => ObservedShips(side).Any(s => !s.IsAirborne && s.Position == cell) || Vision.State(side, cell) == VisibilityState.RadarContact;
    private NavalNavigationQuery NavigationQuery(Ship ship, bool knowledge = true) => new(Board, ship, Vision, Ships, _forbidden, ship.Owner == Side.Pirates && _pirateHomes.TryGetValue(ship.Id, out var home) ? home : null, knowledge);
>>>>>>> Stashed changes
    /// <summary>Costs are integer tenths; unknown terrain is estimated as water.
    /// A query is discarded before any command changes the battle or visibility.</summary>
    public int? StepCost(int id, GridPosition from, GridPosition to, bool knowledge = true)
    {
        var ship = Find(id);
        return ship is null ? null : StepCost(ship, from, to, knowledge);
    }

    private int? StepCost(Ship ship, GridPosition from, GridPosition to, bool knowledge) => NavigationQuery(ship, knowledge).StepCost(from, to);
<<<<<<< Updated upstream
    private NavigationRoutes Flood(Ship ship, int budget)
    {
        var query = NavigationQuery(ship);
        var trade = TradePolicy(ship);
        if (trade is not null) return trade.Find(Board, ship, budget, query);
        var origin = ship.Position;
        var result = PathSearch.Find(origin, budget, Board.GetSurrounding, query.StepCost);
        return new(result.Costs, cell => Reconstruct(origin, cell, result.Previous));
=======
    private (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) Flood(Ship ship, int budget)
    {
        var query = NavigationQuery(ship);
        return PathSearch.Find(ship.Position, budget, Board.GetSurrounding, query.StepCost);
>>>>>>> Stashed changes
    }

    private static IReadOnlyList<GridPosition> Reconstruct(GridPosition from, GridPosition to, IReadOnlyDictionary<GridPosition, GridPosition> previous) => PathSearch.Reconstruct(from, to, previous);
    public IReadOnlyDictionary<GridPosition, int> Reachable(int id)
    {
        var ship = Find(id);
        return ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove ? new Dictionary<GridPosition, int>() : ship.IsAirborne ? FlightRoutes(ship).Costs : Flood(ship, ship.MovementRemainingUnits + 4).Costs.Where(p => p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)).ToDictionary(p => p.Key, p => p.Value);
    }

    public MovementPreview PreviewMovement(int id)
    {
        var ship = Find(id);
<<<<<<< Updated upstream
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove)
            return MovementPreview.Empty;
        if (ship.IsAirborne)
        {
            var flight = FlightRoutes(ship);
            return new(ship.Position, flight.Costs, flight.Previous);
        }
        var routes = Flood(ship, ship.MovementRemainingUnits + 4);
        if (!ship.IsAirborne)
            foreach (var cell in routes.Costs.Keys.Where(p => p != ship.Position && KnownOccupied(ship.Owner, p)).ToArray())
                routes.Costs.Remove(cell);
        return new(routes);
=======
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove) return MovementPreview.Empty;
        var routes = ship.IsAirborne ? FlightRoutes(ship) : Flood(ship, ship.MovementRemainingUnits + 4);
        if (!ship.IsAirborne)
            foreach (var cell in routes.Costs.Keys.Where(p => p != ship.Position && KnownOccupied(ship.Owner, p)).ToArray())
                routes.Costs.Remove(cell);
        return new(ship.Position, routes.Costs, routes.Previous);
>>>>>>> Stashed changes
    }

    public IReadOnlyList<GridPosition> PathTo(int id, GridPosition destination)
    {
        var ship = Find(id);
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove)
            return Array.Empty<GridPosition>();
        if (ship.IsAirborne)
        {
            var routes = FlightRoutes(ship);
            return routes.Costs.ContainsKey(destination) ? Reconstruct(ship.Position, destination, routes.Previous) : Array.Empty<GridPosition>();
        }

        if (destination != ship.Position && KnownOccupied(ship.Owner, destination))
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, ship.MovementRemainingUnits + 4);
<<<<<<< Updated upstream
        return flood.PathTo(destination);
=======
        return flood.Costs.ContainsKey(destination) ? Reconstruct(ship.Position, destination, flood.Previous) : Array.Empty<GridPosition>();
>>>>>>> Stashed changes
    }

    public double PathCost(int id, IReadOnlyList<GridPosition> path)
    {
        var ship = Find(id);
        if (ship is null)
            return 0;
        var query = NavigationQuery(ship);
        int total = 0;
<<<<<<< Updated upstream
        var trade = TradePolicy(ship);
        int run = ship.TradeStreak;
        for (int i = 1; i < path.Count; i++)
        {
            if (query.StepCost(path[i - 1], path[i]) is not { } raw) return double.PositiveInfinity;
            var step = trade?.Step(path[i - 1], path[i], raw, run) ?? (raw, 0);
            total += step.Item1;
            run = step.Item2;
        }
=======
        for (int i = 1; i < path.Count; i++)
            total += query.StepCost(path[i - 1], path[i]) ?? 0;
>>>>>>> Stashed changes
        return total / 10.0;
    }

    public IReadOnlyList<GridPosition> RouteToward(int id, GridPosition destination, int stopRange = 0)
    {
        var ship = Find(id);
        if (ship is null)
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
<<<<<<< Updated upstream
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && Board.InRadius(p.Key, destination, stopRange)).OrderBy(p => p.Value).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => (GridPosition? )p.Key).FirstOrDefault();
        return goal is { } cell ? flood.PathTo(cell) : Array.Empty<GridPosition>();
=======
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && Board.InRadius(p.Key, destination, stopRange)).OrderBy(p => p.Value).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => (GridPosition?)p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
>>>>>>> Stashed changes
    }

    public IReadOnlyList<GridPosition> PathToAttackPosition(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : FindObserved(ship.Owner, targetId);
<<<<<<< Updated upstream
        if (ship is null || !ship.IsArmed || target is null || target.Owner == ship.Owner || target.IsAirborne && !HasAntiAir(ship))
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && WeaponCoversFrom(ship, p.Key, target.Position) && (!target.IsAirborne || AntiAirCovers(ship, p.Key, target.Position))).OrderBy(p => p.Value).Select(p => (GridPosition? )p.Key).FirstOrDefault();
        return goal is { } cell ? flood.PathTo(cell) : Array.Empty<GridPosition>();
=======
        if (ship is null || !ship.IsArmed || target is null || target.Owner == ship.Owner)
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && WeaponCoversFrom(ship, p.Key, target.Position)).OrderBy(p => p.Value).Select(p => (GridPosition?)p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
>>>>>>> Stashed changes
    }

    public GridPosition AffordableDestination(int id, IReadOnlyList<GridPosition> path)
    {
        var ship = Find(id)!;
        var query = NavigationQuery(ship);
<<<<<<< Updated upstream
        int spent = 0, run = ship.TradeStreak;
        var trade = TradePolicy(ship);
        var destination = ship.Position;
        for (int i = 1; i < path.Count; i++)
        {
            var raw = query.StepCost(path[i - 1], path[i]);
            var discounted = raw is { } value ? trade?.Step(path[i - 1], path[i], value, run) ?? (value, 0) : ((int, int)?)null;
            int? step = discounted?.Item1;
            if (step is null || spent + step > ship.MovementRemainingUnits + 4)
                break;
            spent += step.Value;
            run = discounted!.Value.Item2;
=======
        int spent = 0;
        var destination = ship.Position;
        for (int i = 1; i < path.Count; i++)
        {
            int? step = query.StepCost(path[i - 1], path[i]);
            if (step is null || spent + step > ship.MovementRemainingUnits + 4)
                break;
            spent += step.Value;
>>>>>>> Stashed changes
            if (!KnownOccupied(ship.Owner, path[i]))
                destination = path[i];
        }

        return destination;
    }

    public CommandResult Move(Side requester, int id, GridPosition destination)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (ship!.IsAirborne)
            return Fly(ship, destination);
        var planned = PathTo(id, destination);
        if (planned.Count < 2)
            return CommandResult.Rejected("No route is available, or no movement remains.");
        // Determine the last free landing cell before revealing/animating the route. This
        // prevents a newly discovered coast or obstacle from leaving ships stacked together.
        var traversable = new List<GridPosition>
        {
            ship!.Position
        };
        // Actual movement uses full terrain knowledge; visibility changes during animation
        // cannot invalidate this command-local terrain/enemy snapshot.
        var actualQuery = NavigationQuery(ship, knowledge: false);
<<<<<<< Updated upstream
        int budget = ship.MovementRemainingUnits + 4, plannedSpent = 0, plannedRun = ship.TradeStreak;
        var trade = TradePolicy(ship);
        foreach (var next in planned.Skip(1))
        {
            var raw = actualQuery.StepCost(traversable[^1], next);
            var discounted = raw is { } value ? trade?.Step(traversable[^1], next, value, plannedRun) ?? (value, 0) : ((int, int)?)null;
            int? step = discounted?.Item1;
=======
        int budget = ship.MovementRemainingUnits + 4, plannedSpent = 0;
        foreach (var next in planned.Skip(1))
        {
            var step = actualQuery.StepCost(traversable[^1], next);
>>>>>>> Stashed changes
            if (step is null || plannedSpent + step.Value > budget)
                break;
            traversable.Add(next);
            plannedSpent += step.Value;
<<<<<<< Updated upstream
            plannedRun = discounted!.Value.Item2;
        }

        while (traversable.Count > 1 && At(traversable[^1])is not null)
=======
        }

        while (traversable.Count > 1 && At(traversable[^1]) is not null)
>>>>>>> Stashed changes
            traversable.RemoveAt(traversable.Count - 1);
        var actual = new List<GridPosition>
        {
            ship.Position
        };
        var frames = new List<MovementFrame>
        {
            MovementFrame(ship)
        };
        foreach (var next in traversable.Skip(1))
        {
<<<<<<< Updated upstream
            var raw = actualQuery.StepCost(ship.Position, next);
            var discounted = raw is { } value ? trade?.Step(ship.Position, next, value, ship.TradeStreak) ?? (value, 0) : ((int, int)?)null;
            int? step = discounted?.Item1;
=======
            var step = actualQuery.StepCost(ship.Position, next);
>>>>>>> Stashed changes
            if (step is null || step > ship.MovementRemainingUnits + 4)
                break;
            InvalidateWaiting(ship.Id);
            ship.Position = next;
            ship.MovementSpentUnits += step.Value;
<<<<<<< Updated upstream
            ship.TradeStreak = discounted!.Value.Item2;
=======
>>>>>>> Stashed changes
            ship.HasMoved = true;
            actual.Add(next);
            UpdateVision();
            frames.Add(MovementFrame(ship));
        }

        if (actual.Count < 2)
            return CommandResult.Rejected("Passage blocked. Choose another route.");
        return new(true, $"{ship.Definition.Name}: movement complete." + (ship.Position != destination ? " Stopped at a newly discovered obstacle." : ""), CommandKind.Move, id, Path: actual, Movement: frames);
    }

    private MovementFrame MovementFrame(Ship ship) => new(ship.Position, ship.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position), Vision.State(Side.Player, ship.Position) == VisibilityState.RadarContact);
}
