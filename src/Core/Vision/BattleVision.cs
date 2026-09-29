using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Vision;

public enum VisibilityState { Unknown, Explored, RadarContact, Visible }

/// <summary>Terrain memory, current optical sight and radar coverage are separate.
/// Radar reports only occupied enemy coordinates; it never discovers terrain.</summary>
public sealed class BattleVision
{
    private readonly GameBoard _board;
    private readonly HashSet<GridPosition>[] _explored = { new(), new() };
    private readonly HashSet<GridPosition>[] _visible = { new(), new() };
    private readonly HashSet<GridPosition>[] _radar = { new(), new() };
    private readonly HashSet<GridPosition>[] _contacts = { new(), new() };
    private readonly HashSet<GridPosition>[] _flashes = { new(), new() };
    private readonly Dictionary<GridPosition, int>[] _lastSeen = { new(), new() };
    public BattleVision(GameBoard board) => _board = board;

    public static bool InRadius(GridPosition a, GridPosition b, int radius)
    {
        long dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy <= (long)radius * (radius + 1);
    }
    public bool IsVisible(Side side, GridPosition cell) => _visible[(int)side].Contains(cell);
    public bool IsExplored(Side side, GridPosition cell) => _explored[(int)side].Contains(cell);
    public int ExploredCount(Side side) => _explored[(int)side].Count;
    public int LastSeen(Side side, GridPosition cell) => _lastSeen[(int)side].GetValueOrDefault(cell, -1);
    public TerrainType? KnownTerrain(Side side, GridPosition cell) => IsExplored(side, cell) ? _board.GetTile(cell).Terrain : null;
    public IReadOnlyCollection<GridPosition> Contacts(Side side) => _contacts[(int)side].ToArray();
    public VisibilityState State(Side side, GridPosition cell) => IsVisible(side, cell) ? VisibilityState.Visible :
        _contacts[(int)side].Contains(cell) ? VisibilityState.RadarContact : IsExplored(side, cell) ? VisibilityState.Explored : VisibilityState.Unknown;
    public void RevealCombat(Side side, GridPosition cell) => _flashes[(int)side].Add(cell);
    public void ClearCombatFlashes() { foreach (var flashes in _flashes) flashes.Clear(); }

    public void Recompute(IReadOnlyList<Ship> ships, int stamp)
    {
        foreach (var side in Enum.GetValues<Side>())
        {
            int index = (int)side;
            _visible[index].Clear(); _radar[index].Clear(); _contacts[index].Clear();
            foreach (var ship in ships.Where(s => s.Owner == side))
            {
                FillCircle(_visible[index], ship.Position, ship.VisualRange);
                if (ship.RadarRange > 0) FillCircle(_radar[index], ship.Position, ship.RadarRange);
            }
            _visible[index].UnionWith(_flashes[index]);
            _explored[index].UnionWith(_visible[index]);
            foreach (var cell in _visible[index]) _lastSeen[index][cell] = stamp;
            foreach (var enemy in ships.Where(s => s.Owner != side && !s.IsAirborne))
                if (_radar[index].Contains(enemy.Position) && !_visible[index].Contains(enemy.Position))
                    _contacts[index].Add(enemy.Position);
        }
    }

    private void FillCircle(HashSet<GridPosition> target, GridPosition origin, int radius)
    {
        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(_board.Height - 1, origin.Y + radius); y++)
        for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(_board.Width - 1, origin.X + radius); x++)
            if (InRadius(origin, new(x, y), radius)) target.Add(new(x, y));
    }
}
