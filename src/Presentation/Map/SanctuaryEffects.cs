using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Bounded visible-only ritual effects; no emitters, textures or simulation random draws.</summary>
internal static partial class FactionSanctuaryArt
{
    internal const int MaximumShrineParticles = 18;
    private static readonly Vector2[] EffectTriangle = new Vector2[3];
    private static readonly Color[] EffectInk = new Color[1];

    // The shared drawing buffers are consumed immediately by Godot on its main thread.
    private static void LightFace(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        if (MathF.Abs((b - a).Cross(c - a)) < .025f) return;
        EffectTriangle[0] = a;
        EffectTriangle[1] = b;
        EffectTriangle[2] = c;
        EffectInk[0] = tint;
        canvas.DrawPrimitive(EffectTriangle, EffectInk, Array.Empty<Vector2>());
    }

    private static void Star(CanvasItem canvas, Vector2 center, float radius, Color tint)
    {
        canvas.DrawLine(center - new Vector2(radius, 0), center + new Vector2(radius, 0), tint, 1.15f, true);
        canvas.DrawLine(center - new Vector2(0, radius * 1.5f), center + new Vector2(0, radius * 1.5f), tint, 1.15f, true);
        canvas.DrawCircle(center, .75f, tint);
    }

    internal static void DrawPirateEffects(CanvasItem canvas, Func<float, float, float, Vector2> p,
        float time, int seed)
    {
        float clock = time + seed * .37f;
        var chimney = p(3, 0, 22);
        // Overlapping opaque cores make a dense column; the outer wisps soften at the top.
        for (int puff = MaximumShrineParticles - 1; puff >= 0; puff--)
        {
            float phase = (clock * .13f + puff / (float)MaximumShrineParticles) % 1;
            var drift = new Vector2(phase * 22 + MathF.Sin(puff * 2.1f + phase * 3) * 2.3f,
                -phase * 49);
            var at = chimney + drift;
            float radius = 2.3f + phase * 5.4f;
            float alpha = .82f * Math.Clamp((1 - phase) * 3, 0, 1);
            canvas.DrawCircle(at, radius * 1.3f, new(.10f, .14f, .15f, alpha * .25f));
            canvas.DrawCircle(at, radius, new(.065f, .08f, .085f, alpha));
            canvas.DrawCircle(at + new Vector2(-radius * .24f, -radius * .2f), radius * .48f,
                new(.18f, .20f, .20f, alpha * .55f));
        }
    }

