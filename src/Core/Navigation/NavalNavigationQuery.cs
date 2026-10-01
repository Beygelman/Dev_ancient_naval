using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Navigation;
/// <summary>A short-lived navigation snapshot for one synchronous query.
/// Never retain it across commands or changes to visibility. Enemy occupancy
/// and threat zones are indexed once, and terrain costs only when visited.</summary>
internal sealed class NavalNavigationQuery
{
    private readonly GameBoard _board;
    private readonly Ship _ship;
    private readonly BattleVision _vision;
    private readonly bool _knowledge;
    private readonly bool _freeCoast;
    private readonly IReadOnlySet<GridPosition> _forbidden;
    private readonly GridPosition? _patrolHome;
    private readonly HashSet<GridPosition> _blocked = new();
    private readonly HashSet<GridPosition> _threatened = new();
    private readonly Dictionary<GridPosition, Cell> _cells = new();
    private readonly Dictionary<GridPosition, GridPosition[]> _neighbors = new();
    private readonly record struct Cell(bool Passable, bool TerrainPassable, int Cost);
    public NavalNavigationQuery(GameBoard board, Ship ship, BattleVision vision, IEnumerable<Ship> ships, IReadOnlySet<GridPosition> forbidden, GridPosition? patrolHome, bool knowledge, bool freeCoast = false)
    {
        _board = board;
        _ship = ship;
        _vision = vision;
        _forbidden = forbidden;
        _patrolHome = patrolHome;
        _knowledge = knowledge;
        _freeCoast = freeCoast;
        foreach (var other in ships)
        {
            if (other.IsAirborne || other.Owner == ship.Owner || knowledge && !vision.IsVisible(ship.Owner, other.Position))
                continue;
            _blocked.Add(other.Position);
            if (other.IsArmed)
                foreach (var cell in board.GetSurrounding(other.Position))
                    _threatened.Add(cell);
        }
    }

    private TerrainType Terrain(GridPosition cell) => _knowledge ? _vision.KnownTerrain(_ship.Owner, cell) ?? TerrainType.Water : _board.GetTile(cell).Terrain;
    private Cell GetCell(GridPosition position)
    {
        if (_cells.TryGetValue(position, out var existing))
            return existing;
        bool terrainPassable = _board.Contains(position) && !_forbidden.Contains(position) && Terrain(position) != TerrainType.Land && (_patrolHome is not { } home || _board.InRadius(home, position, 5));
        bool narrow = terrainPassable && _board.IsNarrowAt(position, p => _board.Contains(p) && Terrain(p) == TerrainType.Land);
        if (!_freeCoast && _ship.IsMothership && narrow)
            terrainPassable = false;
        bool passable = terrainPassable && !_blocked.Contains(position) && (!_knowledge || _vision.State(_ship.Owner, position) != VisibilityState.RadarContact);
        double multiplier = _freeCoast || !terrainPassable ? 1 : narrow ? _ship.Definition.NarrowMovementCost : Terrain(position) == TerrainType.Coast ? _ship.Definition.CoastMovementCost : 1;
        int cost = Ship.Whole(10 * Math.Max(multiplier, _threatened.Contains(position) ? 2 : 1));
        var cell = new Cell(passable, terrainPassable, cost);
        _cells.Add(position, cell);
        return cell;
    }

    private GridPosition[] EdgeNeighbors(GridPosition position)
    {
        if (_neighbors.TryGetValue(position, out var neighbors))
            return neighbors;
        neighbors = _board.GetNeighbors(position).ToArray();
        _neighbors.Add(position, neighbors);
        return neighbors;
    }

    public int? StepCost(GridPosition from, GridPosition to)
    {
        if (from == to || !_board.GetSurrounding(from).Contains(to))
            return null;
        var destination = GetCell(to);
        if (!destination.Passable)
            return null;
        if (_board.Mesh is not null)
        {
            var fromEdges = EdgeNeighbors(from);
            if (!fromEdges.Contains(to))
            {
                var toEdges = EdgeNeighbors(to);
                foreach (var corner in fromEdges)
                    if (toEdges.Contains(corner) && !GetCell(corner).TerrainPassable)
                        return null;
            }
        }
        else if (from.X != to.X && from.Y != to.Y && (!GetCell(new(from.X, to.Y)).TerrainPassable || !GetCell(new(to.X, from.Y)).TerrainPassable))
        {
            return null;
        }

        return destination.Cost;
    }
}
