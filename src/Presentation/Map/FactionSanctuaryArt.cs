using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Shared fleet monument. Ground x/y rotate independently of upright height z.</summary>
internal static class FactionSanctuaryArt
{
    internal static void DrawPirate(CanvasItem canvas, Func<float, float, float, Vector2> p)
    {
        Box(canvas, p, 0, 0, 0, 10, 9, 2, new("757769"));
        Box(canvas, p, 0, 0, 2, 8, 7, 15, new("5a6260"));
        Pyramid(canvas, p, 0, 0, 17, 5, 6, new("303b3e"));
        var skull = p(0, 3.8f, 11);
        canvas.DrawCircle(skull, 2, new("e8dfc7"));
        canvas.DrawLine(p(-2, 3.8f, 6), p(2, 3.8f, 9), new("e8dfc7"), .8f, true);
        canvas.DrawLine(p(2, 3.8f, 6), p(-2, 3.8f, 9), new("e8dfc7"), .8f, true);
        Box(canvas, p, -3, 0, 17, 1.5f, 1.5f, 4, new("b5ad94"));
        Box(canvas, p, 3, 0, 17, 1.5f, 1.5f, 4, new("b5ad94"));
    }

    internal static void Draw(CanvasItem canvas, Func<float, float, float, Vector2> p, FleetColor faction)
    {
        if (faction == FleetColor.Green)
        {
            Box(canvas, p, 0, 0, 0, 7, 6, 2, new("b9a37e"));
            Box(canvas, p, 0, 0, 2, 2, 2, 11, new("584b36"));
            for (int tier = 0; tier < 3; tier++)
                Crystal(canvas, p, 0, 0, 7 + tier * 4, 8 - tier * 1.6f, 8, new("235740"));
            return;
        }
        if (faction == FleetColor.Red)
        {
            Box(canvas, p, 0, 0, 0, 9, 7, 2, new("6d5554"));
            Crystal(canvas, p, -3.2f, 1, 2, 2.7f, 12, new("a73547"));
            Crystal(canvas, p, 3, 2, 2, 2.4f, 9, new("d95a6b"));
            Crystal(canvas, p, 0, -1, 2, 3.6f, 20, new("bc3c56"));
            return;
        }
        Box(canvas, p, 0, 0, 0, 9, 8, 2, new("b49c74"));
        Box(canvas, p, 0, 0, 2, 8, 7, 14, new("f1e6cf"));
        for (int i = -1; i <= 1; i++)
            Box(canvas, p, i * 2.4f, 3.6f, 2, .6f, .6f, 7, new("dfd2b6"));
        var door = p(0, 3.8f, 2);
        canvas.DrawLine(door, p(0, 3.8f, 6), new("625144"), 1.6f, true);
        switch (faction)
        {
            case FleetColor.Purple:
                Pyramid(canvas, p, 0, 0, 16, 5, 6, new("c4cbd1"));
                break;
            case FleetColor.Yellow:
                Pyramid(canvas, p, 0, 0, 16, 6, 8, new("d98cac"));
                Crystal(canvas, p, 0, 0, 24, 1.8f, 4, new("fff5ef"));
                break;
            case FleetColor.White:
                DrawLetter(canvas, p, new("aa3047"));
                break;
            default:
                var dome = p(0, 0, 18.5f);
                float scale = MathF.Sqrt(MathF.Pow((p(1, 0, 0)-p(0, 0, 0)).X, 2) + MathF.Pow((p(0, 1, 0)-p(0, 0, 0)).X, 2));
                canvas.DrawCircle(dome, 3.8f * scale, new("d3ad4d"));
                canvas.DrawArc(dome, 3.8f * scale, Mathf.Pi, Mathf.Tau, 16, new("ffe298"), 1, true);
                canvas.DrawLine(dome - new Vector2(0, 3.8f*scale), dome - new Vector2(0, 6*scale), new("f5d67d"), 1, true);
                break;
        }
    }
    private static Vector2[] Letter(Func<float, float, float, Vector2> p)
    {
        var top = p(0, 0, 27);
        float height = top.DistanceTo(p(0, 0, 16));
        float width = Math.Max(2.8f, (p(3.5f, 0, 0) - p(-3.5f, 0, 0)).Length());
        var points = new[] { new Vector2(-.5f,1), new Vector2(-.5f,0), new Vector2(.25f,0),
            new Vector2(.5f,.15f), new Vector2(.5f,.38f), new Vector2(.2f,.52f),
            new Vector2(-.5f,.52f), new Vector2(.2f,.52f), new Vector2(.58f,1) };
        return Array.ConvertAll(points, at => top + new Vector2(at.X * width, at.Y * height));
    }
    private static void DrawLetter(CanvasItem canvas, Func<float, float, float, Vector2> p, Color color)
    {
        var letter = Letter(p);
        var shade = Array.ConvertAll(letter, at => at + new Vector2(1.2f, 1));
        canvas.DrawPolyline(shade, color.Darkened(.35f), 3.2f, true);
        canvas.DrawPolyline(letter, color, 2.8f, true);
    }

