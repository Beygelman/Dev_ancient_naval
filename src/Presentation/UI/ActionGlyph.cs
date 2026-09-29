using Godot;

namespace DevAncientNaval.Presentation.UI;

public enum ActionSymbol { Move, Attack, Repair, Build, Close, Fishing, Scout, Standard, Heavy, Radar, Menu, Mortar, Dock }

/// <summary>Small vector icons drawn at screen resolution; no external art required.</summary>
public partial class ActionGlyph : Control
{
    public ActionSymbol Symbol { get; set; }
    public override void _Draw()
    {
        var ink = new Color("e4f4f5");
        var center = Size / 2;
        Vector2 P(float x, float y) => center + new Vector2(x, y);
        void Line(float x1, float y1, float x2, float y2) => DrawLine(P(x1,y1),P(x2,y2),ink,2.2f,true);
        switch(Symbol)
        {
            case ActionSymbol.Mortar:
                DrawCircle(P(0,7),7,ink); Line(-4,3,5,-13); Line(3,5,12,-10); Line(5,-13,12,-10); break;
            case ActionSymbol.Dock:
                Line(-13,0,13,0); Line(-13,6,13,6); Line(-9,-7,-9,12); Line(9,-7,9,12); break;
            case ActionSymbol.Menu:
                Line(-10,-7,10,-7); Line(-10,0,10,0); Line(-10,7,10,7); break;
            case ActionSymbol.Move:
                Line(-10,9,10,-11); Line(-1,-11,10,-11); Line(10,-11,10,0);
                DrawCircle(P(-10,9),2.5f,ink); break;
            case ActionSymbol.Attack:
                DrawArc(center,9,0,Mathf.Tau,32,ink,2,true);
                Line(-14,0,-5,0); Line(5,0,14,0); Line(0,-14,0,-5); Line(0,5,0,14);
                DrawCircle(center,2,ink); break;
            case ActionSymbol.Repair:
                DrawRect(new Rect2(P(-3,-12),new Vector2(6,24)),ink);
                DrawRect(new Rect2(P(-12,-3),new Vector2(24,6)),ink); break;
            case ActionSymbol.Radar:
                DrawArc(center,12,0,Mathf.Tau,32,ink,2,true);
                DrawArc(center,6,0,Mathf.Tau,24,ink,1,true);
                Line(0,0,9,-9); DrawCircle(P(-5,4),2,ink); break;
            case ActionSymbol.Build:
                Line(-9,12,6,-6);
                DrawColoredPolygon(new[] { P(-1,-12),P(4,-16),P(15,-5),P(11,-1) },ink);
                Line(-13,13,13,13); break;
            case ActionSymbol.Close:
                Line(-7,-7,7,7); Line(7,-7,-7,7); break;
            case ActionSymbol.Fishing:
                DrawPolyline(new[] { P(-9,0),P(-2,-7),P(7,-6),P(12,0),P(7,6),P(-2,7),P(-9,0),P(-15,-7),P(-15,7),P(-9,0) },ink,2,true);
                DrawCircle(P(6,-1),1.6f,ink); break;
            default:
                int masts = Symbol == ActionSymbol.Heavy ? 3 : Symbol == ActionSymbol.Standard ? 2 : 1;
                DrawPolyline(new[] { P(-15,6),P(-9,13),P(9,13),P(15,6),P(-15,6) },ink,2,true);
                for(int i=0;i<masts;i++)
                {
                    float x=-9+i*8;
                    Line(x,5,x,-12);
                    DrawColoredPolygon(new[] { P(x+2,-12),P(x+2,3),P(x+8,3) },ink);
                }
                break;
        }
    }
}
