using System;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Two clay seals share their cracks and motion; floor rotation never rotates their digits.</summary>
internal static class AmphoraBadgeArt
{
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
        // Keep the public popup anchor fixed; only the numeral painted on the new
        // rounder belly sits slightly lower, away from a damaged rim.
        canvas.DrawString(ThemeDB.FallbackFont, DigitAnchor(center) + new Vector2(-width * .5f, 3), text, fontSize: size, modulate: Beige);
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
        canvas.DrawSetTransform(center, 0, Vector2.One * .40f);
        ClayAmphoraShape.Draw(canvas, clay, stage, alpha, veteran);
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
