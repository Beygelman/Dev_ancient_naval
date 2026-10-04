using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal sealed record CosmeticRiverInfo(int IslandCells, string EndKind, float MinimumWidth, float MaximumWidth,
    Vector2[] Centerline, GridPosition[] Cells, int Network = 0, int? JoinedNetwork = null);

public partial class BoardView
{
    private IsometricProjection? _riverProjection;
    private readonly Dictionary<GridPosition, List<RiverPaint>> _riverPaint = new();
    private readonly List<CosmeticRiverInfo> _riverInfo = new();
    private sealed record RiverPaint(Vector2[] Shape, Color Color, bool Water, bool Detail = false);
    private Vector2[][] _riverObstacles = Array.Empty<Vector2[]>();
    internal IEnumerable<Vector2[]> CosmeticRiverExclusionShapes => _riverObstacles;
    internal int CosmeticRiverBuildCount { get; private set; }
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
        if (!ReferenceEquals(_sceneryProjection, Projection))
        {
            BuildScenery();
            return;
        }
        if (ReferenceEquals(_riverProjection, Projection)) return;
        _riverProjection = Projection;
        CosmeticRiverBuildCount++;
        _riverPaint.Clear();
        _riverInfo.Clear();
        _riverDeltas.Clear();
        _riverDeltaPaint.Clear();
        _riverMouthShoreCache.Clear();
        var random = new Random(Board.Seed ^ 0x36F12);
        var remaining = Board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToHashSet();
        var features = TerrainFeatures.For(Board);
        var soil = _landShapes.SelectMany(pair => pair.Value.Select(shape => (Cell: pair.Key, Shape: shape,
            Bounds: PolygonBounds(shape)))).ToArray();
        var sand = _beaches.Where(b => b.Polygon.Length > 0).Select(b => (b.Polygon, Bounds: PolygonBounds(b.Polygon))).ToArray();
        var townShapes = Battle.Villages.Select(town => TownEnvelope.Select(p =>
            Projection.GridToWorld(town.Position) + VillagePlacement(town).Point(p)).ToArray())
            .ToArray();
        var mountainShapes = _scenery.Where(p => p.Kind == 2).Select(peak =>
            Geometry2D.ConvexHull(MountainFootprint(peak, peak.Point).Select(p =>
                p + (p - peak.Point).Normalized() * 3).ToArray()).SkipLast(1).ToArray()).ToArray();
        _riverObstacles = townShapes.Concat(mountainShapes).ToArray();
        var obstacles = _riverObstacles.Select(p => (Shape: p, Bounds: PolygonBounds(p))).ToArray();
        var stagedPaint = new Dictionary<GridPosition, List<RiverPaint>>();
        bool ClearPoint(Vector2 point) => !obstacles.Any(o => o.Bounds.Grow(2).HasPoint(point) &&
            (Geometry2D.IsPointInPolygon(point, o.Shape) || Enumerable.Range(0, o.Shape.Length)
                .Any(i => RiverPointSegmentDistance(point, o.Shape[i], o.Shape[(i + 1) % o.Shape.Length]) < 2)));
        var centers = Board.Tiles.ToDictionary(t => t.Position, t => UnprojectRiver(Projection.GridToWorld(t.Position)));
        var clearEdges = new Dictionary<(GridPosition, GridPosition), bool>();
        bool ClearEdge(GridPosition a, GridPosition b)
        {
            if (clearEdges.TryGetValue((a, b), out bool clear)) return clear;
            var from = Projection.GridToWorld(a);
            var to = Projection.GridToWorld(b);
            clear = ClearPoint(from) && ClearPoint(to) && !obstacles.Any(o =>
                o.Bounds.Intersects(PolygonBounds(new[] { from, to }).Grow(2)) && Enumerable.Range(0, o.Shape.Length)
                    .Any(i => RiverSegmentsDistance(from, to, o.Shape[i], o.Shape[(i + 1) % o.Shape.Length]) < 2));
            clearEdges[(a, b)] = clearEdges[(b, a)] = clear;
            return clear;
        }
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
            var fullLand = island.ToHashSet();
            var landSet = island.Where(p => ClearPoint(Projection.GridToWorld(p))).ToHashSet();
            var coasts = island.Where(p => landSet.Contains(p) && Board.GetNeighbors(p).Any(n => !fullLand.Contains(n))).ToArray();
            if (coasts.Length == 0) continue;
            int riverCount = Math.Clamp(1 + island.Count / 70, 1, 6);
            var usedMouths = new List<GridPosition>();
            var networks = new List<(Vector2[] Curve, int Id)>();
            int accepted = 0;
            for (int attempt = 0; attempt < riverCount * 16 && accepted < riverCount; attempt++)
            {
                var mouthCandidates = coasts.Where(p => usedMouths.All(old => Board.Distance(old, p) > 2)).ToArray();
                if (mouthCandidates.Length == 0) break;
                var start = mouthCandidates[random.Next(mouthCandidates.Length)];
                int kind = networks.Count > 0 && random.NextDouble() < .3 ? 3 : random.Next(3);
                // Springs begin beside an actual mountain skirt, never below it.
                var peaks = landSet.Where(p => Board.GetNeighbors(p).Any(features.MountainCells.Contains)).ToArray();
                if (kind == 2 && peaks.Length == 0) kind = 0;
                var candidates = kind == 1 ? coasts : kind == 2 ? peaks : landSet.ToArray();
                var ranked = candidates.Where(p => p != start && Board.Distance(start, p) >= 2).OrderByDescending(p =>
                    Board.Distance(start, p) + (kind == 0 ? features.DistanceFromCoast(p) * 2 : 0)).ToArray();
                if (ranked.Length == 0) continue;
                // Different rivers use different fractions of their island's
                // extent, producing broad lakes, peak springs and two mouths.
                var end = ranked[random.Next(Math.Max(1, Math.Min(ranked.Length, kind == 1 ? 3 : 7)))];
                Vector2? join = null;
                Vector2 joinTangent = Vector2.Zero;
                int? joined = null;
                if (kind == 3)
                {
                    var network = networks[random.Next(networks.Count)];
                    int index = random.Next(Math.Max(1, network.Curve.Length / 4), Math.Max(2, network.Curve.Length * 3 / 4));
                    join = network.Curve[index];
                    joinTangent = (network.Curve[Math.Min(network.Curve.Length - 1, index + 1)] -
                        network.Curve[Math.Max(0, index - 1)]).Normalized();
                    joined = network.Id;
                    end = landSet.MinBy(p => centers[p].DistanceSquaredTo(join.Value));
                    if (Board.Distance(start, end) < 2) continue;
                }
                bool AvoidNetwork(GridPosition a, GridPosition b) => networks.All(n =>
                    n.Curve.Zip(n.Curve.Skip(1)).All(segment =>
                        join is { } merge && n.Id == joined && centers[b].DistanceTo(merge) < .7f ||
                        RiverSegmentsDistance(centers[a], centers[b], segment.First, segment.Second) > .4f));
                float routePhase = (float)random.NextDouble() * Mathf.Tau;
                var route = IslandRoute(start, end, landSet, centers, (a, b) => ClearEdge(a, b) && AvoidNetwork(a, b), routePhase,
                    joined is null ? ranked : Array.Empty<GridPosition>(), out var actualEnd);
                if (route.Length < 2) continue;
                end = actualEnd;
                var knots = route.Select(p => centers[p]).ToList();
                knots.Insert(0, UnprojectRiver(MouthPoint(start)));
                if (kind == 1) knots.Add(UnprojectRiver(MouthPoint(end)));
                if (join is { } joint)
                {
                    float side = Math.Sign((knots[^2] - joint).Dot(joinTangent.Orthogonal()));
                    knots[^1] = joint;
                    knots.Insert(knots.Count - 1, joint + joinTangent * .22f + joinTangent.Orthogonal() * side * .08f);
                }
                Vector2[] curve = Array.Empty<Vector2>();
                foreach (float amplitude in new[] { .34f, .25f, .16f, 0f })
                {
                    var bent = knots.Select((p, i) => i == 0 || i == knots.Count - 1 ? p : p +
                        (knots[Math.Min(i + 1, knots.Count - 1)] - knots[Math.Max(0, i - 1)]).Normalized().Orthogonal() *
                        (MathF.Sin(i * 1.08f + routePhase) * amplitude)).ToArray();
                    var candidate = RiverMeanders(bent, amplitude, routePhase);
                    bool clear = candidate.Skip(2).All(p => ClearPoint(ProjectRiver(p))) &&
                        candidate.Skip(5).Take(Math.Max(0, candidate.Length - 10)).All(p =>
                        {
                            var point = ProjectRiver(p);
                            return _landShapes.GetValueOrDefault(Projection.WorldToGrid(point))?.Any(shape =>
                                Geometry2D.IsPointInPolygon(point, shape)) == true;
                        });
                    if (clear && RiverNetworkClear(candidate, networks, join, joined)) { curve = candidate; break; }
                }
                if (curve.Length == 0) continue;
                float phase = (float)random.NextDouble() * Mathf.Tau;
                var widths = Enumerable.Range(0, curve.Length).Select(i =>
                {
                    float t = i / (float)(curve.Length - 1);
                    float width = .065f + .012f * (1 + MathF.Sin(t * 8 + phase)) + .008f * MathF.Sin(t * 17 + phase) * MathF.Sin(t * 17 + phase);
                    if (kind == 0) width += MathF.Pow(Math.Max(0, (t - .68f) / .32f), 2) * .23f;
                    if (t < .14f || kind == 1 && t > .86f) width += .045f;
                    return width;
                }).ToArray();
                stagedPaint.Clear();
                var cells = new HashSet<GridPosition>();
                for (int i = 1; i < curve.Length; i++)
                {
                    bool atMouth = i < 8 || kind == 1 && i >= curve.Length - 8;
                    var aNormal = RiverNormal(curve, i - 1);
                    var bNormal = RiverNormal(curve, i);
                    Paint(Ribbon(curve[i - 1], curve[i], aNormal, bNormal, widths[i - 1] + .035f, widths[i] + .035f),
                        new Color("759875"), false, atMouth, cells);
                    // Different sun-facing and undercut sides make the bends
                    // readable without widening these narrow cosmetic streams.
                    float bend = Math.Clamp(MathF.Abs(aNormal.Cross(bNormal)) * 2.5f, 0, 1);
                    Paint(Ribbon(curve[i - 1] + aNormal * widths[i - 1] * .53f,
                        curve[i] + bNormal * widths[i] * .53f, aNormal, bNormal, .016f + bend * .016f, .016f + bend * .016f),
                        new Color("b5bd91"), false, atMouth, cells);
                    Paint(Ribbon(curve[i - 1] - aNormal * widths[i - 1] * .53f,
                        curve[i] - bNormal * widths[i] * .53f, aNormal, bNormal, .014f + bend * .013f, .014f + bend * .013f),
                        new Color("4c7968"), false, atMouth, cells);
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
                // White broken cascades and small bars are visual rock/sand;
                // they do not occupy cells, impede sailing or block sight.
                for (int rapid = 0; rapid < 2 + random.Next(3); rapid++)
                {
                    int index = random.Next(5, Math.Max(6, curve.Length - 5));
                    var normal = RiverNormal(curve, index);
                    var at = curve[index];
                    for (int dash = -2; dash <= 2; dash++)
                    {
                        var p = ProjectRiver(at + normal * dash * widths[index] * .14f);
                        Paint(new[] { p + new Vector2(-1.2f, -.5f), p + new Vector2(1.2f, -.5f), p + new Vector2(.8f, .5f) },
                            new Color("c1dad0"), false, false, cells, true);
                    }
                    if (rapid % 2 == 0)
                    {
                        var bar = at + normal * widths[index] * .3f;
                        Paint(Enumerable.Range(0, 12).Select(i => ProjectRiver(bar + new Vector2(MathF.Cos(i * Mathf.Tau / 12) * widths[index] * .18f,
                            MathF.Sin(i * Mathf.Tau / 12) * widths[index] * .09f))).ToArray(), new Color("afa985"), false, false, cells, true);
                    }
                }
                if (cells.Count >= 2)
                {
                    foreach (var (cell, paint) in stagedPaint)
                    {
                        if (!_riverPaint.TryGetValue(cell, out var stored)) _riverPaint[cell] = stored = new();
                        stored.AddRange(paint);
                    }
                    int id = _riverInfo.Count;
                    _riverInfo.Add(new(island.Count, kind == 0 ? "Lake" : kind == 1 ? "Through" : kind == 2 ? "Peak" : "Confluence",
                        widths.Min(), widths.Max(), curve.Select(ProjectRiver).ToArray(), cells.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray(), id, joined));
                    networks.Add((curve, id));
                    usedMouths.Add(start);
                    BuildRiverDelta(curve, widths, false);
                    if (kind == 1) BuildRiverDelta(curve, widths, true);
                    accepted++;
                }
            }
        }

