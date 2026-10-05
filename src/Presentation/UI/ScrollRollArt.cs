using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>The same rolled edge proportions on large ceremonies and compact scroll menus.</summary>
internal static class ScrollRollArt
{
    internal static Rect2 RevealRect(Vector2 size, float reveal)
    {
        float top = size.Y * (1 - Math.Clamp(reveal, 0, 1)) * .5f;
        return new Rect2(new Vector2(0, top), new Vector2(size.X, Math.Max(0, size.Y - top * 2)));
    }

    internal static float Radius(Vector2 size, float reveal)
    {
        float bare = Math.Clamp(MathF.Sqrt(Math.Max(1, size.X * size.Y)) * .021f, 4.8f, 12);
        // Wound paper adds cross-sectional area; long sheets naturally have thicker closed rolls.
        return MathF.Sqrt(bare * bare + Math.Max(0, size.Y) * .85f * (1 - reveal) / MathF.PI);
    }

    internal static float UpperRollY(Vector2 size, float reveal) => RevealRect(size, reveal).Position.Y
        - Radius(size, reveal) * (1 - reveal);

    internal static float LowerRollY(Vector2 size, float reveal) => RevealRect(size, reveal).End.Y
        + Radius(size, reveal) * (1 - reveal);

    internal static void Draw(CanvasItem canvas, Vector2 size, float reveal, bool handles = false,
        FleetColor nation = FleetColor.Blue, float rotationPhase = 0)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        float radius = Radius(size, reveal);
        foreach (float y in new[] { UpperRollY(size, reveal), LowerRollY(size, reveal) })
            DrawRoll(canvas, size.X, y, radius, handles, nation, rotationPhase);
    }

    private static void DrawRoll(CanvasItem canvas, float width, float y, float radius,
        bool handles, FleetColor nation, float phase)
    {
        canvas.DrawRect(new Rect2(-2, y - radius + 3, width + 4, radius * 2), new Color(.13f, .08f, .03f, .18f));
        // Eight narrow tones suggest a solid, glossy cylindrical lip, rather than a flat strip.
        for (int band = 0; band < 12; band++)
        {
            float t = band / 11f;
            float lighting = .42f + .43f * MathF.Sin(t * MathF.PI);
            Color shade = new Color("a98b53").Lerp(new Color("f5e7c1"), lighting);
            if (t > .74f) shade = shade.Darkened((t - .74f) * .5f);
            canvas.DrawRect(new Rect2(0, y - radius + t * radius * 2, width, radius * 2 / 11 + .7f), shade);
        }
        canvas.DrawLine(new(0, y - radius * .47f), new(width, y - radius * .47f), new Color("fbefcf"), 1.5f, true);
        canvas.DrawLine(new(0, y + radius * .82f), new(width, y + radius * .82f), new Color("967644"), 1.1f, true);
        for (int i = 0; i < 18; i++)
        {
            float x = 8 + i * (width - 16) / 17;
            float bend = MathF.Sin(i * 1.37f + phase) * 2;
            canvas.DrawLine(new(x, y - radius * .8f), new(x + bend, y + radius * .65f),
                new Color(.4f, .25f, .10f, i % 3 == 0 ? .15f : .07f), .7f, true);
        }
        foreach (int side in new[] { -1, 1 })
        {
            float x = side < 0 ? 0 : width;
            canvas.DrawSetTransform(new(x, y), 0, new Vector2(.47f, 1));
            canvas.DrawCircle(Vector2.Zero, radius, new Color("b89b62"));
            canvas.DrawArc(Vector2.Zero, radius * .70f, -.7f, 5.6f, 30, new Color("7f6136"), 1, true);
            canvas.DrawArc(Vector2.Zero, radius * .36f, -.7f, 5.3f, 24, new Color("f5e4b7"), 1, true);
            canvas.DrawSetTransform(Vector2.Zero);
            if (handles) DrawHandle(canvas, new(x, y), side, radius, FleetPalette.Color(nation));
        }
    }

    private static void DrawHandle(CanvasItem canvas, Vector2 at, int side, float radius, Color tint)
    {
        var end = at + new Vector2(side * (radius + 10), -radius * .15f);
        canvas.DrawLine(at, end, tint.Darkened(.45f), radius * .9f, true);
        canvas.DrawLine(at + new Vector2(0, -radius * .19f), end + new Vector2(0, -radius * .19f),
            tint.Lightened(.15f), radius * .35f, true);
        canvas.DrawSetTransform(end, -.12f * side, new Vector2(.65f, 1));
        canvas.DrawCircle(Vector2.Zero, radius * .79f, tint.Darkened(.25f));
        canvas.DrawCircle(new(-radius * .16f, -radius * .18f), radius * .65f, tint);
        canvas.DrawArc(new(-radius * .16f, -radius * .18f), radius * .49f, 3.3f, 5.3f, 16,
            tint.Lightened(.65f), radius * .13f, true);
        canvas.DrawSetTransform(Vector2.Zero);
    }
}
