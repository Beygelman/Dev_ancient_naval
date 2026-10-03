using System;
using System.Collections.Generic;
<<<<<<< Updated upstream
using System.Linq;
=======
>>>>>>> Stashed changes
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
<<<<<<< Updated upstream
public partial class FleetView
{
    private readonly Dictionary<int, float> _deckAngles = new();
    private readonly Dictionary<int, float> _barrelAngles = new();
    internal bool TurningForShot { get; private set; }

    internal float DeckAngle(int id) => _deckAngles.GetValueOrDefault(id);
    internal void SetPreviewHeading(int id, float radians)
    {
        EnsureVisualBattle();
        _deckAngles[id] = radians;
        QueueRedraw();
    }

    private static float BaseHeading(ShipClass kind, Side owner) => kind == ShipClass.Mothership ? 0 : owner == Side.Player ? .23f : Mathf.Pi - .23f;
    private async Task AimBattery(ShipSnapshot attacker, Vector2 direction, bool mortar, bool visible)
    {
        if (!visible || direction == Vector2.Zero)
            return;
=======

public partial class FleetView
{
    private readonly Dictionary<int,float> _deckAngles = new();
    private readonly Dictionary<int,float> _barrelAngles = new();
    internal bool TurningForShot { get; private set; }
    internal float DeckAngle(int id) => _deckAngles.GetValueOrDefault(id);
    private static float BaseHeading(ShipClass kind, Side owner) => kind == ShipClass.Mothership ? 0 : owner == Side.Player ? .23f : Mathf.Pi - .23f;

    private async Task AimBattery(ShipSnapshot attacker, Vector2 direction, bool mortar, bool visible)
    {
        if (!visible || direction == Vector2.Zero) return;
>>>>>>> Stashed changes
        TurningForShot = true;
        try
        {
            if (mortar || attacker.Class == ShipClass.CannonTower)
            {
<<<<<<< Updated upstream
                float from = _barrelAngles.GetValueOrDefault(attacker.Id, -1.15f);
                float to = direction.Angle();
                await TweenValue(.24, t => _barrelAngles[attacker.Id] = Mathf.LerpAngle(from, to, MotionProgress(t)));
=======
                float from = _barrelAngles.GetValueOrDefault(attacker.Id,-1.15f);
                float to = direction.Angle();
                await TweenValue(.24,t=>_barrelAngles[attacker.Id]=Mathf.LerpAngle(from,to,MotionProgress(t)));
>>>>>>> Stashed changes
            }
            else
            {
                float from = DeckAngle(attacker.Id);
<<<<<<< Updated upstream
                float wanted = DeckProjection.Heading(direction.Orthogonal()) - BaseHeading(attacker.Class, attacker.Owner);
                // Choose the nearer broadside rather than turning needlessly by 180°.
                float opposite = wanted + Mathf.Pi;
                if (MathF.Abs(Mathf.AngleDifference(from, opposite)) < MathF.Abs(Mathf.AngleDifference(from, wanted)))
                    wanted = opposite;
                double duration = .26 + ShipVisualProfile.For(attacker.Class).Size * .12;
                await TweenValue(duration, t => _deckAngles[attacker.Id] = Mathf.LerpAngle(from, wanted, MotionProgress(t)));
            }
        }
        finally
        {
            TurningForShot = false;
        }
=======
                float wanted = direction.Angle()+Mathf.Pi/2-BaseHeading(attacker.Class, attacker.Owner);
                // Choose the nearer broadside rather than turning needlessly by 180°.
                float opposite = wanted+Mathf.Pi;
                if(MathF.Abs(Mathf.AngleDifference(from,opposite))<MathF.Abs(Mathf.AngleDifference(from,wanted)))wanted=opposite;
                double duration=.26+ShipVisualProfile.For(attacker.Class).Size*.12;
                await TweenValue(duration,t=>_deckAngles[attacker.Id]=Mathf.LerpAngle(from,wanted,MotionProgress(t)));
            }
        }
        finally { TurningForShot = false; }
>>>>>>> Stashed changes
    }

