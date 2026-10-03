using System;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Two clay seals share their cracks and motion; floor rotation never rotates their digits.</summary>
internal static class AmphoraBadgeArt
{
    private static readonly Vector2[] Complete =
    {
        new(-4, -12), new(4, -12), new(4, -7), new(9, -3), new(10, 3),
        new(7, 9), new(3, 12), new(-3, 12), new(-7, 9), new(-10, 3), new(-9, -3), new(-4, -7)
    };
    private static readonly Vector2[] Broken =
    {
        new(-6, -7), new(-2, -4), new(1, -8), new(5, -5), new(8, -3), new(10, 3),
        new(7, 9), new(3, 12), new(-3, 12), new(-7, 9), new(-10, 3), new(-8, -2)
    };
    private static readonly Vector2[] VeteranComplete =
    {
        new(-4, -18), new(4, -18), new(4, -7), new(9, -3), new(10, 3),
        new(7, 9), new(3, 12), new(-3, 12), new(-7, 9), new(-10, 3), new(-9, -3), new(-4, -7)
    };
    private static readonly Vector2[] Shard =
    {
        new(-7, -5), new(-3, -7), new(0, -4), new(4, -6), new(8, -2),
        new(7, 5), new(3, 8), new(1, 11), new(-4, 9), new(-8, 4)
    };
    private static readonly Color Beige = new("f1ddba");
    internal static Vector2 DigitAnchor(Vector2 center) => center + new Vector2(0, 4);
    internal static int Stage(double health, double maximum)
    {
        double ratio = maximum <= 0 ? 0 : Math.Clamp(health / maximum, 0, 1);
        return ratio > .75 ? 0 : ratio > .5 ? 1 : ratio > .25 ? 2 : 3;
    }

