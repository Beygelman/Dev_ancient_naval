using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private readonly Vector2[] _faceTriangle = new Vector2[3];
    private readonly Color[] _faceColor = new Color[1];
    private void DrawProjectedPolygon(Vector2[] points, Color color)
    {
        _faceColor[0] = color;
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (MathF.Abs((points[i] - points[0]).Cross(points[i + 1] - points[0])) < .025f)
                continue;
            _faceTriangle[0] = points[0];
            _faceTriangle[1] = points[i];
            _faceTriangle[2] = points[i + 1];
            Ink.DrawPrimitive(_faceTriangle, _faceColor, Array.Empty<Vector2>());
        }
    }

    private void DrawReefHarbor(Func<float, float, float, Vector2> p, Color accent)
    {
        // A semicircular quay encloses real water and an exposed coral/stone reef.
        var outer = new Vector2[25];
        var inner = new Vector2[25];
        for (int i = 0; i < 25; i++)
        {
            float a = .05f * Mathf.Pi + i / 24f * 1.65f * Mathf.Pi;
            outer[i] = p(MathF.Cos(a) * 32, MathF.Sin(a) * 32, 0);
            inner[i] = p(MathF.Cos(a) * 23, MathF.Sin(a) * 23, 0);
        }

        for (int i = 0; i < 24; i++)
            DrawProjectedPolygon(new[] { outer[i], outer[i + 1], inner[i + 1], inner[i] }, new("d7cbb0"));
        Ink.DrawPolyline(outer, new("f5efdc"), 1.8f, true);
        Ink.DrawPolyline(inner, new("9ba087"), 1.2f, true);
        DrawProjectedPolygon(new[] { p(-9, -2, 0), p(-5, -8, 0), p(3, -7, 0), p(9, -1, 0), p(3, 5, 0), p(-6, 5, 0) }, new("839c86"));
        DrawProjectedPolygon(new[] { p(-5, -2, 0), p(-1, -5, 5), p(5, 0, 0) }, new("b8bda0"));
        for (int i = 0; i < 4; i++)
        {
            float x = -6 + i * 4;
            Ink.DrawLine(p(x, 1, 0), p(x, 1, 4 + i % 2), new("d0a180"), 1.4f, true);
            Ink.DrawLine(p(x, 1, 2), p(x + 2, 1, 3), new("d0a180"), 1.2f, true);
        }

        for (int i = 0; i < 5; i++)
        {
            float a = .3f + i * .9f, x = MathF.Cos(a) * 28, y = MathF.Sin(a) * 28;
            DrawProjectedPolygon(new[] { p(x - 4, y, 0), p(x + 4, y, 0), p(x + 4, y, 8), p(x - 4, y, 8) }, new("eadfc3"));
            DrawProjectedPolygon(new[] { p(x - 5, y, 8), p(x, y - 3, 11), p(x + 5, y, 8) }, accent.Darkened(.16f));
            Ink.DrawLine(p(x, y, 3), p(x, y, 5), new("536961"), 1.4f, true);
        }

        for (int i = 0; i < 25; i += 6)
            Ink.DrawLine(outer[i], inner[i].Lerp(p(0, 0, 0), .25f), new("aa9875"), 3.2f, true);
        Ink.DrawLine(p(-26, 0, 0), p(-26, 0, 19), new("d2bd91"), 1.4f, true);
        DrawProjectedPolygon(new[] { p(-26, 0, 19), p(-16, 0, 17), p(-26, 0, 14) }, accent);
    }
}