    private void DrawMortarBarrel(ShipSnapshot ship, Vector2 basePoint, float size, Color metal)
    {
<<<<<<< Updated upstream
        float direction = _barrelAngles.GetValueOrDefault(ship.Id, -1.15f);
        bool cannon = ship.Class == ShipClass.CannonTower;
        var aim = Vector2.FromAngle(direction);
        Vector2 P(float x, float y) => basePoint + new Vector2(x, y) * size;
        // Heavy oak cheeks, iron straps and a recessed circular turntable
        // give the short bombard a distinct silhouette above ship or tower.
        Ink.DrawColoredPolygon(new[] { P(-11, 0), P(0, -5), P(11, 0), P(0, 6) }, new Color("8e7759"));
        Ink.DrawColoredPolygon(new[] { P(-11, 0), P(0, 6), P(0, 9), P(-11, 3) }, new Color("66543e"));
        Ink.DrawColoredPolygon(new[] { P(0, 6), P(11, 0), P(11, 3), P(0, 9) }, new Color("73614a"));
        var cradle = P(0, -2);
        Ink.DrawColoredPolygon(new[] { P(-8, 0), P(-6, -9), P(-3, -9), P(-2, 2) }, new Color("7d6850"));
        Ink.DrawColoredPolygon(new[] { P(2, 2), P(3, -9), P(6, -9), P(8, 0) }, new Color("b09a70"));
        Ink.DrawLine(P(-8, 2), P(0, 6), new Color("414b48"), 1.5f * size, true);
        Ink.DrawLine(P(0, 6), P(8, 2), new Color("414b48"), 1.5f * size, true);
        var breech = cradle + new Vector2(0, -5 * size);
        var mouth = breech + aim * (cannon ? 17 : 11) * size + new Vector2(0, -(cannon ? 2 : 7) * size);
        var axis = (mouth - breech).Normalized();
        var side = axis.Orthogonal();
        float rearRadius = (cannon ? 4 : 7) * size;
        float mouthRadius = (cannon ? 3.2f : 5.7f) * size;
        var bronze = cannon ? metal.Darkened(.24f) : metal.Lerp(new Color("b39765"), .45f);
        Ink.DrawCircle(breech, rearRadius, bronze.Darkened(.28f));
        Ink.DrawColoredPolygon(new[] { breech - side * rearRadius, mouth - side * mouthRadius,
            mouth + side * mouthRadius, breech + side * rearRadius }, bronze);
        Ink.DrawColoredPolygon(new[] { breech, mouth, mouth + side * mouthRadius,
            breech + side * rearRadius }, bronze.Darkened(.24f));
        Ink.DrawLine(breech - side * rearRadius * .55f, mouth - side * mouthRadius * .55f,
            bronze.Lightened(.27f), 1.3f * size, true);
        foreach (float band in new[] { .22f, .68f })
        {
            var at = breech.Lerp(mouth, band);
            float radius = Mathf.Lerp(rearRadius, mouthRadius, band);
            Ink.DrawLine(at - side * radius, at + side * radius, new Color("525a52"), 2 * size, true);
            Ink.DrawCircle(at - side * radius * .8f, .8f * size, new Color("e0d3a5"));
        }
        var rim = Enumerable.Range(0, 20).Select(i =>
        {
            float angle = i * Mathf.Tau / 20;
            return mouth + side * MathF.Cos(angle) * mouthRadius + axis * MathF.Sin(angle) * mouthRadius * .5f;
        }).ToArray();
        Ink.DrawColoredPolygon(rim, new Color("e0c88f"));
        var hollow = rim.Select(p => mouth + (p - mouth) * .67f).ToArray();
        Ink.DrawColoredPolygon(hollow, new Color("253333"));
        Ink.DrawLine(mouth - side * mouthRadius * .5f + axis * mouthRadius * .25f,
            mouth + side * mouthRadius * .45f + axis * mouthRadius * .25f, new Color("534e3f"), size, true);
        foreach (int cheek in new[] { -1, 1 })
        {
            var pivot = P(cheek * 5, -5);
            Ink.DrawCircle(pivot, 2 * size, new Color("454c47"));
            Ink.DrawCircle(pivot, .9f * size, new Color("d7c7a0"));
        }
=======
        float direction=_barrelAngles.GetValueOrDefault(ship.Id,-1.15f);
        var end=basePoint+Vector2.FromAngle(direction)*17*size+new Vector2(0,-8*size);
        DrawLine(basePoint,end,metal,8*size,true);
        DrawCircle(end,4.6f*size,new Color("29363a"));
        DrawArc(end,4.6f*size,0,Mathf.Tau,16,new Color("d6ceb8"),1.2f,true);
>>>>>>> Stashed changes
    }
}
