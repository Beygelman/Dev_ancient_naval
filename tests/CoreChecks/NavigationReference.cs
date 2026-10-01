using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;

// Frozen 0.14 traversal oracle. Deliberately independent of the optimized policy:
// tests compare fog semantics, corner blocking, tie order and full route costs.
internal sealed class NavigationReference(BattleState battle, GridPosition? home = null)
{
    private readonly BattleState _battle = battle;
    private readonly GridPosition? _home = home;
    private GameBoard Board => _battle.Board;
    private BattleVision Vision => _battle.Vision;
    private IReadOnlyList<Ship> Ships => _battle.Ships;

    private IEnumerable<Ship> ObservedShips(Side side) => _battle.ObservedShips(side);
    private Ship? ObservedAt(Side side, GridPosition cell) => _battle.ObservedAt(side, cell);
    private Ship? At(GridPosition cell) => _battle.At(cell);
    private Ship? Find(int id) => _battle.Find(id);
    private TerrainType Terrain(Ship ship, GridPosition cell, bool knowledge) => knowledge ? Vision.KnownTerrain(ship.Owner, cell) ?? TerrainType.Water : Board.GetTile(cell).Terrain;
    private bool Narrow(Ship ship, GridPosition cell, bool knowledge) => Board.IsNarrowAt(cell, p => Board.Contains(p) && Terrain(ship, p, knowledge) == TerrainType.Land);
    private bool Passable(Ship ship, GridPosition cell, bool knowledge, bool ignoreShips = false)
    {
        if (!Board.Contains(cell) || Terrain(ship, cell, knowledge) == TerrainType.Land || _battle.IsForbidden(cell))
            return false;
        if (!ignoreShips && knowledge && Vision.State(ship.Owner, cell) == VisibilityState.RadarContact)
            return false;
        if (ship.Owner == Side.Pirates && _home is { } home && !Board.InRadius(home, cell, 5))
            return false;
        var occupant = knowledge ? ObservedAt(ship.Owner, cell) : At(cell);
        if (!ignoreShips && occupant is { IsAirborne: false } && occupant.Owner != ship.Owner)
            return false;
        if (ship.Definition.Class == ShipClass.Mothership && Narrow(ship, cell, knowledge))
            return false;
        return true; // Friendly ships allow transit; all occupied destinations remain unavailable.
    }

    /// <summary>Integer tenths: orthogonal and diagonal both cost 10. Unknown cells are
    /// estimated as open water, so route previews cannot reveal terrain or hidden ships.
    /// A four-tenth tolerance rounds the total route down to the nearest whole move point.</summary>
    public int? StepCost(int id, GridPosition from, GridPosition to, bool knowledge = true)
    {
        var ship = Find(id);
        return ship is null ? null : StepCost(ship, from, to, knowledge);
    }

    public int? StepCost(Ship ship, GridPosition from, GridPosition to, bool knowledge)
    {
        if (from == to || !Board.GetSurrounding(from).Contains(to) || !Passable(ship, to, knowledge))
            return null;
        if (Board.Mesh is not null && !Board.GetNeighbors(from).Contains(to) && Board.GetNeighbors(from).Intersect(Board.GetNeighbors(to)).Any(p => !Passable(ship, p, knowledge, ignoreShips: true)))
            return null;
        bool diagonal = Board.Mesh is null && from.X != to.X && from.Y != to.Y;
        if (diagonal && (!Passable(ship, new(from.X, to.Y), knowledge, ignoreShips: true) || !Passable(ship, new(to.X, from.Y), knowledge, ignoreShips: true)))
            return null;
        double multiplier = Narrow(ship, to, knowledge) ? ship.Definition.NarrowMovementCost : Terrain(ship, to, knowledge) == TerrainType.Coast ? ship.Definition.CoastMovementCost : 1;
        bool threatened = (knowledge ? ObservedShips(ship.Owner) : Ships).Any(enemy => enemy.Owner != ship.Owner && enemy.IsArmed && !enemy.IsAirborne && Board.GetSurrounding(enemy.Position).Contains(to));
        return Ship.Whole(10 * Math.Max(multiplier, threatened ? 2 : 1));
    }

    public (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) Flood(Ship ship, int budget)
    {
        var costs = new Dictionary<GridPosition, int>
        {
            [ship.Position] = 0
        };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new PriorityQueue<GridPosition, (int, int)>();
        int order = 0;
        queue.Enqueue(ship.Position, (0, order++));
        while (queue.TryDequeue(out var current, out var priority))
        {
            if (priority.Item1 != costs[current])
                continue;
            foreach (var next in Board.GetSurrounding(current))
            {
                var step = StepCost(ship, current, next, true);
                if (step is null)
                    continue;
                int cost = costs[current] + step.Value;
                if (cost > budget || (costs.TryGetValue(next, out int old) && old <= cost))
                    continue;
                costs[next] = cost;
                previous[next] = current;
                queue.Enqueue(next, (cost, order++));
            }
        }

        return (costs, previous);
    }
}
