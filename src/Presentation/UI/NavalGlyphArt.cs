using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>The same painted clay-seal motifs identify vessels in badges and shipyards.</summary>
internal static class NavalGlyphArt
{
    internal static ActionSymbol Symbol(ShipClass? kind) => kind switch
    {
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
        ActionSymbol.Tower or ActionSymbol.Dock or ActionSymbol.Lighthouse or ActionSymbol.City or ActionSymbol.Balloon or ActionSymbol.Support;

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
