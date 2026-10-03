using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Battle;

namespace DevAncientNaval.Core.Vision;
public enum VisibilityState
{
    Unknown,
    Explored,
    RadarContact,
    Visible
}

/// <summary>Terrain memory, current optical sight and radar coverage are separate.
/// Radar reports only occupied enemy coordinates; it never discovers terrain.</summary>
public sealed partial class BattleVision
{
    private readonly GameBoard _board;
    private readonly HashSet<GridPosition>[] _explored = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new HashSet<GridPosition>()).ToArray();
    private readonly HashSet<GridPosition>[] _visible = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new HashSet<GridPosition>()).ToArray();
    private readonly HashSet<GridPosition>[] _radar = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new HashSet<GridPosition>()).ToArray();
    private readonly HashSet<GridPosition>[] _contacts = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new HashSet<GridPosition>()).ToArray();
    private readonly HashSet<GridPosition>[] _flashes = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new HashSet<GridPosition>()).ToArray();
    private readonly Dictionary<GridPosition, int>[] _lastSeen = Enumerable.Range(0, BattleState.SideSlots).Select(_ => new Dictionary<GridPosition, int>()).ToArray();
<<<<<<< Updated upstream
    private readonly MountainSight? _mountainSight;
    private readonly bool _smallHullRadarStealth;
    private readonly bool _fishingRadarVisible;
    public BattleVision(GameBoard board, bool mountainSightShadows = false, bool smallHullRadarStealth = false, bool fishingRadarVisible = false)
    {
        _board = board;
        _mountainSight = mountainSightShadows ? new MountainSight(board) : null;
        _smallHullRadarStealth = smallHullRadarStealth;
        _fishingRadarVisible = fishingRadarVisible;
    }
    public long Revision { get; private set; }
    private bool _allSeeingPlayer;
    public void SetAllSeeingPlayer(bool enabled)
    {
        if (_allSeeingPlayer == enabled) return;
        _allSeeingPlayer = enabled;
        Revision++;
    }
    private bool AllSeeing(Side side, GridPosition cell) => side == Side.Player && _allSeeingPlayer && _board.Contains(cell);
=======
    public BattleVision(GameBoard board) => _board = board;
    public long Revision { get; private set; }
>>>>>>> Stashed changes

    public static bool InRadius(GridPosition a, GridPosition b, int radius)
    {
        long dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy <= (long)radius * (radius + 1);
    }

    public bool IsVisible(Side side, GridPosition cell) => AllSeeing(side, cell) || _visible[(int)side].Contains(cell);
    public bool IsOpticallyVisible(Side side, GridPosition cell) => _visible[(int)side].Contains(cell);
    public bool IsExplored(Side side, GridPosition cell) => AllSeeing(side, cell) || _explored[(int)side].Contains(cell);
    public int ExploredCount(Side side) => side == Side.Player && _allSeeingPlayer ? _board.Tiles.Count : _explored[(int)side].Count;
    public int LastSeen(Side side, GridPosition cell) => _lastSeen[(int)side].GetValueOrDefault(cell, -1);
    public TerrainType? KnownTerrain(Side side, GridPosition cell) => IsExplored(side, cell) ? _board.GetTile(cell).Terrain : null;
    public IReadOnlyCollection<GridPosition> Contacts(Side side) => side == Side.Player && _allSeeingPlayer ? Array.Empty<GridPosition>() : _contacts[(int)side].ToArray();
    public bool IsRadarContact(Side side, GridPosition cell) => !IsVisible(side, cell) && _contacts[(int)side].Contains(cell);
    public VisibilityState State(Side side, GridPosition cell) => IsVisible(side, cell) ? VisibilityState.Visible : _contacts[(int)side].Contains(cell) ? VisibilityState.RadarContact : IsExplored(side, cell) ? VisibilityState.Explored : VisibilityState.Unknown;
    public void RevealCombat(Side side, GridPosition cell) => _flashes[(int)side].Add(cell);
