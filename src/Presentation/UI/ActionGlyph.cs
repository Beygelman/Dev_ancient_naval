using Godot;

namespace DevAncientNaval.Presentation.UI;
<<<<<<< Updated upstream
public enum ActionSymbol
{
    Move,
    Attack,
    Repair,
    Build,
    Close,
    Fishing,
    Scout,
    Standard,
    Heavy,
    Radar,
    Menu,
    Mortar,
    Dock,
    Bomb,
    Flag,
    Fortify,
    Treasure,
    Information,
    Tower,
    Lighthouse,
    SingleShot,
    DoubleShot,
    Scuttle,
    City,
    Balloon,
    Upgrade
}
=======

public enum ActionSymbol { Move, Attack, Repair, Build, Close, Fishing, Scout, Standard, Heavy, Radar, Menu, Mortar, Dock, Bomb, Flag, Fortify, Treasure, Information, Tower }
>>>>>>> Stashed changes

/// <summary>Small vector icons drawn at screen resolution; no external art required.</summary>
public partial class ActionGlyph : Control
{
    public ActionSymbol Symbol { get; set; }

    public override void _Draw()
    {
        var ink = PapyrusStyle.Ink;
        var center = Size / 2;
<<<<<<< Updated upstream
        if (NavalGlyphArt.IsNaval(Symbol))
        {
            NavalGlyphArt.Draw(this, center, Symbol, ink, 2.25f);
            return;
        }
        Vector2 P(float x, float y) => center + new Vector2(x, y);
        void Line(float x1, float y1, float x2, float y2)
        {
=======
        Vector2 P(float x, float y) => center + new Vector2(x, y);
        void Line(float x1, float y1, float x2, float y2)
        {
>>>>>>> Stashed changes
            var from = P(x1, y1);
            var to = P(x2, y2);
            DrawLine(from, to, new Color(ink, .83f), 2.3f, true);
            var grain = new Vector2(.6f, -.3f);
            DrawLine(from + grain, to.Lerp(from, .13f) + grain, new Color(ink, .45f), .7f, true);
            DrawLine(from.Lerp(to, .23f) - grain, to - grain, new Color(ink, .45f), .7f, true);
        }
<<<<<<< Updated upstream

        switch (Symbol)
        {
            case ActionSymbol.Upgrade:
                Line(-10, 9, 10, 9);
                Line(-6, 4, 6, 4);
                Line(0, 3, 0, -12);
                Line(-6, -6, 0, -12);
                Line(0, -12, 6, -6);
                break;
            case ActionSymbol.Scuttle:
                DrawPolyline(new[] { P(-13, 2), P(-7, 11), P(7, 11), P(13, 2) }, ink, 2, true);
                Line(-10, 3, 10, 3);
                Line(0, -13, 0, 0);
                Line(-5, -5, 0, 0);
                Line(5, -5, 0, 0);
                Line(-12, 15, -5, 13);
                Line(-5, 13, 3, 16);
                Line(3, 16, 11, 13);
                break;
            case ActionSymbol.Information:
                Line(-9, -12, 9, -12);
                Line(-9, 12, 9, 12);
                Line(-9, -12, -9, 12);
                Line(9, -12, 9, 12);
                DrawArc(P(0, -5), 3, 0, Mathf.Tau, 12, ink, 1.7f, true);
                Line(0, 1, 0, 8);
                Line(-3, 8, 3, 8);
                break;
            case ActionSymbol.SingleShot:
                DrawCircle(P(0, 0), 9, ink);
                DrawArc(P(-2, -2), 5, Mathf.Pi, Mathf.Pi * 1.5f, 10, PapyrusStyle.Paper, 1.5f, true);
                break;
            case ActionSymbol.DoubleShot:
                DrawCircle(P(-6, 3), 8, ink);
                DrawCircle(P(7, -5), 8, ink);
                DrawArc(P(-8, 1), 4, Mathf.Pi, Mathf.Pi * 1.5f, 10, PapyrusStyle.Paper, 1.5f, true);
                DrawArc(P(5, -7), 4, Mathf.Pi, Mathf.Pi * 1.5f, 10, PapyrusStyle.Paper, 1.5f, true);
                break;
            case ActionSymbol.Lighthouse:
                DrawPolyline(new[] { P(-9, 12), P(-5, -9), P(5, -9), P(9, 12), P(-9, 12) }, ink, 2, true);
                DrawRect(new Rect2(P(-6, -15), new Vector2(12, 6)), ink, false, 2);
                Line(-8, -16, 0, -21);
                Line(0, -21, 8, -16);
                Line(-9, -12, -16, -15);
                Line(9, -12, 16, -15);
                Line(-12, 15, 12, 15);
                break;
            case ActionSymbol.Tower:
                Line(-10, 13, -10, -11);
                Line(10, 13, 10, -11);
                Line(-10, 13, 10, 13);
                Line(-10, -7, 10, -7);
                Line(-10, -11, -5, -11);
                Line(-5, -11, -5, -7);
                Line(5, -7, 5, -11);
                Line(5, -11, 10, -11);
                DrawRect(new Rect2(P(-3, -1), new Vector2(6, 6)), ink);
                Line(0, 2, 12, -1);
                break;
            case ActionSymbol.Treasure:
                DrawRect(new Rect2(P(-12, -5), new Vector2(24, 17)), ink, false, 2);
                DrawArc(P(0, -5), 12, Mathf.Pi, Mathf.Tau, 18, ink, 2, true);
                DrawRect(new Rect2(P(-2, -2), new Vector2(4, 7)), ink);
                break;
            case ActionSymbol.Bomb:
                DrawCircle(P(0, 3), 9, ink);
                Line(2, -6, 5, -12);
                Line(5, -12, 10, -12);
                Line(10, -12, 12, -9);
                break;
            case ActionSymbol.Flag:
                Line(-9, -13, -9, 13);
                Line(-13, 13, -4, 13);
                DrawColoredPolygon(new[] { P(-7, -12), P(13, -9), P(5, -3), P(-7, -4) }, ink);
                break;
            case ActionSymbol.Fortify:
                DrawPolyline(new[] { P(-11, -10), P(0, -14), P(11, -10), P(9, 5), P(0, 13), P(-9, 5), P(-11, -10) }, ink, 2, true);
                Line(-5, 0, -1, 5);
                Line(-1, 5, 6, -5);
                break;
            case ActionSymbol.Mortar:
                DrawCircle(P(0, 7), 7, ink);
                Line(-4, 3, 5, -13);
                Line(3, 5, 12, -10);
                Line(5, -13, 12, -10);
                break;
            case ActionSymbol.Dock:
                Line(-13, 0, 13, 0);
                Line(-13, 6, 13, 6);
                Line(-9, -7, -9, 12);
                Line(9, -7, 9, 12);
                break;
            case ActionSymbol.Menu:
                Line(-10, -7, 10, -7);
                Line(-10, 0, 10, 0);
                Line(-10, 7, 10, 7);
                break;
            case ActionSymbol.Move:
                Line(-10, 9, 10, -11);
                Line(-1, -11, 10, -11);
                Line(10, -11, 10, 0);
                DrawCircle(P(-10, 9), 2.5f, ink);
                break;
            case ActionSymbol.Attack:
                DrawArc(center, 9, 0, Mathf.Tau, 32, ink, 2, true);
                Line(-14, 0, -5, 0);
                Line(5, 0, 14, 0);
                Line(0, -14, 0, -5);
                Line(0, 5, 0, 14);
                DrawCircle(center, 2, ink);
                break;
            case ActionSymbol.Repair:
                Line(-10, 12, 7, -9);
                Line(-12, -9, 11, 9);
                Line(1, -13, 12, -5);
                Line(-12, -13, -4, -5);
                Line(-8, 8, 10, 8);
                break;
            case ActionSymbol.Radar:
                DrawArc(center, 12, 0, Mathf.Tau, 32, ink, 2, true);
                DrawArc(center, 6, 0, Mathf.Tau, 24, ink, 1, true);
                Line(0, 0, 9, -9);
                DrawCircle(P(-5, 4), 2, ink);
                break;
            case ActionSymbol.Build:
                Line(-9, 12, 6, -6);
                DrawColoredPolygon(new[] { P(-1, -12), P(4, -16), P(15, -5), P(11, -1) }, ink);
                Line(-13, 13, 13, 13);
                break;
            case ActionSymbol.Close:
                Line(-7, -7, 7, 7);
                Line(7, -7, -7, 7);
                break;
            case ActionSymbol.Fishing:
                DrawPolyline(new[] { P(-9, 0), P(-2, -7), P(7, -6), P(12, 0), P(7, 6), P(-2, 7), P(-9, 0), P(-15, -7), P(-15, 7), P(-9, 0) }, ink, 2, true);
                DrawCircle(P(6, -1), 1.6f, ink);
                break;
=======
        switch (Symbol)
        {
            case ActionSymbol.Information:
                Line(-9, -12, 9, -12); Line(-9, 12, 9, 12);
                Line(-9, -12, -9, 12); Line(9, -12, 9, 12);
                DrawArc(P(0, -5), 3, 0, Mathf.Tau, 12, ink, 1.7f, true);
                Line(0, 1, 0, 8); Line(-3, 8, 3, 8); break;
            case ActionSymbol.Tower:
                Line(-10, 13, -10, -11); Line(10, 13, 10, -11);
                Line(-10, 13, 10, 13); Line(-10, -7, 10, -7);
                Line(-10, -11, -5, -11); Line(-5, -11, -5, -7);
                Line(5, -7, 5, -11); Line(5, -11, 10, -11);
                DrawRect(new Rect2(P(-3, -1), new Vector2(6, 6)), ink);
                Line(0, 2, 12, -1); break;
            case ActionSymbol.Treasure:
                DrawRect(new Rect2(P(-12,-5),new Vector2(24,17)),ink,false,2);
                DrawArc(P(0,-5),12,Mathf.Pi,Mathf.Tau,18,ink,2,true);
                DrawRect(new Rect2(P(-2,-2),new Vector2(4,7)),ink); break;
            case ActionSymbol.Bomb:
                DrawCircle(P(0, 3), 9, ink); Line(2, -6, 5, -12); Line(5, -12, 10, -12); Line(10, -12, 12, -9); break;
            case ActionSymbol.Flag:
                Line(-9, -13, -9, 13); Line(-13, 13, -4, 13);
                DrawColoredPolygon(new[] { P(-7, -12), P(13, -9), P(5, -3), P(-7, -4) }, ink); break;
            case ActionSymbol.Fortify:
                DrawPolyline(new[] { P(-11, -10), P(0, -14), P(11, -10), P(9, 5), P(0, 13), P(-9, 5), P(-11, -10) }, ink, 2, true);
                Line(-5, 0, -1, 5); Line(-1, 5, 6, -5); break;
            case ActionSymbol.Mortar:
                DrawCircle(P(0, 7), 7, ink); Line(-4, 3, 5, -13); Line(3, 5, 12, -10); Line(5, -13, 12, -10); break;
            case ActionSymbol.Dock:
                Line(-13, 0, 13, 0); Line(-13, 6, 13, 6); Line(-9, -7, -9, 12); Line(9, -7, 9, 12); break;
            case ActionSymbol.Menu:
                Line(-10, -7, 10, -7); Line(-10, 0, 10, 0); Line(-10, 7, 10, 7); break;
            case ActionSymbol.Move:
                Line(-10, 9, 10, -11); Line(-1, -11, 10, -11); Line(10, -11, 10, 0);
                DrawCircle(P(-10, 9), 2.5f, ink); break;
            case ActionSymbol.Attack:
                DrawArc(center, 9, 0, Mathf.Tau, 32, ink, 2, true);
                Line(-14, 0, -5, 0); Line(5, 0, 14, 0); Line(0, -14, 0, -5); Line(0, 5, 0, 14);
                DrawCircle(center, 2, ink); break;
            case ActionSymbol.Repair:
                Line(-10, 12, 7, -9); Line(-12, -9, 11, 9);
                Line(1, -13, 12, -5); Line(-12, -13, -4, -5);
                Line(-8, 8, 10, 8); break;
            case ActionSymbol.Radar:
                DrawArc(center, 12, 0, Mathf.Tau, 32, ink, 2, true);
                DrawArc(center, 6, 0, Mathf.Tau, 24, ink, 1, true);
                Line(0, 0, 9, -9); DrawCircle(P(-5, 4), 2, ink); break;
            case ActionSymbol.Build:
                Line(-9, 12, 6, -6);
                DrawColoredPolygon(new[] { P(-1, -12), P(4, -16), P(15, -5), P(11, -1) }, ink);
                Line(-13, 13, 13, 13); break;
            case ActionSymbol.Close:
                Line(-7, -7, 7, 7); Line(7, -7, -7, 7); break;
            case ActionSymbol.Fishing:
                DrawPolyline(new[] { P(-9, 0), P(-2, -7), P(7, -6), P(12, 0), P(7, 6), P(-2, 7), P(-9, 0), P(-15, -7), P(-15, 7), P(-9, 0) }, ink, 2, true);
                DrawCircle(P(6, -1), 1.6f, ink); break;
>>>>>>> Stashed changes
            default:
                int masts = Symbol == ActionSymbol.Heavy ? 3 : Symbol == ActionSymbol.Standard ? 2 : 1;
                DrawPolyline(new[] { P(-15, 6), P(-9, 13), P(9, 13), P(15, 6), P(-15, 6) }, ink, 2, true);
                for (int i = 0; i < masts; i++)
                {
                    float x = -9 + i * 8;
                    Line(x, 5, x, -12);
                    DrawColoredPolygon(new[] { P(x + 2, -12), P(x + 2, 3), P(x + 8, 3) }, ink);
                }

                break;
        }
    }
}
