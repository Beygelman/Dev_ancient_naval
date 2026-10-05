using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>The same painted clay-seal motifs identify vessels in badges and shipyards.</summary>
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
        switch (symbol)
        {
            case ActionSymbol.Mothership:
                // Twin hulls carry a stepped floating city. Broken secondary
                // strokes preserve a painted motif at both seal and HUD sizes.
                void Brush(float x, float y, float a, float b, float width = 1)
                {
                    Line(x, y, a, b, width);
                    var from = P(x, y) + new Vector2(.22f, -.18f) * scale;
                    var to = P(a, b) + new Vector2(.22f, -.18f) * scale;
                    canvas.DrawLine(from.Lerp(to, .16f), from.Lerp(to, .71f),
                        new Color(ink, ink.A * .37f), .34f * scale, true);
                }
                Poly(new[] { new Vector2(-8, 2), new(-5, 5), new(6, 5), new(9, 2) });
                Poly(new[] { new Vector2(-10, 5), new(-7, 8), new(4, 8), new(7, 5) });
                Brush(-8, 2, 8, 2, .8f);
                Brush(-10, 5, 7, 5, .9f);
                Brush(-7, 1, -8, 4, .65f);
                Brush(5, 1, 6, 4, .65f);
                Brush(-7, 1, 5, 1, .9f);
                foreach (var home in new[] { new Rect2(-6.5f, -3.4f, 3, 4.4f),
                    new Rect2(-2.7f, -5.2f, 3.4f, 6.2f), new Rect2(1.6f, -2.1f, 3.1f, 3.1f) })
                {
                    Brush(home.Position.X, home.End.Y, home.Position.X, home.Position.Y, .8f);
                    Brush(home.Position.X, home.Position.Y, home.End.X, home.Position.Y, .9f);
                    Brush(home.End.X, home.Position.Y, home.End.X, home.End.Y, .8f);
                    Brush(home.GetCenter().X, home.End.Y - 2, home.GetCenter().X, home.End.Y - .6f, .65f);
                }
                canvas.DrawArc(P(-1, -5.2f), 1.65f * scale, Mathf.Pi, Mathf.Tau, 10, ink, .85f * scale, true);
                Brush(-5.5f, -3.4f, -5.5f, -5.2f, .75f);
                Brush(4.5f, -2.1f, 4.5f, -3.3f, .65f);
                Brush(5.8f, 1, 5.8f, -6, .75f);
                Poly(new[] { new Vector2(5.8f, -6), new(9, -5), new(5.8f, -3.9f) }, true);
                Brush(-8, 9.5f, -3, 9, .6f);
                Brush(0, 9.2f, 6, 8.7f, .6f);
                break;
            case ActionSymbol.Support:
                Poly(new[] { new Vector2(-7,2), new(-4,5), new(4,5), new(7,2), new(-7,2) });
                for (int home = 0; home < 2; home++)
                {
                    float x = -4 + home * 4;
                    Poly(new[] { new Vector2(x,2), new(x,-2), new(x+1.5f,-4), new(x+3,-2), new(x+3,2) });
                    Line(x+1.5f,0,x+1.5f,1,.7f);
                }
                Line(0,-3,0,-7); Line(0,-7,4,-5); Line(4,-5,4,-2);
                break;
            case ActionSymbol.Fishing:
                Poly(new[] { new Vector2(-3,0), new(0,-3), new(4,-2.5f), new(6,0), new(4,2.5f), new(0,3), new(-3,0) });
                Poly(new[] { new Vector2(-3,0), new(-6,-3), new(-6,3), new(-3,0) }, true);
                canvas.DrawCircle(P(3.5f,-.6f), .6f * scale, ink);
                Line(0,-1,1,1,.65f);
                break;
            case ActionSymbol.Mortar:
                // A steep, open mortar cup and its low carriage, rather than a cannon silhouette.
                Poly(new[] { new Vector2(-3,3), new(1,4), new(5,-3), new(2,-5), new(-3,3) }, true);
                canvas.DrawLine(P(1,-5),P(5,-3),new Color(ink.Lightened(.48f),ink.A),1.3f*scale,true);
                Line(-5,5,5,5,1.4f);
                Line(-3,3,-4,5);
                canvas.DrawArc(P(0,-5),5*scale,-2.2f,-.6f,8,new Color(ink,.6f),.6f*scale,true);
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
                canvas.DrawArc(P(0,-3),4*scale,0,Mathf.Tau,20,ink,1.1f*scale,true);
                Line(-2,1,-1,4); Line(2,1,1,4); Line(-1,4,1,4);
                break;
            default:
                Poly(new[] { new Vector2(-6,1), new(-3,5), new(4,5), new(7,1) });
                Line(-5,1,6,1,.75f);
                int count = symbol == ActionSymbol.Heavy ? 3 : symbol == ActionSymbol.Standard ? 2 : 1;
                for (int mast = 0; mast < count; mast++)
                {
                    float x=count==1?-1:-3+mast*3;
                    Line(x,1,x,-6,.85f);
                    Poly(new[] { new Vector2(x+.7f,-5), new(x+.7f,-.5f), new(x+2.5f,-1.5f), new(x+.7f,-5) },true);
                }
                break;
        }
    }
}