        // Shore cuts depend on the static estuary plan, not observation.
        // Warm every run now so discovering a coast never pays polygon-union
        // edge splitting in the command / camera interaction frame.
        using (DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Board.Estuary.Shore.Plan"))
            foreach (var shore in _shoreLines) RiverMouthShoreEdges(shore.Edge);

        Vector2 MouthPoint(GridPosition coast)
        {
            var origin = Projection.GridToWorld(coast);
            var segment = _beaches.OrderBy(b => ((b.Edge[0] + b.Edge[1]) * .5f).DistanceSquaredTo(origin)).FirstOrDefault();
            return segment is null ? origin : (segment.Edge[0] + segment.Edge[1]) * .5f;
        }
        void Paint(Vector2[] ribbon, Color color, bool water, bool mouth, HashSet<GridPosition> cells, bool detail = false)
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
                // The estuary owns the beach opening. Ordinary bank ribbons
                // stop at its inland edge instead of punching square cuts.
                foreach (var beach in sand.Where(b => b.Bounds.Intersects(bounds)))
                        pieces = pieces.SelectMany(p => ConvexSoilClip.Subtract(p, beach.Polygon, .25f)).ToList();
                foreach (var obstacle in obstacles.Where(t => t.Bounds.Intersects(bounds)))
                    pieces = pieces.SelectMany(p => ConvexSoilClip.Subtract(p, obstacle.Shape, .4f)).ToList();
                foreach (var shape in pieces.Where(p => p.Length >= 3 && PolygonArea(p) > .1f && Geometry2D.TriangulatePolygon(p).Length >= 3))
                {
                    if (!stagedPaint.TryGetValue(ground.Cell, out var paints)) stagedPaint[ground.Cell] = paints = new();
                    paints.Add(new(shape, color, water, detail));
                    cells.Add(ground.Cell);
                }
            }
        }
    }

    private GridPosition[] IslandRoute(GridPosition start, GridPosition end, HashSet<GridPosition> island,
        IReadOnlyDictionary<GridPosition, Vector2> centers, Func<GridPosition, GridPosition, bool> legalEdge, float phase,
        IReadOnlyList<GridPosition> alternatives, out GridPosition actualEnd)
    {
        actualEnd = end;
        var previous = new Dictionary<GridPosition, GridPosition> { [start] = start };
        var costs = new Dictionary<GridPosition, float> { [start] = 0 };
        var pending = new PriorityQueue<GridPosition, (float Cost, int Y, int X)>();
        pending.Enqueue(start, (0, start.Y, start.X));
        while (pending.TryDequeue(out var p, out var priority))
        {
            if (priority.Cost > costs[p]) continue;
            if (p == end) break;
            foreach (var next in Board.GetNeighbors(p).Where(island.Contains).OrderBy(n => n.Y).ThenBy(n => n.X))
            {
                if (!legalEdge(p, next)) continue;
                var at = centers[next];
                // A continuous terrain-cost field bends the path around broad
                // lobes rather than following screen rows or repeated zigzags.
                float bend = 1 + .9f * (1 + MathF.Sin(at.X * .91f + phase) * MathF.Cos(at.Y * .73f - phase));
                float cost = costs[p] + centers[p].DistanceTo(at) * bend;
                if (costs.TryGetValue(next, out float old) && cost >= old) continue;
                costs[next] = cost;
                previous[next] = p;
                pending.Enqueue(next, (cost, next.Y, next.X));
            }
        }
        if (!previous.ContainsKey(end))
        {
            var reachable = alternatives.Where(previous.ContainsKey).Select(p => (GridPosition?)p).FirstOrDefault();
            if (reachable is null) return Array.Empty<GridPosition>();
            end = actualEnd = reachable.Value;
        }
        var route = new List<GridPosition> { end };
        while (route[^1] != start) route.Add(previous[route[^1]]);
        route.Reverse();
        return route.ToArray();
    }
    private static float RiverPointSegmentDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        var axis = b - a;
        return point.DistanceTo(a + axis * (axis.LengthSquared() < .000001f ? 0 :
            Math.Clamp((point - a).Dot(axis) / axis.LengthSquared(), 0, 1)));
    }
    internal static float RiverSegmentsDistance(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var ab = b - a;
        var cd = d - c;
        float cross = ab.Cross(cd);
        if (MathF.Abs(cross) > .000001f)
        {
            float t = (c - a).Cross(cd) / cross, u = (c - a).Cross(ab) / cross;
            if (t is >= 0 and <= 1 && u is >= 0 and <= 1) return 0;
        }
        return Math.Min(Math.Min(RiverPointSegmentDistance(a, c, d), RiverPointSegmentDistance(b, c, d)),
            Math.Min(RiverPointSegmentDistance(c, a, b), RiverPointSegmentDistance(d, a, b)));
    }
    private static bool RiverNetworkClear(Vector2[] curve, IReadOnlyList<(Vector2[] Curve, int Id)> networks,
        Vector2? join, int? joined)
    {
        for (int i = 1; i < curve.Length; i++)
        {
            for (int j = i + 5; j < curve.Length; j++)
                if (RiverSegmentsDistance(curve[i - 1], curve[i], curve[j - 1], curve[j]) < .025f) return false;
            foreach (var network in networks)
            {
                bool merging = network.Id == joined && join is { } point && curve[i].DistanceTo(point) < .65f &&
                    i >= curve.Length - 14;
                if (merging) continue;
                for (int j = 1; j < network.Curve.Length; j++)
                    if (RiverSegmentsDistance(curve[i - 1], curve[i], network.Curve[j - 1], network.Curve[j]) < .36f) return false;
            }
        }
        return true;
    }
    private Vector2 ProjectRiver(Vector2 p) => new((p.X - p.Y) * Projection.TileWidth / 2, (p.X + p.Y) * Projection.TileHeight / 2);
    private Vector2 UnprojectRiver(Vector2 p) => new(p.X / Projection.TileWidth + p.Y / Projection.TileHeight,
        p.Y / Projection.TileHeight - p.X / Projection.TileWidth);
    private static Vector2[] RiverMeanders(IReadOnlyList<Vector2> knots, float amplitude, float phase)
    {
        var result = new List<Vector2>();
        for (int i = 0; i < knots.Count - 1; i++)
        {
            var a = knots[Math.Max(0, i - 1)]; var b = knots[i];
            var c = knots[i + 1]; var d = knots[Math.Min(knots.Count - 1, i + 2)];
            for (int step = 0; step < 8; step++)
            {
                float t = step / 8f;
                var p = .5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t +
                    (-a + 3 * b - 3 * c + d) * t * t * t);
                var tangent = .5f * ((-a + c) + 2 * (2 * a - 5 * b + 4 * c - d) * t +
                    3 * (-a + 3 * b - 3 * c + d) * t * t);
                float envelope = MathF.Pow(MathF.Sin(t * Mathf.Pi), 2);
                float bend = MathF.Sin(t * Mathf.Tau + phase + i * .63f);
                float reach = Math.Min(1, b.DistanceTo(c));
                // Do not meander the final short confluence splice or a mouth.
                float strength = i == 0 || i == knots.Count - 2 || reach < .45f ? 0 : amplitude;
                result.Add(p + tangent.Normalized().Orthogonal() * envelope * bend * strength * reach);
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
        return _riverPaint.TryGetValue(cell, out var paints) && paints.Any(p => p.Water && Geometry2D.IsPointInPolygon(point, p.Shape)) ||
            _riverDeltas.Any(delta => Geometry2D.IsPointInPolygon(point, delta.Channel));
    }
    private void DrawCosmeticRivers(Node2D canvas, ISet<GridPosition> cells)
    {
        EnsureCosmeticRivers();
        DrawRiverDeltas(canvas, cells);
        foreach (var cell in cells)
            if (_riverPaint.TryGetValue(cell, out var paints))
                // Every bank is below every water body. A merging tributary
                // cannot paint a green seam across the existing main channel.
                foreach (var paint in paints.OrderBy(p => p.Detail ? 2 : p.Water ? 1 : 0))
                    canvas.DrawColoredPolygon(paint.Shape, paint.Color);
    }
}