    internal static void Draw(CanvasItem canvas, Vector2 center, double health, double maximum, Color owner, ShipClass? kind, AmphoraMotion motion, bool veteran = false)
    {
        center += motion.Shake;
        var rear = center + new Vector2(-11, -17);
        Jug(canvas, rear, new Color("bca77f"), motion, veteran);
        Glyph(canvas, rear + new Vector2(0, 1), kind, new Color("5f4d35"));
        Jug(canvas, center, owner.Darkened(.30f), motion, veteran);
        string text = Math.Max(0, health).ToString("0");
        int size = text.Length > 2 ? 10 : 12;
        float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: size).X;
        canvas.DrawString(ThemeDB.FallbackFont, DigitAnchor(center) - new Vector2(width * .5f, 0), text, fontSize: size, modulate: Beige);
    }

    private static void Jug(CanvasItem canvas, Vector2 center, Color clay, AmphoraMotion motion, bool veteran)
    {
        if (motion.Healing && motion.PreviousStage > motion.Stage && motion.Restore < 1)
        {
            Body(canvas, center, clay, motion.PreviousStage, 1, veteran);
            Body(canvas, center, clay, motion.Stage, motion.Restore, veteran);
            Fragments(canvas, center, clay.Lightened(.2f), motion.PreviousStage - motion.Stage, (1 - motion.Restore) * 16, 1 - motion.Restore);
        }
        else
            Body(canvas, center, clay, motion.Stage, 1, veteran);
        if (!motion.Healing && motion.Progress < 1 && motion.Stage > motion.PreviousStage)
            Fragments(canvas, center, clay.Lightened(.2f), motion.Stage - motion.PreviousStage, motion.Progress * 23, 1 - motion.Progress);
        if (motion.Flash > .015f)
        {
            float light = motion.Flash;
            canvas.DrawLine(center + new Vector2(-5, -7), center + new Vector2(-3, -2), new Color(1, .94f, .73f, light), 1.5f, true);
            canvas.DrawLine(center + new Vector2(-7, -3), center + new Vector2(-1, -6), new Color(1, .94f, .73f, light), 1.1f, true);
            canvas.DrawArc(center + new Vector2(0, -1), 11, -2.2f, -.8f, 10, new Color(1, .93f, .72f, light * .6f), 1, true);
        }
    }

    private static void Body(CanvasItem canvas, Vector2 center, Color clay, int stage, float alpha, bool veteran)
    {
        if (alpha <= .01f)
            return;
        var silhouette = stage <= 1 ? Complete : stage == 2 ? Broken : Shard;
        if (veteran && stage <= 1)
            silhouette = VeteranComplete;
        canvas.DrawSetTransform(center);
        canvas.DrawColoredPolygon(silhouette, new Color(clay, alpha));
        for (int edge = 0; edge < silhouette.Length; edge++)
            canvas.DrawLine(silhouette[edge], silhouette[(edge + 1) % silhouette.Length], new Color(clay.Lightened(.2f), alpha * .8f), .55f, true);
        // The lit shoulder and dark base give the small seal a clay volume.
        canvas.DrawColoredPolygon(new[] { new Vector2(-8, -2), new Vector2(-4, -5), new Vector2(-3, 6), new Vector2(-6, 8), new Vector2(-8, 3) }, new Color(clay.Lightened(.15f), alpha));
        canvas.DrawLine(new Vector2(-5, 9), new Vector2(4, 10), new Color(clay.Darkened(.26f), alpha), 1.2f, true);
        if (stage < 2)
        {
            float lip = veteran ? -18 : -12;
            canvas.DrawLine(new Vector2(-5, lip), new Vector2(5, lip), new Color(clay.Lightened(.3f), alpha), 2, true);
            if (veteran)
            {
                var burgundy = new Color(new Color("783541"), alpha);
                canvas.DrawLine(new Vector2(-4, -14), new Vector2(4, -14), burgundy, 1.5f, true);
                canvas.DrawLine(new Vector2(-4, -10), new Vector2(4, -10), burgundy, 1.5f, true);
            }
            canvas.DrawArc(new Vector2(-8, -4), 4, 1.5f, 4.6f, 10, new Color(clay.Darkened(.16f), alpha), 1.8f, true);
            canvas.DrawArc(new Vector2(8, -4), 4, -1.5f, 1.6f, 10, new Color(clay.Darkened(.16f), alpha), 1.8f, true);
        }
        var crack = new Color(clay.Darkened(.55f), alpha * .85f);
        if (stage >= 1)
            canvas.DrawPolyline(new[] { new Vector2(1, -9), new Vector2(-1, -5), new Vector2(2, -3), new Vector2(0, 0) }, crack, .85f, true);
        if (stage >= 2)
        {
            canvas.DrawPolyline(new[] { new Vector2(-7, 2), new Vector2(-3, 4), new Vector2(-4, 8) }, crack, .8f, true);
            canvas.DrawPolyline(new[] { new Vector2(7, 0), new Vector2(5, 3), new Vector2(7, 6) }, crack, .8f, true);
        }
        if (stage >= 3)
            canvas.DrawLine(new Vector2(-2, 6), new Vector2(1, 9), crack, .8f, true);
        canvas.DrawSetTransform(Vector2.Zero);
    }

    private static void Fragments(CanvasItem canvas, Vector2 center, Color clay, int count, float spread, float alpha)
    {
        for (int i = 0; i < Math.Min(8, count * 3); i++)
        {
            float phase = i * 2.4f;
            var direction = new Vector2(MathF.Cos(phase), MathF.Sin(phase) * .6f);
            var at = center + direction * (12 + spread) + new Vector2(0, spread * spread * .025f);
            canvas.DrawColoredPolygon(new[] { at + new Vector2(-1, -2), at + new Vector2(2, -1), at + new Vector2(0, 2) }, new Color(clay, alpha));
        }
    }

    private static void Glyph(CanvasItem canvas, Vector2 center, ShipClass? kind, Color ink) =>
        DevAncientNaval.Presentation.UI.NavalGlyphArt.Draw(canvas, center,
            DevAncientNaval.Presentation.UI.NavalGlyphArt.Symbol(kind), ink);
}
