using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>One reference-shaped clay vessel: flared mouth, short neck, pear body and curled ears.</summary>
internal static class ClayAmphoraShape
{
    private static readonly Vector2[] Complete = CompleteProfile(0);
    private static readonly Vector2[] Veteran = CompleteProfile(14);
    private static readonly Vector2[] Broken = DamagedProfile(false);
    private static readonly Vector2[] Shard = DamagedProfile(true);
    private static readonly Vector2[] LeftEar = Ear(-1);
    private static readonly Vector2[] RightEar = Ear(1);
    private static readonly Vector2[] TurnedLeftEar = TurnEar(LeftEar, true);
    private static readonly Vector2[] TurnedRightEar = TurnEar(RightEar, false);
    private static readonly Vector2[] Lip = Ellipse(new(0, -30), new(17.4f, 2.3f));
    private static readonly Vector2[] VeteranLip = Ellipse(new(0, -44), new(17.4f, 2.3f));
    private static readonly Vector2[] InnerLip = Ellipse(new(0, -30.5f), new(14.8f, 1.1f));
    private static readonly Vector2[] VeteranInnerLip = Ellipse(new(0, -44.5f), new(14.8f, 1.1f));
    private static readonly Vector2[] Highlight = {
        new(-7, -9), new(-15, -6), new(-21, 0), new(-21, 10), new(-16, 20), new(-13, 24),
        new(-14, 13), new(-12, 1), new(-3, -6)
    };
    private static readonly Vector2[] Shade = {
        new(7, -9), new(17, -4), new(22, 3), new(21, 13), new(15, 24), new(10, 28),
        new(6, 28), new(11, 18), new(13, 3)
    };
    private static readonly Vector2[][][] Highlights = ClippedPieces(Highlight);
    private static readonly Vector2[][][] Shades = ClippedPieces(Shade);
    private static readonly GrainLayer[][] Grain = { BuildGrain(false), BuildGrain(true) };
    private readonly record struct GrainLayer(Vector2[] Segments, bool Dark);
    internal static Vector2[] Profile(int stage, bool veteran = false) =>
        stage < 2 ? veteran ? Veteran : Complete : stage == 2 ? Broken : Shard;

    internal static void Draw(CanvasItem canvas, Color clay, int stage = 0, float alpha = 1,
        bool veteran = false, bool turned = false)
    {
        if (alpha <= .01f) return;
        var shape = Profile(stage, veteran);
        var dark = new Color(clay.Darkened(.30f), alpha);
        var light = new Color(clay.Lightened(.32f), alpha);
        if (stage < 2)
        {
            // The farther ear is partly hidden behind the shoulder; the nearer ear
            // is layered above it after the body to convey the yaw of the large vase.
            DrawEar(canvas, turned ? TurnedLeftEar : LeftEar, clay, alpha, turned);
            if (!turned) DrawEar(canvas, RightEar, clay, alpha, false);
        }
        canvas.DrawColoredPolygon(shape, new Color(clay, alpha));
        canvas.DrawPolyline(shape, dark, .95f, true);
        canvas.DrawLine(shape[^1], shape[0], dark, .95f, true);
        int piece = stage < 2 ? veteran ? 1 : 0 : stage == 2 ? 2 : 3;
        foreach (var polygon in Highlights[piece])
            canvas.DrawColoredPolygon(polygon, new Color(clay.Lightened(.35f), alpha * .54f));
        foreach (var polygon in Shades[piece])
            canvas.DrawColoredPolygon(polygon, new Color(clay.Darkened(.24f), alpha * .79f));
        DrawGrain(canvas, clay, stage, alpha);
        canvas.DrawLine(new(-10, 28), new(9, 28), dark, 1.05f, true);
        if (stage < 2)
        {
            canvas.DrawColoredPolygon(veteran ? VeteranLip : Lip, light);
            canvas.DrawColoredPolygon(veteran ? VeteranInnerLip : InnerLip,
                new Color(clay.Darkened(.47f), alpha));
            float lipY = veteran ? -44 : -30;
            canvas.DrawLine(new(-14, lipY - 1), new(11, lipY - 1),
                new Color(clay.Lightened(.62f), alpha * .84f), .8f, true);
            if (turned) DrawEar(canvas, TurnedRightEar, clay, alpha, false);
            if (veteran)
            {
                var burgundy = new Color(new Color("783541"), alpha);
                canvas.DrawLine(new(-7.5f, -29), new(7.5f, -29), burgundy, 2.8f, true);
                canvas.DrawLine(new(-7.5f, -21), new(7.5f, -21), burgundy, 2.8f, true);
            }
        }
        DrawCracks(canvas, clay, stage, alpha);
    }

