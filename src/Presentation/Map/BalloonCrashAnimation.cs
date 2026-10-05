using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    private readonly Dictionary<int, BalloonCrash> _crashes = new();
    private readonly HashSet<int> _crashImpacts = new();
    private readonly Dictionary<int, (Vector2 Center, float Progress)> _fallingBalloons = new();
    private PresentedCommand? _crashPresentation;
    internal int BalloonCrashImpactCount => _crashImpacts.Count;

    private void PrepareBalloonCrashes(CommandResult result, PresentedCommand? presentation)
    {
        _crashes.Clear();
        _crashImpacts.Clear();
        _crashPresentation = presentation;
        foreach (var crash in result.BalloonCrashes)
        {
            _crashes[crash.Balloon.Id] = crash;
            foreach (var hit in crash.Ships)
                if (hit.TargetVisibleToPlayer)
                {
                    _snapshots.TryAdd(hit.Target.Id, hit.Target);
                    _suppressed.Add(hit.Target.Id);
                }
        }
    }

    private void CommitBalloonCrash(int id)
    {
        if (!_crashes.TryGetValue(id, out var crash) || !_crashImpacts.Add(id)) return;
        _crashPresentation?.Impact(crash.ImpactKey);
        var center = Projection.GridToWorld(crash.Balloon.Position);
        bool originVisible = crash.Balloon.Owner == Core.Units.Side.Player
            || Battle.Vision.IsVisible(Core.Units.Side.Player, crash.Balloon.Position);
        if (originVisible)
        {
            EmitRipple(center, 2.6f);
            _mist.Add(new(center, _clock, 1.35f));
            for (int petal = 0; petal < 6; petal++)
            {
                var at = center + new Vector2(MathF.Cos(petal * Mathf.Tau / 6) * 14,
                    MathF.Sin(petal * Mathf.Tau / 6) * 7);
                EmitRipple(at, 1.3f);
            }
        }
        foreach (var hit in crash.Ships)
        {
            if (!hit.TargetVisibleToPlayer) continue;
            var point = Projection.GridToWorld(hit.Target.Position);
            HitEffect(hit.Target, point, (point - center).Normalized(), false);
            if (hit.TargetSunk) _snapshots.Remove(hit.Target.Id);
            else _snapshots[hit.Target.Id] = hit.Target with { Health = hit.Target.Health - hit.Damage };
            _blastDamage[hit.Target.Id] = (HealthAnchor(point, hit.Target.Class), $"−{hit.Damage:0}");
        }
        foreach (var hit in crash.Villages)
            if (hit.VisibleToPlayer)
            {
                var town = Battle.VillageAt(hit.Position);
                var anchor = town is not null && Landscape is not null ? Landscape.TownHealthAnchor(town)
                    : BoardView.TownHealthAnchor(Projection.GridToWorld(hit.Position));
                _blastDamage[-1 - _blastDamage.Count] = (anchor, $"−{hit.Damage:0}");
            }
    }

    private async Task SinkBalloonVictims(int id)
    {
        if (!_crashes.TryGetValue(id, out var crash)) return;
        foreach (var hit in crash.Ships.Where(hit => hit.TargetSunk))
            await Sink(hit.Target, hit.TargetVisibleToPlayer);
        _blastDamage.Clear();
    }

    private void DrawFallingBalloonFire()
    {
        foreach (var fall in _fallingBalloons.Values)
        {
            float burn = Math.Clamp(1 - fall.Progress / .48f, 0, 1);
            if (burn <= 0) continue;
            var flame = fall.Center + new Vector2(fall.Progress * 14, -29 + fall.Progress * 37);
            Ink.DrawCircle(flame, 8 * burn, new(1, .53f, .20f, .21f));
            for (int tongue = 0; tongue < 5; tongue++)
            {
                float sway = MathF.Sin(_clock * 9 + tongue * 2.4f);
                var basePoint = flame + new Vector2((tongue - 2) * 2.2f, 1);
                var tip = basePoint + new Vector2(sway * 3, -(7 + tongue % 3 * 4) * burn);
                DrawProjectedPolygon(new[] { basePoint + new Vector2(-2, 0), tip, basePoint + new Vector2(2, 0) },
                    new(tongue % 2 == 0 ? "fbcf70" : "ec8650"));
            }
            for (int spark = 0; spark < 10; spark++)
            {
                float phase = (_clock * .7f + spark * .1f) % 1;
                var at = flame + new Vector2(MathF.Sin(spark * 3.1f) * phase * 17, -phase * 29);
                Ink.DrawCircle(at, .6f, new(1, .75f, .34f, (1 - phase) * burn));
            }
        }
    }
}
