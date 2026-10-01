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
    internal int MountainCount => _scenery.Count(item => item.Kind == 2);

    private sealed record Scenery(GridPosition Cell, Vector2 Point, float Size, int Kind, float Shade);
    private void DrawIslandScenery(Node2D canvas, ISet<GridPosition> cells)
    {
        if (!ReferenceEquals(_sceneryProjection, Projection))
            BuildScenery();
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
        else
        {
            var peak = p + new Vector2(s * item.Shade * .2f, -s * 1.65f);
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-s, 0), peak, p + new Vector2(s, s * .2f), p + new Vector2(0, s * .35f) }, C("748d7b"));
            canvas.DrawColoredPolygon(new[] { peak, p + new Vector2(s, s * .2f), p + new Vector2(s * .12f, s * .08f) }, C("536f67"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-s * .6f, -s * .2f), peak, p + new Vector2(-s * .15f, -s * .2f) }, C("a6b29a"));
            if (s > 19)
                canvas.DrawColoredPolygon(new[] { peak, peak.Lerp(p + new Vector2(-s, 0), .24f), peak + new Vector2(0, s * .26f), peak.Lerp(p + new Vector2(s, s * .2f), .23f) }, C("d9decb"));
        }
    }

    private void BuildScenery()
    {
        EnsureIslandGeometry();
        _sceneryProjection = Projection;
        _scenery.Clear();
        var random = new Random(Board.Seed ^ 92173);
        var towns = Battle.Villages.Select(v => Projection.GridToWorld(v.Position)).ToArray();
        var bounds = Projection.BoardBounds(Board);
        bool LandPoint(Vector2 p, out GridPosition cell)
        {
            cell = Projection.WorldToGrid(p);
            return _landShapes.TryGetValue(cell, out var shapes) && shapes.Any(shape => Geometry2D.IsPointInPolygon(p, shape));
        }

        // Correlated continuous fields determine forests/ridges. A tile is merely
        // the visibility owner of a sample, never the boundary of its grove.
        for (float y = bounds.Position.Y; y < bounds.End.Y; y += 14)
            for (float x = bounds.Position.X; x < bounds.End.X; x += 20)
            {
                var p = new Vector2(x + (float)random.NextDouble() * 15, y + (float)random.NextDouble() * 11);
                if (!LandPoint(p, out var cell))
                    continue;
                _scenery.Add(new(cell, p, 2 + (float)random.NextDouble() * 4, 0, (float)random.NextDouble() - .5f));
                if (towns.Any(town => town.DistanceSquaredTo(p) < 1100))
                    continue;
                float field = MathF.Sin(p.X * .018f + Board.Seed) + MathF.Cos(p.Y * .022f) + .5f * MathF.Sin((p.X + p.Y) * .009f);
                if (field > -.55f || random.Next(14) == 0)
                    _scenery.Add(new(cell, p, 5 + (float)random.NextDouble() * 9, 1, random.Next(3)));
            }

        for (float y = bounds.Position.Y; y < bounds.End.Y; y += 34)
            for (float x = bounds.Position.X; x < bounds.End.X; x += 47)
            {
                var p = new Vector2(x + (float)random.NextDouble() * 25, y + (float)random.NextDouble() * 20);
                if (!LandPoint(p, out var cell) || towns.Any(t => t.DistanceSquaredTo(p) < 2000))
                    continue;
                float ridge = MathF.Abs(MathF.Sin(p.X * .009f + MathF.Sin(p.Y * .014f + Board.Seed) * 1.6f));
                if (ridge > .57f && random.Next(12) != 0)
                    continue;
                // A peak's footprint stays on land, while its height may overlap
                // the next tile exactly as a real continuous mountain chain does.
                if (!LandPoint(p + new Vector2(-16, 0), out _) || !LandPoint(p + new Vector2(16, 0), out _))
                    continue;
                _scenery.Add(new(cell, p, 22 + (float)random.NextDouble() * 25, 2, (float)random.NextDouble() - .5f));
            }

        _scenery.Sort((a, b) => a.Point.Y.CompareTo(b.Point.Y));
    }
}
