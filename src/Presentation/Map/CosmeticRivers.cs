using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal sealed record CosmeticRiverInfo(int IslandCells, string EndKind, float MinimumWidth, float MaximumWidth,
    Vector2[] Centerline, GridPosition[] Cells);

public partial class BoardView
{
    private IsometricProjection? _riverProjection;
    private readonly Dictionary<GridPosition, List<RiverPaint>> _riverPaint = new();
    private readonly List<CosmeticRiverInfo> _riverInfo = new();
    private sealed record RiverPaint(Vector2[] Shape, Color Color, bool Water);
    internal int CosmeticRiverCount { get { EnsureCosmeticRivers(); return _riverInfo.Count; } }
    internal IReadOnlyList<CosmeticRiverInfo> CosmeticRiverDiagnostics { get { EnsureCosmeticRivers(); return _riverInfo; } }
    internal IEnumerable<(GridPosition Cell, Vector2[] Shape)> CosmeticRiverWaterShapes
    {
        get
        {
            EnsureCosmeticRivers();
            return _riverPaint.SelectMany(p => p.Value.Where(v => v.Water).Select(v => (p.Key, v.Shape)));
        }
    }

    // These are illustrations over unchanged land, not new navigation cells.
    // Their independent seed is never drawn from BattleState's event generator.
    private void EnsureCosmeticRivers()
    {
        EnsureIslandGeometry();
        if (ReferenceEquals(_riverProjection, Projection)) return;
        _riverProjection = Projection;
        _riverPaint.Clear();
        _riverInfo.Clear();
        var random = new Random(Board.Seed ^ 0x36F12);
        var remaining = Board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToHashSet();
        var features = TerrainFeatures.For(Board);
        var soil = _landShapes.SelectMany(pair => pair.Value.Select(shape => (Cell: pair.Key, Shape: shape,
            Bounds: PolygonBounds(shape)))).ToArray();
        var sand = _beaches.Where(b => b.Polygon.Length > 0).Select(b => (b.Polygon, Bounds: PolygonBounds(b.Polygon))).ToArray();
        var towns = Battle.Villages.Select(town => TownEnvelope.Select(p =>
            Projection.GridToWorld(town.Position) + VillagePlacement(town).Point(p)).ToArray())
            .Select(p => (Shape: p, Bounds: PolygonBounds(p))).ToArray();
        while (remaining.Count > 0)
        {
            var island = new List<GridPosition>();
            var queue = new Queue<GridPosition>();
            queue.Enqueue(remaining.OrderBy(p => p.Y).ThenBy(p => p.X).First());
            while (queue.TryDequeue(out var cell))
            {
                if (!remaining.Remove(cell)) continue;
                island.Add(cell);
                foreach (var next in Board.GetNeighbors(cell)) if (remaining.Contains(next)) queue.Enqueue(next);
            }
            if (island.Count < 8) continue;
            var landSet = island.ToHashSet();
            var coasts = island.Where(p => Board.GetNeighbors(p).Any(n => !landSet.Contains(n))).ToArray();
            if (coasts.Length == 0) continue;
            int riverCount = island.Count >= 300 ? 3 : island.Count >= 90 ? 2 : 1;
            var usedMouths = new List<GridPosition>();
            for (int river = 0; river < riverCount; river++)
            {
                var mouthCandidates = coasts.Where(p => usedMouths.All(old => Board.Distance(old, p) > 4)).ToArray();
                if (mouthCandidates.Length == 0) mouthCandidates = coasts;
                var start = mouthCandidates[random.Next(mouthCandidates.Length)];
                usedMouths.Add(start);
                int kind = random.Next(3);
                var peaks = island.Where(features.MountainCells.Contains).ToArray();
                if (kind == 2 && peaks.Length == 0) kind = 0;
                var candidates = kind == 1 ? coasts : kind == 2 ? peaks : island.ToArray();
                var ranked = candidates.Where(p => p != start).OrderByDescending(p =>
                    Board.Distance(start, p) + (kind == 0 ? features.DistanceFromCoast(p) * 2 : 0)).ToArray();
                if (ranked.Length == 0) continue;
                // Different rivers use different fractions of their island's
                // extent, producing broad lakes, peak springs and two mouths.
                var end = ranked[random.Next(Math.Max(1, Math.Min(ranked.Length, kind == 1 ? 3 : 7)))];
                var route = IslandRoute(start, end, landSet, random);
                if (route.Length < 2) continue;
                var knots = route.Select(p => UnprojectRiver(Projection.GridToWorld(p))).ToList();
                knots.Insert(0, UnprojectRiver(MouthPoint(start)));
                if (kind == 1) knots.Add(UnprojectRiver(MouthPoint(end)));
                var curve = RiverSpline(knots);
                float phase = (float)random.NextDouble() * Mathf.Tau;
                var widths = Enumerable.Range(0, curve.Length).Select(i =>
                {
                    float t = i / (float)(curve.Length - 1);
                    float width = .065f + .012f * (1 + MathF.Sin(t * 8 + phase)) + .008f * MathF.Sin(t * 17 + phase) * MathF.Sin(t * 17 + phase);
                    if (kind == 0) width += MathF.Pow(Math.Max(0, (t - .68f) / .32f), 2) * .23f;
                    if (t < .14f || kind == 1 && t > .86f) width += .045f;
                    return width;
                }).ToArray();
                var cells = new HashSet<GridPosition>();
                for (int i = 1; i < curve.Length; i++)
                {
                    bool atMouth = i < 8 || kind == 1 && i >= curve.Length - 8;
                    var aNormal = RiverNormal(curve, i - 1);
                    var bNormal = RiverNormal(curve, i);
                    Paint(Ribbon(curve[i - 1], curve[i], aNormal, bNormal, widths[i - 1] + .035f, widths[i] + .035f),
                        new Color("759875"), false, atMouth, cells);
                    Paint(Ribbon(curve[i - 1], curve[i], aNormal, bNormal, widths[i - 1], widths[i]),
                        new Color(atMouth ? "30596b" : "467a85"), true, atMouth, cells);
                    // A deeper narrow middle gives the broad river a readable
                    // current without introducing sea-colored scars outside it.
                    Paint(Ribbon(curve[i - 1], curve[i], aNormal, bNormal, widths[i - 1] * .63f, widths[i] * .63f),
                        new Color(atMouth ? "30596b" : "3b6c79"), true, atMouth, cells);
                }
                if (kind == 0)
                {
                    float radius = widths[^1] * .58f;
                    Paint(Enumerable.Range(0, 32).Select(i => ProjectRiver(curve[^1] +
                        Vector2.FromAngle(i * Mathf.Tau / 32) * radius * (1 + .1f * MathF.Sin(i * .7f + phase)))).ToArray(),
                        new Color("467a85"), true, false, cells);
                }
                if (random.NextDouble() < .55)
                {
                    // Two narrow distributaries reconnect with the broad
                    // trunk. Their sand bars remain ordinary cosmetic ground.
                    int join = Math.Min(curve.Length - 1, 10);
                    var cross = RiverNormal(curve, 1);
                    foreach (int side in new[] { -1, 1 })
                    {
                        var delta = RiverSpline(new[] { curve[0] + cross * side * .65f,
                            curve[3] + cross * side * .4f, curve[join] });
                        for (int i = 1; i < delta.Length; i++)
                            Paint(Ribbon(delta[i - 1], delta[i], RiverNormal(delta, i - 1), RiverNormal(delta, i), .045f, .045f),
                                new Color("30596b"), true, true, cells);
                    }
                }
                // White broken cascades and small bars are visual rock/sand;
                // they do not occupy cells, impede sailing or block sight.
                for (int rapid = 0; rapid < 2 + random.Next(3); rapid++)
                {
                    int index = random.Next(5, Math.Max(6, curve.Length - 5));
                    var normal = RiverNormal(curve, index);
                    var at = curve[index];
                    for (int dash = -2; dash <= 2; dash++)
                    {
                        var p = ProjectRiver(at + normal * dash * .18f);
                        Paint(new[] { p + new Vector2(-3, -1), p + new Vector2(3, -1), p + new Vector2(2, 1) },
                            new Color("c1dad0"), false, false, cells);
                    }
                    if (rapid % 2 == 0)
                    {
                        var bar = at + normal * widths[index] * .3f;
                        Paint(Enumerable.Range(0, 12).Select(i => ProjectRiver(bar + new Vector2(MathF.Cos(i * Mathf.Tau / 12) * .23f,
                            MathF.Sin(i * Mathf.Tau / 12) * .11f))).ToArray(), new Color("afa985"), false, false, cells);
                    }
                }
                if (cells.Count > 0)
                    _riverInfo.Add(new(island.Count, kind == 0 ? "Lake" : kind == 1 ? "Through" : "Peak",
                        widths.Min(), widths.Max(), curve.Select(ProjectRiver).ToArray(), cells.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray()));
            }
        }

        Vector2 MouthPoint(GridPosition coast)
        {
            var origin = Projection.GridToWorld(coast);
            var segment = _beaches.OrderBy(b => ((b.Edge[0] + b.Edge[1]) * .5f).DistanceSquaredTo(origin)).FirstOrDefault();
            return segment is null ? origin : (segment.Edge[0] + segment.Edge[1]) * .5f;
        }
        void Paint(Vector2[] ribbon, Color color, bool water, bool mouth, HashSet<GridPosition> cells)
        {
            if (Geometry2D.TriangulatePolygon(ribbon).Length < 3) return;
            var bounds = PolygonBounds(ribbon);
            foreach (var ground in soil)
            {
                if (!ground.Bounds.Intersects(bounds)) continue;
                // Half-plane subtraction requires convex subjects. Partition
                // concave intersections before cutting out beach/city footprints.
                var pieces = Geometry2D.IntersectPolygons(ribbon, ground.Shape).Where(p => p.Length >= 3)
                    .SelectMany(p =>
                    {
                        var indices = Geometry2D.TriangulatePolygon(p);
                        return Enumerable.Range(0,indices.Length / 3).Select(i => new[] {p[indices[i*3]],p[indices[i*3+1]],p[indices[i*3+2]]});
                    }).ToList();
                // Keep banks off beaches except inside the actual mouth. The
                // outer island contour always clips even those mouth stamps.
                if (!mouth)
                    foreach (var beach in sand.Where(b => b.Bounds.Intersects(bounds)))
                        pieces = pieces.SelectMany(p => ConvexSoilClip.Subtract(p, beach.Polygon, .25f)).ToList();
                foreach (var town in towns.Where(t => t.Bounds.Intersects(bounds)))
                    pieces = pieces.SelectMany(p => ConvexSoilClip.Subtract(p, town.Shape, .4f)).ToList();
                foreach (var shape in pieces.Where(p => p.Length >= 3 && PolygonArea(p) > .1f && Geometry2D.TriangulatePolygon(p).Length >= 3))
                {
                    if (!_riverPaint.TryGetValue(ground.Cell, out var paints)) _riverPaint[ground.Cell] = paints = new();
                    paints.Add(new(shape, color, water));
                    cells.Add(ground.Cell);
                }
            }
        }
    }

