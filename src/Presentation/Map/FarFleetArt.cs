using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>One distant model tier. It preserves a class-specific solid silhouette
/// and faction paint, but omits sub-pixel planks, windows, ropes and clay grain.</summary>
internal static class FarFleetArt
{
    internal static void Draw(CanvasItem canvas, SeaGeometryBatch batch, ShipSnapshot ship, Vector2 center,
        float yaw, Color accent)
    {
        batch.Clear();
        var profile = ShipVisualProfile.For(ship.Class);
        Vector2 P(float x, float y, float z) => center + DeckProjection.Point(x, y, z, yaw, profile.Size, profile.DeckWidth) + new Vector2(0, -5);
        void Polygon(Color color, params Vector2[] p) => batch.Polygon(p, color, Transform2D.Identity);
        void Box(float x, float y, float width, float depth, float height, Color color)
        {
            var a = P(x, y, 2); var b = P(x + width, y, 2); var c = P(x + width, y + depth, 2); var d = P(x, y + depth, 2);
            var lift = new Vector2(0, -height * profile.Size);
            Polygon(color.Darkened(.28f), a, b, b + lift, a + lift);
            Polygon(color.Darkened(.16f), b, c, c + lift, b + lift);
            Polygon(color.Lightened(.12f), a + lift, b + lift, c + lift, d + lift);
        }
        if (ship.Class == ShipClass.Balloon)
        {
            var top = center + new Vector2(0, -62);
            Polygon(accent.Darkened(.25f), top + new Vector2(-19, -12), top + new Vector2(-19, 7), top + new Vector2(0, 24), top + new Vector2(0, -25));
            Polygon(accent.Lightened(.18f), top + new Vector2(0, -25), top + new Vector2(19, -12), top + new Vector2(19, 7), top + new Vector2(0, 24));
            Polygon(new Color("a89570"), top + new Vector2(-6, 34), top + new Vector2(6, 34), top + new Vector2(5, 43), top + new Vector2(-5, 43));
            batch.Line(top + new Vector2(-10, 19), top + new Vector2(-5, 35), new Color("dbc897"));
            batch.Line(top + new Vector2(10, 19), top + new Vector2(5, 35), new Color("dbc897"));
        }
        else if (ship.Class is ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)
        {
            Box(-13, -10, 26, 20, ship.Class == ShipClass.Lighthouse ? 43 : 22, new Color("bdba9c"));
            Box(-10, -8, 20, 16, ship.Class == ShipClass.Lighthouse ? 49 : 27, accent);
            if (ship.Class != ShipClass.Lighthouse)
                Polygon(new Color("4a5a59"), P(0, -4, 32), P(24, -4, 28), P(24, 4, 28), P(0, 4, 32));
        }
        else if (ship.Class == ShipClass.FishingDock)
        {
            for (int pier = 0; pier < 5; pier++)
            {
                float angle = -MathF.PI * .8f + pier * MathF.PI * .4f;
                var a = center + Vector2.FromAngle(angle) * new Vector2(18, 10);
                var b = center + Vector2.FromAngle(angle) * new Vector2(31, 17);
                var side = (b - a).Normalized().Orthogonal() * 3;
                Polygon(new Color("c5b080"), a - side, b - side, b + side, a + side);
            }
            Box(-8, -8, 16, 14, 12, accent);
        }
        else
        {
            void Hull(float offset, float width)
            {
                Polygon(new Color("544c3b"), P(-28, offset - width, -5), P(16, offset - width, -5), P(32, offset, -5), P(16, offset + width, -5), P(-28, offset + width, -5));
                Polygon(accent.Darkened(.15f), P(-27, offset - width, 0), P(15, offset - width, 0), P(32, offset, 0), P(15, offset + width, 0), P(-27, offset + width, 0));
                Polygon(new Color("d7c398"), P(-23, offset - width + 2, 2), P(14, offset - width + 2, 2), P(27, offset, 2), P(14, offset + width - 2, 2), P(-23, offset + width - 2, 2));
            }
            if (ship.Class == ShipClass.Mothership)
            {
                Hull(-11, 7); Hull(11, 7);
                Polygon(new Color("c6b38b"), P(-26, -13, 3), P(21, -13, 3), P(27, 0, 3), P(21, 13, 3), P(-26, 13, 3));
                Box(-19, -9, 12, 10, 12, new Color("d5c9a6"));
                Box(-3, -7, 12, 12, 24, new Color("e7dec1"));
                Box(11, 1, 11, 9, 16, new Color("c8b891"));
                Polygon(accent, P(-2, -7, 29), P(10, -7, 29), P(10, 5, 29), P(-2, 5, 29));
            }
            else
            {
                Hull(0, 10);
                if (ship.Class == ShipClass.Togus)
                {
                    Box(-12, -6, 14, 12, 9, new Color("8d947e"));
                    Polygon(new Color("50585b"), P(-3, -4, 14), P(15, -4, 22), P(15, 4, 22), P(-3, 4, 14));
                }
                else
                {
                    int masts = ship.Class == ShipClass.Kolonel ? 3 : ship.Class == ShipClass.Invader ? 2 : 1;
                    for (int mast = 0; mast < masts; mast++)
                    {
                        float x = (mast - (masts - 1) * .5f) * 15;
                        batch.Line(P(x, 0, 3), P(x, 0, 34), new Color("786749"));
                        Polygon(new Color("f1e3bb"), P(x, -12, 32), P(x, 12, 32), P(x, 10, 12), P(x, -8, 12));
                        Polygon(accent, P(x, -12, 32), P(x, 12, 32), P(x, 12, 28), P(x, -12, 28));
                    }
                }
            }
        }
        batch.Submit(canvas, 1.5f);
    }

