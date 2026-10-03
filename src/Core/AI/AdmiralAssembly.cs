using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>Second-turn fleet assembly, bounded to four observed objectives and
/// 24 own hulls. Forecasts a fresh turn's charges, declining fire that cannot
/// finish a healthy flagship while escort guns are still coming into position.
/// No plan survives a command: save/resume needs no transient planner state.</summary>
internal static class AdmiralAssembly
{
    private sealed record Station(Ship Actual, Ship Ready, int Cost, double Exposure);
    private sealed record Formation(Ship Target, Station Move, double Loss, int Guns, double Damage);

    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        var armed = allies.Where(s => s.IsArmed && !s.IsAirborne && s.HealthRatio >= .5)
            .OrderBy(s => s.IsMothership).ThenBy(s => s.Id).Take(24).ToArray();
        if (armed.Count(s => !s.IsMothership) < 2) return null;
        var reachable = new Dictionary<int, IReadOnlyDictionary<GridPosition, int>>();
        var formations = new List<Formation>();
        foreach (var target in enemies.Where(e => !e.IsAirborne)
            .OrderByDescending(e => e.IsMothership).ThenByDescending(e => e.IsArmed)
            .ThenBy(e => e.Health).ThenBy(e => e.Id).Take(4))
        {
            var reserved = new HashSet<GridPosition>();
            var stations = new List<Station>();
            foreach (var ship in armed)
            {
                var current = Ready(ship, battle, ship.Position);
                if (battle.WeaponCovers(current, target.Position))
                {
                    reserved.Add(ship.Position);
                    stations.Add(new(ship, current, 0, danger(ship, ship.Position)));
                    continue;
                }
                if (ship.IsMothership || !ship.CanMove || ship.HasMoved) continue;
                if (!reachable.TryGetValue(ship.Id, out var cells))
                    reachable[ship.Id] = cells = battle.Reachable(ship.Id);
                // Preview costs contain no occupied landing cells. Current own
                // gun stations are also reserved before considering an advance.
                var coverage = Ready(ship, battle, ship.Position);
                var station = cells.Where(p => p.Key != ship.Position && !reserved.Contains(p.Key))
                    .Where(p =>
                    {
                        coverage.Position = p.Key;
                        return battle.WeaponCovers(coverage, target.Position);
                    })
                    .OrderBy(p => danger(ship, p.Key)).ThenBy(p => p.Value)
                    .ThenBy(p => p.Key.Y).ThenBy(p => p.Key.X)
                    .Select(p => (Cell: (GridPosition?)p.Key, Cost: p.Value))
                    .FirstOrDefault();
                if (station.Cell is not { } position) continue;
                reserved.Add(position);
                stations.Add(new(ship, Ready(ship, battle, position), station.Cost, danger(ship, position)));
            }
            var movers = stations.Where(s => s.Cost > 0).ToArray();
            if (stations.Count < 2 || movers.Length == 0) continue;
            // A hostile fleet has finite shots. It cannot apply its full volley
            // independently to every escort. Use both pooled exposure and each
            // hull's share, then replay reduced-health guns with return fire.
            double incoming = stations.Max(s => s.Exposure);
            double escortsHealth = stations.Where(s => !s.Actual.IsMothership).Sum(s => s.Ready.Health);
            if (incoming >= escortsHealth * .7) continue;
            int exposed = Math.Max(1, stations.Count(s => s.Exposure > 0));
            foreach (var station in stations)
                if (!station.Actual.IsMothership)
                    station.Ready.Health = Math.Max(1, station.Ready.Health - station.Exposure / exposed);
            var forecast = Volley(battle, target, stations);
            // The target may move on its turn. This is readiness for a nearby
            // engagement, not certainty: every next command re-observes/replans.
            if (!forecast.Lethal || forecast.Loss + incoming >= escortsHealth * .8) continue;
            var first = movers.OrderBy(s => s.Exposure).ThenBy(s => s.Cost)
                .ThenBy(s => s.Actual.Id).First();
            formations.Add(new(target, first, forecast.Loss + incoming,
                stations.Count, forecast.Damage));
        }
        var best = formations.OrderByDescending(p => p.Target.IsMothership)
            .ThenBy(p => p.Loss).ThenByDescending(p => p.Guns)
            .ThenByDescending(p => p.Damage).ThenBy(p => p.Target.Id).FirstOrDefault();
        return best is null ? null : battle.Move(battle.ActiveSide, best.Move.Actual.Id, best.Move.Ready.Position);
    }

    private static (bool Lethal, double Damage, double Loss) Volley(BattleState battle,
        Ship original, IReadOnlyList<Station> stations)
    {
        var target = Ready(original, battle, original.Position);
        double loss = 0;
        for (int order = 0; order < 48 && target.Health > 0; order++)
        {
            var next = stations.Where(s => s.Ready.Health > 0 && s.Ready.AttacksRemaining > 0)
                .Select(s =>
                {
                    double damage = battle.Damage(s.Ready, target);
                    bool twice = target.Health > damage && battle.Rules.DoubleSalvo
                        && s.Ready.AttacksRemaining >= 2 && !battle.UsesMortar(s.Ready, target.Position)
                        && (s.Ready.Definition.Class == ShipClass.Kolonel || s.Ready.IsMothership && s.Ready.SecondAttackUpgrade);
                    double hit = Math.Min(target.Health, damage * (twice ? 2 : 1));
                    var after = Ready(target, battle, target.Position);
                    after.Health -= hit;
                    double counter = battle.CanCounterattack(after, s.Ready)
                        ? battle.Damage(after, s.Ready, true) : 0;
                    return new { Station = s, Hit = hit, Counter = counter, Charges = twice ? 2 : 1 };
                })
                .Where(s => s.Counter < s.Station.Ready.Health)
                .OrderByDescending(s => s.Hit >= target.Health).ThenBy(s => s.Counter)
                .ThenByDescending(s => s.Hit).ThenBy(s => s.Station.Actual.Id).FirstOrDefault();
            if (next is null) break;
            target.Health -= next.Hit;
            next.Station.Ready.Health -= next.Counter;
            next.Station.Ready.AttacksUsed += next.Charges;
            loss += next.Counter;
        }
        return (target.Health <= 0, original.Health - target.Health, loss);
    }

    private static Ship Ready(Ship ship, BattleState battle, GridPosition position)
    {
        var probe = SavedShip.From(ship).Restore(battle.Rules);
        probe.Position = position;
        probe.AttacksUsed = 0;
        probe.HasMoved = false;
        probe.IsExhausted = false;
        probe.MovementLocked = false;
        return probe;
    }
}
