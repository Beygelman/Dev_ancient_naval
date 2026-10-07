using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Open nautical outlines remain legible on both clay seals and shipyard paper.</summary>
internal static class NavalGlyphArt
{
    /// <summary>A listing hull disappears through the water, stamped in pale ink on a dry-brush warning tile.</summary>
    internal static void DrawScuttle(CanvasItem canvas, Vector2 center)
    {
        Vector2 P(float x, float y) => center + new Vector2(x, y);
        var burgundy = PaintedVoyageChoice.Burgundy;
        var pale = PapyrusStyle.Paper;
        // Unequal bristle ends make a painted tile, not a modern square button.
        // Fixed grain belongs only to this glyph; no gameplay or cosmetic RNG is consumed.
        for (int bristle = 0; bristle < 21; bristle++)
        {
            float y = -15 + bristle * 1.5f;
            float left = -17 + bristle * 7 % 5 * .45f;
            float right = 17 - bristle * 11 % 7 * .35f;
            canvas.DrawLine(P(left, y), P(right, y + Mathf.Sin(bristle * 1.7f) * .35f),
                new Color(burgundy, bristle % 7 == 0 ? .70f : .91f), 1.8f, true);
        }
        for (int fleck = 0; fleck < 12; fleck++)
        {
            float x = -16 + fleck * 13 % 31;
            float y = fleck % 2 == 0 ? -14 + fleck % 4 : 12 + fleck % 3;
            canvas.DrawLine(P(x, y), P(x + 1.7f, y - .2f), new Color(pale, .20f), .55f, true);
        }

        // The stern and mast still stand above water; the low bow at the right
        // has already gone under. The uninterrupted water surface cuts the hull.
        canvas.DrawColoredPolygon(new[] { P(-13, -2), P(11, 6.2f), P(4, 7), P(-7, 4.2f) }, pale);
        canvas.DrawLine(P(-12, -2), P(9, 5.2f), pale, 1.8f, true);
        canvas.DrawLine(P(-6, 1), P(-10, -12), pale, 1.7f, true);
        canvas.DrawColoredPolygon(new[] { P(-8.5f, -11), P(-5.5f, -.9f), P(1.2f, 1.3f) }, pale);
        canvas.DrawLine(P(-10, -12), P(-4.8f, -10.3f), pale, 1.3f, true);
        canvas.DrawLine(P(-10, -12), P(-11.2f, -8.7f), pale, 1.15f, true);
        canvas.DrawLine(P(-9.5f, 1), P(4.8f, 5.3f), new Color(burgundy, .85f), .8f, true);

        // A solid waterline masks the submerged keel; pale wave strokes continue
        // across and below it, keeping the sinking meaning clear at 32px.
        canvas.DrawLine(P(-1, 7), P(15, 7), burgundy, 3.2f, true);
        canvas.DrawPolyline(new[] { P(-14, 6.8f), P(-10, 5.9f), P(-6, 7), P(-2, 6.2f), P(2, 7.2f), P(6, 6.5f), P(10, 7.3f), P(14, 6.5f) }, pale, 1.55f, true);
        canvas.DrawPolyline(new[] { P(-13, 11), P(-8, 10), P(-4, 11.1f), P(0, 10.3f) }, pale, 1.3f, true);
        canvas.DrawPolyline(new[] { P(3, 10.8f), P(7, 10), P(11, 11), P(14, 10.4f) }, pale, 1.3f, true);
        canvas.DrawArc(P(11, 1.5f), 1.2f, 0, Mathf.Tau, 10, pale, .85f, true);
        canvas.DrawCircle(P(13, -3), .7f, pale);
    }