    private GridPosition[] IslandRoute(GridPosition start, GridPosition end, HashSet<GridPosition> island, Random random)
    {
        var previous = new Dictionary<GridPosition, GridPosition> { [start] = start };
        var pending = new Queue<GridPosition>();
        pending.Enqueue(start);
        while (pending.TryDequeue(out var p) && !previous.ContainsKey(end))
            foreach (var next in Board.GetNeighbors(p).Where(island.Contains).OrderBy(_ => random.Next()))
                if (previous.TryAdd(next, p)) pending.Enqueue(next);
        if (!previous.ContainsKey(end)) return Array.Empty<GridPosition>();
        var route = new List<GridPosition> { end };
        while (route[^1] != start) route.Add(previous[route[^1]]);
        route.Reverse();
        return route.ToArray();
    }
    private Vector2 ProjectRiver(Vector2 p) => new((p.X - p.Y) * Projection.TileWidth / 2, (p.X + p.Y) * Projection.TileHeight / 2);
    private Vector2 UnprojectRiver(Vector2 p) => new(p.X / Projection.TileWidth + p.Y / Projection.TileHeight,
        p.Y / Projection.TileHeight - p.X / Projection.TileWidth);
    private static Vector2[] RiverSpline(IReadOnlyList<Vector2> knots)
    {
        var result = new List<Vector2>();
        for (int i = 0; i < knots.Count - 1; i++)
        {
            var a = knots[Math.Max(0, i - 1)]; var b = knots[i];
            var c = knots[i + 1]; var d = knots[Math.Min(knots.Count - 1, i + 2)];
            for (int step = 0; step < 8; step++)
            {
                float t = step / 8f;
                result.Add(.5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t +
                    (-a + 3 * b - 3 * c + d) * t * t * t));
            }
        }
        result.Add(knots[^1]);
        return result.ToArray();
    }
    private static Vector2 RiverNormal(Vector2[] points, int index)
    {
        var axis = points[Math.Min(points.Length - 1, index + 1)] - points[Math.Max(0, index - 1)];
        return axis.LengthSquared() < .00001f ? Vector2.Right : axis.Normalized().Orthogonal();
    }
    private Vector2[] Ribbon(Vector2 a, Vector2 b, Vector2 normalA, Vector2 normalB, float widthA, float widthB) => new[]
    {
        ProjectRiver(a - normalA * widthA * .5f), ProjectRiver(b - normalB * widthB * .5f),
        ProjectRiver(b + normalB * widthB * .5f), ProjectRiver(a + normalA * widthA * .5f)
    };
    private bool CosmeticRiverAt(Vector2 point)
    {
        var cell = Projection.WorldToGrid(point);
        return _riverPaint.TryGetValue(cell, out var paints) && paints.Any(p => p.Water && Geometry2D.IsPointInPolygon(point, p.Shape));
    }
    private void DrawCosmeticRivers(Node2D canvas, ISet<GridPosition> cells)
    {
        EnsureCosmeticRivers();
        foreach (var cell in cells)
            if (_riverPaint.TryGetValue(cell, out var paints))
                foreach (var paint in paints) canvas.DrawColoredPolygon(paint.Shape, paint.Color);
    }
}
