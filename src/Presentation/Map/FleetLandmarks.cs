using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private void DrawAncientTower(Func<float, float, Vector2> p, Color accent)
    {
<<<<<<< Updated upstream
        Ink.DrawColoredPolygon(new[] { p(-21, 0), p(-12, -10), p(11, -10), p(22, 0), p(12, 11), p(-12, 11) }, new Color("6b7b73"));
        Ink.DrawColoredPolygon(new[] { p(-12, 1), p(0, 8), p(0, -18), p(-12, -25) }, new Color("9aab91"));
        Ink.DrawColoredPolygon(new[] { p(0, 8), p(13, 1), p(13, -25), p(0, -18) }, new Color("657f77"));
        Ink.DrawColoredPolygon(new[] { p(-14, -25), p(0, -33), p(15, -25), p(0, -17) }, new Color("c8c4a2"));
        Ink.DrawLine(p(-12, -6), p(0, 1), accent, 3, true);
        Ink.DrawLine(p(0, 1), p(13, -6), accent, 3, true);
        DrawTowerMasonry(p, accent, true);
=======
        DrawColoredPolygon(new[] { p(-21, 0), p(-12, -10), p(11, -10), p(22, 0), p(12, 11), p(-12, 11) }, new Color("6b7b73"));
        DrawColoredPolygon(new[] { p(-12, 1), p(0, 8), p(0, -18), p(-12, -25) }, new Color("9aab91"));
        DrawColoredPolygon(new[] { p(0, 8), p(13, 1), p(13, -25), p(0, -18) }, new Color("657f77"));
        DrawColoredPolygon(new[] { p(-14, -25), p(0, -33), p(15, -25), p(0, -17) }, new Color("c8c4a2"));
        DrawLine(p(-12, -6), p(0, 1), accent, 3, true);
        DrawLine(p(0, 1), p(13, -6), accent, 3, true);
>>>>>>> Stashed changes
    }

    private void DrawCannonTower(Func<float, float, Vector2> p, Color accent)
    {
<<<<<<< Updated upstream
        Ink.DrawColoredPolygon(new[] { p(-18, 0), p(-9, -8), p(13, -6), p(20, 3), p(8, 11), p(-13, 8) }, new Color("6d817b"));
        Ink.DrawColoredPolygon(new[] { p(-10, 2), p(1, 8), p(1, -17), p(-10, -23) }, new Color("d8c5a0"));
        Ink.DrawColoredPolygon(new[] { p(1, 8), p(13, 1), p(13, -24), p(1, -17) }, new Color("a49577"));
        Ink.DrawColoredPolygon(new[] { p(-13, -23), p(0, -30), p(16, -24), p(2, -16) }, new Color("e9d7af"));
        Ink.DrawLine(p(-9, -8), p(1, -2), accent, 3, true);
        Ink.DrawLine(p(1, -2), p(12, -9), accent, 3, true);
        DrawTowerMasonry(p, accent, false);
    }

    private void DrawTowerMasonry(Func<float, float, Vector2> p, Color accent, bool ancient)
    {
        var mortar = new Color(ancient ? "56685d" : "8d8065");
        for (int course = 0; course < 5; course++)
        {
            float y = 1 - course * 4.6f;
            Ink.DrawLine(p(-10, y - 5), p(0, y + 1), mortar, .65f, true);
            Ink.DrawLine(p(0, y + 1), p(12, y - 5), mortar, .65f, true);
            for (int block = 0; block < 3; block++)
            {
                float x = -9 + block * 3.5f + course % 2 * 1.3f;
                Ink.DrawLine(p(x, y + x * .55f), p(x, y + x * .55f - 3), mortar, .6f, true);
            }
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Ink.DrawLine(p(side * 5, -8), p(side * 5, -13), new Color("485650"), 1.4f, true);
            Ink.DrawLine(p(side * 5-1, -14), p(side * 5+1, -14), new Color("eee0bd"), 1, true);
            for (int merlon = 0; merlon < 4; merlon++)
            {
                float x = side * (2 + merlon * 3);
                Ink.DrawLine(p(x, -23 - MathF.Abs(x) * .5f), p(x, -27 - MathF.Abs(x) * .5f), new Color("e3d3ad"), 2.4f, true);
            }
        }
        Ink.DrawLine(p(-15, 0), p(-15,-14), new Color("958366"), 1, true);
        Ink.DrawColoredPolygon(new[] { p(-15,-14),p(-7,-13),p(-15,-9) }, accent);
        for (int step = 0; step < 3; step++)
            Ink.DrawLine(p(-4,8+step*2),p(4,8+step*2),new Color("d0bc92"),1.3f,true);
        Ink.DrawCircle(p(16,5),2.8f,new Color("6e8c68"));
        Ink.DrawCircle(p(-18,4),2,new Color("b9ae82"));
=======
        DrawColoredPolygon(new[] { p(-18, 0), p(-9, -8), p(13, -6), p(20, 3), p(8, 11), p(-13, 8) }, new Color("6d817b"));
        DrawColoredPolygon(new[] { p(-10, 2), p(1, 8), p(1, -17), p(-10, -23) }, new Color("d8c5a0"));
        DrawColoredPolygon(new[] { p(1, 8), p(13, 1), p(13, -24), p(1, -17) }, new Color("a49577"));
        DrawColoredPolygon(new[] { p(-13, -23), p(0, -30), p(16, -24), p(2, -16) }, new Color("e9d7af"));
        DrawLine(p(-9, -8), p(1, -2), accent, 3, true);
        DrawLine(p(1, -2), p(12, -9), accent, 3, true);
>>>>>>> Stashed changes
    }

    private void DrawPirateFlag(Func<float, float, Vector2> p)
    {
<<<<<<< Updated upstream
        Ink.DrawLine(p(-12, -20), p(-12, -39), new Color("c6bd95"), 1.5f, true);
        Ink.DrawColoredPolygon(new[] { p(-12, -39), p(1, -36), p(0, -27), p(-12, -30) }, new Color("252e34"));
        Ink.DrawCircle(p(-6, -34), 2.2f, new Color("f0e7cf"));
        Ink.DrawLine(p(-9, -31), p(-3, -28), new Color("f0e7cf"), 1.1f, true);
        Ink.DrawLine(p(-3, -31), p(-9, -28), new Color("f0e7cf"), 1.1f, true);
=======
        DrawLine(p(-12, -20), p(-12, -39), new Color("c6bd95"), 1.5f, true);
        DrawColoredPolygon(new[] { p(-12, -39), p(1, -36), p(0, -27), p(-12, -30) }, new Color("252e34"));
        DrawCircle(p(-6, -34), 2.2f, new Color("f0e7cf"));
        DrawLine(p(-9, -31), p(-3, -28), new Color("f0e7cf"), 1.1f, true);
        DrawLine(p(-3, -31), p(-9, -28), new Color("f0e7cf"), 1.1f, true);
>>>>>>> Stashed changes
    }
}
