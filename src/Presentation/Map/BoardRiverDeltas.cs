using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal sealed record RiverDeltaInfo(Vector2[] Channel, Vector2 OceanEnd, Color OceanColor,
    GridPosition OceanCell, Vector2 InlandAnchor, Vector2[] Centerline)
{
    internal Rect2 Bounds { get; } = Channel.Aggregate(new Rect2(Channel[0], Vector2.Zero), (bounds, point) => bounds.Expand(point));
}

public partial class BoardView
{
    private readonly List<RiverDeltaInfo> _riverDeltas = new();
    private readonly Dictionary<Vector2[], Vector2[][]> _riverMouthShoreCache = new();
    internal int RiverShoreSplitBuildCount { get; private set; }
    private readonly Dictionary<GridPosition, List<(Vector2[] Shape, Color[] Colors, bool Bank)>> _riverDeltaPaint = new();
    internal IReadOnlyList<RiverDeltaInfo> CosmeticRiverDeltas { get { EnsureCosmeticRivers(); return _riverDeltas; } }

    internal IEnumerable<(GridPosition Cell, Vector2[] Shape, Color[] Colors)> CosmeticRiverDeltaStamps =>
        _riverDeltaPaint.SelectMany(pair => pair.Value.Select(stamp => (pair.Key, stamp.Shape, stamp.Colors)));

