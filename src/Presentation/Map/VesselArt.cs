using System;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Small vessels use the city ship's floor/height projection and plaster, wood and cloth palette.</summary>
internal sealed class VesselArt
{
    private readonly Vector2[] _rim = new Vector2[25];
    private readonly Vector2[] _top = new Vector2[24];
    private readonly Vector2[] _bottom = new Vector2[24];
    private readonly Vector2[] _triangle = new Vector2[3];
    private readonly Color[] _color = new Color[1];
    private readonly int[] _order = new int[6];
    private CanvasItem _canvas = null!;
    private Func<float, float, float, Vector2> _p = null!;

    internal void Draw(CanvasItem canvas, Func<float, float, float, Vector2> project, ShipClass kind, Color accent, bool exhausted)
    {
        _canvas = canvas;
        _p = project;
        bool heavy = kind == ShipClass.Togus;
        bool fishing = kind == ShipClass.Fishing;
        float length = kind switch
        {
            ShipClass.Kolonel => 34,
            ShipClass.Invader => 31,
            ShipClass.Togus => 29,
            _ => 29
        };
        float width = heavy ? 12 : kind == ShipClass.Kolonel ? 11 : 10;
        var wood = new Color(kind == ShipClass.PirateSchooner ? "625344" : "90714c");
        Hull(length, width, wood, accent);

        // A raised stern deck and pale deckhouse echo the terraced flagship.
        Box(-length * .59f, 0, 1, 16, width * 1.45f, 4, new("b9a47e"), false);
        for (int plank = -4; plank < 5; plank++)
            _canvas.DrawLine(_p(-length + 8, plank * 1.7f, 1.25f), _p(length - 10, plank * 1.7f, 1.25f), new("b9a27b"), .55f, true);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int stanchion = 0; stanchion < 6; stanchion++)
            {
                float x = -length + 8 + stanchion * (length * 2 - 17) / 5;
                _canvas.DrawLine(_p(x, side * (width - 1.5f), 1), _p(x, side * (width - 1.5f), 4), new("d1b589"), .9f, true);
            }
            _canvas.DrawLine(_p(-length + 8, side * (width - 1.5f), 4), _p(length - 9, side * (width - 1.5f), 4), new("e1caa1"), .8f, true);
        }
        // Cannon carriages have true deck anchors; dark muzzles face out over the rail.
        if (!fishing && !heavy)
        {
            int guns = kind == ShipClass.Kolonel ? 3 : kind == ShipClass.Invader ? 2 : 1;
            for (int gun = 0; gun < guns; gun++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = -7 + gun * 10;
                    Box(x, side * (width - 4), 1.4f, 4, 4, 1.6f, new("86654a"), false);
                    var from = _p(x, side * (width - 4), 3.8f);
                    var end = _p(x, side * (width + 2), 3.8f);
                    _canvas.DrawLine(from, end, new("555d58"), 3, true);
                    _canvas.DrawCircle(end, 1.7f, new("253638"));
                }
        }

        int masts = heavy ? 1 : kind == ShipClass.Kolonel ? 3 : kind is ShipClass.Invader or ShipClass.PirateSchooner ? 2 : 1;
        int count = masts + 1;
        for (int i = 0; i < count; i++)
            _order[i] = i;
        // Sort deckhouses and sails by their ground anchor, keeping height out of depth.
        for (int i = 1; i < count; i++)
            for (int j = i; j > 0 && FootDepth(_order[j], masts) < FootDepth(_order[j - 1], masts); j--)
                (_order[j], _order[j - 1]) = (_order[j - 1], _order[j]);
        for (int i = 0; i < count; i++)
        {
            int part = _order[i];
            if (part == masts)
            {
                Cabin(-length * .61f, 0, 5, fishing ? 9 : 12, fishing ? 7 : 9, fishing ? 7 : 9, accent);
                if (kind == ShipClass.Kolonel)
                    Cabin(-length * .31f, -3, 1.3f, 8, 6, 7, accent.Darkened(.1f));
            }
            else
            {
                float x = MastX(part, masts);
                float height = heavy ? 25 : kind == ShipClass.Kolonel ? 43 - part * 3 : kind == ShipClass.Invader ? 38 - part * 4 : 35;
                Sail(x, width * (heavy ? .66f : .9f), height, accent, kind == ShipClass.PirateSchooner, exhausted);
            }
        }
        _canvas.DrawLine(_p(length - 2, 0, 1), _p(length + 10, 0, 5), new("bd9b6b"), 1.3f, true);
        _canvas.DrawLine(_p(length + 9, 0, 5), _p(13, 0, heavy ? 24 : 34), new("81765c"), .55f, true);
        if (fishing)
            FishingGear(length, width);
        if (heavy)
        {
            Ring(5, 0, 1.7f, 10, 9, new("807557"));
            Ring(5, 0, 3, 7, 6, new("c7b899"));
            for (int crate = 0; crate < 3; crate++)
                Box(16 + crate * 4, -4 + crate % 2 * 6, 1.3f, 3, 3, 3, new("aa9470"), false);
        }
    }

    private float FootDepth(int part, int masts) => part == masts ? _p(-20, 0, 0).Y : _p(MastX(part, masts), 0, 0).Y;
    private static float MastX(int index, int count) => count == 1 ? 1 : -6 + index * 15;

    internal void DrawDeckHouse(CanvasItem canvas, Func<float, float, float, Vector2> project, float x, float y, float width, float depth, float height, Color accent)
    {
        _canvas = canvas;
        _p = project;
        Cabin(x, y, 1, width, depth, height, accent);
    }

    private void Hull(float length, float width, Color wood, Color accent)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.Tau / 24;
            float x = MathF.Cos(a) * length;
            float y = MathF.Sin(a) * width * (1 - MathF.Max(0, MathF.Cos(a)) * .22f);
            _top[i] = _p(x, y, 1);
            _bottom[i] = _p(x * .88f, y * .82f, -7);
        }
        for (int i = 0; i < 24; i++)
        {
            int next = (i + 1) % 24;
            if (!Face(_bottom[i], _bottom[next], _top[next], _top[i], wood.Darkened(.2f + i / 24f * .13f), true))
                continue;
            _canvas.DrawLine(_bottom[i].Lerp(_top[i], .35f), _bottom[next].Lerp(_top[next], .35f), new("d1b485"), .65f, true);
            _canvas.DrawLine(_bottom[i].Lerp(_top[i], .75f), _bottom[next].Lerp(_top[next], .75f), accent.Darkened(.23f), 1.5f, true);
        }
        _canvas.DrawColoredPolygon(_top, new("dcc69b"));
        for (int i = 0; i < 24; i++)
            _rim[i] = _top[i];
        _rim[24] = _top[0];
        _canvas.DrawPolyline(_rim, new("f0ddb4"), 1.1f, true);
    }

    private void Cabin(float x, float y, float z, float width, float depth, float height, Color accent)
    {
        Box(x, y, z, width, depth, height, new("e7d8b4"), true);
        _canvas.DrawLine(_p(x + width * .15f, y - depth * .15f, z + height), _p(x + width * .15f, y - depth * .15f, z + height + 3), new("b9a27b"), 1.7f, true);
        _canvas.DrawLine(_p(x - width * .45f, y + depth * .45f, z + 2), _p(x + width * .45f, y + depth * .45f, z + 2), accent.Darkened(.18f), 1.3f, true);
        var flag = _p(x, y, z + height + 8);
        _canvas.DrawLine(_p(x, y, z + height), flag, new("c5a777"), .8f, true);
        Triangle(flag, flag + new Vector2(7, 2), flag + new Vector2(0, 5), accent);
        // Small roof parapets make the deckhouses read as plaster buildings, not flat cards.
        _canvas.DrawLine(_p(x - width * .5f, y - depth * .5f, z + height + 1), _p(x + width * .5f, y - depth * .5f, z + height + 1), new("f5e8c9"), 1, true);
    }

    private void Box(float x, float y, float z, float width, float depth, float height, Color plaster, bool windows)
    {
        Span<Vector2> floor = stackalloc Vector2[4];
        Span<Vector2> roof = stackalloc Vector2[4];
        for (int corner = 0; corner < 4; corner++)
        {
            float px = x + (corner is 0 or 3 ? -width : width) * .5f;
            float py = y + (corner < 2 ? -depth : depth) * .5f;
            floor[corner] = _p(px, py, z);
            roof[corner] = _p(px, py, z + height);
        }
        for (int side = 0; side < 4; side++)
        {
            int next = (side + 1) % 4;
            if (!Face(floor[side], floor[next], roof[next], roof[side], plaster.Darkened(side % 2 == 0 ? .2f : .06f), true))
                continue;
            if (windows)
                for (int window = 0; window < 2; window++)
                {
                    float t = .3f + window * .4f;
                    var at = floor[side].Lerp(floor[next], t).Lerp(roof[side].Lerp(roof[next], t), .57f);
                    _canvas.DrawLine(at, at + new Vector2(0, -2), new("675f4b"), 1.3f, true);
                }
        }
        Face(roof[0], roof[1], roof[2], roof[3], plaster.Lightened(.08f));
    }

    private void Sail(float x, float width, float height, Color accent, bool pirate, bool exhausted)
    {
        _canvas.DrawLine(_p(x, 0, 1), _p(x, 0, height + 3), new("8f7652"), 1.7f, true);
        _canvas.DrawLine(_p(x, -width - 1, height - 3), _p(x, width + 1, height - 3), new("b69968"), 1.1f, true);
        var cloth = new Color(pirate ? "404645" : exhausted ? "c7c1ad" : "f1e6c8");
        const int strips = 6;
        for (int strip = 0; strip < strips; strip++)
        {
            float u = strip / (float)strips, v = (strip + 1f) / strips;
            Vector2 Sheet(float t, bool top)
            {
                float y = (t * 2 - 1) * width;
                float billow = MathF.Sin(t * Mathf.Pi) * 4;
                return _p(x + (top ? .6f : billow), y, top ? height - 4 + MathF.Sin(t * Mathf.Pi) : 11 + MathF.Sin(t * Mathf.Pi) * 2);
            }
            Face(Sheet(u, false), Sheet(v, false), Sheet(v, true), Sheet(u, true), cloth.Darkened(MathF.Abs(u - .38f) * .17f));
            _canvas.DrawLine(Sheet(u, false), Sheet(u, true), new Color("b2a585", .45f), .5f, true);
            if (strip == 2)
                _canvas.DrawLine(Sheet(u, false).Lerp(Sheet(u, true), .5f), Sheet(v, false).Lerp(Sheet(v, true), .5f), pirate ? new("e3dbc2") : accent.Darkened(.1f), 2.5f, true);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            _canvas.DrawLine(_p(x, 0, height + 1), _p(x - 7, side * 8, 2), new("796e55"), .55f, true);
            _canvas.DrawLine(_p(x, side * width, 12), _p(x + 9, side * 7, 2), new("8d7e60"), .55f, true);
        }
    }

    private void FishingGear(float length, float width)
    {
        Box(13, -3, 1.5f, 6, 5, 3, new("a99470"), false);
        Ring(15, 4, 2, 5, 4, new("c7bc8f"));
        for (int i = 0; i < 5; i++)
        {
            _canvas.DrawLine(_p(7 + i * 3, width - 2, 2), _p(7 + i * 3, width + 5, -2), new("7f7e5d"), .65f, true);
            _canvas.DrawLine(_p(7, width - 2 + i * 1.5f, 2 - i * .7f), _p(length - 8, width - 2 + i * 1.5f, 2 - i * .7f), new("7f7e5d"), .65f, true);
        }
    }

    private void Ring(float x, float y, float z, float rx, float ry, Color color)
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.Tau / 24;
            _top[i] = _p(x + MathF.Cos(angle) * rx, y + MathF.Sin(angle) * ry, z);
        }
        _canvas.DrawColoredPolygon(_top, color);
    }

    private bool Face(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, bool cull = false)
    {
        if (cull && (b - a).Cross(c - a) <= .025f)
            return false;
        Triangle(a, b, c, color);
        Triangle(a, c, d, color);
        return true;
    }

    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (MathF.Abs((b - a).Cross(c - a)) < .025f)
            return;
        _triangle[0] = a;
        _triangle[1] = b;
        _triangle[2] = c;
        _color[0] = color;
        _canvas.DrawPrimitive(_triangle, _color, Array.Empty<Vector2>());
    }
}
