using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private async Task AnimateOutposts(CommandResult result, PresentedCommand? presentation)
    {
        foreach (var shot in result.OutpostShots ?? Array.Empty<OutpostShot>())
            if (shot.TargetVisibleToPlayer)
            {
                _snapshots.TryAdd(shot.Target.Id, shot.Target);
                _suppressed.Add(shot.Target.Id);
            }

        foreach (var shot in result.OutpostShots ?? Array.Empty<OutpostShot>())
        {
            if (!shot.OriginVisibleToPlayer && !shot.TargetVisibleToPlayer)
            {
                presentation?.Impact("outpost:" + shot.VillageId);
                continue;
            }

            if (shot.TargetVisibleToPlayer && FocusTarget is not null) await FocusTarget(shot.Target.Position);
            var from = Projection.GridToWorld(shot.Origin) + new Vector2(0, -16);
            var to = Projection.GridToWorld(shot.Target.Position) + new Vector2(0, -6);
            var direction = (to - from).Normalized();
            if (shot.OriginVisibleToPlayer)
                EmitSmoke(from, direction, .9f, false);
            await TweenValue(.48, t => ProjectilePosition = (shot.OriginVisibleToPlayer || t > .85f) ? from.Lerp(to, t) + new Vector2(0, -110 * t * (1 - t)) : null);
            ProjectilePosition = null;
            presentation?.Impact("outpost:" + shot.VillageId);
            if (shot.TargetVisibleToPlayer)
            {
                HitEffect(shot.Target, to, direction, false);
                if (shot.TargetSunk)
                    _snapshots.Remove(shot.Target.Id);
                else
                    _snapshots[shot.Target.Id] = shot.Target with
                    {
                        Health = shot.Target.Health - shot.Damage
                    };
                _feedbackPosition = HealthAnchor(Projection.GridToWorld(shot.Target.Position), shot.Target.Class);
                _feedbackColor = new("ff8f85");
                _feedback = $"−{shot.Damage:0}";
                await TweenValue(.32, _ =>
                {
                });
                _feedback = "";
                if (shot.TargetSunk)
                    await Sink(shot.Target, true);
            }
        }
    }
}
