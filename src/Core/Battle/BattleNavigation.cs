using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    private bool KnownOccupied(Side side, GridPosition cell) => ObservedShips(side).Any(s => !s.IsAirborne && s.Position == cell) || Vision.State(side, cell) == VisibilityState.RadarContact;
    private NavalNavigationQuery NavigationQuery(Ship ship, bool knowledge = true) => new(Board, ship, Vision, Ships, _forbidden, ship.Owner == Side.Pirates && _pirateHomes.TryGetValue(ship.Id, out var home) ? home : null, knowledge);
    /// <summary>Costs are integer tenths; unknown terrain is estimated as water.
    /// A query is discarded before any command changes the battle or visibility.</summary>
    public int? StepCost(int id, GridPosition from, GridPosition to, bool knowledge = true)
    {
        var ship = Find(id);
        return ship is null ? null : StepCost(ship, from, to, knowledge);
    }

    private int? StepCost(Ship ship, GridPosition from, GridPosition to, bool knowledge) => NavigationQuery(ship, knowledge).StepCost(from, to);
    private (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) Flood(Ship ship, int budget)
    {
        var query = NavigationQuery(ship);
        return PathSearch.Find(ship.Position, budget, Board.GetSurrounding, query.StepCost);
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
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove)
            return MovementPreview.Empty;
        var routes = ship.IsAirborne ? FlightRoutes(ship) : Flood(ship, ship.MovementRemainingUnits + 4);
        if (!ship.IsAirborne)
            foreach (var cell in routes.Costs.Keys.Where(p => p != ship.Position && KnownOccupied(ship.Owner, p)).ToArray())
                routes.Costs.Remove(cell);
        return new(ship.Position, routes.Costs, routes.Previous);
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
        return flood.Costs.ContainsKey(destination) ? Reconstruct(ship.Position, destination, flood.Previous) : Array.Empty<GridPosition>();
    }

    public double PathCost(int id, IReadOnlyList<GridPosition> path)
    {
        var ship = Find(id);
        if (ship is null)
            return 0;
        var query = NavigationQuery(ship);
        int total = 0;
        for (int i = 1; i < path.Count; i++)
            total += query.StepCost(path[i - 1], path[i]) ?? 0;
        return total / 10.0;
    }

    public IReadOnlyList<GridPosition> RouteToward(int id, GridPosition destination, int stopRange = 0)
    {
        var ship = Find(id);
        if (ship is null)
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && Board.InRadius(p.Key, destination, stopRange)).OrderBy(p => p.Value).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => (GridPosition? )p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
    }

    public IReadOnlyList<GridPosition> PathToAttackPosition(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : FindObserved(ship.Owner, targetId);
        if (ship is null || !ship.IsArmed || target is null || target.Owner == ship.Owner || target.IsAirborne && !ship.IsMothership)
            return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
        var goal = flood.Costs.Where(p => (p.Key == ship.Position || !KnownOccupied(ship.Owner, p.Key)) && WeaponCoversFrom(ship, p.Key, target.Position) && (!target.IsAirborne || Board.InRadius(p.Key, target.Position, Rules.Balloon.AntiAirRange))).OrderBy(p => p.Value).Select(p => (GridPosition? )p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
    }

    public GridPosition AffordableDestination(int id, IReadOnlyList<GridPosition> path)
    {
        var ship = Find(id)!;
        var query = NavigationQuery(ship);
        int spent = 0;
        var destination = ship.Position;
        for (int i = 1; i < path.Count; i++)
        {
            int? step = query.StepCost(path[i - 1], path[i]);
            if (step is null || spent + step > ship.MovementRemainingUnits + 4)
                break;
            spent += step.Value;
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
        int budget = ship.MovementRemainingUnits + 4, plannedSpent = 0;
        foreach (var next in planned.Skip(1))
        {
            var step = actualQuery.StepCost(traversable[^1], next);
            if (step is null || plannedSpent + step.Value > budget)
                break;
            traversable.Add(next);
            plannedSpent += step.Value;
        }

        while (traversable.Count > 1 && At(traversable[^1])is not null)
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
            var step = actualQuery.StepCost(ship.Position, next);
            if (step is null || step > ship.MovementRemainingUnits + 4)
                break;
            InvalidateWaiting(ship.Id);
            ship.Position = next;
            ship.MovementSpentUnits += step.Value;
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
