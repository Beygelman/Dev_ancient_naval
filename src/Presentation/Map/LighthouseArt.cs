using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    internal static Vector2 LighthouseOffset => new(-20, -10);
    internal int LighthouseDrawCount { get; private set; }

    internal static float LighthouseBeamAngle(float seconds, int id) => seconds * .16f + id * .73f;

    private void DrawLighthouse(Vector2 center, Color accent, int id, bool effects)
    {
        LighthouseDrawCount++;
        var anchor = center + LighthouseOffset;
        const float scale = .85f;
        Vector2 P(float x, float y, float z) => anchor + new Vector2((x - y) * scale, (x + y) * .42f * scale - z * scale);
        if (effects) DrawLighthouseBeam(P, LighthouseBeamAngle(_clock, id));
        var rock = new[] { new Vector2(-15, 1), new(-11, -5), new(-4, -8), new(7, -7),
            new(15, -2), new(13, 5), new(4, 9), new(-8, 7) };
        var surface = Array.ConvertAll(rock, p => anchor + p);
        Ink.DrawColoredPolygon(surface, new Color("87938a"));
        for (int i = 0; i < rock.Length; i++)
        {
            int next = (i + 1) % rock.Length;
            if (rock[next].X >= rock[i].X) continue;
            var bottomA = surface[i] + new Vector2(0, 3);
            var bottomB = surface[next] + new Vector2(0, 3);
            Ink.DrawColoredPolygon(new[] { surface[i], surface[next], bottomB, bottomA }, new Color("586b66"));
        }
        Ink.DrawArc(anchor + new Vector2(0, 3), 17, .05f, Mathf.Pi - .1f, 20, new Color("b6d6c8", .3f), .8f, true);

        void Octagonal(float radius, float topRadius, float floor, float height, Color plaster)
        {
            var lower = new Vector2[8];
            var upper = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.Tau / 8;
                lower[i] = P(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, floor);
                upper[i] = P(MathF.Cos(angle) * topRadius, MathF.Sin(angle) * topRadius, floor + height);
            }
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                if ((lower[next] - lower[i]).Cross(upper[next] - lower[i]) > .001f)
                    Ink.DrawColoredPolygon(new[] { lower[i], lower[next], upper[next], upper[i] }, plaster.Darkened(i % 4 * .055f));
            }
            Ink.DrawColoredPolygon(upper, plaster.Lightened(.08f));
        }
        Octagonal(8, 7, 0, 4, new Color("b5ae93"));
        Octagonal(5.7f, 4.6f, 4, 25, new Color("e9ddbd"));
        for (int band = 0; band < 5; band++)
        {
            float z = 5 + band * 4.5f;
            Ink.DrawLine(P(-4, 4, z), P(0, 5.5f, z), new Color("ac9f84"), .55f, true);
            Ink.DrawLine(P(0, 5.5f, z), P(4, 4, z), new Color("ac9f84"), .55f, true);
            Ink.DrawLine(P(band % 2 == 0 ? -2 : 2, 5, z), P(band % 2 == 0 ? -2 : 2, 5, z + 2.7f), new Color("b7a98d"), .5f, true);
        }
        Ink.DrawLine(P(0, 5.4f, 5), P(0, 5.4f, 11), new Color("736449"), 2.2f, true);
        Ink.DrawLine(P(-1.7f, 5.5f, 11.5f), P(1.7f, 5.5f, 11.5f), new Color("f5e9c8"), 1, true);
        Ink.DrawLine(P(0, 4.9f, 18), P(0, 4.9f, 22), new Color("758781"), 1.6f, true);
        Octagonal(7.4f, 7.4f, 28, 2, new Color("c6b998"));
        FactionSanctuaryArt.Box(Ink, P, 0, 0, 30, 7, 7, 9, new Color("b9c7b7"));
        var light = P(0, 0, 35);
        Ink.DrawCircle(light, 4.5f, new Color("ffdf92", .18f));
        Ink.DrawCircle(light, 1.8f, new Color("ffe7a8"));
        for (int corner = -1; corner <= 1; corner += 2)
        {
            Ink.DrawLine(P(corner * 3.5f, 3.5f, 30), P(corner * 3.5f, 3.5f, 39), new Color("746c52"), .8f, true);
            Ink.DrawLine(P(3.5f, corner * 3.5f, 30), P(3.5f, corner * 3.5f, 39), new Color("746c52"), .8f, true);
        }
        Ink.DrawLine(P(-3.5f, 3.5f, 34.5f), P(3.5f, 3.5f, 34.5f), new Color("8f8569"), .6f, true);
        FactionSanctuaryArt.Pyramid(Ink, P, 0, 0, 39, 5.5f, 7, accent.Darkened(.18f));
        Ink.DrawLine(P(0, 0, 46), P(0, 0, 49), new Color("c6ae7c"), .8f, true);
        Ink.DrawLine(P(-2, 0, 48), P(2, 0, 48), new Color("c6ae7c"), .8f, true);
        for (int rail = -1; rail <= 1; rail++)
            Ink.DrawLine(P(rail * 4, 6, 29), P(rail * 4, 6, 33), new Color("827b61"), .65f, true);
        Ink.DrawLine(P(-5, 6, 32), P(5, 6, 32), new Color("b1a17b"), .8f, true);
        for (int stair = 0; stair < 4; stair++)
            Ink.DrawLine(P(-2.5f, 7 + stair * 1.4f, 3 - stair * .7f), P(2.5f, 7 + stair * 1.4f, 3 - stair * .7f), new Color("d6c8a8"), 1, true);
    }

    private void DrawLighthouseBeam(Func<float, float, float, Vector2> p, float heading)
    {
        const float spread = .26f;
        var origin = p(0, 0, 35);
        // Overlapping short bands yield a soft spreading cone without building a mesh each tick.
        for (int band = 4; band >= 0; band--)
        {
            float inner = band * 22 + 2, outer = (band + 1) * 22 + 2;
            var a = p(MathF.Cos(heading - spread) * inner, MathF.Sin(heading - spread) * inner, 35);
            var b = p(MathF.Cos(heading - spread) * outer, MathF.Sin(heading - spread) * outer, 35);
            var c = p(MathF.Cos(heading + spread) * outer, MathF.Sin(heading + spread) * outer, 35);
            var d = p(MathF.Cos(heading + spread) * inner, MathF.Sin(heading + spread) * inner, 35);
            DrawProjectedPolygon(new[] { a, b, c, d }, new(1, .93f, .67f, .23f - band * .032f));
        }
        var tip = p(MathF.Cos(heading) * 112, MathF.Sin(heading) * 112, 35);
        Ink.DrawLine(origin, tip, new(1, .96f, .79f, .32f), .8f, true);
        Ink.DrawCircle(origin, 4, new(1, .92f, .6f, .46f));
    }
}
