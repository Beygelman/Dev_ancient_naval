using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    private void DrawBalloonEnvelope(Vector2 center, Color color)
    {
        Ink.DrawCircle(center, 20, color.Darkened(.3f));
        // Curved colored gores form one volume rather than a flat circular marker.
        for (int strip = 0; strip < 7; strip++)
        {
            float left = -1 + strip * 2f / 7;
            float right = -1 + (strip + 1) * 2f / 7;
            var panel = new Vector2[18];
            for (int row = 0; row < 9; row++)
            {
                float t = row * Mathf.Pi / 8;
                float width = MathF.Sin(t) * 20;
                float y = -MathF.Cos(t) * 20;
                panel[row] = center + new Vector2(left * width, y);
                panel[17 - row] = center + new Vector2(right * width, y);
            }
            var cloth = strip % 3 == 0 ? new Color("f0dfb3").Lerp(color,.18f) : color;
            DrawProjectedPolygon(panel, cloth.Lightened(strip is 1 or 2 ? .15f : 0).Darkened(MathF.Abs(strip - 2) * .045f));
        }
        for (int seam = 0; seam <= 7; seam++)
        {
            var points = new Vector2[17];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i * Mathf.Pi / 16;
                points[i] = center + new Vector2((-1 + seam*2f/7)*MathF.Sin(t)*20,-MathF.Cos(t)*20);
            }
            Ink.DrawPolyline(points, new Color("887c60"), .5f, true);
        }
        Ink.DrawArc(center, 16.5f, .18f, Mathf.Pi-.18f, 24, new Color("e3ce93"), 1, true);
        Ink.DrawColoredPolygon(new[] { center+new Vector2(0,-5), center+new Vector2(3,0), center+new Vector2(0,5), center+new Vector2(-3,0) }, new Color("f5e5b3"));
        Ink.DrawArc(center + new Vector2(-3, -3), 13, -2.5f, -1.25f, 16, new Color(1, .96f, .78f, .5f), 1.4f, true);
        Ink.DrawRect(new Rect2(center + new Vector2(-4, 18), new Vector2(8, 3)), color.Darkened(.25f));
    }
    private void DrawBalloonBasket(Vector2 envelope, Color accent)
    {
        Vector2 P(float x,float y,float z) => envelope + new Vector2(x-y,(x+y)*.4f+36-z);
        FactionSanctuaryArt.Box(Ink,P,0,0,0,10,7,7,new Color("b59a6d"));
        for (int i = -3; i <= 3; i += 2)
            Ink.DrawLine(P(i,3.6f,1),P(i,3.6f,6),new Color("846f50"),.6f,true);
        for (int z = 1; z < 7; z += 2)
            Ink.DrawLine(P(-5,3.6f,z),P(5,3.6f,z),new Color("dcc79e"),.6f,true);
        Ink.DrawLine(P(-5,3.6f,7),P(5,3.6f,7),accent.Darkened(.35f),1.5f,true);
        foreach (int x in new[] {-7,7})
            Ink.DrawLine(envelope+new Vector2(x*1.6f,12),P(x*.65f,0,7),new Color("998563"),.8f,true);
        Ink.DrawCircle(P(0,0,9),1.7f,new Color("eccd99"));
        Ink.DrawLine(P(0,0,7),P(0,0,9),accent.Darkened(.4f),2,true);
    }
}
