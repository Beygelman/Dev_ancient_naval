using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Grid;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Plays visibility-safe commands and commits staged Core impacts after each flight.</summary>
public partial class FleetView
{
    private async Task TweenValue(double duration, Action<float> update)
    {
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            update(t);
            QueueRedraw();
        }), 0f, 1f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Animate(CommandResult result, Vector2? targetBefore = null, PresentedCommand? presentation = null)
    {
        EnsureVisualBattle();
        ShowIncome(result);
        CompletedSalvos.Clear();
        _playedWrecks.Clear();
        foreach (var doomed in presentation?.DestroyedShips ?? Array.Empty<ShipSnapshot>())
            if (doomed.Owner == Side.Player || Battle.Vision.IsVisible(Side.Player, doomed.Position))
            {
                _snapshots.TryAdd(doomed.Id, doomed);
                _suppressed.Add(doomed.Id);
            }

        _feedbackColor = new("ff8f85");
        try
        {
            await AnimateOutposts(result, presentation);
            foreach (var splash in result.Splash ?? Array.Empty<CombatShot>())
                if (splash.TargetVisibleToPlayer)
                {
                    _snapshots.TryAdd(splash.Target.Id, splash.Target);
                    _suppressed.Add(splash.Target.Id);
                }

            var actor = Battle.FindObserved(Side.Player, result.ActorId);
            if (result.Kind == CommandKind.Move && result.Movement is { Count: > 1 } frames)
            {
                await AnimateTravel(result.ActorId, frames);
            }
            else if (result.Kind == CommandKind.Bomb || (result.Kind == CommandKind.EndTurn && (result.Shots is { Count: > 0 } || result.Path is { Count: > 0 })))
            {
                await AnimateBombs(result, presentation);
            }
            else if (result.Kind == CommandKind.Attack && result.Shots is { Count: > 0 } shots)
            {
                // Preserve pre-impact health and ships which the model has already sunk.
                foreach (var shot in shots)
                {
                    if (shot.AttackerVisibleToPlayer)
                        _snapshots.TryAdd(shot.Attacker.Id, shot.Attacker);
                    if (shot.TargetVisibleToPlayer)
                        _snapshots.TryAdd(shot.Target.Id, shot.Target);
                    _suppressed.Add(shot.Attacker.Id);
                    _suppressed.Add(shot.Target.Id);
                }

                foreach (var shot in shots)
                {
                    await AnimateSalvo(shot.Attacker, shot.Target.Position, shot.Target, shot.IsMortar, shot.AttackerVisibleToPlayer, shot.TargetVisibleToPlayer);
                    presentation?.Impact(shot.IsCounterattack ? "counter" : "attack");
                    var to = Projection.GridToWorld(shot.Target.Position) + new Vector2(0, -6);
                    if (shot.TargetSunk)
                        _snapshots[shot.Target.Id] = shot.Target with
                        {
                            Health = 0
                        };
                    else if (shot.TargetVisibleToPlayer)
                        _snapshots[shot.Target.Id] = shot.Target with
                        {
                            Health = shot.Target.Health - shot.Damage
                        };
                    if (shot.Promoted && shot.AttackerVisibleToPlayer)
                        _snapshots[shot.Attacker.Id] = shot.Attacker with
                        {
                            IsVeteran = true,
                            MaxHealth = Ship.Whole(shot.Attacker.MaxHealth * 1.25),
                            Health = Ship.Whole(shot.Attacker.MaxHealth * 1.25),
                            Progress = 3
                        };
                    _impact = shot.TargetVisibleToPlayer ? to : null;
                    _feedbackPosition = HealthAnchor(Projection.GridToWorld(shot.Target.Position), shot.Target.Class);
                    _feedbackColor = new(shot.IsCounterattack ? "ffe28c" : "ff8f85");
                    _feedback = shot.TargetVisibleToPlayer ? (shot.IsCounterattack ? "Counter −" : "−") + shot.Damage.ToString("0") : "";
                    if (!shot.IsCounterattack)
                        ApplySplash(result);
                    await TweenValue(0.38, t =>
                    {
                        _impactSize = 4 + 26 * t;
                        _feedbackRise = t * 22;
                    });
                    _impact = null;
                    _feedback = "";
                    if (shot.TargetSunk)
                        await Sink(shot.Target, shot.TargetVisibleToPlayer);
                    if (!shot.IsCounterattack)
                        foreach (var splash in result.Splash ?? Array.Empty<CombatShot>())
                            if (splash.TargetSunk)
                                await Sink(splash.Target, splash.TargetVisibleToPlayer);
                    _blastDamage.Clear();
                    _impact = null;
                    _feedback = "";
                }
            }
            else if (result.Kind == CommandKind.Attack && result.StructureHit is { } hit)
            {
                if (hit.AttackerVisibleToPlayer)
                    _snapshots[hit.Attacker.Id] = hit.Attacker;
                _suppressed.Add(hit.Attacker.Id);
                await AnimateSalvo(hit.Attacker, hit.Position, null, hit.IsMortar, hit.AttackerVisibleToPlayer, hit.TargetVisibleToPlayer);
                presentation?.Impact("village");
                ApplySplash(result);
                var town = Projection.GridToWorld(hit.Position) + new Vector2(0, -9);
                _feedbackPosition = Projection.GridToWorld(hit.Position) + new Vector2(-8, -34);
                _feedback = hit.TargetVisibleToPlayer ? $"−{result.Amount:0.##}" : "";
                if (hit.CounterDamage > 0 && (hit.AttackerVisibleToPlayer || hit.TargetVisibleToPlayer))
                {
                    var to = Projection.GridToWorld(hit.Attacker.Position) + new Vector2(0, -5);
                    if (hit.TargetVisibleToPlayer)
                        EmitSmoke(town, (to - town).Normalized(), .8f, false);
                    await TweenValue(.24, t => ProjectilePosition = town.Lerp(to, t) + new Vector2(0, -180 * t * (1 - t)));
                    ProjectilePosition = null;
                    presentation?.Impact("village-counter");
                    if (hit.AttackerVisibleToPlayer)
                    {
                        HitEffect(hit.Attacker, to, (to - town).Normalized(), false);
                        _feedbackPosition = HealthAnchor(Projection.GridToWorld(hit.Attacker.Position));
                        _feedbackColor = new("ffe28c");
                        _feedback = $"Counter −{hit.CounterDamage:0.##}";
                    }

                    if (Battle.Find(hit.Attacker.Id)is null)
                        await Sink(hit.Attacker, hit.AttackerVisibleToPlayer);
                    else if (hit.AttackerVisibleToPlayer)
                        _snapshots[hit.Attacker.Id] = hit.Attacker with
                        {
                            Health = hit.Attacker.Health - hit.CounterDamage
                        };
                }

                await TweenValue(.2, _ =>
                {
                });
            }
            else if (result.Kind == CommandKind.Repair && (actor is not null || Battle.Villages.Any(v => v.Id == result.TargetId)))
            {
                _feedbackColor = new("85e6a0");
                _feedback = $"+{result.Amount:0.##}";
                _feedbackPosition = actor is not null ? HealthAnchor(Projection.GridToWorld(actor.Position), actor.Definition.Class) : Projection.GridToWorld(Battle.Villages.First(v => v.Id == result.TargetId).Position) + new Vector2(-8, -34);
                await TweenValue(0.25, t => _feedbackRise = t * 20);
            }

            foreach (var doomed in presentation?.DestroyedShips ?? Array.Empty<ShipSnapshot>())
                if (!_playedWrecks.Contains(doomed.Id))
                    await Sink(doomed, doomed.Owner == Side.Player || Battle.Vision.IsVisible(Side.Player, doomed.Position));
            presentation?.Finish();
            foreach (var heal in result.HealingReceipts ?? Array.Empty<HealingReceipt>())
                if (heal.VisibleToPlayer)
                {
                    _feedbackPosition = heal.IsVillage ? Projection.GridToWorld(heal.Position) + new Vector2(-8, -34) : HealthAnchor(Projection.GridToWorld(heal.Position));
                    _feedbackColor = new("85e6a0");
                    _feedback = $"+{heal.Amount:0}";
                    await TweenValue(.3, t => _feedbackRise = t * 20);
                }
        }
        finally
        {
            presentation?.Finish();
            _sinking.Clear();
            _movingId = 0;
            _movingShip = null;
            _movingVisible = false;
            _snapshots.Clear();
            _suppressed.Clear();
            _projectiles.Clear();
            _muzzle = null;
            _impact = null;
            ProjectilePosition = null;
            _feedback = "";
            _blastCenter = null;
            _blastDamage.Clear();
            QueueRedraw();
        }
    }

    private async Task AnimateBombs(CommandResult result, PresentedCommand? presentation)
    {
        var shots = result.Shots ?? Array.Empty<CombatShot>();
        foreach (var shot in shots)
        {
            if (shot.TargetVisibleToPlayer)
                _snapshots.TryAdd(shot.Target.Id, shot.Target);
            _suppressed.Add(shot.Target.Id);
        }

        var centers = (result.Path ?? Array.Empty<GridPosition>()).Concat(shots.Where(s => s.AttackerVisibleToPlayer).Select(s => s.Attacker.Position)).Distinct().ToArray();
        foreach (var cell in centers)
        {
            var group = shots.Where(s => s.Attacker.Position == cell).ToArray();
            bool visible = result.Kind == CommandKind.EndTurn || Battle.FindObserved(Side.Player, result.ActorId)is not null || Battle.Vision.IsVisible(Side.Player, cell) || group.Any(s => s.TargetVisibleToPlayer);
            if (!visible)
                continue;
            var center = Projection.GridToWorld(cell);
            if (result.Kind == CommandKind.Bomb)
                await TweenValue(.34, t => ProjectilePosition = center + new Vector2(0, -56 * (1 - t * t)));
            ProjectilePosition = null;
            presentation?.Impact("bomb");
            ApplyBombDamage(group);
            if (result.Kind == CommandKind.EndTurn)
            {
                _blastCenter = center;
                await TweenValue(.5, t => _blastProgress = t);
                _blastCenter = null;
            }
            else
            {
                _impact = center;
                await TweenValue(.3, t => _impactSize = 5 + 30 * t);
                _impact = null;
            }

            foreach (var shot in group)
                if (shot.TargetSunk)
                    await Sink(shot.Target, shot.TargetVisibleToPlayer);
            _blastDamage.Clear();
        }

        // Nearby visible ships can be caught by a blast whose source remains in
        // fog. Show their damage without revealing the hidden balloon's location.
        foreach (var shot in shots.Where(s => !centers.Contains(s.Attacker.Position) && s.TargetVisibleToPlayer))
        {
            ApplyBombDamage(new[] { shot });
            _impact = Projection.GridToWorld(shot.Target.Position);
            await TweenValue(.24, t => _impactSize = 5 + 24 * t);
            _impact = null;
            _blastDamage.Clear();
        }
    }

    private void ApplyBombDamage(IEnumerable<CombatShot> shots)
    {
        foreach (var shot in shots)
        {
            if (shot.TargetSunk)
                _snapshots.Remove(shot.Target.Id);
            else if (shot.TargetVisibleToPlayer)
                _snapshots[shot.Target.Id] = shot.Target with
                {
                    Health = shot.Target.Health - shot.Damage
                };
            if (shot.TargetVisibleToPlayer)
                _blastDamage[shot.Target.Id] = (HealthAnchor(Projection.GridToWorld(shot.Target.Position)), $"−{shot.Damage:0}");
        }
    }
}
