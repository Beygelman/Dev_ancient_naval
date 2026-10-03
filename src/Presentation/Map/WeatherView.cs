using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class WorldAmbience
{
    private static readonly Vector2[] CloudFacet =
    {
        new(-3, -1),
        new(-1, -1),
        new(-1, -2),
        new(1, -2),
        new(1, -1),
        new(3, -1),
        new(3, 1),
        new(2, 1),
        new(2, 2),
        new(0, 2),
        new(0, 1),
        new(-2, 1),
        new(-2, 0),
        new(-3, 0)
    };
    private readonly Vector2[] _cloudTop = new Vector2[CloudFacet.Length];
    private readonly Vector2[] _cloudSide = new Vector2[4];
    internal const float CloudThickness = 16;
    internal const float CloudAltitude = 132;
    private BoardTerrainLayer? _sky;
    public override void _Ready()
    {
        AddChild(_waves);
        AddChild(_treasuryGlow);
        _sky = new BoardTerrainLayer
        {
            Name = "HighClouds",
            ZIndex = 3,
            DrawWorld = DrawSky
        };
        AddChild(_sky);
    }

    private bool VisibleSurface(Vector2 point)
    {
        var cell = BoardView.Projection.WorldToGrid(point);
        return _battle!.Board.Contains(cell) && _battle.Vision.IsVisible(DevAncientNaval.Core.Units.Side.Player, cell);
    }

    private void DrawSky(Node2D canvas)
    {
        DrawClouds(canvas, shadows: false);
        DrawGulls(canvas, shadows: false);
    }

    private void DrawClouds(Node2D canvas, bool shadows)
    {
        if (_battle is null)
            return;
        var bounds = _seaBounds;
        for (int cloud = 0; cloud < 5; cloud++)
        {
            float x = Mathf.PosMod(cloud * bounds.Size.X * .27f + _time * 4 + _battle.Board.Seed % 701, bounds.Size.X);
            var center = new Vector2(bounds.Position.X + x, bounds.Position.Y + bounds.Size.Y * (.13f + cloud * .17f));
            if (!_drawBounds.Grow(160).HasPoint(center) || !VisibleSurface(center))
                continue;
            if (shadows)
            {
                canvas.DrawSetTransform(center, 0, new Vector2(31, 16));
                canvas.DrawColoredPolygon(CloudFacet, new Color(.02f, .08f, .1f, .085f));
            }
            else
            {
                // A single connected square footprint has shaded extruded walls.
                // Faces share their boundaries, avoiding dark overlapping cloud discs.
                canvas.DrawSetTransform(center + new Vector2(-36, -CloudAltitude));
                for (int vertex = 0; vertex < CloudFacet.Length; vertex++)
                    _cloudTop[vertex] = CloudFacet[vertex] * new Vector2(31, 16) + new Vector2(-5, -CloudThickness);
                for (int edge = 0; edge < CloudFacet.Length; edge++)
                {
                    int next = (edge + 1) % CloudFacet.Length;
                    var a = CloudFacet[edge] * new Vector2(31, 16);
                    var b = CloudFacet[next] * new Vector2(31, 16);
                    if ((b - a).Cross(_cloudTop[next] - a) <= .025f)
                        continue;
                    _cloudSide[0] = a;
                    _cloudSide[1] = b;
                    _cloudSide[2] = _cloudTop[next];
                    _cloudSide[3] = _cloudTop[edge];
                    canvas.DrawColoredPolygon(_cloudSide, MathF.Abs(b.X - a.X) > 1
                        ? new Color(.72f, .83f, .82f, .22f)
                        : new Color(.62f, .75f, .76f, .19f));
                }
                canvas.DrawColoredPolygon(_cloudTop, new Color(.95f, .97f, .91f, .24f));
                canvas.DrawLine(_cloudTop[2], _cloudTop[3], new Color(1, 1, .94f, .18f), 1, true);
            }
            canvas.DrawSetTransform(Vector2.Zero);
        }
    }

    private void DrawGulls(Node2D canvas, bool shadows)
    {
        foreach (var gull in _gulls)
        {
            float age = _time - gull.Born;
            var water = gull.Circling ? gull.Start + new Vector2(MathF.Cos(age * .38f + gull.Phase) * 28, MathF.Sin(age * .38f + gull.Phase) * 12) : gull.Start + gull.Velocity * age + new Vector2(MathF.Sin(age * .24f + gull.Phase) * 13, 0);
            if (!_drawBounds.HasPoint(water) || !VisibleWater(water))
                continue;
            float fade = Math.Min(1, Math.Min(age, gull.Lifetime - age));
            if (shadows)
            {
                canvas.DrawSetTransform(water, 0, new Vector2(1, .35f));
                canvas.DrawCircle(Vector2.Zero, 5, new Color(.04f, .12f, .17f, .2f * fade));
                canvas.DrawSetTransform(Vector2.Zero);
            }
            else
            {
                var bird = water + new Vector2(0, -38 - MathF.Sin(age * .4f + gull.Phase) * 5);
                SetGullWings(bird, MathF.Sin(age * 4.2f + gull.Phase) * 2.6f, 7, 3);
                canvas.DrawPolyline(_gullWings, new Color(.95f, .96f, .86f, .85f * fade), 1.8f, true);
            }
        }

        foreach (var dock in _docks)
        {
            var center = dock.Center;
            if (!_drawBounds.HasPoint(center))
                continue;
            for (int i = 0; i < 3; i++)
            {
                float phase = _time * .43f + i * 1.7f + dock.Id;
                var water = center + new Vector2(MathF.Cos(phase) * (25 + i * 4), MathF.Sin(phase) * 14);
                if (!VisibleWater(water))
                    continue;
                if (shadows)
                {
                    canvas.DrawSetTransform(water, 0, new Vector2(1, .32f));
                    canvas.DrawCircle(Vector2.Zero, 4, new Color(0, .08f, .1f, .2f));
                    canvas.DrawSetTransform(Vector2.Zero);
                }
                else
                {
                    var bird = water + new Vector2(0, -37 - i * 4);
                    SetGullWings(bird, MathF.Sin(_time * 4.8f + i) * 2.4f, 6, 2);
                    canvas.DrawPolyline(_gullWings, new Color("f2f0dc"), 1.7f, true);
                }
            }
        }
    }
}
