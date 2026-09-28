using System;
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
    private int _movingId;
    private Vector2 _movingPosition;
    private Vector2? _shotFrom;
    private Vector2 _shotTo;
    private string _feedback = "";
    private Vector2 _feedbackPosition;

    public override void _Draw()
    {
        foreach (var ship in Battle.Ships.OrderBy(s => s.Position.X + s.Position.Y))
        {
            var center = ship.Id == _movingId ? _movingPosition : Projection.GridToWorld(ship.Position);
            DrawShip(ship, center);
        }
        if (_shotFrom is { } from)
        {
            DrawLine(from, _shotTo, new Color("ffdc94"), 3, true);
            DrawCircle(_shotTo, 9, new Color(1, 0.75f, 0.35f, 0.7f));
        }
        if (_feedback.Length > 0)
            DrawString(ThemeDB.FallbackFont, _feedbackPosition + new Vector2(-20, -35), _feedback,
                fontSize: 23, modulate: new Color("fff0be"));
    }

    private void DrawShip(Ship ship, Vector2 center)
    {
        float size = ship.Definition.Class switch { ShipClass.Mothership => 1.2f, ShipClass.Garrison => 0.75f, ShipClass.Kolonel => 1.05f, _ => 0.9f };
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
        if (ship.Definition.Class == ShipClass.Mothership)
        {
            DrawColoredPolygon(new[] { Point(-15, -4), Point(0, -8), Point(16, 0), Point(0, 5) }, accent.Darkened(0.15f));
            DrawLine(Point(-5, -3), Point(-5, -26), accent, 3, true);
            DrawColoredPolygon(new[] { Point(-5, -26), Point(13, -18), Point(-5, -15) }, new Color("fff2ca"));
        }
        else
        {
            int masts = ship.Definition.Class == ShipClass.Kolonel ? 3 : ship.Definition.Class == ShipClass.Invader ? 2 : 1;
            for (int i = 0; i < masts; i++)
            {
                float x = -10 + i * 10;
                DrawLine(Point(x, 0), Point(x, -26), accent, 2, true);
                DrawColoredPolygon(new[] { Point(x + 1, -26), Point(x + 1, -6), Point(x + 14, -8) },
                    new Color(ship.IsExhausted ? "82959b" : "f2e5c5"));
            }
        }
        var hpPosition = center + new Vector2(-22, 20);
        DrawRect(new Rect2(hpPosition, new Vector2(44, 5)), new Color("10212b"));
        DrawRect(new Rect2(hpPosition, new Vector2(44f * ship.Health / ship.Definition.MaxHealth, 5)), accent);
        if (ship.IsExhausted) DrawCircle(center + new Vector2(28, 14), 4, new Color("c8c4b4"));
    }

    public async Task Animate(CommandResult result, Vector2? targetBefore)
    {
        var actor = Battle.Find(result.ActorId);
        if (result.Kind == CommandKind.Move && result.Path is { Count: > 1 } path)
        {
            _movingId = result.ActorId;
            _movingPosition = Projection.GridToWorld(path[0]);
            for (int i = 1; i < path.Count; i++)
            {
                var from = _movingPosition;
                var to = Projection.GridToWorld(path[i]);
                var tween = CreateTween();
                tween.TweenMethod(Callable.From<float>(t => { _movingPosition = from.Lerp(to, t); QueueRedraw(); }), 0f, 1f, 0.09);
                await ToSignal(tween, Tween.SignalName.Finished);
            }
            _movingId = 0;
        }
        else if (result.Kind == CommandKind.Attack && actor is not null && targetBefore is { } target)
        {
            _shotFrom = Projection.GridToWorld(actor.Position) + new Vector2(0, -10);
            _shotTo = target + new Vector2(0, -10);
            _feedback = $"−{result.Amount}";
            _feedbackPosition = target;
            QueueRedraw();
            await ToSignal(GetTree().CreateTimer(0.28), SceneTreeTimer.SignalName.Timeout);
            _shotFrom = null;
            _feedback = "";
        }
        else if (result.Kind == CommandKind.Repair && actor is not null)
        {
            _feedback = $"+{result.Amount}";
            _feedbackPosition = Projection.GridToWorld(actor.Position);
            QueueRedraw();
            await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
            _feedback = "";
        }
        QueueRedraw();
    }
}
