using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
public partial class FleetView
{
    private void ApplySplash(CommandResult result)
    {
        foreach (var shot in result.Splash ?? Array.Empty<CombatShot>())
        {
<<<<<<< Updated upstream
            if (!shot.TargetVisibleToPlayer)
                continue;
            var point = Projection.GridToWorld(shot.Target.Position);
            HitEffect(shot.Target, point, (point - Projection.GridToWorld(shot.Attacker.Position)).Normalized(), false);
            if (shot.TargetSunk)
                _snapshots.Remove(shot.Target.Id);
            else
                _snapshots[shot.Target.Id] = shot.Target with
                {
                    Health = shot.Target.Health - shot.Damage
                };
            _blastDamage[shot.Target.Id] = (HealthAnchor(point, shot.Target.Class), $"−{shot.Damage:0.##}");
        }

        foreach (var hit in result.AreaHits ?? Array.Empty<AreaHit>())
            if (hit.VisibleToPlayer)
                _blastDamage[-1 - _blastDamage.Count] = (BoardView.TownHealthAnchor(Projection.GridToWorld(hit.Position)), $"−{hit.Damage:0.##}");
=======
            if (!shot.TargetVisibleToPlayer) continue;
            var point = Projection.GridToWorld(shot.Target.Position);
            HitEffect(shot.Target, point, (point - Projection.GridToWorld(shot.Attacker.Position)).Normalized(), false);
            if (shot.TargetSunk) _snapshots.Remove(shot.Target.Id);
            else _snapshots[shot.Target.Id] = shot.Target with { Health = shot.Target.Health - shot.Damage };
            _blastDamage[shot.Target.Id] = (point, $"−{shot.Damage:0.##}");
        }
        foreach (var hit in result.AreaHits ?? Array.Empty<AreaHit>())
            if (hit.VisibleToPlayer)
                _blastDamage[-1 - _blastDamage.Count] = (Projection.GridToWorld(hit.Position), $"−{hit.Damage:0.##}");
    }

    private void DrawFishingHarbor(Func<float, float, Vector2> p, Color accent)
    {
        // An open horseshoe: the center is actual sea, not a filled boat deck.
        Vector2[] Arc(float radius) => Enumerable.Range(0, 25).Select(i =>
        { float a = Mathf.Pi * (.05f + i / 24f * 1.65f); return p(MathF.Cos(a) * radius, MathF.Sin(a) * radius * .55f); }).ToArray();
        var outer = Arc(33); var inner = Arc(22);
        for (int i = 0; i < outer.Length - 1; i++)
            DrawColoredPolygon(new[] { outer[i], outer[i + 1], inner[i + 1], inner[i] }, new Color("d7cbb0"));
        DrawPolyline(outer, new Color("f5efdc"), 2.3f, true);
        DrawPolyline(inner, new Color("918d78"), 1.5f, true);
        foreach (int index in new[] { 3, 7, 12, 17, 21 })
        {
            float a = Mathf.Pi * (.05f + index / 24f * 1.65f);
            float x = MathF.Cos(a) * 28, y = MathF.Sin(a) * 15;
            DrawColoredPolygon(new[] { p(x - 4, y), p(x + 5, y + 1), p(x + 5, y - 9), p(x - 4, y - 10) }, new Color("ede2c8"));
            DrawColoredPolygon(new[] { p(x - 6, y - 10), p(x, y - 15), p(x + 7, y - 9), p(x + 1, y - 6) }, accent.Darkened(.22f));
            DrawLine(p(x, y - 1), p(x, y - 5), new Color("536a65"), 2);
        }
        foreach (int index in new[] { 0, 5, 11, 18, 24 })
        {
            var a = outer[index]; var b = inner[index].Lerp(p(0, 0), .3f);
            DrawLine(a, b, new Color("b29d75"), 4.5f, true);
            DrawCircle(b, 2.5f, new Color("eee0ba"));
        }
        DrawLine(p(-27, -2), p(-27, -24), new Color("e6d6af"), 2, true);
        DrawColoredPolygon(new[] { p(-27, -24), p(-14, -20), p(-27, -16) }, accent);
>>>>>>> Stashed changes
    }
}