    // Estuaries are retained shoreline illustrations. They cut the painted
    // beach, not the navigable board or its saved optical terrain features.
    private void BuildRiverDelta(Vector2[] river, float[] widths, bool reverse)
    {
        int first = reverse ? river.Length - 1 : 0;
        int direction = reverse ? -1 : 1;
        int inside = -1;
        // Sample count is not a distance from the coast: wide beaches and a
        // bend running along the shore may hide the first many river samples.
        // Start the estuary at the first actually painted inland water patch.
        for (int step = 1; step < river.Length / 2; step++)
        {
            int candidate = first + direction * step;
            var point = ProjectRiver(river[candidate]);
            var cell = Projection.WorldToGrid(point);
            if (_riverPaint.TryGetValue(cell, out var paints) && paints.Any(p => p.Water &&
                Geometry2D.IsPointInPolygon(point, p.Shape)))
            {
                inside = candidate;
                break;
            }
        }
        if (inside < 0) return;
        var mouth = ProjectRiver(river[first]);
        var inland = ProjectRiver(river[inside]);
        int nearMouth = reverse ? Math.Max(0, first - 1) : Math.Min(river.Length - 1, first + 1);
        var outward = (mouth - ProjectRiver(river[nearMouth])).Normalized();
        if (outward.LengthSquared() < .5f) return;
        float length = Math.Clamp(Projection.TileHeight * .30f, 9, 18);
        var beach = _beaches.MinBy(b => ((b.Edge[0] + b.Edge[1]) * .5f).DistanceSquaredTo(mouth));
        if (beach is not null)
        {
            var coastOutward = (mouth - beach.Inside).Normalized();
            if (coastOutward.LengthSquared() > .5f) outward = (outward * .4f + coastOutward * .6f).Normalized();
        }
        var ocean = mouth + outward * length;
        var oceanCell = Projection.WorldToGrid(ocean);
        bool OpenWater() => Board.Contains(oceanCell) && Board.GetTile(oceanCell).Terrain != TerrainType.Land &&
            !(_landShapes.GetValueOrDefault(oceanCell)?.Any(p => Geometry2D.IsPointInPolygon(ocean, p)) ?? false);
        // Rounded coast corners sometimes occupy part of a logical land cell.
        // Seek the nearby sea rather than silently leaving a closed river mouth.
        foreach (float reach in new[] { 1f, 1.5f, 2f })
        {
            ocean = mouth + outward * length * reach;
            oceanCell = Projection.WorldToGrid(ocean);
            if (OpenWater()) break;
        }
        if (!OpenWater()) return;
        var sea = RiverOceanColor(oceanCell);
        float throat = Math.Clamp(ProjectRiver(RiverNormal(river, inside) * widths[inside] * .5f).Length(), 1.7f, 4);
        float flare = throat * 1.5f;
        var flowKnots = new List<Vector2>();
        for (int index = inside; reverse ? index < first : index > first; index += reverse ? 2 : -2)
            flowKnots.Add(ProjectRiver(river[index]));
        flowKnots.Add(mouth);
        flowKnots.Add(mouth.Lerp(ocean, .55f));
        flowKnots.Add(ocean);
        // Follow the actual river bend into the mouth; a straight shortcut
        // across its last S bend made a broad angular notch in the beach.
        var path = RiverMeanders(flowKnots, 0, 0);
        int shoreIndex = Enumerable.Range(0, path.Length).MinBy(i => path[i].DistanceSquaredTo(mouth));
        float shoreFraction = shoreIndex / (float)(path.Length - 1);
        var openingWidths = new float[path.Length];
        var left = new Vector2[path.Length];
        var right = new Vector2[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            float t = i / (float)(path.Length - 1);
            float opening = throat + (flare - throat) * Mathf.SmoothStep(0, 1, t);
            // The translucent last section tapers to a round, invisible cap.
            if (t > .85f) opening *= MathF.Sqrt(Math.Max(.05f, 1 - MathF.Pow((t - .85f) / .15f, 2)));
            openingWidths[i] = opening;
            var normal = RiverNormal(path, i);
            left[i] = path[i] - normal * opening;
            right[i] = path[i] + normal * opening;
        }
        // Tight meanders can make a naive parallel-offset outline cross
        // itself. Convex rounded segments overlap at their endpoints; their
        // union remains a valid continuous mouth even around a hairpin bend.
        var waterSegments = Enumerable.Range(1, path.Length - 1).Select(i =>
            DeltaCapsule(path[i - 1], path[i], openingWidths[i - 1], openingWidths[i])).ToArray();
        var channel = waterSegments[0];
        foreach (var segment in waterSegments.Skip(1))
        {
            var union = Geometry2D.MergePolygons(channel, segment);
            if (union.Any()) channel = union.OrderByDescending(PolygonArea).First();
        }
        var obstacles = _riverObstacles.Select(p => (Shape: p, Bounds: PolygonBounds(p))).ToArray();
        var tiles = Board.Tiles.Select(t => (Cell: t.Position, Shape: Projection.Diamond(t.Position)))
            .Select(t => (t.Cell, t.Shape, Bounds: PolygonBounds(t.Shape))).ToArray();
        for (int i = 1; i < path.Length; i++)
        {
            float before = (i - 1f) / (path.Length - 1), after = i / (float)(path.Length - 1);
            // Rounded pale banks carve a widening opening into the old sandy
            // rim. Their sea-facing alpha goes to zero, avoiding a yellow cap.
            var sandBefore = new Color("e7d7a6") { A = Math.Clamp(1 - before * before * 1.4f, 0, 1) };
            var sandAfter = new Color("e7d7a6") { A = Math.Clamp(1 - after * after * 1.4f, 0, 1) };
            var beforeNormal = RiverNormal(path, i - 1);
            var afterNormal = RiverNormal(path, i);
            Stamp(new[] { left[i - 1] - beforeNormal * 2, left[i] - afterNormal * 2, left[i], left[i - 1] },
                new[] { sandBefore, sandAfter, sandAfter, sandBefore }, true);
            Stamp(new[] { right[i - 1], right[i], right[i] + afterNormal * 2, right[i - 1] + beforeNormal * 2 },
                new[] { sandBefore, sandAfter, sandAfter, sandBefore }, true);
            Color Water(float t)
            {
                float outside = Math.Clamp((t - shoreFraction) / Math.Max(.01f, 1 - shoreFraction), 0, 1);
                // The ocean already contains a luminous shallow-water layer.
                // Fade the river over it instead of covering it with dark fans.
                // The inland half retains the river tone. Blending starts at
                // the shoreline, so no dark gradient road cuts through grass.
                var tint = new Color("467a85").Lerp(sea.Lightened(.10f), Mathf.SmoothStep(0, 1, outside));
                tint.A = 1 - Mathf.SmoothStep(0, .90f, outside);
                return tint;
            }
            var colorBefore = Water(before);
            var colorAfter = Water(after);
            Stamp(waterSegments[i - 1], new[] { colorBefore, colorAfter }, false, path[i - 1], path[i]);
        }
        _riverDeltas.Add(new(channel, ocean, sea, oceanCell, inland, path));

        void Stamp(Vector2[] shape, Color[] colors, bool bank, Vector2? from = null, Vector2? to = null)
        {
            var bounds = PolygonBounds(shape);
            foreach (var tile in tiles)
            {
                var cell = tile.Cell;
                if (!tile.Bounds.Intersects(bounds)) continue;
                var pieces = Geometry2D.IntersectPolygons(shape, tile.Shape).Where(p => p.Length >= 3).ToList();
                if (bank)
                    // Estuary banks reshape the existing sand rim. Never lay
                    // a beige road uphill from the beach into river grass.
                    pieces = pieces.SelectMany(piece => _beaches.Where(beach => beach.Polygon.Length >= 3 &&
                        PolygonBounds(beach.Polygon).Intersects(bounds)).SelectMany(beach =>
                            Geometry2D.IntersectPolygons(piece, beach.Polygon))).Where(piece => piece.Length >= 3).ToList();
                foreach (var obstacle in obstacles.Where(o => o.Bounds.Intersects(bounds)))
                    pieces = pieces.SelectMany(p => ConvexSoilClip.Subtract(p, obstacle.Shape, .4f)).ToList();
                foreach (var piece in pieces.Where(p => PolygonArea(p) > .03f && Geometry2D.TriangulatePolygon(p).Length >= 3))
                {
                    if (!_riverDeltaPaint.TryGetValue(cell, out var paints)) _riverDeltaPaint[cell] = paints = new();
                    // Interpolated vertex colors retain the fade at cell seams.
                    var gradient = piece.Select(p =>
                    {
                        var start = from ?? shape[0];
                        var axis = (to ?? shape[1]) - start;
                        float t = axis.LengthSquared() < .001f ? 0 : Math.Clamp((p - start).Dot(axis) / axis.LengthSquared(), 0, 1);
                        return colors[0].Lerp(colors[1], t);
                    }).ToArray();
                    // Sand is confined to the actual island side of the mouth.
                    if (bank && !(_landShapes.GetValueOrDefault(cell)?.Any(p => Geometry2D.IsPointInPolygon(piece.Aggregate(Vector2.Zero, (a, b) => a + b) / piece.Length, p)) ?? false))
                        continue;
                    paints.Add((piece, gradient, bank));
                }
            }
        }
    }

