using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

// Debris is solid floor/height geometry, never a cropped image of the vessel.
internal enum WreckPartKind { Keel, Deck, House, Sanctuary, Mast, Gun, Rubble, Cloth, Basket, Strut }
internal sealed record WreckFace(Vector3[] Vertices, Color Color, bool TwoSided = false);
internal sealed record WreckPart(WreckPartKind Kind, Vector3 Pivot, Vector3 Drift,
    Vector3 Spin, float Release, float Sink, WreckFace[] Faces);

internal sealed class WreckMesh
{
    private readonly List<WreckFace> _faces = new();
    internal WreckFace[] Faces => _faces.ToArray();
    internal void Face(Color color, params Vector3[] vertices) => _faces.Add(new(vertices, color));
    internal void Cloth(Color color, params Vector3[] vertices) => _faces.Add(new(vertices, color, true));

    internal void Box(float x, float y, float z, float w, float d, float h, Color color, bool windows = false)
    {
        var b = new[] {new Vector3(x-w/2,y-d/2,z),new(x+w/2,y-d/2,z),
            new(x+w/2,y+d/2,z),new(x-w/2,y+d/2,z)};
        var t = Array.ConvertAll(b, p => p + Vector3.Back * h);
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Face(color, b[i], b[j], t[j], t[i]);
            if (windows)
                for (int n = 0; n < 2; n++)
                {
                    var at = b[i].Lerp(b[j], .28f + n * .43f) + Vector3.Back * h * .5f;
                    var along = (b[j] - b[i]).Normalized() * .6f;
                    Cloth(new Color("675f4b"), at - along, at + along, at + along + Vector3.Back * 2, at - along + Vector3.Back * 2);
                }
        }
        Face(color.Lightened(.08f), t);
        Array.Reverse(b); Face(color.Darkened(.3f), b);
    }

    internal void Extrude(Vector2[] floor, float z, float thickness, Color deck, Color side)
    {
        var top = Array.ConvertAll(floor, p => new Vector3(p.X, p.Y, z));
        var bottom = Array.ConvertAll(top, p => p - Vector3.Back * thickness);
        for (int i = 0; i < top.Length; i++)
        { int j = (i + 1) % top.Length; Face(side, bottom[i], bottom[j], top[j], top[i]); }
        Face(deck, top); Array.Reverse(bottom); Face(side.Darkened(.2f), bottom);
    }

    internal void Pyramid(float x, float y, float z, float r, float h, Color color)
    {
        var points = new[] { new Vector3(x - r, y - r, z), new(x + r, y - r, z), new(x + r, y + r, z), new(x - r, y + r, z) };
        var tip = new Vector3(x, y, z + h);
        for (int i = 0; i < 4; i++) Face(color, points[i], points[(i + 1) % 4], tip);
        Array.Reverse(points); Face(color.Darkened(.2f), points);
    }

    internal void Cylinder(Vector3 a, Vector3 b, float r, Color color, int facets = 8)
    {
        var axis = (b - a).Normalized();
        var u = axis.Cross(MathF.Abs(axis.Z) < .8f ? Vector3.Back : Vector3.Right).Normalized();
        var v = axis.Cross(u).Normalized();
        var start = new Vector3[facets]; var end = new Vector3[facets];
        for (int i = 0; i < facets; i++)
        { float angle = i * Mathf.Tau / facets; var radial = (u * MathF.Cos(angle) + v * MathF.Sin(angle)) * r; start[i] = a + radial; end[i] = b + radial; }
        for (int i = 0; i < facets; i++) { int j = (i + 1) % facets; Face(color, start[i], start[j], end[j], end[i]); }
        Face(color.Lightened(.15f), end); Array.Reverse(start); Face(color.Darkened(.4f), start);
    }

    internal static Vector2[] Ellipse(float x, float y, float rx, float ry, int steps = 24)
    {
        var polygon = new Vector2[steps];
        for (int i = 0; i < steps; i++) { float a = i * Mathf.Tau / steps; polygon[i] = new(x + MathF.Cos(a) * rx, y + MathF.Sin(a) * ry); }
        return polygon;
    }
}
