using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal readonly record struct TownPlacement(Vector2 Offset, float Scale, bool Compact = false, float FloorSlope = 0)
{
    internal Vector2 Point(Vector2 local) => Offset + new Vector2(local.X,local.Y+local.X*FloorSlope) * Scale;
    internal Transform2D Transform(Vector2 origin) => new(new Vector2(Scale,Scale*FloorSlope),new Vector2(0,Scale),origin+Offset);
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
        (VillagePlacement(town).Compact ? CompactTownFootprint.Concat(CompactTownEnvelope) : TownFootprint.Concat(TownEnvelope)).Select(VillagePlacement(town).Point);

    private static readonly Vector2[] CompactTownEnvelope =
    {
        new(0, -6), new(18, -1), new(0, 4), new(-18, -1)
    };
    private static readonly Vector2[] CompactTownFootprint = new[]
        { new Vector2(0,-3), new Vector2(13,-1), new Vector2(0,1), new Vector2(-13,-1) }
        .SelectMany(p=>new[] {p+new Vector2(-4.6f,0),p+new Vector2(4.6f,0),p+new Vector2(0,-2.3f),p+new Vector2(0,2.3f)})
        .Concat(new[] {new Vector2(0,-4),new Vector2(-7,-2),new Vector2(7,-2)}
            .SelectMany(p=>new[] {p+new Vector2(-4,0),p+new Vector2(2,4),p+new Vector2(7,0)})).ToArray();
    private Vector2[][] TownSoilShapes(DevAncientNaval.Core.Grid.GridPosition start)
    {
        var result = new HashSet<DevAncientNaval.Core.Grid.GridPosition> {start};
        var pending = new Queue<DevAncientNaval.Core.Grid.GridPosition>();
        pending.Enqueue(start);
        while(pending.TryDequeue(out var cell))
            foreach(var next in Board.GetNeighbors(cell))
                if(Board.GetTile(next).Terrain == DevAncientNaval.Core.World.TerrainType.Land &&
                    Board.Distance(next,start)<=3 && result.Add(next)) pending.Enqueue(next);
        // Smoothing can carry the same island's grassy edge across a logical
        // sea-cell seam. Retain those pieces, but never a nearby other island.
        var cellShape=Projection.Diamond(start);
        var island=_islandContours.Where(p=>SignedCoastArea(p)>0)
            .MaxBy(p=>Geometry2D.IntersectPolygons(cellShape,p).Sum(PolygonArea));
        var islandBounds=island is null ? default : PolygonBounds(island);
        var visualCells=result.SelectMany(p=>Board.GetSurrounding(p).Append(p)).ToHashSet();
        return _landShapes.Where(pair=>visualCells.Contains(pair.Key)).SelectMany(pair=>pair.Value)
            .Where(shape=>
            {
                var midpoint=shape.Aggregate(Vector2.Zero,(sum,p)=>sum+p)/shape.Length;
                return island is not null && islandBounds.HasPoint(midpoint) && Geometry2D.IsPointInPolygon(midpoint,island);
            }).ToArray();
    }

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
        // A settlement is anchored to one functional cell, but its decorative
        // streets may cross a seam into contiguous inland ground. Confining a
        // whole future city to a half-beach cell made every building miniature.
        var land = TownSoilShapes(town.Position);
        var landBounds=land.Select(shape=>(Shape:shape,Bounds:PolygonBounds(shape))).ToArray();
        var neighborhood = new Rect2(origin - new Vector2(128, 128), new Vector2(256, 256));
        var sand = _beaches.Where(b => b.Polygon.Length > 0)
            .Select(b => (b.Polygon, Bounds: PolygonBounds(b.Polygon)))
            .Where(b => b.Bounds.Intersects(neighborhood)).ToArray();
        bool OnLand(Vector2 p) => landBounds.Any(piece => piece.Bounds.HasPoint(p) && Geometry2D.IsPointInPolygon(p, piece.Shape)) &&
            !sand.Any(b => b.Bounds.HasPoint(p) && Geometry2D.IsPointInPolygon(p, b.Polygon));
        bool Fits(Vector2 offset, Vector2[] envelope, IEnumerable<Vector2> footprint, float slope=0)
        {
            const float scale = .64f;
            // Envelope vertices are just as important as the interior grid;
            // area clipping alone can accept a vertex exactly on a sandy edge.
            // The same addition order is used by all drawing/diagnostic paths.
            bool Clear(Vector2 local)
            {
                var p = origin + (offset + new Vector2(local.X,local.Y+local.X*slope) * scale);
                return OnLand(p) && OnLand(p + new Vector2(.3f, 0)) && OnLand(p - new Vector2(.3f, 0)) &&
                    OnLand(p + new Vector2(0, .3f)) && OnLand(p - new Vector2(0, .3f));
            }
            if (!envelope.All(Clear) || !footprint.All(Clear)) return false;
            var worldEnvelope = envelope.Select(p => origin + (offset + new Vector2(p.X,p.Y+p.X*slope) * scale)).ToArray();
            float expected = PolygonArea(worldEnvelope);
            // Area containment catches narrow inlets and enclosed sandy holes
            // between sample points; buildings cannot bridge across either.
            float covered = land.SelectMany(shape => Geometry2D.IntersectPolygons(worldEnvelope, shape)).Sum(PolygonArea);
            if (covered < expected - .01f) return false;
            var bounds = PolygonBounds(worldEnvelope);
            return !sand.Any(b => b.Bounds.Intersects(bounds) &&
                Geometry2D.IntersectPolygons(worldEnvelope, b.Polygon).Sum(PolygonArea) > .01f);
        }
        var offsets = Enumerable.Range(-20, 41).SelectMany(x => Enumerable.Range(-26, 53)
            .Select(y => new Vector2(x * 4, y * 3))).OrderBy(p => p.LengthSquared()).ToArray();
        // All levels share the fit of the complete future town, so upgrades
        // cannot shift houses, disconnect mill blades or push walls onto sand.
        foreach (var offset in offsets)
            if (Fits(offset, TownEnvelope, TownFootprint))
                return _townPlacements[town.Id] = new(offset, .64f);
        // Historical narrow islets use fewer houses on shorter streets. The
        // houses and sanctuary remain the same medium size; no miniatures.
        var compactOffsets = Enumerable.Range(-40,81).SelectMany(x=>Enumerable.Range(-55,111)
            .Select(y=>new Vector2(x*2,y))).OrderBy(p=>p.LengthSquared());
        foreach(float slope in new[] {0f,.35f,-.35f,.6f,-.6f,.85f,-.85f})
            foreach (var offset in compactOffsets)
                if (Fits(offset, CompactTownEnvelope, CompactTownFootprint,slope))
                    return _townPlacements[town.Id] = new(offset, .64f, true,slope);
        return _townPlacements[town.Id] = new(Vector2.Zero, 0);
    }
}
