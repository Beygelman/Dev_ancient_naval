using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
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
        TurningForShot = true;
        try
        {
            if (mortar || attacker.Class == ShipClass.CannonTower)
            {
                float from = _barrelAngles.GetValueOrDefault(attacker.Id, -1.15f);
                float to = direction.Angle();
                await TweenValue(.24, t => _barrelAngles[attacker.Id] = Mathf.LerpAngle(from, to, MotionProgress(t)));
            }
            else
            {
                float from = DeckAngle(attacker.Id);
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
    }

    private void DrawMortarBarrel(ShipSnapshot ship, Vector2 basePoint, float size, Color metal)
    {
        float direction = _barrelAngles.GetValueOrDefault(ship.Id, -1.15f);
        var end = basePoint + Vector2.FromAngle(direction) * 17 * size + new Vector2(0, -8 * size);
        Ink.DrawLine(basePoint, end, metal, 8 * size, true);
        Ink.DrawCircle(end, 4.6f * size, new Color("29363a"));
        Ink.DrawArc(end, 4.6f * size, 0, Mathf.Tau, 16, new Color("d6ceb8"), 1.2f, true);
    }
}