    internal static ActionSymbol Symbol(ShipClass? kind) => kind switch
    {
        ShipClass.Mothership => ActionSymbol.Mothership,
        ShipClass.Fishing => ActionSymbol.Support,
        ShipClass.Garrison or ShipClass.PirateSchooner => ActionSymbol.Scout,
        ShipClass.Invader => ActionSymbol.Standard,
        ShipClass.Kolonel => ActionSymbol.Heavy,
        ShipClass.Togus or ShipClass.AncientGun => ActionSymbol.Mortar,
        ShipClass.CannonTower => ActionSymbol.Tower,
        ShipClass.FishingDock => ActionSymbol.Dock,
        ShipClass.Lighthouse => ActionSymbol.Lighthouse,
        ShipClass.Balloon => ActionSymbol.Balloon,
        _ => ActionSymbol.City
    };

    internal static bool IsNaval(ActionSymbol symbol) => symbol is ActionSymbol.Fishing or
        ActionSymbol.Scout or ActionSymbol.Standard or ActionSymbol.Heavy or ActionSymbol.Mortar or
        ActionSymbol.Tower or ActionSymbol.Dock or ActionSymbol.Lighthouse or ActionSymbol.City or ActionSymbol.Balloon or ActionSymbol.Support or ActionSymbol.Mothership;

