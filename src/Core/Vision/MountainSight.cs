using System.Numerics;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Vision;

/// <summary>Physical ray occlusion on the irregular cell mesh. Address X/Y is
/// never a geometric coordinate on an organic board. Immutable board coverage
/// is retained across fog recomputations, without caching observed enemies.</summary>
internal sealed class MountainSight
{
    private readonly GameBoard _board;
    private readonly Dictionary<(GridPosition, int, bool), GridPosition[]> _coverage = new();
    private readonly Dictionary<GridPosition, IReadOnlyList<Vector2>> _polygons = new();
    private (GridPosition Cell, Vector2 Low, Vector2 High)[]? _blockerBounds;

    internal MountainSight(GameBoard board) => _board = board;

    internal IReadOnlyList<GridPosition> Coverage(GridPosition origin, int radius, bool square)
    {
        var key = (origin, radius, square);
        if (_coverage.TryGetValue(key, out var cached))
            return cached;
        var candidates = _board.Mesh is { } mesh ? mesh.Within(origin, radius)
            : _board.Tiles.Where(t => square
                ? Math.Abs(t.Position.X - origin.X) <= radius && Math.Abs(t.Position.Y - origin.Y) <= radius
                : BattleVision.InRadius(origin, t.Position, radius)).Select(t => t.Position).ToArray();
        Vector2 from = _board.Center(origin);
        // Mesh graph distance and physical ray distance differ near distorted
        // cells. Include every mountain touching the physical coverage bounds,
        // even when its address is outside the graph-radius candidate set.
        _blockerBounds ??= TerrainFeatures.For(_board).MountainCells.Select(cell =>
        {
            var polygon = Polygon(cell);
            var low = polygon.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
            var high = polygon.Aggregate(new Vector2(float.MinValue), Vector2.Max);
            return (cell, low, high);
        }).ToArray();
        var lowBound = from;
        var highBound = from;
        foreach (var cell in candidates)
        {
            var center = _board.Center(cell);
            lowBound = Vector2.Min(lowBound, center);
            highBound = Vector2.Max(highBound, center);
        }
        var blockers = _blockerBounds.Where(b => b.Cell != origin
            && b.High.X >= lowBound.X && b.Low.X <= highBound.X
            && b.High.Y >= lowBound.Y && b.Low.Y <= highBound.Y).Select(b => b.Cell).ToArray();
        var visible = candidates.Where(target => blockers.All(blocker => blocker == target
            || !Intersects(from, _board.Center(target), Polygon(blocker)))).ToArray();
        // Bound moving-source caches on very long voyages; rebuilding is safe.
        if (_coverage.Count >= 2048)
            _coverage.Clear();
        _coverage[key] = visible;
        return visible;
    }

    private IReadOnlyList<Vector2> Polygon(GridPosition cell)
    {
        if (_polygons.TryGetValue(cell, out var retained))
            return retained;
        if (_board.Mesh is { } mesh)
            return _polygons[cell] = mesh.Faces[cell].Select(id => mesh.Vertices[id]).ToArray();
        var p = _board.Center(cell);
        return _polygons[cell] = new[] { p + new Vector2(-.5f,-.5f), p + new Vector2(.5f,-.5f),
            p + new Vector2(.5f,.5f), p + new Vector2(-.5f,.5f) };
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static bool Intersects(Vector2 from, Vector2 to, IReadOnlyList<Vector2> polygon)
    {
        Vector2 ray = to - from;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 corner = polygon[i], edge = polygon[(i + 1) % polygon.Count] - corner;
            float denominator = Cross(ray, edge);
            if (Math.Abs(denominator) < .00001f)
                continue;
            float distance = Cross(corner - from, edge) / denominator;
            float along = Cross(corner - from, ray) / denominator;
            if (distance > .0001f && distance < .9999f && along >= -.0001f && along <= 1.0001f)
                return true;
        }
        return false;
    }
}
