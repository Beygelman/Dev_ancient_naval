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
        foreach (var cell in land)
        {
            var tile = Projection.Diamond(cell);
            var tileBounds = new Rect2(tile[0], Vector2.Zero);
            foreach (var point in tile)
                tileBounds = tileBounds.Expand(point);
            var shapes = new List<Vector2[]>();
            for (int i = 0; i < contours.Length; i++)
            {
                if (!bounds[i].Intersects(tileBounds))
                    continue;
                foreach (var shape in Geometry2D.IntersectPolygons(tile, contours[i]))
                    if (shape.Length >= 3)
                        shapes.Add(shape);
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

    private readonly Vector2[] _beachTriangle = new Vector2[3];
    private readonly Color[] _beachColor = new Color[1];
    private void DrawBeaches(Node2D canvas, ISet<GridPosition> cells)
    {
        foreach (var beach in _beaches)
        {
            if (!cells.Contains(beach.Cell))
                continue;
            var color = new Color("e7d7a6");
            if (beach.Polygon.Length > 0)
            {
                // Thin shore quads can be below the generic polygon triangulator's
                // tolerance at large map coordinates. Their validated convex fan
                // uses explicit triangles without asking it to solve them again.
                _beachColor[0] = color;
                _beachTriangle[0] = beach.Polygon[0];
                for (int i = 1; i < beach.Polygon.Length - 1; i++)
                {
                    _beachTriangle[1] = beach.Polygon[i];
                    _beachTriangle[2] = beach.Polygon[i + 1];
                    canvas.DrawPrimitive(_beachTriangle, _beachColor, Array.Empty<Vector2>());
                }
            }

            canvas.DrawPolyline(beach.Edge, new Color(color, .7f), 1.5f, true);
        }
    }

    private void DrawCoastalWater(Node2D canvas, ISet<GridPosition> cells)
    {
        var firstColors = new Color[3];
        var secondColors = new Color[3];
        foreach (var beach in _beaches)
        {
            if (!cells.Contains(beach.Cell))
                continue;
            var shallow = new Color("80c7be");
            shallow.A = .40f;
            var sea = new Color(shallow, 0);
            firstColors[0] = firstColors[1] = secondColors[0] = shallow;
            firstColors[2] = secondColors[1] = secondColors[2] = sea;
            if (beach.WaterTriangles[0].Length > 0)
                canvas.DrawPrimitive(beach.WaterTriangles[0], firstColors, Array.Empty<Vector2>());
            if (beach.WaterTriangles[1].Length > 0)
                canvas.DrawPrimitive(beach.WaterTriangles[1], secondColors, Array.Empty<Vector2>());
        }
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
