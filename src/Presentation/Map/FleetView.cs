using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView : Node2D
{
    public BattleState Battle { get; set; } = null!;
    public IsometricProjection Projection { get; set; } = null!;
    public int? SelectedId { get; set; }
    public Vector2? ProjectilePosition { get; private set; }
    private readonly Dictionary<int, ShipSnapshot> _snapshots = new();
    private readonly HashSet<int> _suppressed = new();
    private int _movingId;
    private bool _movingVisible;
    private Vector2 _movingPosition;
    private ShipSnapshot? _movingShip;
    private Vector2? _muzzle, _impact;
    private float _impactSize;
    private string _feedback = "";
    private Vector2 _feedbackPosition;

    public override void _Draw()
    {
        var drawn = Battle.ObservedShips(Side.Player).Where(s => !_suppressed.Contains(s.Id) && s.Id != _movingId)
            .Select(ShipSnapshot.From).Where(s => !_snapshots.ContainsKey(s.Id)).Concat(_snapshots.Values);
        foreach (var ship in drawn.OrderBy(s => s.Position.X + s.Position.Y))
            DrawShip(ship, Projection.GridToWorld(ship.Position));
        if (_movingVisible && _movingShip is { } moving) DrawShip(moving, _movingPosition);
        if (_muzzle is { } muzzle)
        {
            DrawCircle(muzzle, 10, new Color(1, 0.65f, 0.2f, 0.6f));
            DrawCircle(muzzle, 5, new Color("fff3b0"));
        }
        if (ProjectilePosition is { } projectile)
        {
            DrawCircle(projectile + new Vector2(-3, -2), 6, new Color(1, 0.7f, 0.3f, 0.3f));
            DrawCircle(projectile, 3.5f, new Color("ffebae"));
        }
        if (_impact is { } impact)
        {
            DrawCircle(impact, _impactSize, new Color(1, 0.57f, 0.2f, Math.Max(0, 0.85f - _impactSize / 38)));
            DrawArc(impact, _impactSize + 4, 0, Mathf.Tau, 24, new Color("ffdb9d"), 2, true);
        }
        if (_feedback.Length > 0)
            DrawString(ThemeDB.FallbackFont, _feedbackPosition + new Vector2(-20, -35), _feedback,
                fontSize: 23, modulate: new Color("fff0be"));
    }

    private void DrawShip(ShipSnapshot ship, Vector2 center)
    {
        if (ship.Class == ShipClass.Balloon)
        {
            var balloon = center + new Vector2(0,-62);
            var color = new Color(ship.Owner == Side.Player ? "85dce5" : "eaa58d");
            DrawCircle(center,12,new Color(0,0,0,0.2f));
            DrawLine(balloon+new Vector2(-12,10),balloon+new Vector2(-6,32),color,2,true);
            DrawLine(balloon+new Vector2(12,10),balloon+new Vector2(6,32),color,2,true);
            DrawCircle(balloon,20,color); DrawArc(balloon,20,0,Mathf.Tau,32,new Color("f8ecc7"),2,true);
            DrawLine(balloon+new Vector2(0,-19),balloon+new Vector2(0,18),new Color("e2f6dd"),4,true);
            DrawRect(new Rect2(balloon+new Vector2(-7,29),new Vector2(14,9)),new Color("baa478"));
            if (ship.Id == SelectedId) DrawArc(balloon,25,0,Mathf.Tau,32,new Color("ffe298"),2,true);
            return;
        }
        float size = ship.Class switch { ShipClass.Mothership => 1.2f, ShipClass.Garrison => 0.75f,
            ShipClass.Kolonel => 1.05f, ShipClass.Togus => 0.7f, ShipClass.Fishing => 0.62f, _ => 0.9f };
        float facing = ship.Owner == Side.Player ? 1 : -1;
        Vector2 Point(float x, float y) => center + new Vector2(x * facing * size, y * size - 5);
        var accent = new Color(ship.Owner == Side.Player ? "67d6e9" : "f48d72");
        var hull = new[] { Point(-25, -1), Point(-19, -10), Point(14, -3), Point(29, 9), Point(12, 14), Point(-23, 6) };
        DrawSetTransform(center + new Vector2(0, 4), 0, new Vector2(1, 0.45f));
        DrawCircle(Vector2.Zero, 30 * size, new Color(0, 0, 0, 0.3f));
        DrawSetTransform(Vector2.Zero);
        if (ship.Id == SelectedId) DrawArc(center, 34 * size, 0, Mathf.Tau, 40, new Color("ffe298"), 2, true);
        DrawColoredPolygon(hull, new Color(ship.Owner == Side.Player ? "285b6b" : "773e38"));
        DrawPolyline(hull.Append(hull[0]).ToArray(), accent, 2, true);
        if (ship.Class == ShipClass.FishingDock)
        {
            DrawColoredPolygon(new[] {Point(-22,-14),Point(23,-9),Point(23,12),Point(-22,7)},new Color("a58a63"));
            for(int x=-20;x<=20;x+=8) DrawLine(Point(x,-12),Point(x,9),new Color("e9d8a9"),2,true);
            DrawLine(Point(-17,5),Point(-17,-25),accent,3,true); DrawLine(Point(17,9),Point(17,-21),accent,3,true);
        }
        else if (ship.Class == ShipClass.Togus)
        {
            DrawCircle(Point(0,0),8,accent); DrawLine(Point(-3,-2),Point(7,-23),new Color("d1c7a8"),8,true);
            DrawCircle(Point(7,-23),4,new Color("182c33"));
        }
        else if (ship.Class == ShipClass.Mothership)
        {
            DrawColoredPolygon(new[] { Point(-15, -4), Point(0, -8), Point(16, 0), Point(0, 5) }, accent.Darkened(0.15f));
            DrawLine(Point(-5, -3), Point(-5, -26), accent, 3, true);
            DrawColoredPolygon(new[] { Point(-5, -26), Point(13, -18), Point(-5, -15) }, new Color("fff2ca"));
        }
        else
        {
            int masts = ship.Class == ShipClass.Kolonel ? 3 : ship.Class == ShipClass.Invader ? 2 : 1;
            for (int i = 0; i < masts; i++)
            {
                float x = -10 + i * 10;
                DrawLine(Point(x, 0), Point(x, -26), accent, 2, true);
                DrawColoredPolygon(new[] { Point(x + 1, -26), Point(x + 1, -6), Point(x + 14, -8) },
                    new Color(ship.IsExhausted ? "82959b" : "f2e5c5"));
            }
            if (ship.Class == ShipClass.Fishing)
            {
                DrawCircle(Point(13, 3), 7, new Color("c6bf93"));
                for (int x = 7; x <= 19; x += 4) DrawLine(Point(x, -3), Point(x, 10), new Color("746e54"), 1);
            }
        }
        if(ship.Class==ShipClass.Mothership&&ship.HasMortar)
        {
            DrawCircle(Point(10,0),6,new Color("cfc3a0")); DrawLine(Point(10,0),Point(18,-17),new Color("dfd3b1"),6,true);
        }
        if (ship.IsVeteran)
        {
            DrawLine(Point(-20, 4), Point(20, 10), new Color("ffce59"), 3, true);
            DrawLine(Point(18, 0), Point(18, -24), new Color("ffce59"), 2, true);
            DrawColoredPolygon(new[] { Point(18, -24), Point(31, -19), Point(18, -14) }, new Color("ffce59"));
        }
        var hpPosition = center + new Vector2(-22, 20);
        DrawRect(new Rect2(hpPosition, new Vector2(44, 5)), new Color("10212b"));
        DrawRect(new Rect2(hpPosition, new Vector2(44f * (float)(ship.Health / ship.MaxHealth), 5)),
            ship.Health / ship.MaxHealth < 0.25 ? new Color("ff795c") : accent);
        var hpText = ship.Health.ToString("0");
        var hpAt = center + new Vector2(24,-22);
        DrawRect(new Rect2(hpAt+new Vector2(-3,-17),new Vector2(hpText.Length*13+6,23)),new Color(0.02f,0.07f,0.1f,0.62f));
        DrawString(ThemeDB.FallbackFont,hpAt,hpText,fontSize:22,modulate:new Color("ffffff"));
        for (int slot=0;slot<ship.ProgressGoal;slot++)
        {
            var rect = new Rect2(center + new Vector2(-ship.ProgressGoal*6+slot*12,30),new Vector2(9,6));
            DrawRect(rect,slot<ship.Progress ? new Color(ship.Class==ShipClass.Mothership?"83e9ba":"ffd66e") : new Color(0.03f,0.09f,0.12f,0.7f));
            DrawRect(rect,new Color(0.65f,0.82f,0.86f,0.8f),false,1);
        }
        if (ship.IsExhausted) DrawCircle(center + new Vector2(28, 14), 4, new Color("c8c4b4"));
    }

    private async Task TweenValue(double duration, Action<float> update)
    {
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(t => { update(t); QueueRedraw(); }), 0f, 1f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Animate(CommandResult result, Vector2? targetBefore = null)
    {
        try
        {
            var actor = Battle.FindObserved(Side.Player, result.ActorId);
            if (result.Kind == CommandKind.Move && result.Movement is { Count: > 1 } frames)
            {
                // The model has already resolved the order: replay only its observed segments.
                var moving = Battle.Find(result.ActorId)!;
                _movingShip = ShipSnapshot.From(moving); _movingId = moving.Id;
                _movingPosition = Projection.GridToWorld(frames[0].Position);
                for (int i = 1; i < frames.Count; i++)
                {
                    var from = Projection.GridToWorld(frames[i - 1].Position);
                    var to = Projection.GridToWorld(frames[i].Position);
                    _movingVisible = frames[i - 1].VisibleToPlayer && frames[i].VisibleToPlayer;
                    if (_movingVisible) await TweenValue(0.10, t => _movingPosition = from.Lerp(to, t));
                    else { _movingPosition = to; QueueRedraw(); }
                }
                _movingId = 0; _movingShip = null;
            }
            else if (result.Kind == CommandKind.Attack && result.Shots is { } shots)
            {
                // Preserve pre-impact health and ships which the model has already sunk.
                foreach (var shot in shots)
                {
                    _snapshots.TryAdd(shot.Attacker.Id, shot.Attacker);
                    _snapshots.TryAdd(shot.Target.Id, shot.Target);
                    _suppressed.Add(shot.Attacker.Id); _suppressed.Add(shot.Target.Id);
                }
                foreach (var shot in shots)
                {
                    var from = Projection.GridToWorld(shot.Attacker.Position) + new Vector2(0, -14);
                    var to = Projection.GridToWorld(shot.Target.Position) + new Vector2(0, -6);
                    _muzzle = from;
                    await TweenValue(0.10, _ => { });
                    _muzzle = null;
                    float height = shot.IsMortar?Math.Clamp(from.DistanceTo(to)*.65f,110,240):Math.Clamp(from.DistanceTo(to)*.35f,45,120);
                    await TweenValue(shot.IsMortar?0.7:0.42, t => ProjectilePosition = from.Lerp(to, t) + new Vector2(0, -4 * height * t * (1 - t)));
                    ProjectilePosition = null;
                    if (shot.TargetSunk) _snapshots.Remove(shot.Target.Id);
                    else _snapshots[shot.Target.Id] = shot.Target with { Health = shot.Target.Health - shot.Damage };
                    if (shot.Promoted)
                        _snapshots[shot.Attacker.Id] = shot.Attacker with
                        { IsVeteran = true, MaxHealth = Ship.Whole(shot.Attacker.MaxHealth * 1.25), Health = Ship.Whole(shot.Attacker.MaxHealth * 1.25), Progress = 3 };
                    _impact = to; _feedbackPosition = to;
                    _feedback = (shot.IsCounterattack ? "Ответ −" : "−") + shot.Damage.ToString("0.##");
                    await TweenValue(0.24, t => _impactSize = 4 + 26 * t);
                    _impact = null; _feedback = "";
                }
            }
            else if (result.Kind == CommandKind.Repair && actor is not null)
            {
                _feedback = $"+{result.Amount:0.##}"; _feedbackPosition = Projection.GridToWorld(actor.Position);
                await TweenValue(0.25, _ => { });
            }
        }
        finally
        {
            _movingId = 0; _movingShip = null; _movingVisible = false;
            _snapshots.Clear(); _suppressed.Clear(); _muzzle = null; _impact = null; ProjectilePosition = null; _feedback = "";
            QueueRedraw();
        }
    }
}
