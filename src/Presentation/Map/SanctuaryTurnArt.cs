using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal static partial class FactionSanctuaryArt
{
    private static readonly Vector2[] TurnHalo = new Vector2[33];

    internal static void DrawTurnPulse(CanvasItem canvas, Func<float, float, float, Vector2> p,
        FleetColor nation, float progress, int seed)
    {
        float t = Math.Clamp(progress, 0, 1);
        float strength = Math.Clamp((1 - MathF.Exp(-t * 18)) * MathF.Exp(-t * 3.4f) * (1 - t) * 2.8f, 0, 1);
        if (strength <= .001f) return;
        var ink = nation switch
        {
            FleetColor.Purple => new Color("e0dbff"),
            FleetColor.Green => new Color("beefa9"),
            FleetColor.Red => new Color("ffabb9"),
            FleetColor.Yellow => new Color("ffcee5"),
            FleetColor.White => new Color("fff0df"),
            _ => new Color("ffe49c")
        };
        float summit = nation switch
        {
            FleetColor.Blue => 18.5f,
            FleetColor.Green => 21,
            FleetColor.Yellow or FleetColor.White => 27.5f,
            _ => 22
        };
        var light = p(0, 0, summit);
        float scale = Math.Max(.4f, p(1, 0, 0).DistanceTo(p(0, 0, 0)));
        // The brief brightening preserves each nation's original ritual vocabulary.
        canvas.DrawSetTransform(Vector2.Zero);
        canvas.SelfModulate = new Color(1, 1, 1, strength);
        DrawEffects(canvas, p, nation, t * 4.8f, seed);
        for (int halo = 0; halo < 2; halo++)
        {
            float radius = 7 + t * 22 + halo * 4;
            for (int vertex = 0; vertex < TurnHalo.Length; vertex++)
            {
                float angle = vertex * Mathf.Tau / (TurnHalo.Length - 1);
                TurnHalo[vertex] = p(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, 2);
            }
            canvas.DrawPolyline(TurnHalo, new Color(ink, .32f - halo * .1f), 1.15f, true);
        }
        canvas.DrawCircle(light, scale * (5 + t * 5), new Color(ink, .18f));
        canvas.DrawCircle(light, scale * 2.1f, new Color("fff8df") { A = .65f });
        for (int ray = 0; ray < 8; ray++)
        {
            float angle = ray * Mathf.Tau / 8 + seed * .31f;
            var axis = Vector2.FromAngle(angle);
            canvas.DrawLine(light + axis * (5 + t * 5) * scale,
                light + axis * (11 + t * 14) * scale, new Color(ink, .6f), 1.25f, true);
        }
        if (nation == FleetColor.Purple)
        {
            var corners = new[] { p(-5, -5, 16), p(5, -5, 16), p(5, 5, 16), p(-5, 5, 16) };
            var tip = p(0, 0, 22);
            for (int face = 0; face < 4; face++)
            {
                var a = corners[face]; var b = corners[(face + 1) % 4];
                if ((b - a).Cross(tip - a) > .025f) LightFace(canvas, a, b, tip, new Color("edf3ff") { A = .3f });
            }
        }
        else if (nation == FleetColor.White)
            canvas.DrawPolyline(Letter(p), new Color("fff3df") { A = .62f }, 2.2f, true);
    }
}