    // Call only for visible town/hull art, never an explored fog snapshot.
    internal static void DrawEffects(CanvasItem canvas, Func<float, float, float, Vector2> p,
        FleetColor faction, float time, int seed = 0)
    {
        float clock = time + seed * .37f;
        if (faction == FleetColor.Blue)
        {
            var dome = p(0, 0, 18.5f);
            canvas.DrawCircle(dome, 7.2f, new(1, .85f, .42f, .17f));
            for (int wave = 0; wave < 3; wave++)
            {
                float phase = (clock * .18f + wave / 3f) % 1;
                float alpha = .56f * MathF.Sin(phase * Mathf.Pi);
                canvas.DrawArc(dome, 4 + phase * 17, 0, Mathf.Tau, 28, new(1, .9f, .57f, alpha), 1.35f, true);
            }
            for (int ray = 0; ray < 12; ray++)
            {
                float angle = ray * Mathf.Tau / 12 + clock * .075f;
                var axis = Vector2.FromAngle(angle);
                float length = 8 + MathF.Sin(clock * .8f + ray) * 2;
                canvas.DrawLine(dome + axis * 6, dome + axis * length, new(1, .91f, .65f, .64f), 1.05f, true);
            }
        }
        else if (faction == FleetColor.Purple)
        {
            for (int glint = 0; glint < 5; glint++)
            {
                float phase = (clock * .20f + glint * .2f) % 1;
                float light = MathF.Pow(MathF.Sin(phase * Mathf.Pi), 4);
                Star(canvas, p(-3 + phase * 6, (glint % 2 - .5f) * 2, 17 + glint % 3 * 1.8f),
                    1.5f + light * 1.3f, new(.94f, .98f, 1, light * .94f));
            }
            canvas.DrawLine(p(-4, 0, 16), p(0, 0, 22), new(.91f, .95f, 1, .54f), 1.15f, true);
        }
        else if (faction == FleetColor.Red)
        {
            for (int mote = 0; mote < MaximumShrineParticles; mote++)
            {
                float phase = (clock * .18f + mote / (float)MaximumShrineParticles) % 1;
                var at = p(MathF.Sin(mote * 2.4f + phase * 3) * (4 + phase * 3),
                    MathF.Cos(mote) * 3 + phase * 3, 6 + phase * 28);
                float alpha = MathF.Sin(phase * Mathf.Pi) * .84f;
                canvas.DrawCircle(at, 1.8f + phase * .8f, new(1, .15f, .28f, alpha * .22f));
                canvas.DrawCircle(at, .7f + phase * .55f, new(1, .35f, .42f, alpha));
            }
            canvas.DrawLine(p(0, -1, 14), p(0, -1, 22), new(1, .63f, .65f, .65f), 1.3f, true);
        }
        else if (faction == FleetColor.Yellow)
        {
            float glow = .47f + .15f * MathF.Sin(clock * .85f);
            var crown = p(0, 0, 22);
            canvas.DrawCircle(crown, 8, new(1, .57f, .79f, glow * .35f));
            for (int petal = 0; petal < 4; petal++)
            {
                float angle = petal * Mathf.Pi / 2 + Mathf.Pi / 4;
                float x = MathF.Cos(angle), y = MathF.Sin(angle);
                LightFace(canvas, p(x * 2.2f - y * 1.8f, y * 2.2f + x * 1.8f, 19),
                    p(x * 6.5f, y * 6.5f, 23), p(x * 2.2f + y * 1.8f, y * 2.2f - x * 1.8f, 19),
                    new(1, .79f, .89f, glow));
            }
            Star(canvas, p(0, 0, 27.5f), 2.3f, new(1, .96f, .98f, .83f));
            for (int mote = 0; mote < 12; mote++)
            {
                float phase = (clock * .11f + mote / 12f) % 1;
                var at = p(MathF.Cos(mote * 2.4f + phase * 2) * (3 + phase * 6),
                    MathF.Sin(mote * 2.4f + phase * 2) * (3 + phase * 6), 22 + phase * 10);
                canvas.DrawCircle(at, .8f, new(1, .76f, .88f, MathF.Sin(phase * Mathf.Pi) * .78f));
            }
        }
        else if (faction == FleetColor.Green)
        {
            float sway = MathF.Sin(clock * .65f) * 1.15f;
            for (int leaf = 0; leaf < MaximumShrineParticles; leaf++)
            {
                float phase = (clock * .09f + leaf / (float)MaximumShrineParticles) % 1;
                var at = p(MathF.Sin(leaf * 2.7f) * 5 + phase * 16 + sway,
                    MathF.Cos(leaf * 1.8f) * 5, 20 - phase * 18);
                canvas.DrawLine(at, at + new Vector2(1.9f, MathF.Sin(clock + leaf) * .9f),
                    new(.53f, .75f, .38f, MathF.Sin(phase * Mathf.Pi) * .9f), 1.45f, true);
            }
            for (int tier = 0; tier < 3; tier++)
                canvas.DrawArc(p(sway, 0, 12 + tier * 4), 4.2f - tier * .7f, Mathf.Pi, Mathf.Tau, 8,
                    new(.42f, .68f, .34f, .62f), 1.3f, true);
        }
        else if (faction == FleetColor.White)
        {
            var letter = Letter(p);
            float top = p(0, 0, 27).Y, height = Math.Max(1, p(0, 0, 16).Y - top);
            float wave = (clock * .22f % 1) * height + top;
            for (int edge = 1; edge < letter.Length; edge++)
                for (int step = 0; step < 5; step++)
                {
                    var from = letter[edge - 1].Lerp(letter[edge], step / 5f);
                    var to = letter[edge - 1].Lerp(letter[edge], (step + 1) / 5f);
                    float light = Math.Max(0, 1 - MathF.Abs((from.Y + to.Y) * .5f - wave) / 3.5f);
                    if (light > 0) canvas.DrawLine(from, to, new(1, .7f, .72f, light * .94f), 4.1f, true);
                }
            Star(canvas, letter[2], 1.6f, new(1, .83f, .79f, .45f + .4f * MathF.Sin(clock * .8f)));
        }
    }
}