    private static Vector2[] DeltaCapsule(Vector2 a, Vector2 b, float radiusA, float radiusB)
    {
        float angle = (b - a).Angle();
        var points = Enumerable.Range(0, 9).Select(i => b +
            Vector2.FromAngle(angle - Mathf.Pi / 2 + i * Mathf.Pi / 8) * radiusB)
            .Concat(Enumerable.Range(0, 9).Select(i => a +
                Vector2.FromAngle(angle + Mathf.Pi / 2 + i * Mathf.Pi / 8) * radiusA)).ToArray();
        return Geometry2D.ConvexHull(points).SkipLast(1).ToArray();
    }

    internal Color RiverOceanColor(GridPosition cell)
    {
        var location = Board.Center(cell);
        float tone = (float)Math.Sin(location.X * .33 + location.Y * .27 + Board.Seed % 17) * .022f;
        var color = new Color("30596b");
        color = tone > 0 ? color.Lightened(tone) : color.Darkened(-tone);
        return color.Darkened(Math.Min(8, Math.Max(0, TerrainFeatures.For(Board).DistanceFromCoast(cell) - 1)) * .012f);
    }

    private Vector2[][] RiverMouthShoreEdges(Vector2[] edge)
    {
        if (!_riverMouthShoreCache.TryGetValue(edge, out var pieces))
            _riverMouthShoreCache[edge] = pieces = BuildRiverMouthShoreEdges(edge).ToArray();
        return pieces;
    }

    private IEnumerable<Vector2[]> BuildRiverMouthShoreEdges(Vector2[] edge)
    {
        RiverShoreSplitBuildCount++;
        var bounds = PolygonBounds(edge);
        var mouths = _riverDeltas.Where(d => d.Bounds.Intersects(bounds)).ToArray();
        if (mouths.Length == 0) { yield return edge; yield break; }
        // Split only wave runs crossing a mouth. Unaffected long shore runs
        // remain batched and no breakers are painted across an estuary.
        for (int i = 1; i < edge.Length; i++)
        {
            var a = edge[i - 1];
            var b = edge[i];
            var axis = b - a;
            var cuts = new List<float> { 0, 1 };
            foreach (var mouth in mouths)
                for (int j = 0; j < mouth.Channel.Length; j++)
                {
                    var hit = Geometry2D.SegmentIntersectsSegment(a, b, mouth.Channel[j], mouth.Channel[(j + 1) % mouth.Channel.Length]);
                    if (hit.VariantType != Variant.Type.Nil && axis.LengthSquared() > .001f)
                        cuts.Add(Math.Clamp((hit.AsVector2() - a).Dot(axis) / axis.LengthSquared(), 0, 1));
                }
            cuts.Sort();
            for (int j = 1; j < cuts.Count; j++)
            {
                if (cuts[j] - cuts[j - 1] < .001f) continue;
                var midpoint = a + axis * ((cuts[j] + cuts[j - 1]) * .5f);
                if (!mouths.Any(d => Geometry2D.IsPointInPolygon(midpoint, d.Channel)))
                    yield return new[] { a + axis * cuts[j - 1], a + axis * cuts[j] };
            }
        }
    }

    private void DrawRiverDeltas(Node2D canvas, ISet<GridPosition> cells)
    {
        foreach (var cell in cells)
            if (_riverDeltaPaint.TryGetValue(cell, out var stamps))
                foreach (var stamp in stamps.OrderBy(s => s.Bank ? 0 : 1)) canvas.DrawPolygon(stamp.Shape, stamp.Colors);
    }
}
