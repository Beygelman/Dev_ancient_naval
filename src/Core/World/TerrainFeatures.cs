using System.Numerics;
using System.Runtime.CompilerServices;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>Immutable, world-seeded scenery and sight obstacles shared by Core and rendering.</summary>
public sealed class TerrainFeatures
{
    private static readonly ConditionalWeakTable<GameBoard, TerrainFeatures> Cache = new();
    private readonly Dictionary<GridPosition, int> _depth = new();
    private readonly Dictionary<GridPosition, float> _forest = new();
    private readonly Dictionary<GridPosition, IReadOnlyList<Vector2>> _footprints = new();
    private readonly HashSet<GridPosition> _mountains = new();
    private readonly float _forestPhase;

    public static TerrainFeatures For(GameBoard board) => Cache.GetValue(board, b => new TerrainFeatures(b));
    public IReadOnlySet<GridPosition> MountainCells => _mountains;
    /// <summary>Land shoreline is zero; adjacent shallow water is one. Deeper cells have larger values.</summary>
    public int DistanceFromCoast(GridPosition cell) => _depth.GetValueOrDefault(cell);
    public float InlandDepth(GridPosition cell) => Math.Clamp(DistanceFromCoast(cell) / 8f, 0, 1);
    public float ForestDensity(GridPosition cell) => _forest.GetValueOrDefault(cell);
    /// <summary>An inset ground footprint in unprojected board coordinates; mountain height may rise above it.</summary>
    public IReadOnlyList<Vector2> Footprint(GridPosition cell) => _footprints.GetValueOrDefault(cell) ?? Array.Empty<Vector2>();

    private TerrainFeatures(GameBoard board)
    {
        _forestPhase = (board.Seed & 4095) * .0143f;
        BuildDepth(board);
        var remaining = board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToHashSet();
        while (remaining.Count > 0)
        {
            var component = new List<GridPosition>();
            var pending = new Queue<GridPosition>();
            pending.Enqueue(remaining.OrderBy(p => p.Y).ThenBy(p => p.X).First());
            while (pending.TryDequeue(out var cell))
            {
                if (!remaining.Remove(cell)) continue;
                component.Add(cell);
                foreach (var next in board.GetNeighbors(cell))
                    if (remaining.Contains(next)) pending.Enqueue(next);
            }
            BuildRange(board, component);
        }

        foreach (var tile in board.Tiles.Where(t => t.Terrain == TerrainType.Land))
        {
            var center = board.Center(tile.Position);
            _forest[tile.Position] = _mountains.Contains(tile.Position) ? 0 : ForestDensityAt(center);
            var points = board.Mesh is { } mesh ? mesh.Faces[tile.Position].Select(v => mesh.Vertices[v]) :
                new[] { center + new Vector2(-.5f, -.5f), center + new Vector2(.5f, -.5f),
                    center + new Vector2(.5f, .5f), center + new Vector2(-.5f, .5f) };
            _footprints[tile.Position] = Array.AsReadOnly(points.Select(p => Vector2.Lerp(center, p, .66f)).ToArray());
        }
    }

    /// <summary>Continuous grove field; samples are not snapped to tile centers.</summary>
    public float ForestDensityAt(Vector2 location)
    {
        float field = MathF.Sin(location.X * .42f + _forestPhase) + MathF.Cos(location.Y * .39f - _forestPhase * .7f)
            + .45f * MathF.Sin((location.X + location.Y) * .21f + _forestPhase * 1.3f);
        return field < .1f ? 0 : Math.Clamp((field - .1f) * .52f, .18f, .92f);
    }

    private void BuildDepth(GameBoard board)
    {
        var pending = new Queue<GridPosition>();
        foreach (var tile in board.Tiles)
        {
            bool land = tile.Terrain == TerrainType.Land;
            int edges = board.Mesh is { } mesh ? mesh.Faces[tile.Position].Count : 4;
            if (board.GetSurrounding(tile.Position).Any(p => (board.GetTile(p).Terrain == TerrainType.Land) != land) ||
                land && board.GetNeighbors(tile.Position).Count() < edges)
            {
                _depth[tile.Position] = land ? 0 : 1;
                pending.Enqueue(tile.Position);
            }
        }
        while (pending.TryDequeue(out var cell))
        {
            bool land = board.GetTile(cell).Terrain == TerrainType.Land;
            foreach (var next in board.GetNeighbors(cell))
                if ((board.GetTile(next).Terrain == TerrainType.Land) == land && !_depth.ContainsKey(next))
                {
                    _depth[next] = _depth[cell] + 1;
                    pending.Enqueue(next);
                }
        }
    }

    private void BuildRange(GameBoard board, List<GridPosition> island)
    {
        var interior = island.Where(p => board.GetSurrounding(p).All(n =>
            board.GetTile(n).Terrain == TerrainType.Land) && board.GetNeighbors(p).Count() ==
            (board.Mesh is { } mesh ? mesh.Faces[p].Count : 4)).ToArray();
        if (interior.Length == 0) return;
        var center = island.Aggregate(Vector2.Zero, (sum, p) => sum + board.Center(p)) / island.Count;
        double xx = 0, yy = 0, xy = 0;
        foreach (var p in island)
        {
            var d = board.Center(p) - center;
            xx += d.X * d.X;
            yy += d.Y * d.Y;
            xy += d.X * d.Y;
        }
        // Follow the long axis of each island; screen-row/address axes play no role.
        double angle = .5 * Math.Atan2(2 * xy, xx - yy);
        var along = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        var across = new Vector2(-along.Y, along.X);
        float phase = (board.Seed ^ (island[0].X * 997 + island[0].Y * 313)) * .001f;
        double Score(GridPosition p)
        {
            var d = board.Center(p) - center;
            float u = Vector2.Dot(d, along), v = Vector2.Dot(d, across);
            return Math.Abs(v - .7f * MathF.Sin(u * .45f + phase)) - Math.Min(3, _depth[p]) * .10;
        }
        int count = Math.Max(1, (int)Math.Ceiling(interior.Length * .28));
        foreach (var cell in interior.OrderBy(Score).ThenBy(p => p.Y).ThenBy(p => p.X).Take(count))
            _mountains.Add(cell);
    }
}