    // Bounded foreground light and foliage; static buildings remain retained.
    // Call only for a visible town/hull, never for its explored fog snapshot.
    internal static void DrawEffects(CanvasItem canvas, Func<float, float, float, Vector2> p,
        FleetColor faction, float time, int seed = 0)
    {
        float clock = time + seed * .37f;
        if (faction == FleetColor.Blue)
        {
            var dome = p(0,0,18.5f);
            for (int wave = 0; wave < 2; wave++)
            {
                float phase = (clock * .18f + wave * .5f) % 1;
                var light = new Color(1,.88f,.5f,.24f * MathF.Sin(phase * Mathf.Pi));
                canvas.DrawArc(dome, 4 + phase * 11, 0, Mathf.Tau, 24, light, .9f, true);
            }
        }
        else if (faction == FleetColor.Purple)
        {
            var at = p(-3 + (clock * .45f % 1) * 6,0,19);
            float light = MathF.Pow(Math.Max(0, MathF.Sin(clock * 1.1f)), 6);
            var tint = new Color(.91f,.98f,1,light * .72f);
            canvas.DrawLine(at - new Vector2(2.2f,0),at + new Vector2(2.2f,0),tint,.9f,true);
            canvas.DrawLine(at - new Vector2(0,3),at + new Vector2(0,3),tint,.9f,true);
        }
        else if (faction == FleetColor.Red)
        {
            for (int mote = 0; mote < 6; mote++)
            {
                float phase = (clock * .22f + mote * .167f) % 1;
                var at = p(MathF.Sin(mote * 2.4f + phase * 3) * 4,MathF.Cos(mote) * 3,9 + phase * 21);
                canvas.DrawCircle(at,.55f + phase * .65f,new Color(1,.19f,.29f,(1-phase)*.55f));
            }
        }
        else if (faction == FleetColor.Yellow)
        {
            float glow = .22f + .12f * MathF.Sin(clock * .85f);
            var tip = p(0,0,24);
            var left = p(-5,-5,16);
            var right = p(5,-5,16);
            canvas.DrawPrimitive(new[] {left,right,tip},new[] {new Color(1,.66f,.82f,glow)},Array.Empty<Vector2>());
            canvas.DrawLine(tip,p(0,0,28),new Color(1,.93f,.96f,.5f),.8f,true);
        }
        else if (faction == FleetColor.Green)
        {
            float sway = MathF.Sin(clock * .65f) * .85f;
            for (int leaf = 0; leaf < 9; leaf++)
            {
                float phase = (clock * .09f + leaf * .113f) % 1;
                var at = p(MathF.Sin(leaf*2.7f)*5 + phase * 12 + sway,
                    MathF.Cos(leaf*1.8f)*5,19 - phase * 17);
                canvas.DrawLine(at,at + new Vector2(1.3f,MathF.Sin(clock+leaf)*.65f),
                    new Color(.43f,.68f,.35f,MathF.Sin(phase*Mathf.Pi)*.7f),1.2f,true);
            }
            for (int tier = 0; tier < 3; tier++)
                canvas.DrawArc(p(sway,0,12 + tier * 4),3.8f-tier*.7f,Mathf.Pi,Mathf.Tau,8,
                    new Color(.35f,.59f,.29f,.36f),1.1f,true);
        }
        else if (faction == FleetColor.White)
        {
            var letter = Letter(p);
            float top = p(0,0,27).Y, height = Math.Max(1,p(0,0,16).Y-top);
            float wave = (clock * .22f % 1) * height + top;
            for (int edge = 1; edge < letter.Length; edge++)
            {
                var a = letter[edge-1]; var b = letter[edge];
                for (int step = 0; step < 5; step++)
                {
                    var from = a.Lerp(b,step/5f); var to = a.Lerp(b,(step+1)/5f);
                    float light = Math.Max(0,1-MathF.Abs((from.Y+to.Y)*.5f-wave)/3);
                    if (light > 0) canvas.DrawLine(from,to,new Color(1,.54f,.58f,light*.65f),2.7f,true);
                }
            }
        }
    }
    internal static void Box(CanvasItem canvas, Func<float, float, float, Vector2> p, float x, float y, float z, float w, float d, float h, Color color)
    {
        var bottom = new[] { p(x-w/2,y-d/2,z), p(x+w/2,y-d/2,z), p(x+w/2,y+d/2,z), p(x-w/2,y+d/2,z) };
        var top = new[] { p(x-w/2,y-d/2,z+h), p(x+w/2,y-d/2,z+h), p(x+w/2,y+d/2,z+h), p(x-w/2,y+d/2,z+h) };
        for (int i = 0; i < 4; i++)
        {
            int j = (i+1)%4;
            if ((bottom[j]-bottom[i]).Cross(top[j]-bottom[i]) > .025f)
                Face(canvas, bottom[i], bottom[j], top[j], top[i], color.Darkened(i % 2 == 0 ? .22f : .06f));
        }
        Face(canvas, top[0], top[1], top[2], top[3], color.Lightened(.08f));
    }
    internal static void Pyramid(CanvasItem canvas, Func<float, float, float, Vector2> p, float x, float y, float z, float r, float height, Color color)
    {
        var corners = new[] { p(x-r,y-r,z), p(x+r,y-r,z), p(x+r,y+r,z), p(x-r,y+r,z) };
        var tip = p(x,y,z+height);
        for (int i = 0; i < 4; i++)
        {
            var a = corners[i];
            var b = corners[(i+1)%4];
            if ((b-a).Cross(tip-a) > .025f)
                Triangle(canvas, a, b, tip, color.Darkened(i % 2 == 0 ? .2f : 0));
        }
    }
    private static void Crystal(CanvasItem canvas, Func<float, float, float, Vector2> p, float x, float y, float z, float r, float h, Color color)
    {
        Box(canvas,p,x,y,z,r*1.35f,r*1.35f,h*.57f,color);
        Pyramid(canvas,p,x,y,z+h*.57f,r*.675f,h*.43f,color.Lightened(.1f));
    }
    private static void Face(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        Triangle(canvas,a,b,c,color);
        Triangle(canvas,a,c,d,color);
    }
    private static void Triangle(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (MathF.Abs((b-a).Cross(c-a)) > .025f)
            canvas.DrawPrimitive(new[] {a,b,c}, new[] {color}, Array.Empty<Vector2>());
    }
}