    private static void DrawEar(CanvasItem canvas, Vector2[] ear, Color clay, float alpha, bool farther)
    {
        float thickness = farther ? 2.5f : 3.6f;
        canvas.DrawPolyline(ear, new Color(clay.Darkened(.34f), alpha), thickness + .9f, true);
        canvas.DrawPolyline(ear, new Color(clay.Lightened(farther ? .10f : .23f), alpha), thickness, true);
        canvas.DrawPolyline(ear, new Color(clay.Lightened(.60f), alpha * (farther ? .3f : .7f)),
            farther ? .45f : .7f, true);
    }

    private static void DrawGrain(CanvasItem canvas, Color clay, int stage, float alpha)
    {
        // Identical disconnected strokes share one retained submission per tint.
        // The tiny world seals otherwise multiply dozens of commands at every port.
        foreach (var layer in Grain[stage >= 3 ? 1 : 0])
            canvas.DrawMultiline(layer.Segments,
                new Color(layer.Dark ? clay.Darkened(.30f) : clay.Lightened(.48f), alpha * .16f),
                .40f, true);
    }

    private static GrainLayer[] BuildGrain(bool shard)
    {
        var dark = new List<Vector2>();
        var light = new List<Vector2>();
        for (int grain = 0; grain < 48; grain++)
        {
            float x = -18 + grain * 17 % 37;
            float y = (shard ? 3 : -3) + grain * 13 % (shard ? 21 : 29);
            if (Math.Abs(x) > 21 - Math.Max(0, y - 15) * .68f) continue;
            var segments = grain % 4 == 0 ? dark : light;
            segments.Add(new(x, y));
            segments.Add(new(x + .8f + grain % 3, y + .25f));
        }
        return new[] { new GrainLayer(dark.ToArray(), true), new GrainLayer(light.ToArray(), false) };
    }

    private static void DrawCracks(CanvasItem canvas, Color clay, int stage, float alpha)
    {
        var crack = new Color(clay.Darkened(.55f), alpha * .85f);
        if (stage >= 1)
            canvas.DrawPolyline(new[] { new Vector2(2, -19), new Vector2(-2, -10),
                new Vector2(4, -6), new Vector2(0, 0) }, crack, 1.4f, true);
        if (stage >= 2)
        {
            canvas.DrawPolyline(new[] { new Vector2(-16, 3), new Vector2(-8, 8), new Vector2(-10, 18) }, crack, 1.4f, true);
            canvas.DrawPolyline(new[] { new Vector2(17, 0), new Vector2(11, 7), new Vector2(15, 15) }, crack, 1.3f, true);
        }
        if (stage >= 3) canvas.DrawLine(new(-4, 15), new(3, 22), crack, 1.3f, true);
    }

    private static Vector2[] CompleteProfile(float extension)
    {
        var p = new Outline(new(-17.5f, -29 - extension));
        p.Curve(new(-18, -33 - extension), new(18, -33 - extension), new(17.5f, -29 - extension), 12);
        p.Curve(new(14, -25 - extension), new(8, -22 - extension), new(7.5f, -18 - extension));
        p.Line(new(7.5f, -11));
        p.Curve(new(12, -9), new(22, -7), new(23, 1));
        p.Curve(new(25, 10), new(20, 23), new(12, 28));
        p.Curve(new(7, 30), new(-7, 30), new(-12, 28));
        p.Curve(new(-20, 23), new(-25, 10), new(-23, 1));
        p.Curve(new(-22, -7), new(-12, -9), new(-7.5f, -11));
        p.Line(new(-7.5f, -18 - extension));
        p.Curve(new(-8, -22 - extension), new(-14, -25 - extension), p.First, close: true);
        return p.Points.ToArray();
    }

