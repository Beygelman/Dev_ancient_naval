using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Shared fleet monument. Ground x/y rotate independently of upright height z.</summary>
internal static partial class FactionSanctuaryArt
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
                DrawRoseCrown(canvas, p);
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
        canvas.DrawPolyline(shade, color.Darkened(.35f), 4.8f, true);
        canvas.DrawPolyline(letter, color, 4.2f, true);
        // Wide slab serifs keep the sacred R legible at town scale.
        foreach (var at in new[] { letter[0], letter[1], letter[^1] })
        {
            canvas.DrawLine(at + new Vector2(-2.4f, 1), at + new Vector2(2.4f, 1), color.Darkened(.35f), 3.1f, true);
            canvas.DrawLine(at + new Vector2(-2.4f, 0), at + new Vector2(2.4f, 0), color, 2.7f, true);
        }
    }

    private static void DrawRoseCrown(CanvasItem canvas, Func<float, float, float, Vector2> p)
    {
        Pyramid(canvas, p, 0, 0, 16, 6, 5, new("d98cac"));
        for (int petal = 0; petal < 4; petal++)
        {
            float angle = petal * Mathf.Pi / 2 + Mathf.Pi / 4;
            float x = MathF.Cos(angle), y = MathF.Sin(angle);
            var a = p(x * 2.2f - y * 1.8f, y * 2.2f + x * 1.8f, 19);
            var b = p(x * 6.5f, y * 6.5f, 23);
            var c = p(x * 2.2f + y * 1.8f, y * 2.2f - x * 1.8f, 19);
            Triangle(canvas, a, b, c, new Color(petal % 2 == 0 ? "ecadc7" : "c879a2"));
            canvas.DrawLine(a, b, new("ffe0e8"), .8f, true);
        }
        Crystal(canvas, p, 0, 0, 21, 2.1f, 7, new("fff5ef"));
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
