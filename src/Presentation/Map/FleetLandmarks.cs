using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private void DrawAncientTower(Func<float, float, Vector2> p, Color accent)
    {
        Ink.DrawColoredPolygon(new[] { p(-21, 0), p(-12, -10), p(11, -10), p(22, 0), p(12, 11), p(-12, 11) }, new Color("6b7b73"));
        Ink.DrawColoredPolygon(new[] { p(-12, 1), p(0, 8), p(0, -18), p(-12, -25) }, new Color("9aab91"));
        Ink.DrawColoredPolygon(new[] { p(0, 8), p(13, 1), p(13, -25), p(0, -18) }, new Color("657f77"));
        Ink.DrawColoredPolygon(new[] { p(-14, -25), p(0, -33), p(15, -25), p(0, -17) }, new Color("c8c4a2"));
        Ink.DrawLine(p(-12, -6), p(0, 1), accent, 3, true);
        Ink.DrawLine(p(0, 1), p(13, -6), accent, 3, true);
    }

    private void DrawCannonTower(Func<float, float, Vector2> p, Color accent)
    {
        Ink.DrawColoredPolygon(new[] { p(-18, 0), p(-9, -8), p(13, -6), p(20, 3), p(8, 11), p(-13, 8) }, new Color("6d817b"));
        Ink.DrawColoredPolygon(new[] { p(-10, 2), p(1, 8), p(1, -17), p(-10, -23) }, new Color("d8c5a0"));
        Ink.DrawColoredPolygon(new[] { p(1, 8), p(13, 1), p(13, -24), p(1, -17) }, new Color("a49577"));
        Ink.DrawColoredPolygon(new[] { p(-13, -23), p(0, -30), p(16, -24), p(2, -16) }, new Color("e9d7af"));
        Ink.DrawLine(p(-9, -8), p(1, -2), accent, 3, true);
        Ink.DrawLine(p(1, -2), p(12, -9), accent, 3, true);
    }

    private void DrawPirateFlag(Func<float, float, Vector2> p)
    {
        Ink.DrawLine(p(-12, -20), p(-12, -39), new Color("c6bd95"), 1.5f, true);
        Ink.DrawColoredPolygon(new[] { p(-12, -39), p(1, -36), p(0, -27), p(-12, -30) }, new Color("252e34"));
        Ink.DrawCircle(p(-6, -34), 2.2f, new Color("f0e7cf"));
        Ink.DrawLine(p(-9, -31), p(-3, -28), new Color("f0e7cf"), 1.1f, true);
        Ink.DrawLine(p(-3, -31), p(-9, -28), new Color("f0e7cf"), 1.1f, true);
    }
}
