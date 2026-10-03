using System;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class WorldAmbience
{
    private readonly System.Collections.Generic.Dictionary<int, (float Born, Vector2 Origin)> _dockAlarms = new();
    internal int ScaredGullCount => _gulls.FindAll(g => g.Scared).Count;
    private Vector2 GullPosition(Gull gull)
    {
        float age = _time - gull.Born;
        if (gull.Scared) return gull.Start + gull.Velocity * age;
        return gull.Circling
            ? gull.Start + new Vector2(MathF.Cos(age * .38f + gull.Phase) * 28, MathF.Sin(age * .38f + gull.Phase) * 12)
            : gull.Start + gull.Velocity * age + new Vector2(MathF.Sin(age * .24f + gull.Phase) * 13, 0);
    }

    public void ScareGulls(GridPosition gun)
    {
        if (_battle is null || !_battle.Vision.IsVisible(Core.Units.Side.Player, gun)) return;
        var origin = BoardView.Projection.GridToWorld(gun);
        for (int i = 0; i < _gulls.Count; i++)
        {
            var gull = _gulls[i];
            var at = GullPosition(gull);
            var cell = BoardView.Projection.WorldToGrid(at);
            if (!_battle.Board.Contains(cell) || !_battle.Board.InRadius(gun, cell, 3)) continue;
            var away = (at - origin).Normalized();
            if (away == Vector2.Zero) away = -gull.Velocity.Normalized();
            float speed = gull.Scared ? gull.Velocity.Length() : gull.Velocity.Length() * 2;
            _gulls[i] = gull with { Start = at, Velocity = away * speed, Born = _time,
                Lifetime = Math.Max(1, gull.Lifetime - (_time - gull.Born)), Circling = false, Scared = true };
        }
        foreach (var dock in _docks)
        {
            var cell = BoardView.Projection.WorldToGrid(dock.Center);
            if (_battle.Board.InRadius(gun, cell, 3)) _dockAlarms[dock.Id] = (_time, origin);
        }
        QueueRedraw();
    }
}
