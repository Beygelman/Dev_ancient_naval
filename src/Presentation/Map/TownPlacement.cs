using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal readonly record struct TownPlacement(Vector2 Offset, float Scale)
{
    internal Vector2 Point(Vector2 local) => Offset + local * Scale;
}

public partial class BoardView
{
    private readonly Dictionary<int, TownPlacement> _townPlacements = new();
    private IsometricProjection? _townPlacementProjection;
    // Includes tower bases, house doors, field margins and the monument's
    // ground plinth. Vertical building height remains upright, never a mask
    // which would cut off a church roof at the ground's shoreline.
    private static readonly Vector2[] TownFootprint = Enumerable.Range(-9, 19)
        .SelectMany(x => Enumerable.Range(-9, 16).Select(y => new Vector2(x * 5, y * 4)))
        .Where(p => MathF.Abs(p.X) / 47 + MathF.Abs(p.Y + 8) / 34 <= 1)
        .Concat(TownWallCorners.SelectMany(p => new[] { p + new Vector2(-5, 0), p + new Vector2(5, 0),
            p + new Vector2(0, -3), p + new Vector2(0, 3) }))
        .ToArray();
    private static readonly Vector2[] TownEnvelope = new[]
    {
        new Vector2(0, -42), new Vector2(47, -8),
        new Vector2(0, 26), new Vector2(-47, -8)
    };

    internal TownPlacement VillagePlacement(Village town) => VillagePlacement(ObserveTownArt(town));
    internal IEnumerable<Vector2> VillageFootprintPoints(Village town) =>
        TownFootprint.Concat(TownEnvelope).Select(VillagePlacement(town).Point);

    private TownPlacement VillagePlacement(TownArtState town)
    {
        EnsureIslandGeometry();
        if (!ReferenceEquals(_townPlacementProjection, Projection))
        {
            _townPlacementProjection = Projection;
            _townPlacements.Clear();
        }
        if (_townPlacements.TryGetValue(town.Id, out var cached)) return cached;
        var origin = Projection.GridToWorld(town.Position);
        var land = _landShapes.GetValueOrDefault(town.Position) ?? Array.Empty<Vector2[]>();
        var neighborhood = new Rect2(origin - new Vector2(128, 128), new Vector2(256, 256));
        var sand = _beaches.Where(b => b.Polygon.Length > 0)
            .Select(b => (b.Polygon, Bounds: PolygonBounds(b.Polygon)))
            .Where(b => b.Bounds.Intersects(neighborhood)).ToArray();
        bool OnLand(Vector2 p) => land.Any(shape => Geometry2D.IsPointInPolygon(p, shape)) &&
            !sand.Any(b => b.Bounds.HasPoint(p) && Geometry2D.IsPointInPolygon(p, b.Polygon));
        bool Fits(Vector2 offset, float scale)
        {
            // Envelope vertices are just as important as the interior grid;
            // area clipping alone can accept a vertex exactly on a sandy edge.
            // The same addition order is used by all drawing/diagnostic paths.
            bool Clear(Vector2 local)
            {
                var p = origin + (offset + local * scale);
                return OnLand(p) && OnLand(p + new Vector2(.3f, 0)) && OnLand(p - new Vector2(.3f, 0)) &&
                    OnLand(p + new Vector2(0, .3f)) && OnLand(p - new Vector2(0, .3f));
            }
            if (!TownFootprint.Concat(TownEnvelope).All(Clear)) return false;
            var envelope = TownEnvelope.Select(p => origin + (offset + p * scale)).ToArray();
            float expected = PolygonArea(envelope);
            // Area containment catches narrow inlets and enclosed sandy holes
            // between sample points; buildings cannot bridge across either.
            float covered = land.SelectMany(shape => Geometry2D.IntersectPolygons(envelope, shape)).Sum(PolygonArea);
            if (covered < expected - .01f) return false;
            var bounds = PolygonBounds(envelope);
            return !sand.Any(b => b.Bounds.Intersects(bounds) &&
                Geometry2D.IntersectPolygons(envelope, b.Polygon).Sum(PolygonArea) > .01f);
        }
        var offsets = Enumerable.Range(-4, 9).SelectMany(x => Enumerable.Range(-5, 10)
            .Select(y => new Vector2(x * 4, y * 3))).OrderBy(p => p.LengthSquared()).ToArray();
        // All levels share the fit of the complete future town, so upgrades
        // cannot shift houses, disconnect mill blades or push walls onto sand.
        for (int step = 0; step < 16; step++)
        {
            float scale = .82f - step * .035f;
            foreach (var offset in offsets)
                if (Fits(offset, scale))
                    return _townPlacements[town.Id] = new(offset, scale);
        }
        // Very narrow legacy settlement cells may not fit a normal town. The
        // bounded fallback still searches a real land point before shrinking.
        foreach (var offset in offsets)
            if (Fits(offset, .12f))
                return _townPlacements[town.Id] = new(offset, .12f);
        return _townPlacements[town.Id] = new(Vector2.Zero, 0);
    }
}