    internal static void Draw(CanvasItem canvas, Vector2 center, ActionSymbol symbol, Color ink, float scale = 1)
    {
        Vector2 P(float x, float y) => center + new Vector2(x, y) * scale;
        void Line(float x, float y, float a, float b, float width = 1) =>
            canvas.DrawLine(P(x, y), P(a, b), ink, width * scale, true);
        void Poly(Vector2[] points, bool filled = false)
        {
            for (int i = 0; i < points.Length; i++) points[i] = center + points[i] * scale;
            if (filled) canvas.DrawColoredPolygon(points, ink);
            else canvas.DrawPolyline(points, ink, scale, true);
        }
        // The open hull and triangular sails share the reference's silhouette.
        // Avoid a deck stroke through the sail: tiny clay seals must read as ships,
        // rather than dark overlapping rectangles. Geometry is independent of RNG.
        void Hull(float y = 3, float width = 8)
        {
            Poly(new[] { new Vector2(-width - 1, y - 2), new(-width + 1, y - 2),
                new(-width * .62f, y + 3), new(width * .62f, y + 3),
                new(width - 1, y - 2), new(width + 1, y - 2) });
        }
        void Sail(float x, float top, float foot, float reach)
        {
            Line(x, foot + 3, x, top, 1.1f);
            Poly(new[] { new Vector2(x, top), new(x + reach, foot),
                new(x, foot), new(x, top) });
        }
        switch (symbol)
        {
            case ActionSymbol.Mothership:
                // A broad twin keel with a skyline, no sails: a city afloat.
                Hull(2, 8.5f);
                Poly(new[] { new Vector2(-7.3f, 6.5f), new(-4.8f, 8), new(4.8f, 8), new(7.3f, 6.5f) });
                Line(-6, 1, -6, -3.5f); Line(-6, -3.5f, -3, -3.5f); Line(-3, -3.5f, -3, 1);
                Line(3, 1, 3, -2.5f); Line(3, -2.5f, 6, -2.5f); Line(6, -2.5f, 6, 1);
                Poly(new[] { new Vector2(-2, 1), new(-2, -5), new(2, -5), new(2, 1) });
                canvas.DrawArc(P(0, -5), 2 * scale, Mathf.Pi, Mathf.Tau, 14, ink, scale, true);
                Line(0, -7, 0, -8.5f, .8f);
                Line(0, -2, 0, .4f, .75f);
                Line(-4.5f, -.7f, -4.5f, .6f, .65f); Line(4.5f, -.6f, 4.5f, .6f, .65f);
                break;
            case ActionSymbol.Support:
                Hull();
                Sail(-3, -8, -1, 4);
                // Cargo/workshop beneath the boom distinguishes the support hull.
                Poly(new[] { new Vector2(2, 2), new(2, -1), new(6, -1), new(6, 2) });
                Line(2, -1, 6, 2, .65f);
                break;
            case ActionSymbol.Fishing:
                Poly(new[] { new Vector2(-3,0), new(0,-3), new(4,-2.5f), new(6,0), new(4,2.5f), new(0,3), new(-3,0) });
                Poly(new[] { new Vector2(-3,0), new(-6,-3), new(-6,3), new(-3,0) }, true);
                canvas.DrawCircle(P(3.5f,-.6f), .6f * scale, ink);
                Line(0,-1,1,1,.65f);
                break;
            case ActionSymbol.Mortar:
                Hull();
                // An open elevated mortar cup on a broad sea carriage.
                Poly(new[] { new Vector2(-3, 2), new(0, 3), new(4, -4),
                    new(1, -6), new(-3, 2) });
                Line(1, -6, 4, -4, 1.4f);
                Line(-5, 2, 5, 2, .7f);
                canvas.DrawArc(P(0, -5), 5 * scale, -2.2f, -.6f, 12,
                    new Color(ink, ink.A * .55f), .6f * scale, true);
                break;
            case ActionSymbol.Tower:
                Poly(new[] { new Vector2(-4,5), new(-4,-5), new(-2,-5), new(-2,-3), new(0,-3), new(0,-5), new(2,-5), new(2,-3), new(4,-3), new(4,5), new(-4,5) });
                Line(-5,5,5,5,1.3f);
                canvas.DrawRect(new Rect2(P(-1,1),new Vector2(2,4)*scale),ink);
                Line(-3,-1,3,-1,.6f);
                break;
            case ActionSymbol.City:
                for (int home = 0; home < 3; home++)
                {
                    float x=-4+home*3, top=-3-home%2*2;
                    canvas.DrawRect(new Rect2(P(x,top),new Vector2(2.5f,4-top)*scale),ink,false,.8f*scale);
                    Line(x-.5f,top,x+1.25f,top-1.8f,.75f);
                    Line(x+1.25f,top-1.8f,x+3,top,.75f);
                }
                Line(-5,5,5,5,1.2f);
                break;
            case ActionSymbol.Dock:
                Line(-5,1,5,1,1.3f); Line(-5,4,5,4,1.3f);
                Line(-3,-1,-3,6); Line(3,-1,3,6);
                Poly(new[] { new Vector2(-3,-1), new(-3,-4), new(0,-6), new(3,-4), new(3,-1) });
                break;
            case ActionSymbol.Lighthouse:
                Poly(new[] { new Vector2(-4,6), new(-2,-3), new(2,-3), new(4,6), new(-4,6) });
                canvas.DrawRect(new Rect2(P(-2.5f,-6),new Vector2(5,3)*scale),ink,false,scale);
                Line(-3,-6,0,-8); Line(0,-8,3,-6);
                Line(-4,-4,-7,-5,.7f); Line(4,-4,7,-5,.7f);
                break;
            case ActionSymbol.Balloon:
                canvas.DrawArc(P(0, -3), 5 * scale, 0, Mathf.Tau, 32, ink, 1.1f * scale, true);
                canvas.DrawArc(P(0, -3), 2.4f * scale, -Mathf.Pi / 2, Mathf.Pi / 2, 16, ink, .65f * scale, true);
                Line(-3, 1, -2, 5); Line(3, 1, 2, 5);
                Poly(new[] { new Vector2(-2, 5), new(2, 5), new(1.5f, 7.5f), new(-1.5f, 7.5f), new(-2, 5) });
                break;
            default:
                Hull();
                int count = symbol == ActionSymbol.Heavy ? 3 : symbol == ActionSymbol.Standard ? 2 : 1;
                for (int mast = 0; mast < count; mast++)
                {
                    float x = count == 1 ? -2 : -5 + mast * 4;
                    Sail(x, count == 1 ? -8 : -7 - (mast == 1 ? 1 : 0),
                        count == 1 ? -1 : 0, count == 1 ? 5.5f : 3.2f);
                }
                if (symbol == ActionSymbol.Heavy)
                    for (int gun = 0; gun < 3; gun++) canvas.DrawCircle(P(-3 + gun * 3, 4.3f), .65f * scale, ink);
                break;
        }
    }
}
