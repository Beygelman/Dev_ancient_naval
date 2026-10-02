using System;
using System.Linq;
using System.Collections.Generic;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private IsometricProjection? _islandProjection;
    private Vector2[][] _islandContours = Array.Empty<Vector2[]>();
    private readonly Dictionary<GridPosition, Vector2[][]> _landShapes = new();
    private readonly List<BeachStrip> _beaches = new();
    private readonly Dictionary<GridPosition, BeachStrip[]> _beachesByCell = new();
    private readonly SeaGeometryBatch _shoreBatch = new();
    private readonly List<(GridPosition Cell, Vector2[] Edge, Vector2 Inside)> _shoreLines = new();
    private sealed record BeachStrip(GridPosition Cell, Vector2[] Polygon, Vector2[] Edge, Vector2 Inside, Vector2[][] WaterTriangles);
    internal IReadOnlyList<Vector2[]> IslandContours
    {
        get
        {
            EnsureIslandGeometry();
            return _islandContours;
        }
    }

    internal IEnumerable<Vector2[]> BeachPolygons
    {
        get
        {
            EnsureIslandGeometry();
            return _beaches.Where(beach => beach.Polygon.Length > 0).Select(beach => beach.Polygon);
        }
    }

    internal int BeachSegmentCount
    {
        get
        {
            EnsureIslandGeometry();
            return _beaches.Count;
        }
    }

    internal IEnumerable<Vector2[]> CoastalWaterTriangles
    {
        get
        {
            EnsureIslandGeometry();
            return _beaches.SelectMany(beach => beach.WaterTriangles).Where(points => points.Length > 0);
        }
    }

    internal static float BeachWidth(Vector2 p) => 4 + 16 * MathF.Pow(.5f + .5f * MathF.Sin(p.X * .008f + MathF.Sin(p.Y * .02f) * 2), 1.5f);
    private void EnsureIslandGeometry()
    {
        if (ReferenceEquals(_islandProjection, Projection))
            return;
        _islandProjection = Projection;
        _landShapes.Clear();
        _beaches.Clear();
        _shoreLines.Clear();
        var land = Board.Tiles.Where(tile => tile.Terrain == TerrainType.Land).Select(tile => tile.Position).ToArray();
        var landSet = land.ToHashSet();
        var contours = Projection.BoundaryLoops(land).Select(RoundIslandContour).ToArray();
        _islandContours = contours;
        var bounds = contours.Select(points =>
        {
            var box = new Rect2(points[0], Vector2.Zero);
            foreach (var point in points)
                box = box.Expand(point);
            return box;
        }).ToArray();
        // The smooth contour is the shared visual shoreline. Clip it in every
        // intersecting cell, including concave bends that cross a water seam,
        // so later water tiles cannot punch holes behind the sand band.
        var holes = contours.Select((points, i) => (Points: points, Bounds: bounds[i]))
            .Where(entry => SignedCoastArea(entry.Points) < 0).ToArray();
        foreach (var cell in Board.Tiles.Select(t => t.Position))
        {
            var tile = Projection.Diamond(cell);
            var tileBounds = new Rect2(tile[0], Vector2.Zero);
            foreach (var point in tile)
                tileBounds = tileBounds.Expand(point);
            var shapes = new List<Vector2[]>();
            for (int i = 0; i < contours.Length; i++)
            {
                if (SignedCoastArea(contours[i]) < 0 || !bounds[i].Intersects(tileBounds))
                    continue;
                foreach (var shape in Geometry2D.IntersectPolygons(tile, contours[i]))
                    // Clipping a rounded bend tangent to a seam can return a
                    // zero-area sliver. Retain only drawable surface pieces.
                    if (shape.Length >= 3 && Geometry2D.TriangulatePolygon(shape).Length >= 3)
                        shapes.Add(shape);
            }

            foreach (var hole in holes.Where(h => h.Bounds.Intersects(tileBounds)))
            {
                // Difference can return an outer ring and a lake hole. Draw
                // independent pieces instead of filling the hole as a polygon.
                foreach (var cut in Geometry2D.IntersectPolygons(tile, hole.Points))
                {
                    var indices = Geometry2D.TriangulatePolygon(cut);
                    for (int triangle = 0; triangle < indices.Length; triangle += 3)
                    {
                        var lake = new[] { cut[indices[triangle]], cut[indices[triangle + 1]], cut[indices[triangle + 2]] };
                        if (SignedCoastArea(lake) < 0) Array.Reverse(lake);
                        shapes = shapes.SelectMany(shape => ConvexSoilClip.Subtract(shape, lake))
                            .Where(shape => shape.Length >= 3 && Geometry2D.TriangulatePolygon(shape).Length >= 3).ToList();
                    }
                }
            }
            _landShapes[cell] = shapes.ToArray();
        }

        foreach (var contour in contours)
        {
            int firstBeach = _beaches.Count;
            var inner = new Vector2[contour.Length];
            var outer = new Vector2[contour.Length];
            for (int i = 0; i < contour.Length; i++)
            {
                var direction = (contour[(i + 1) % contour.Length] - contour[(i + contour.Length - 1) % contour.Length]).Normalized();
                var normal = new Vector2(-direction.Y, direction.X);
                inner[i] = contour[i] + normal * BeachWidth(contour[i]);
                float shallows = 22 + 10 * (.5f + .5f * MathF.Sin(contour[i].X * .013f + contour[i].Y * .018f));
                outer[i] = contour[i] - normal * shallows;
            }

            // An inset wider than a rounded promontory folds back on itself.
            // Relax the shared inset points before emitting either neighbouring
            // segment, so every retained beach strip is a convex polygon.
            for (int pass = 0; pass < 16; pass++)
            {
                bool adjusted = false;
                for (int i = 0; i < contour.Length; i++)
                {
                    int next = (i + 1) % contour.Length;
                    if (ValidBeachQuad(contour[i], contour[next], inner[next], inner[i]))
                        continue;
                    inner[i] = inner[i].Lerp(contour[i], .35f);
                    inner[next] = inner[next].Lerp(contour[next], .35f);
                    adjusted = true;
                }

                if (!adjusted)
                    break;
            }

            for (int i = 0; i < contour.Length; i++)
            {
                int next = (i + 1) % contour.Length;
                var inside = (inner[i] + inner[next]) * .5f;
                var cell = Projection.WorldToGrid(inside);
                if (!landSet.Contains(cell))
                    cell = land.MinBy(position => Projection.GridToWorld(position).DistanceSquaredTo(inside));
                var polygon = ValidBeachQuad(contour[i], contour[next], inner[next], inner[i]) ? new[]
                {
                    contour[i],
                    contour[next],
                    inner[next],
                    inner[i]
                }

                : Array.Empty<Vector2>();
                // Very small river bends can pass the cross-product test yet
                // collapse at the triangulator's floating-point tolerance. Keep
                // their shoreline stroke, but omit the subpixel sand sliver.
                if (polygon.Length > 0 && Geometry2D.TriangulatePolygon(polygon).Length != 6)
                    polygon = Array.Empty<Vector2>();
                _beaches.Add(new(cell, polygon, new[] { contour[i], contour[next] }, inside, new[] { CoastalTriangle(contour[i], contour[next], outer[next]), CoastalTriangle(contour[i], outer[next], outer[i]) }));
            }

            // Keep wave runs long enough to draw them with one native call.
            for (int start = 0; start < contour.Length; start += 16)
            {
                int count = Math.Min(16, contour.Length - start);
                var edge = new Vector2[(count + 1) / 2 + 1];
                var inside = Vector2.Zero;
                for (int i = 0; i < edge.Length; i++)
                {
                    int index = (start + Math.Min(count, i * 2)) % contour.Length;
                    edge[i] = contour[index];
                    inside += inner[index];
                }

                _shoreLines.Add((_beaches[firstBeach + start].Cell, edge, inside / edge.Length));
            }
        }
        _beachesByCell.Clear();
        foreach (var group in _beaches.GroupBy(beach => beach.Cell))
            _beachesByCell.Add(group.Key, group.ToArray());
    }

    internal bool VisualLandContains(Vector2 point)
    {
        EnsureIslandGeometry();
        return _landShapes.TryGetValue(Projection.WorldToGrid(point), out var shapes) &&
            shapes.Any(shape => Geometry2D.IsPointInPolygon(point, shape));
    }
    private static float SignedCoastArea(Vector2[] points)
    {
        float area = 0;
        var origin = points[0];
        for (int i = 1; i < points.Length - 1; i++)
            area += (points[i] - origin).Cross(points[i + 1] - origin);
        return area * .5f;
    }

    private static bool ValidBeachQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => (b - a).Cross(c - b) > .001f && (c - b).Cross(d - c) > .001f && (d - c).Cross(a - d) > .001f && (a - d).Cross(b - a) > .001f;
    private static Vector2[] CoastalTriangle(Vector2 a, Vector2 b, Vector2 c) => MathF.Abs((b - a).Cross(c - a)) > .001f ? new[]
    {
        a,
        b,
        c
    }

    : Array.Empty<Vector2>();
    private static Vector2[] RoundIslandContour(Vector2[] points)
    {
        // Round the complete connected island contour. Independent tile insets
        // make saw-tooth beaches and mismatched corner widths.
        var reduced = new List<Vector2>
        {
            points[0]
        };
        for (int i = 1; i < points.Length; i++)
            if (points[i].DistanceSquaredTo(reduced[^1]) >= 900)
                reduced.Add(points[i]);
        if (reduced.Count < 3)
        {
            int samples = Math.Min(6, points.Length);
            reduced = Enumerable.Range(0, samples).Select(index => points[index * points.Length / samples]).ToList();
        }

        var result = reduced.ToArray();
        for (int pass = 0; pass < 4; pass++)
        {
            var next = new Vector2[result.Length * 2];
            for (int i = 0; i < result.Length; i++)
            {
                var a = result[i];
                var b = result[(i + 1) % result.Length];
                next[i * 2] = a.Lerp(b, .29f);
                next[i * 2 + 1] = a.Lerp(b, .71f);
            }

            result = next;
        }

        return result;
    }

    internal IEnumerable<(Vector2[] Edge, Vector2 Inside)> VisibleShoreSegments()
    {
        EnsureIslandGeometry();
        foreach (var shore in _shoreLines)
            if (Battle.Vision.IsVisible(Side.Player, shore.Cell))
                yield return (shore.Edge, shore.Inside);
    }

    private void DrawBeaches(Node2D canvas, ISet<GridPosition> cells)
    {
        _shoreBatch.Clear();
        var color = new Color("e7d7a6");
        foreach (var cell in cells)
        {
            if (!_beachesByCell.TryGetValue(cell, out var beaches)) continue;
            foreach (var beach in beaches)
            {
                if (beach.Polygon.Length > 0) _shoreBatch.Polygon(beach.Polygon, color, Transform2D.Identity);
                _shoreBatch.Polyline(beach.Edge, new Color(color, .7f));
            }
        }
        _shoreBatch.Submit(canvas, 1.5f);
    }

    private void DrawCoastalWater(Node2D canvas, ISet<GridPosition> cells)
    {
        _shoreBatch.Clear();
        var shallow = new Color("80c7be") { A = .40f };
        var sea = new Color(shallow, 0);
        foreach (var cell in cells)
        {
            if (!_beachesByCell.TryGetValue(cell, out var beaches)) continue;
            foreach (var beach in beaches)
            {
                _shoreBatch.Triangle(beach.WaterTriangles[0], shallow, shallow, sea);
                _shoreBatch.Triangle(beach.WaterTriangles[1], shallow, sea, sea);
            }
        }
        _shoreBatch.Submit(canvas);
    }

    private void DrawTreasuries(Node2D canvas)
    {
        foreach (var treasury in Battle.ObservedTreasuries(Side.Player))
        {
            var c = Projection.GridToWorld(treasury.Position);
            Vector2 P(float x, float y) => c + new Vector2(x, y);
            canvas.DrawSetTransform(c, 0, new Vector2(1, .45f));
            canvas.DrawCircle(Vector2.Zero, 21, new Color(.85f, .7f, .3f, .13f));
            canvas.DrawArc(Vector2.Zero, 21, 0, Mathf.Tau, 32, new Color("b8b181"), 1.2f, true);
            canvas.DrawSetTransform(Vector2.Zero);
            canvas.DrawColoredPolygon(new[] { P(-16, 0), P(0, -8), P(17, 0), P(0, 9) }, new Color("81958b"));
            canvas.DrawColoredPolygon(new[] { P(-11, -3), P(5, 0), P(5, -10), P(-11, -13) }, new Color("b08a4d"));
            canvas.DrawColoredPolygon(new[] { P(5, 0), P(13, -4), P(13, -14), P(5, -10) }, new Color("715f42"));
            canvas.DrawColoredPolygon(new[] { P(-12, -13), P(-5, -18), P(14, -14), P(5, -9) }, new Color("ead28c"));
            canvas.DrawLine(P(-6, -12), P(-6, -3), new Color("ffe6a0"), 2, true);
            canvas.DrawCircle(P(2, -6), 2, new Color("ffefac"));
        }
    }
}
