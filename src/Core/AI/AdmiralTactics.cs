using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>A bounded, stateless remaining-turn forecast using observed targets
/// and legal own-ship movement previews. Replan after each command/impact: this
/// also works when presentation calculates commands on temporary aggregates.</summary>
internal static class AdmiralTactics
{
    private sealed record Gun(Ship Actual, Ship Forecast, int Cost);
    private sealed record Action(int ShipId, GridPosition Position, bool Double, bool Move);
    private sealed record Plan(Ship Target, Action First, bool Lethal, double Damage, double Loss, int Orders);

    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        var previews = new Dictionary<int, IReadOnlyDictionary<GridPosition, int>>();
        var plans = new List<Plan>();
        // Only optical enemy records enter this forecast. Radar is handled by
        // the existing anonymous AttackAt policy without reading ship stats.
        foreach (var target in enemies.OrderBy(e => e.Health).ThenBy(e => e.Id).Take(12))
        {
            var reserved = new HashSet<GridPosition>();
            var guns = new List<Gun>();
            foreach (var ship in allies.Where(s => s.IsArmed && s.AttacksRemaining > 0).OrderBy(s => s.Id))
            {
                if (battle.CanAttack(ship.Id, target.Id))
                {
                    guns.Add(new(ship, Clone(ship, battle), 0));
                    continue;
                }
                // Mortars cannot fire after moving; already-moved ships receive
                // no second movement order, preventing within-turn oscillation.
                if (!ship.CanMove || ship.HasMoved || ship.HasMortar)
                    continue;
                if (!previews.TryGetValue(ship.Id, out var reachable))
                    previews[ship.Id] = reachable = battle.Reachable(ship.Id);
                var position = reachable.Where(p => p.Key != ship.Position && !reserved.Contains(p.Key))
                    .Select(p => new { Cell = p.Key, p.Value })
                    .Where(p => battle.Board.InRadius(p.Cell, target.Position, ship.Definition.AttackRange)
                        && (!target.IsAirborne || battle.AntiAirCovers(ship, p.Cell, target.Position))
                        && (battle.Board.InRadius(p.Cell, target.Position, ship.VisualRange)
                            || allies.Any(other => other.Id != ship.Id && battle.Board.InRadius(other.Position, target.Position, other.VisualRange))))
                    .OrderBy(p => danger(ship, p.Cell)).ThenBy(p => p.Value)
                    .ThenBy(p => p.Cell.Y).ThenBy(p => p.Cell.X).FirstOrDefault();
                if (position is null || ship.IsMothership && danger(ship, position.Cell) >= ship.Health * .6)
                    continue;
                reserved.Add(position.Cell);
                var probe = Clone(ship, battle, position.Cell);
                probe.HasMoved = true;
                guns.Add(new(ship, probe, position.Value));
            }
            if (guns.Count == 0) continue;
            if (Forecast(battle, target, guns) is { } plan)
                plans.Add(plan);
        }
        var best = plans.Where(p => p.Lethal || (!p.Target.IsMothership || p.Target.HealthRatio <= .5)
                && p.Loss < allies.First(s=>s.Id==p.First.ShipId).Health * .6
                && danger(allies.First(s=>s.Id==p.First.ShipId),p.First.Position) < allies.First(s=>s.Id==p.First.ShipId).Health)
            .OrderByDescending(p => p.Lethal)
            .ThenBy(p => p.Lethal ? p.Target.Health : 1000)
            .ThenBy(p => p.Loss).ThenBy(p => p.Orders)
            .ThenByDescending(p => p.Damage).ThenBy(p => p.Target.Id).FirstOrDefault();
        if (best is null) return null;
        var first = best.First;
        return first.Move
            ? battle.Move(battle.ActiveSide, first.ShipId, first.Position)
            : battle.Attack(battle.ActiveSide, first.ShipId, best.Target.Id, first.Double);
    }

    private static Plan? Forecast(BattleState battle, Ship original, List<Gun> guns)
    {
        var target = Clone(original, battle);
        Action? first = null;
        double loss = 0;
        int orders = 0;
        var moved = new HashSet<int>();
        for (int step = 0; step < 32 && target.Health > 0; step++)
        {
            var options = guns.Where(g => g.Forecast.Health > 0 && g.Forecast.AttacksRemaining > 0)
                .Select(g =>
                {
                    double damage = battle.Damage(g.Forecast, target);
                    bool twice = target.Health > damage && CanDouble(battle, g.Forecast, target);
                    double hit = Math.Min(target.Health, damage * (twice ? 2 : 1));
                    var after = Clone(target, battle);
                    after.Health -= hit;
                    double counter = after.Health > 0 && battle.CanCounterattack(after, g.Forecast)
                        ? Math.Min(g.Forecast.Health, battle.Damage(after, g.Forecast, true)) : 0;
                    return new { Gun = g, Damage = hit, Counter = counter, Double = twice, Kills = hit >= target.Health };
                })
                // A predictable suicidal shot is not readiness. A decisive
                // non-flagship trade is allowed, but never spend the flagship.
                .Where(o => o.Counter < o.Gun.Forecast.Health || o.Kills && !o.Gun.Actual.IsMothership)
                .OrderByDescending(o => o.Kills).ThenBy(o => o.Counter)
                .ThenBy(o => o.Gun.Cost > 0 ? 1 : 0).ThenByDescending(o => o.Damage)
                .ThenBy(o => o.Gun.Actual.Id).ToArray();
            var shot = options.FirstOrDefault();
            if (shot is null) break;
            first ??= new(shot.Gun.Actual.Id, shot.Gun.Forecast.Position, shot.Double,
                shot.Gun.Actual.Position != shot.Gun.Forecast.Position);
            target.Health -= shot.Damage;
            shot.Gun.Forecast.Health -= shot.Counter;
            shot.Gun.Forecast.AttacksUsed += shot.Double ? 2 : 1;
            loss += shot.Counter;
            orders += 1 + (shot.Gun.Cost > 0 && moved.Add(shot.Gun.Actual.Id) ? 1 : 0);
        }
        if (first is null) return null;
        return new(original, first, target.Health <= 0, original.Health - target.Health, loss, orders);
    }

    private static bool CanDouble(BattleState battle, Ship ship, Ship target) => battle.Rules.DoubleSalvo
        && ship.AttacksRemaining >= 2 && !battle.UsesMortar(ship, target.Position)
        && (ship.Definition.Class == ShipClass.Kolonel || ship.IsMothership && ship.SecondAttackUpgrade);

    private static Ship Clone(Ship ship, BattleState battle, GridPosition? position = null)
    {
        var copy = SavedShip.From(ship).Restore(battle.Rules);
        if (position is { } cell) copy.Position = cell;
        return copy;
    }
}
