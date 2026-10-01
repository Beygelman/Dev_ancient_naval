using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private void ApplySplash(CommandResult result)
    {
        foreach (var shot in result.Splash ?? Array.Empty<CombatShot>())
        {
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
                _blastDamage[-1 - _blastDamage.Count] = (Projection.GridToWorld(hit.Position), $"−{hit.Damage:0.##}");
    }
}
