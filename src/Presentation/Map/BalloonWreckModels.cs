using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>The envelope tears into curved cloth gores; its solid basket and rig fall separately.</summary>
internal static class BalloonWreckModels
{
    internal static WreckPart[] Build(Color accent)
    {
        var parts = new List<WreckPart>();
        void Add(WreckPartKind kind, WreckMesh mesh, Vector3 pivot, Vector3 drift, Vector3 spin,
            float release, float sink) => parts.Add(new(kind, pivot, drift, spin, release, sink, mesh.Faces));
        var basket = new WreckMesh();
        basket.Box(0, 0, 36, 13, 10, 8, new("a88959"));
        for (int slat = -5; slat <= 5; slat += 2)
            basket.Box(slat, 5.05f, 37, .55f, .55f, 6, new("d2b789"));
        Add(WreckPartKind.Basket, basket, new(0, 0, 36), new(5, 7, 0), new(.95f, .4f, .6f), .08f, 54);
        var burner = new WreckMesh();
        burner.Cylinder(new(0, 0, 45), new(0, 0, 50), 2.1f, new("b79b66"));
        burner.Box(0, 0, 44, 7, 5, 1.1f, new("555a50"));
        Add(WreckPartKind.Gun, burner, new(0, 0, 44), new(-8, 4, 2), new(1.6f, 1.1f, .2f), .17f, 57);
        for (int rig = 0; rig < 4; rig++)
        {
            float angle = rig * Mathf.Pi / 2 + Mathf.Pi / 4;
            float x = MathF.Cos(angle), y = MathF.Sin(angle);
            var rope = new WreckMesh();
            rope.Cylinder(new(x * 6, y * 6, 43), new(x * 17, y * 17, 72), .5f, new("8f7652"));
            Add(WreckPartKind.Strut, rope, new(x * 6, y * 6, 43), new(x * 18, y * 18, 4),
                new(x * 1.4f, y * 1.4f, .3f), .10f + rig * .025f, 38);
        }
        for (int panel = 0; panel < 8; panel++)
        {
            var cloth = new WreckMesh();
            for (int band = 0; band < 8; band++)
            {
                Vector3 Point(int edge, int tier)
                {
                    float lat = -Mathf.Pi / 2 + tier * Mathf.Pi / 8;
                    float lon = (panel + edge) * Mathf.Tau / 8;
                    return new(MathF.Cos(lon) * MathF.Cos(lat) * 28,
                        MathF.Sin(lon) * MathF.Cos(lat) * 28, 88 + MathF.Sin(lat) * 29);
                }
                // Scorched ragged lower edges open as the gores peel away from their crown.
                var tint = panel % 3 == 0 ? new Color("e7d8b4") : accent;
                if (band < 2) tint = tint.Darkened(.26f);
                cloth.Cloth(tint, Point(0, band), Point(1, band), Point(1, band + 1), Point(0, band + 1));
            }
            float angle = panel * Mathf.Tau / 8;
            var pivot = new Vector3(MathF.Cos(angle) * 8, MathF.Sin(angle) * 8, 98);
            Add(WreckPartKind.Cloth, cloth, pivot, new(MathF.Cos(angle) * 20, MathF.Sin(angle) * 20, -5),
                new(MathF.Sin(angle) * 1.1f, -MathF.Cos(angle) * 1.1f, panel % 2 == 0 ? .4f : -.4f),
                panel * .037f, 42 + panel % 3 * 3);
        }
        return parts.ToArray();
    }
}
