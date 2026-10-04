using System;
using System.Collections.Generic;
using Godot;
using DevAncientNaval.Core.Battle;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Reusable, rotation-safe city geometry with upright buildings and visible faces.</summary>
internal sealed class CityShipArt : IComparer<int>
{
    internal readonly record struct Home(float X, float Y, float Z, float W, float D, float H, int Tone);
    internal static readonly Home[] Homes =
    {
        new(-27, -6, 0, 7, 5, 7, 0),
        new(27, -5, 0, 6, 5, 6, 2),
        new(-26, 9, 0, 6, 5, 5, 4),
        new(23, 11, 0, 7, 5, 7, 1),
        new(-10, 19, 0, 6, 4, 5, 3),
        new(10, 18, 0, 6, 4, 6, 0),
        new(-18, -7, 3, 7, 5, 9, 1),
        new(17, -7, 3, 6, 5, 8, 3),
        new(-17, 8, 3, 7, 5, 7, 2),
        new(14, 9, 3, 6, 4, 6, 4),
        new(-7, -13, 3, 6, 4, 8, 2),
        new(5, -12, 3, 6, 4, 7, 0),
        new(-9, 0, 6, 6, 5, 9, 3),
        new(9, 1, 6, 6, 5, 8, 1)
    };
    private static readonly Color[] Plaster =
    {
        new("eadbb5"),
        new("e2ccaa"),
        new("dbc29b"),
        new("f0e3c5"),
        new("cdb996")
    };
    private readonly int[] _order = new int[Homes.Length + 1];
    private readonly Vector2[] _top = new Vector2[32], _bottom = new Vector2[32], _outline = new Vector2[33];
    private readonly Vector2[] _triangle = new Vector2[3];
    private readonly Color[] _triangleColor = new Color[1];
    private readonly Vector2[] _base = new Vector2[4], _roof = new Vector2[4];
    private Func<float, float, float, Vector2> _p = null !;
    private CanvasItem _canvas = null !;
    public int Compare(int a, int b)
    {
        var one = a == Homes.Length ? _p(0, -2, 0) : _p(Homes[a].X, Homes[a].Y, 0);
        var two = b == Homes.Length ? _p(0, -2, 0) : _p(Homes[b].X, Homes[b].Y, 0);
        int depth = one.Y.CompareTo(two.Y);
        return depth != 0 ? depth : a.CompareTo(b);
    }

    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (MathF.Abs((b - a).Cross(c - a)) < .025f)
            return;
        _triangle[0] = a;
        _triangle[1] = b;
        _triangle[2] = c;
        _triangleColor[0] = color;
        _canvas.DrawPrimitive(_triangle, _triangleColor, Array.Empty<Vector2>());
    }

    private void Face(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, bool cull = false)
    {
        if (cull && (b - a).Cross(c - a) <= .025f)
            return;
        Triangle(a, b, c, color);
        Triangle(a, c, d, color);
    }

    public void Draw(CanvasItem canvas, Func<float, float, float, Vector2> project, Color accent, float time, float fracture = 0, FleetColor faction = FleetColor.Blue, int cityLevel = 5)
    {
        _canvas = canvas;
        _p = (x, y, z) => project(x, y + MathF.Sign(y) * fracture * 14, z - fracture * MathF.Abs(y) * .24f);
        // Two long round-ended keels remain legible underneath one continuous deck.
        for (int keel = 0; keel < 2; keel++)
        {
            float offset = keel == 0 ? -21 : 21;
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.Tau / 32;
                float x = MathF.Cos(a) * 7 + MathF.Sign(MathF.Cos(a)) * 35;
                float y = offset + MathF.Sin(a) * 7;
                _top[i] = _p(x, y, -1);
                _bottom[i] = _p(x, y, -9);
            }

            for (int i = 0; i < 32; i++)
            {
                int next = (i + 1) % 32;
                Face(_bottom[i], _bottom[next], _top[next], _top[i], accent.Darkened(.55f), true);
                if ((_bottom[next] - _bottom[i]).Cross(_top[next] - _bottom[i]) > .025f)
                    _canvas.DrawLine(_bottom[i].Lerp(_top[i], .45f), _bottom[next].Lerp(_top[next], .45f), new("b79b64"), .6f, true);
            }

            // Floor projection is invertible at every yaw: these convex surfaces never collapse.
            _canvas.DrawColoredPolygon(_top, new("7b806b"));
            for (int i = 0; i < 32; i++)
                _outline[i] = _top[i];
            _outline[32] = _top[0];
            _canvas.DrawPolyline(_outline, new("d6be83"), 1, true);
            _canvas.DrawLine(_p(37, offset, 0), _p(43, offset, 5), new("dbc78f"), 1.5f, true);
            _canvas.DrawCircle(_p(43, offset, 5), 1.3f, new("f2e5bd"));
        }

        // Three smooth shared terraces, with only camera-facing retaining walls.
        for (int level = 0; level < 3; level++)
        {
            float rx = 36 - level * 10, ry = 23 - level * 6.5f, cx = level == 1 ? -3 : 0, cy = -level, z = level * 3;
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.Tau / 32;
                float x = cx + MathF.Cos(a) * rx, y = cy + MathF.Sin(a) * ry;
                _top[i] = _p(x, y, z);
                _bottom[i] = _p(x, y, z - 3);
            }

            for (int i = 0; i < 32; i++)
                Face(_bottom[i], _bottom[(i + 1) % 32], _top[(i + 1) % 32], _top[i], new(level == 0 ? "887555" : "ac9976"), true);
            if (fracture < .01f)
                _canvas.DrawColoredPolygon(_top, new(level == 1 ? "ccb88d" : "ddcaa0"));
            else
            {
                // Clip in deck space before projecting. A crack cannot stretch
                // a face across the two separated halves.
                for (int half = 0; half < 2; half++)
                {
                    float sign = half == 0 ? 1 : -1;
                    var cut = new List<Vector2>(36);
                    for (int edge = 0; edge < 32; edge++)
                    {
                        float aa = edge * Mathf.Tau / 32, bb = (edge + 1) * Mathf.Tau / 32;
                        var one = new Vector2(cx + MathF.Cos(aa) * rx, cy + MathF.Sin(aa) * ry);
                        var two = new Vector2(cx + MathF.Cos(bb) * rx, cy + MathF.Sin(bb) * ry);
                        bool inside = one.Y * sign >= 0, nextInside = two.Y * sign >= 0;
                        if (inside)
                            cut.Add(one);
                        if (inside != nextInside)
                            cut.Add(one.Lerp(two, -one.Y / (two.Y - one.Y)));
                    }

                    Vector2 Fragment(Vector2 p) => project(p.X, p.Y + sign * fracture * 14, z - fracture * MathF.Abs(p.Y) * .24f);
                    for (int vertex = 1; vertex < cut.Count - 1; vertex++)
                        Triangle(Fragment(cut[0]), Fragment(cut[vertex]), Fragment(cut[vertex + 1]), new(level == 1 ? "ccb88d" : "ddcaa0"));
                }
            }

            for (int i = 0; i < 32; i++)
                _outline[i] = _top[i];
            _outline[32] = _top[0];
            _canvas.DrawPolyline(_outline, new("eadab4"), .8f, true);
        }

        // Planks and courtyard steps belong to the floor, never to screen-space height.
        for (int i = 0; i < 7; i++)
            _canvas.DrawLine(_p(-20 + i * 5, 15, 0.1f), _p(-18 + i * 5, 19, 0.1f), new("bba078"), .65f, true);
        for (int i = 0; i < 4; i++)
            _canvas.DrawLine(_p(-5, 13 - i, i * .7f), _p(3, 13 - i, i * .7f), new("f1dfb2"), 1, true);
        int homeCount = Math.Min(Homes.Length, 3 + cityLevel * 3);
        for (int i = 0; i < homeCount; i++)
            _order[i] = i;
        _order[homeCount] = Homes.Length;
        Array.Sort(_order, 0, homeCount + 1, this);
        for (int slot = 0; slot <= homeCount; slot++)
        {
            int index = _order[slot];
            if (index == Homes.Length)
            {
                FactionSanctuaryArt.Draw(_canvas, (x, y, z) => _p(x, y - 2, z * (1 + .1f * (cityLevel - 1)) + 6), faction);
                if (fracture < .01f) FactionSanctuaryArt.DrawEffects(_canvas,
                    (x,y,z) => _p(x,y-2,z*(1+.1f*(cityLevel-1))+6), faction,time);
            }
            else
                House(Homes[index] with { H = Homes[index].H + (cityLevel - 3) * 1.1f }, accent.Darkened(index % 4 * .035f), index);
        }
        var flag = _p(29, 2, 17);
        _canvas.DrawLine(_p(29, 2, 0), flag, new("c8ab73"), 1, true);
        Triangle(flag, flag + new Vector2(9, 1 + MathF.Sin(time * 1.3f)), flag + new Vector2(0, 5), accent);
        // Cloth remains vertical while its rope anchors follow the deck.
        var aRoof = _p(-18, -7, 12);
        var bRoof = _p(-7, -13, 11);
        _canvas.DrawLine(aRoof, bRoof, new("79664d"), .65f, true);
        for (int i = 1; i < 4; i++)
        {
            var at = aRoof.Lerp(bRoof, i / 4f) + new Vector2(0, 1.5f);
            Face(at, at + new Vector2(2, 0), at + new Vector2(2, 3), at + new Vector2(0, 3), i % 2 == 0 ? accent : new("f2e5c4"));
        }
    }

    private void Box(Home h, Color color, Color accent, int index)
    {
        float x = h.X, y = h.Y, z = h.Z, w = h.W / 2, d = h.D / 2;
        _base[0] = _p(x - w, y - d, z);
        _base[1] = _p(x + w, y - d, z);
        _base[2] = _p(x + w, y + d, z);
        _base[3] = _p(x - w, y + d, z);
        _roof[0] = _p(x - w, y - d, z + h.H);
        _roof[1] = _p(x + w, y - d, z + h.H);
        _roof[2] = _p(x + w, y + d, z + h.H);
        _roof[3] = _p(x - w, y + d, z + h.H);
        for (int side = 0; side < 4; side++)
        {
            int next = (side + 1) % 4;
            if ((_base[next] - _base[side]).Cross(_roof[next] - _base[side]) <= .025f)
                continue;
            Face(_base[side], _base[next], _roof[next], _roof[side], color.Darkened(side % 2 == 0 ? .18f : .05f));
            var window = _base[side].Lerp(_base[next], .38f).Lerp(_roof[side].Lerp(_roof[next], .38f), .55f);
            _canvas.DrawLine(window, window + new Vector2(0, -1.7f), new("625e4b"), .85f, true);
            if (index % 3 == 0)
                _canvas.DrawLine(_base[side].Lerp(_base[next], .7f) + new Vector2(0, -2), _base[side].Lerp(_base[next], .7f) + new Vector2(0, -4), accent, .9f, true);
        }

        Face(_roof[0], _roof[1], _roof[2], _roof[3], color.Lightened(.06f));
    }

    private void House(Home h, Color accent, int index)
    {
        Box(h, Plaster[h.Tone], accent, index);
        if (index % 3 == 0)
            _canvas.DrawLine(_p(h.X + 1, h.Y, h.Z + h.H), _p(h.X + 1, h.Y, h.Z + h.H + 2.2f), new("b39a75"), 1.4f, true);
        if (index % 4 == 1)
        {
            var a = _p(h.X - h.W / 2, h.Y + h.D / 2 + 1, h.Z + 4);
            var b = _p(h.X + h.W / 2, h.Y + h.D / 2 + 1, h.Z + 4);
            Face(a, b, b + new Vector2(0, 2), a + new Vector2(0, 2), accent.Darkened(.08f));
        }
    }

}