<<<<<<< Updated upstream
    public void ClearCombatFlashes()
    {
        foreach (var flashes in _flashes)
            flashes.Clear();
    }

    public void Recompute(IReadOnlyList<Ship> ships, int stamp, IEnumerable<Village>? villages = null)
    {
=======
    public void ClearCombatFlashes() { foreach (var flashes in _flashes) flashes.Clear(); }

    public void Recompute(IReadOnlyList<Ship> ships, int stamp, IEnumerable<Village>? villages = null)
    {
>>>>>>> Stashed changes
        Revision++;
        foreach (var side in Enum.GetValues<Side>())
        {
            int index = (int)side;
            _visible[index].Clear();
            _radar[index].Clear();
            _contacts[index].Clear();
            foreach (var ship in ships.Where(s => s.Owner == side))
            {
<<<<<<< Updated upstream
                if (_mountainSight is not null && !ship.IsAirborne)
                    _visible[index].UnionWith(_mountainSight.Coverage(ship.Position, ship.VisualRange, ship.Definition.Class == ShipClass.Fishing));
                else if (ship.Definition.Class == ShipClass.Fishing)
                    FillSquare(_visible[index], ship.Position, ship.VisualRange);
                else
                    FillCircle(_visible[index], ship.Position, ship.VisualRange);
                if (ship.RadarRange > 0)
                    FillCircle(_radar[index], ship.Position, ship.RadarRange);
            }

            if (villages is not null)
                foreach (var village in villages.Where(v => v.Owner == side))
                    if (_mountainSight is not null)
                        _visible[index].UnionWith(_mountainSight.Coverage(village.Position, village.VisualRange, false));
                    else
                        FillCircle(_visible[index], village.Position, village.VisualRange);
=======
                if (ship.Definition.Class == ShipClass.Fishing)
                    FillSquare(_visible[index], ship.Position, ship.VisualRange);
                else FillCircle(_visible[index], ship.Position, ship.VisualRange);
                if (ship.RadarRange > 0) FillCircle(_radar[index], ship.Position, ship.RadarRange);
            }
            if (villages is not null)
                foreach (var village in villages.Where(v => v.Owner == side)) FillCircle(_visible[index], village.Position, village.VisualRange);
>>>>>>> Stashed changes
            _visible[index].UnionWith(_flashes[index]);
            _explored[index].UnionWith(_visible[index]);
            foreach (var cell in _visible[index])
                _lastSeen[index][cell] = stamp;
            foreach (var enemy in ships.Where(s => s.Owner != side && !s.IsAirborne
                && (!_smallHullRadarStealth || s.Definition.Class != ShipClass.Garrison && (s.Definition.Class != ShipClass.Fishing || _fishingRadarVisible))))
                if (_radar[index].Contains(enemy.Position) && !_visible[index].Contains(enemy.Position))
                    _contacts[index].Add(enemy.Position);
        }
    }

    private void FillSquare(HashSet<GridPosition> target, GridPosition origin, int radius)
    {
<<<<<<< Updated upstream
        if (_board.Mesh is not null)
        {
            FillCircle(target, origin, radius);
            return;
        }

        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(_board.Height - 1, origin.Y + radius); y++)
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(_board.Width - 1, origin.X + radius); x++)
                if (_board.Contains(new(x, y)))
                    target.Add(new(x, y));
=======
        if (_board.Mesh is not null) { FillCircle(target, origin, radius); return; }
        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(_board.Height - 1, origin.Y + radius); y++)
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(_board.Width - 1, origin.X + radius); x++)
                if (_board.Contains(new(x, y))) target.Add(new(x, y));
>>>>>>> Stashed changes
    }

    private void FillCircle(HashSet<GridPosition> target, GridPosition origin, int radius)
    {
        if (_board.Mesh is not null)
        {
<<<<<<< Updated upstream
            target.UnionWith(_board.Mesh.Within(origin, radius));
            return;
        }

        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(_board.Height - 1, origin.Y + radius); y++)
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(_board.Width - 1, origin.X + radius); x++)
                if (_board.Contains(new(x, y)) && InRadius(origin, new(x, y), radius))
                    target.Add(new(x, y));
=======
            foreach (var tile in _board.Tiles) if (_board.InRadius(origin, tile.Position, radius)) target.Add(tile.Position);
            return;
        }
        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(_board.Height - 1, origin.Y + radius); y++)
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(_board.Width - 1, origin.X + radius); x++)
                if (_board.Contains(new(x, y)) && InRadius(origin, new(x, y), radius)) target.Add(new(x, y));
>>>>>>> Stashed changes
    }
}