    internal static void Health(CanvasItem canvas, Vector2 center, ShipSnapshot ship, Color accent, AmphoraMotion motion)
        => Health(canvas, center, ship.Health, ship.MaxHealth, ship.Class, accent, motion);

    internal static void Health(CanvasItem canvas, Vector2 center, double health, double maximum, ShipClass? kind, Color accent, AmphoraMotion motion)
    {
        // Even at chart scale health retains the two clay-vessel vocabulary and
        // the exact observed digits. Only invisible decorative grain is omitted.
        var anchor = center + motion.Shake;
        canvas.DrawSetTransform(anchor);
        int stage = motion.Healing && motion.Restore < .5f ? motion.PreviousStage : motion.Stage;
        var front = stage switch
        {
            1 => new[] { new Vector2(-7, -8), new(-4, -5), new(-10, 0), new(-8, 9), new(0, 12), new(8, 9), new(8, 0), new(3, -2), new(1, -8) },
            2 => new[] { new Vector2(-8, -3), new(-10, 0), new(-8, 9), new(0, 12), new(8, 9), new(7, 0), new(2, 3), new(-2, -2) },
            3 => new[] { new Vector2(-6, -2), new(-7, 6), new(-3, 10), new(5, 9), new(7, 3), new(3, -3), new(0, 1) },
            _ => new[] { new Vector2(-7, -8), new(-4, -5), new(-10, 0), new(-8, 9), new(0, 12), new(8, 9), new(10, 0), new(4, -5), new(7, -8) }
        };
        var rear = front.SelectPoint(new Vector2(-9, -14));
        canvas.DrawColoredPolygon(rear, new Color("d5c294"));
        var clay = accent.Darkened(.2f).Lerp(new Color("e8dfbd"), motion.Flash * .32f);
        canvas.DrawColoredPolygon(front, clay);
        double ratio = maximum > 0 ? health / maximum : 0;
        if (ratio <= .75f) canvas.DrawPolyline(new[] { new Vector2(3, -4), new(-1, 1), new(4, 5), new(1, 9) }, new Color("332c33"), 1, true);
        DevAncientNaval.Presentation.UI.NavalGlyphArt.Draw(canvas, new Vector2(-9, -14),
            DevAncientNaval.Presentation.UI.NavalGlyphArt.Symbol(kind), new Color("5d513b"));
        var text = Math.Max(0, health).ToString("0");
        var width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 12).X;
        canvas.DrawString(ThemeDB.FallbackFont, new Vector2(-width * .5f, 7), text, fontSize: 12, modulate: new Color("f3dfb4"));
        canvas.DrawSetTransform(Vector2.Zero);
    }

    private static Vector2[] SelectPoint(this Vector2[] points, Vector2 offset)
    {
        var shifted = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) shifted[i] = points[i] + offset;
        return shifted;
    }
}
