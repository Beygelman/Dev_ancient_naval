using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;

/// <summary>Decisions use this side's observations, radar contacts and terrain memory.</summary>
public static class SimpleOpponent
{
    public static CommandResult Step(BattleState battle)
    {
        if (battle.IsOver) return CommandResult.Rejected("Бой завершён.");
        var side = battle.ActiveSide;
        if (battle.PendingUpgrade(side) is { } upgrading)
            return battle.ChooseUpgrade(side, upgrading.Id, upgrading.PendingUpgradeLevel == 2 ? UpgradeChoice.Income : UpgradeChoice.SecondAttack);
        var allies = battle.OwnShips(side).ToArray();
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side && !s.IsAirborne).ToArray();
        foreach (var ship in allies)
        {
            var target = enemies.Where(t => battle.CanAttack(ship.Id, t.Id))
                .OrderBy(t => t.Health <= battle.Damage(ship, t) ? 0 : 1)
                .ThenBy(t => t.Definition.Class == ShipClass.Mothership ? 0 : 1).ThenBy(t => t.Health).FirstOrDefault();
            if (target is not null) return battle.Attack(side, ship.Id, target.Id);
        }
        foreach (var ship in allies.Where(s => s.HasRadar))
            foreach (var contact in battle.Vision.Contacts(side))
                if (battle.TargetCells(ship.Id).Contains(contact)) return battle.AttackAt(side, ship.Id, contact);
        foreach (var ship in allies)
            if (ship.CanRepair && ship.HealthRatio <= 0.5) return battle.Repair(side, ship.Id);
        foreach (var collector in allies.Where(s => s.Definition.CollectionRange > 0))
            if (battle.Credits(side) >= BattleState.CollectionPrice && battle.CollectionCells(collector.Id).FirstOrDefault() is var fish && battle.CollectionCells(collector.Id).Contains(fish))
                return battle.Collect(side, collector.Id, fish);
        foreach (var ship in allies.Where(s => s.Definition.RadarPrice > 0))
            if (battle.Credits(side) >= 8 && battle.RadarBlockReason(side, ship.Id) is null) return battle.BuyRadar(side, ship.Id);
        foreach (var mother in allies.Where(s => s.Definition.Class == ShipClass.Mothership))
        {
            // Building locks movement. Reserve regular turns for advancing the flagship,
            // otherwise low-cost replacements can keep both fleets anchored indefinitely.
            if (battle.Round % 3 == 0 && mother.CanMove) continue;
            var preferred = allies.Count(s => s.Definition.Class == ShipClass.Fishing) < 2 && enemies.Length == 0
                ? ShipClass.Fishing : allies.Length % 3 == 0 ? ShipClass.Kolonel : ShipClass.Invader;
            foreach (var type in new[] { preferred, ShipClass.Garrison }.Distinct())
            {
                if (battle.BuildBlockReason(side, mother.Id, type) is not null) continue;
                var spawn = battle.SpawnCells(mother.Id).OrderBy(p => enemies.Length > 0
                    ? enemies.Min(e => BattleState.Distance(p, e.Position))
                    : BattleState.Distance(p, new(battle.Board.Width / 2, battle.Board.Height / 2))).First();
                return battle.Build(side, mother.Id, type, spawn);
            }
        }
        foreach (var ship in allies)
        {
            // One movement order per ship per turn prevents oscillation when a route is blocked.
            if (!ship.CanMove || ship.HasMoved) continue;
            if (ship.IsAirborne)
            {
                var destination = battle.Reachable(ship.Id).Keys.Where(p=>p!=ship.Position).OrderBy(p => battle.Vision.LastSeen(side,p)).FirstOrDefault(ship.Position);
                if (destination != ship.Position) return battle.Move(side, ship.Id, destination);
                continue;
            }
            if (!ship.IsArmed)
            {
                if (battle.Mothership(side) is { Level: < 4 })
                {
                    var resourceRoute = battle.KnownFish(side).Select(p => battle.RouteToward(ship.Id, p, ship.Definition.CollectionRange))
                        .Where(p => p.Count > 1).OrderBy(p => p.Count).FirstOrDefault();
                    if (resourceRoute is not null)
                    {
                        var destination = battle.AffordableDestination(ship.Id, resourceRoute);
                        if (destination != ship.Position) return battle.Move(side, ship.Id, destination);
                    }
                }
                var mother = allies.First(s => s.Definition.Class == ShipClass.Mothership);
                if (BattleState.Distance(ship.Position, mother.Position) > 2) continue;
                var safe = battle.Reachable(ship.Id).Keys.Where(p => p != ship.Position)
                    .OrderByDescending(p => enemies.Length == 0 ? 0 : enemies.Min(e => BattleState.Distance(p, e.Position)))
                    .ThenByDescending(p => BattleState.Distance(p, mother.Position)).FirstOrDefault(ship.Position);
                if (safe != ship.Position) return battle.Move(side, ship.Id, safe);
                continue;
            }
            var route = enemies.Select(e => battle.PathToAttackPosition(ship.Id, e.Id))
                .Where(p => p.Count > 1).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
            route ??= battle.Vision.Contacts(side).Select(p => battle.RouteToward(ship.Id, p, Math.Min(ship.Definition.VisualRange, ship.Definition.AttackRange)))
                .Where(p => p.Count > 1).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
            if (route is not null)
            {
                var destination = battle.AffordableDestination(ship.Id, route);
                if (destination != ship.Position) return battle.Move(side, ship.Id, destination);
            }
            var available = battle.Reachable(ship.Id);
            int Discovery(GridPosition p) => battle.Board.Tiles.Count(t => !battle.Vision.IsExplored(side, t.Position) &&
                BattleVision.InRadius(p, t.Position, ship.Definition.VisualRange));
            var frontier = available.Keys.Where(p => p != ship.Position).OrderByDescending(Discovery)
                .ThenBy(p => battle.Vision.LastSeen(side, p)).ThenByDescending(p => available[p]).FirstOrDefault(ship.Position);
            if (frontier != ship.Position && Discovery(frontier) > 0) return battle.Move(side, ship.Id, frontier);
            var goal = battle.Board.Tiles.Where(t => battle.Vision.KnownTerrain(side, t.Position) != TerrainType.Land)
                .OrderBy(t => battle.Vision.LastSeen(side, t.Position)).ThenBy(t => BattleState.Distance(ship.Position, t.Position))
                .Take(12).Select(t => battle.RouteToward(ship.Id, t.Position)).FirstOrDefault(p => p.Count > 1);
            if (goal is not null)
            {
                var destination = battle.AffordableDestination(ship.Id, goal);
                if (destination != ship.Position) return battle.Move(side, ship.Id, destination);
            }
        }
        return battle.EndTurn(side);
    }
}
