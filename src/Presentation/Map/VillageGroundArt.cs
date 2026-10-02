using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    private IsometricProjection? _townGroundProjection;
    private readonly Dictionary<(int Id, int Level), TownGroundStamp> _townGround = new();
    private sealed record FieldStamp(Vector2[][] Shapes, Vector2[] Stalks);
    private sealed record TownGroundStamp(Vector2[][] Texture, FieldStamp[] Fields);

    internal IEnumerable<Vector2[]> VillageGroundShapes(Village town) => TownGround(town).Texture;
    internal IEnumerable<Vector2[]> VillageFieldShapes(Village town) => TownGround(town).Fields.SelectMany(f => f.Shapes);
    internal bool VillageSoilContains(Village town, Vector2 local)
    {
        EnsureIslandGeometry();
        var point = Projection.GridToWorld(town.Position) + local;
        return _landShapes.GetValueOrDefault(town.Position)?.Any(shape => Geometry2D.IsPointInPolygon(point, shape)) == true &&
            !_beaches.Any(b => b.Polygon.Length > 0 && Geometry2D.IsPointInPolygon(point, b.Polygon));
    }
    internal string VillageSoilDiagnostic(Village town, Vector2 local)
    {
        var point = Projection.GridToWorld(town.Position) + local;
        bool land = _landShapes.GetValueOrDefault(town.Position)?.Any(shape => Geometry2D.IsPointInPolygon(point, shape)) == true;
        var sand = _beaches.Select((b, i) => (b, i)).Where(p => p.b.Polygon.Length > 0 && Geometry2D.IsPointInPolygon(point, p.b.Polygon))
            .Select(p => p.i + ":" + string.Join(";", p.b.Polygon)).ToArray();
        return $"local={local}, world={point}, land={land}, sand={string.Join('|', sand)}";
    }

    private TownGroundStamp TownGround(Village town) => TownGround(ObserveTownArt(town));
    private TownGroundStamp TownGround(TownArtState town)
    {
        EnsureIslandGeometry();
        if (!ReferenceEquals(_townGroundProjection, Projection))
        {
            _townGroundProjection = Projection;
            _townGround.Clear();
        }
        var key = (town.Id, town.Level);
        if (_townGround.TryGetValue(key, out var cached)) return cached;
        var origin = Projection.GridToWorld(town.Position);
        var placement = VillagePlacement(town);
        var land = _landShapes.GetValueOrDefault(town.Position) ?? Array.Empty<Vector2[]>();
        var neighborhood = new Rect2(origin - new Vector2(128, 128), new Vector2(256, 256));
        var nearbySand = _beaches.Where(b => b.Polygon.Length > 0)
            .Select(b => (b.Polygon, Bounds: PolygonBounds(b.Polygon)))
            .Where(b => b.Bounds.Intersects(neighborhood)).ToArray();
        Vector2[][] ClipToSoil(Vector2[] local)
        {
            var world = local.Select(p => placement.Point(p) + origin).ToArray();
            var bounds = PolygonBounds(world);
            var pieces = new List<Vector2[]>();
            foreach (var shape in land.SelectMany(shape => Geometry2D.IntersectPolygons(world, shape)))
            {
                var indices = Geometry2D.TriangulatePolygon(shape);
                for (int i = 0; i < indices.Length; i += 3)
                    pieces.Add(new[] { shape[indices[i]], shape[indices[i + 1]], shape[indices[i + 2]] });
            }
            foreach (var beach in nearbySand)
                if (beach.Bounds.Intersects(bounds))
                    // Keep a quarter-pixel guard against native boundary rounding
                    // and antialiasing bleeding a soil sliver into the sand.
                    pieces = pieces.SelectMany(shape => ConvexSoilClip.Subtract(shape, beach.Polygon, .25f)).ToList();
            return pieces.Where(shape => shape.Length >= 3 && PolygonArea(shape) > .03f && Geometry2D.TriangulatePolygon(shape).Length >= 3)
                .Select(shape => shape.Select(p => p - origin).ToArray()).ToArray();
        }
        bool OnSoil(Vector2 local)
        {
            var p = local + origin;
            return land.Any(shape => Geometry2D.IsPointInPolygon(p, shape)) &&
                !nearbySand.Any(b => b.Bounds.HasPoint(p) && Geometry2D.IsPointInPolygon(p, b.Polygon));
        }
        var random = new Random(Board.Seed ^ town.Id * 9127);
        var patches = new List<Vector2[]>();
        // Soft, broken patches and cobbles have no common rectangular backdrop.
        for (int patch = 0; patch < 20; patch++)
        {
            var at = new Vector2((float)random.NextDouble() * 54 - 27, (float)random.NextDouble() * 24 - 12);
            float r = 2 + (float)random.NextDouble() * 6;
            var points = Enumerable.Range(0, 7).Select(i => at + new Vector2(MathF.Cos(i * Mathf.Tau / 7) * r,
                MathF.Sin(i * Mathf.Tau / 7) * r * .42f) * (.8f + (float)random.NextDouble() * .4f)).ToArray();
            patches.AddRange(ClipToSoil(points));
        }
        var fields = new List<FieldStamp>();
        for (int field = 0; field < 2 + town.Level / 2; field++)
        {
            Vector2[][] shapes = Array.Empty<Vector2[]>();
            var at = new Vector2(-23 + field * 12, 20 + field % 2 * 2);
            // A coast-facing town may have a very shallow front lawn. Move the
            // wheat up against its houses until a real land patch is available.
            for (int attempt = 0; attempt < 12 && shapes.Length == 0; attempt++)
            {
                var anchor = at - new Vector2(0, attempt * 2);
                shapes = ClipToSoil(new[] { anchor, anchor + new Vector2(10, -4), anchor + new Vector2(18, 0), anchor + new Vector2(8, 5) });
                if (shapes.Sum(PolygonArea) < 15) shapes = Array.Empty<Vector2[]>();
                if (shapes.Length > 0) at = anchor;
            }
            var stalks = Enumerable.Range(0, 8).Select(i => placement.Point(at + new Vector2(3 + i % 4 * 3, i / 4 * 2))).Where(p =>
                shapes.Any(shape => Geometry2D.IsPointInPolygon(p, shape)) &&
                OnSoil(p) && OnSoil(p + new Vector2(-1.1f, -5)) && OnSoil(p + new Vector2(1.1f, -5))).ToArray();
            fields.Add(new(shapes, stalks));
        }
        return _townGround[key] = new(patches.ToArray(), fields.ToArray());
    }

    private void DrawTownGround(Node2D canvas, TownArtState town, Vector2 center)
    {
        int index = 0;
        foreach (var shape in TownGround(town).Texture)
        {
            var color = new Color(index++ % 3 == 0 ? "c3bd99" : "719662", .28f);
            canvas.DrawColoredPolygon(shape.Select(p => p + center).ToArray(), color);
        }
    }

    private static float PolygonArea(Vector2[] points)
    {
        float twiceArea = 0;
        // Subtract a common origin before multiplying. World coordinates can
        // be thousands of pixels from zero while a clipped sliver is tiny.
        // Local cross products avoid cancellation in native coast-fit checks.
        var origin = points[0];
        for (int i = 0; i < points.Length; i++) twiceArea += (points[i] - origin).Cross(points[(i + 1) % points.Length] - origin);
        return MathF.Abs(twiceArea) * .5f;
    }

    private static Rect2 PolygonBounds(Vector2[] points)
    {
        var bounds = new Rect2(points[0], Vector2.Zero);
        foreach (var point in points) bounds = bounds.Expand(point);
        return bounds;
    }
}
