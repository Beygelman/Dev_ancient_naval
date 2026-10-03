using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private IsometricProjection? _sceneryProjection;
    private readonly List<Scenery> _scenery = new();
    internal int TreeCount => _scenery.Count(item => item.Kind == 1);
    internal int ConnectingMountainCount => _scenery.Count(item => item.Kind == 2 && item.Minor);
    internal IEnumerable<float> SummitSizes => _scenery.Where(item => item.Kind == 2 && !item.Minor).Select(item => item.Size);
    internal int MountainCount => _scenery.Count(item => item.Kind == 2);
    internal IEnumerable<float> MountainHeights => _scenery.Where(item => item.Kind == 2 && !item.Minor).Select(item => item.Size * item.HeightRatio);
    internal IEnumerable<float> SnowyMountainHeights => _scenery.Where(item => item.Kind == 2 && item.Snow).Select(item => item.Size * item.HeightRatio);
    internal IEnumerable<GridPosition> RenderedMountainCells => _scenery.Where(item => item.Kind == 2).Select(item => item.Cell);
    internal IEnumerable<Vector2[]> MountainGroundFootprints => _scenery.Where(item => item.Kind == 2)
        .Select(item => MountainFootprint(item, item.Point));

    private sealed record Scenery(GridPosition Cell, Vector2 Point, float Size, int Kind, float Shade, bool Minor = false,
        float HeightRatio = 1.7f, bool Snow = false);
    private void DrawIslandScenery(Node2D canvas, ISet<GridPosition> cells)
    {
        if (!ReferenceEquals(_sceneryProjection, Projection))
            BuildScenery();
        DrawCosmeticRivers(canvas, cells);
        foreach (var item in _scenery)
            if (item.Kind == 0 && cells.Contains(item.Cell))
                DrawSceneryObject(canvas, item, item.Point);
    }

    private static void DrawSceneryObject(Node2D canvas, Scenery item, Vector2 point)
    {
        const float dim = 0;
        var p = point;
        float s = item.Size;
        Color C(string hex) => new Color(hex).Darkened(dim);
        if (item.Kind == 0)
        {
            canvas.DrawSetTransform(p, item.Shade, new Vector2(1, .5f));
            canvas.DrawCircle(Vector2.Zero, s, new Color(C(item.Shade > 0 ? "acbf77" : "538e63"), .26f));
            canvas.DrawSetTransform(Vector2.Zero);
            canvas.DrawLine(p + new Vector2(-2, 1), p + new Vector2(-3, -2), C("7a9f60"), 1, true);
        }
        else if (item.Kind == 1)
        {
            canvas.DrawSetTransform(p + new Vector2(5, 2), -.1f, new Vector2(1, .4f));
            canvas.DrawCircle(Vector2.Zero, s * .6f, new Color(0, .1f, .08f, .22f));
            canvas.DrawSetTransform(Vector2.Zero);
            canvas.DrawLine(p, p + new Vector2(0, -s * .9f), C("796f50"), Math.Max(1, s * .2f));
            if (item.Shade < 1)
            {
                canvas.DrawColoredPolygon(new[] { p + new Vector2(0, -s * 2), p + new Vector2(s * .7f, -s * .65f), p + new Vector2(-s * .7f, -s * .65f) }, C("39785d"));
                canvas.DrawColoredPolygon(new[] { p + new Vector2(0, -s * 2), p + new Vector2(-s * .7f, -s * .65f), p + new Vector2(-s * .15f, -s * .8f) }, C("639865"));
            }
            else if (item.Shade < 2)
            {
                canvas.DrawColoredPolygon(new[] { p + new Vector2(-s * .7f, -s), p + new Vector2(-s * .55f, -s * 1.9f), p + new Vector2(s * .3f, -s * 2.2f), p + new Vector2(s * .85f, -s * 1.25f), p + new Vector2(s * .2f, -s * .65f) }, C("427f54"));
                canvas.DrawLine(p + new Vector2(-s * .35f, -s * 1.45f), p + new Vector2(s * .3f, -s * 1.7f), C("78a768"), 2, true);
            }
            else
            {
                canvas.DrawLine(p, p + new Vector2(s * .3f, -s * 1.8f), C("817655"), 1.8f, true);
                var top = p + new Vector2(s * .3f, -s * 1.8f);
                for (int leaf = 0; leaf < 5; leaf++)
                {
                    float angle = Mathf.Pi + leaf * .7f;
                    var end = top + Vector2.FromAngle(angle) * s;
                    canvas.DrawLine(top, end, C("5c945e"), 3, true);
                }
            }
        }
        else if (item.Kind == 3)
            IslandRuinsArt.Draw(canvas, point, item.Size / 18f, (int)item.Shade, false);
        else if (item.Kind == 4)
            DrawTreasuryRuin(canvas, point, (int)item.Shade);
        else
        {
            var ground = MountainFootprint(item, p);
            var shoulder = ground.Select(v => p + (v - p) * .68f + new Vector2(0, -s * .27f)).ToArray();
            var peak = p + new Vector2(s * item.Shade * .32f, -s * item.HeightRatio);
            // A low vegetated, irregular skirt blends into the island. Several
            // folded rock faces replace the old four-sided, flat-bottomed prism.
            for (int i = 0; i < ground.Length; i++)
            {
                int next = (i + 1) % ground.Length;
                if ((ground[next] - ground[i]).Cross(shoulder[next] - ground[i]) > .001f)
                {
                    var color = C("72976a").Darkened(i % 4 * .025f);
                    SceneryTriangle(canvas, ground[i], ground[next], shoulder[next], color);
                    SceneryTriangle(canvas, ground[i], shoulder[next], shoulder[i], color);
                }
                if ((shoulder[next] - shoulder[i]).Cross(peak - shoulder[i]) > .001f)
                    SceneryTriangle(canvas, shoulder[i], shoulder[next], peak, C(i < 3 ? "91a18a" : "657f72").Darkened(i % 3 * .035f));
            }
            canvas.DrawLine(peak, shoulder[8], C("bac3a7"), .9f, true);
            canvas.DrawLine(peak, shoulder[2], C("536f67"), .7f, true);
            if (item.Snow)
            {
                var a = peak.Lerp(shoulder[4], .25f);
                var b = peak + new Vector2(s * .05f, s * .32f);
                var c = peak.Lerp(shoulder[1], .22f);
                SceneryTriangle(canvas, peak, a, b, C("d7dccb"));
                SceneryTriangle(canvas, peak, b, c, C("d7dccb"));
            }
        }
    }

    private void BuildScenery()
    {
        EnsureIslandGeometry();
        EnsureCosmeticRivers();
        _sceneryProjection = Projection;
        _scenery.Clear();
        var random = new Random(Board.Seed ^ 92173);
        var features = TerrainFeatures.For(Board);
        var towns = Battle.Villages.Select(v => Projection.GridToWorld(v.Position)).ToArray();
        var bounds = Projection.BoardBounds(Board);
        var sandBins = new Dictionary<(int X, int Y), List<(Vector2[] Shape, Rect2 Bounds)>>();
        foreach (var beach in _beaches.Where(b => b.Polygon.Length > 0))
        {
            var box = PolygonBounds(beach.Polygon);
            for (int y = (int)MathF.Floor(box.Position.Y / 64); y <= (int)MathF.Floor(box.End.Y / 64); y++)
                for (int x = (int)MathF.Floor(box.Position.X / 64); x <= (int)MathF.Floor(box.End.X / 64); x++)
                {
                    if (!sandBins.TryGetValue((x, y), out var entries)) sandBins[(x, y)] = entries = new();
                    entries.Add((beach.Polygon, box));
                }
        }
        bool LandPoint(Vector2 p, out GridPosition cell)
        {
            cell = Projection.WorldToGrid(p);
            if (!_landShapes.TryGetValue(cell, out var shapes) || !shapes.Any(shape => Geometry2D.IsPointInPolygon(p, shape))) return false;
            return !sandBins.TryGetValue(((int)MathF.Floor(p.X / 64), (int)MathF.Floor(p.Y / 64)), out var beaches) ||
                !beaches.Any(b => b.Bounds.HasPoint(p) && Geometry2D.IsPointInPolygon(p, b.Shape));
        }

        // Correlated continuous fields determine forests/ridges. A tile is merely
        // the visibility owner of a sample, never the boundary of its grove.
        for (float y = bounds.Position.Y; y < bounds.End.Y; y += 13)
            for (float x = bounds.Position.X; x < bounds.End.X; x += 18)
            {
                var p = new Vector2(x + (float)random.NextDouble() * 15, y + (float)random.NextDouble() * 11);
                if (!LandPoint(p, out var cell))
                    continue;
                if (CosmeticRiverAt(p)) continue;
                _scenery.Add(new(cell, p, 2 + (float)random.NextDouble() * 4, 0, (float)random.NextDouble() - .5f));
                if (towns.Any(town => town.DistanceSquaredTo(p) < 1100))
                    continue;
                var location = new System.Numerics.Vector2(p.X / Projection.TileWidth + p.Y / Projection.TileHeight,
                    p.Y / Projection.TileHeight - p.X / Projection.TileWidth);
                float density = features.MountainCells.Contains(cell) ? 0 : features.ForestDensityAt(location);
                if (density > 0 && random.NextDouble() < density * .72f)
                    _scenery.Add(new(cell, p, 5 + (float)random.NextDouble() * 9, 1, random.Next(3)));
            }

        // Large summits may span neighboring inland cells, but every skirt
        // remains inside real land and away from the beach.
        Scenery Peak(GridPosition cell, Vector2 p, float size)
        {
            var peak = new Scenery(cell, p, size, 2, (float)random.NextDouble() - .5f,
                HeightRatio: 1.35f + (float)random.NextDouble() * .9f);
            bool Fits(Scenery candidate)
            {
                var skirt = MountainFootprint(candidate, p);
                return skirt.SelectMany((point, i) => new[] { point, point.Lerp(skirt[(i + 1) % skirt.Length], .5f) })
                    .All(point => LandPoint(point, out var groundCell) && Board.GetTile(groundCell).Terrain == TerrainType.Land);
            }
            while (size > 1 && !Fits(peak))
            {
                size *= .88f;
                peak = peak with { Size = size };
            }
            return peak;
        }
        foreach (var cell in features.MountainCells.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            var p = Projection.GridToWorld(cell);
            float band = (float)random.NextDouble();
            float size = band < .25f ? 22 + (float)random.NextDouble() * 15 :
                band < .75f ? 37 + (float)random.NextDouble() * 22 : 62 + (float)random.NextDouble() * 28;
            _scenery.Add(Peak(cell, p, size));
            // Smaller connecting peaks belong to the same sight-blocking ridge;
            // they do not create new invisible gameplay obstacles.
            foreach (var next in Board.GetNeighbors(cell).Where(features.MountainCells.Contains)
                .Where(n => n.Y > cell.Y || n.Y == cell.Y && n.X > cell.X).OrderBy(n => n.Y).ThenBy(n => n.X))
            {
                var middle = p.Lerp(Projection.GridToWorld(next), .5f);
                if (!LandPoint(middle, out _)) continue;
                _scenery.Add(Peak(cell, middle, 21 + (float)random.NextDouble() * 13) with { Minor = true });
            }
        }
        var summits = _scenery.Where(p => p.Kind == 2 && !p.Minor).Select(p => p.Size * p.HeightRatio).Order().ToArray();
        float snowLine = summits.Length == 0 ? float.MaxValue : Math.Max(58, summits[(int)((summits.Length - 1) * .82f)]);
        for (int i = 0; i < _scenery.Count; i++)
            if (_scenery[i] is { Kind: 2, Minor: false } peak && peak.Size * peak.HeightRatio >= snowLine)
                _scenery[i] = peak with { Snow = true };
        AddDecorativeRuins(LandPoint);
        AddTreasuryRuins();
        _scenery.Sort((a, b) => a.Point.Y.CompareTo(b.Point.Y));
    }

    private static Vector2[] MountainFootprint(Scenery item, Vector2 p) => Enumerable.Range(0, 12).Select(i =>
    {
        float angle = i * Mathf.Tau / 12;
        float uneven = 1 + .12f * MathF.Sin(i * 2.7f + item.Shade * 5);
        return p + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .38f) * item.Size * uneven;
    }).ToArray();

    private static void SceneryTriangle(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (MathF.Abs((b - a).Cross(c - a)) > .001f)
            canvas.DrawPrimitive(new[] { a, b, c }, new[] { color }, Array.Empty<Vector2>());
    }
}