    private static Vector2[] DamagedProfile(bool shard)
    {
        var p = new Outline(shard ? new(-18, -3) : new(-8, -11));
        if (shard)
        {
            p.Line(new(-12, -5)); p.Line(new(-7, -1)); p.Line(new(-2, -5));
            p.Line(new(4, -2)); p.Line(new(12, -4)); p.Line(new(20, 0));
        }
        else
        {
            p.Line(new(-4, -7)); p.Line(new(-1, -12)); p.Line(new(5, -8)); p.Line(new(8, -11));
            p.Curve(new(14, -8), new(22, -6), new(23, 1));
        }
        p.Curve(new(25, 10), new(20, 23), new(12, 28));
        p.Curve(new(7, 30), new(-7, 30), new(-12, 28));
        p.Curve(new(-20, 23), new(-25, 10), new(-23, 1));
        p.Curve(new(-22, -5), new(-14, -7), p.First, close: true);
        return p.Points.ToArray();
    }

    private static Vector2[] Ear(int side)
    {
        var p = new Outline(new(side * 11, -21));
        p.Curve(new(side * 10, -20), new(side * 13, -18), new(side * 14, -20), 5);
        p.Curve(new(side * 15, -23), new(side * 11, -25), new(side * 9, -22), 6);
        p.Curve(new(side * 13, -29), new(side * 20, -28), new(side * 21, -20), 9);
        p.Curve(new(side * 23, -12), new(side * 14, -7), new(side * 18, -2), 9);
        p.Curve(new(side * 21, 3), new(side * 27, 0), new(side * 25, -3), 8);
        p.Curve(new(side * 24, -5), new(side * 21, -4), new(side * 22, -2), 7);
        p.Curve(new(side * 23, -1), new(side * 24, -2), new(side * 23, -3), 5);
        return p.Points.ToArray();
    }

    private static Vector2[] TurnEar(Vector2[] source, bool farther)
    {
        var target = new Vector2[source.Length];
        for (int i = 0; i < target.Length; i++)
            target[i] = source[i] * new Vector2(farther ? .77f : 1.05f, farther ? .90f : 1.03f)
                + new Vector2(farther ? -1 : 1, farther ? -3 : .5f);
        return target;
    }

    private static Vector2[] Ellipse(Vector2 center, Vector2 radius)
    {
        var points = new Vector2[32];
        for (int i = 0; i < points.Length; i++) points[i] = center + Vector2.FromAngle(i * Mathf.Tau / points.Length) * radius;
        return points;
    }

    private static Vector2[][][] ClippedPieces(Vector2[] art)
    {
        var result = new Vector2[4][][];
        for (int i = 0; i < result.Length; i++)
        {
            var cuts = Geometry2D.IntersectPolygons(art, i == 1 ? Veteran : Profile(i == 0 ? 0 : i));
            result[i] = new Vector2[cuts.Count][];
            for (int cut = 0; cut < cuts.Count; cut++) result[i][cut] = cuts[cut];
        }
        return result;
    }

    private sealed class Outline
    {
        internal List<Vector2> Points { get; } = new();
        internal Vector2 First => Points[0];
        private Vector2 Last => Points[^1];
        internal Outline(Vector2 from) => Points.Add(from);
        internal void Line(Vector2 to) => Points.Add(to);
        internal void Curve(Vector2 a, Vector2 b, Vector2 to, int steps = 8, bool close = false)
        {
            var from = Last;
            for (int i = 1; i <= steps - (close ? 1 : 0); i++)
            {
                float t = i / (float)steps, u = 1 - t;
                Points.Add(from * (u * u * u) + a * (3 * u * u * t) + b * (3 * u * t * t) + to * (t * t * t));
            }
        }
    }
}
