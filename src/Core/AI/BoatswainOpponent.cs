using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;
/// <summary>Forgiving tactics with the same prices, fog and legal orders.</summary>
internal static class BoatswainOpponent
{
    internal static CommandResult Step(BattleState battle)
    {
        var side = battle.ActiveSide;
        var allies = battle.OwnShips(side).ToArray();
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side).ToArray();
        foreach (var ship in allies)
        {
            if (battle.CanLootTreasury(side, ship.Id))
                return battle.LootTreasury(side, ship.Id);
            var target = enemies.FirstOrDefault(e => battle.CanAttack(ship.Id, e.Id));
            if (target is not null)
                return battle.Attack(side, ship.Id, target.Id);
        }

        foreach (var town in battle.ObservedVillages(side))
        {
            if (battle.CanCaptureVillage(side, town.Id))
                return battle.CaptureVillage(side, town.Id);
            var gun = allies.FirstOrDefault(s => battle.CanAttackVillage(s.Id, town.Id));
            if (gun is not null)
                return battle.AttackVillage(side, gun.Id, town.Id);
        }

        foreach (var ship in allies)
        {
            var fish = battle.CollectionCells(ship.Id);
            if (fish.Count > 0 && battle.Credits(side) >= battle.CollectionCost(side))
                return battle.Collect(side, ship.Id, fish.First());
            if (ship.CanRepair && ship.HealthRatio < .35)
                return battle.Repair(side, ship.Id);
        }

        var mother = battle.Mothership(side)!;
        if (VillageDevelopment.Step(battle, enemies) is { } development)
            return development;
        if (allies.Count(s => s.CountsTowardFleet) < 5 && battle.Round % 2 == 0 && battle.BuildBlockReason(side, mother.Id, ShipClass.Garrison)is null)
            return battle.Build(side, mother.Id, ShipClass.Garrison, battle.SpawnCells(mother.Id).First());
        foreach (var ship in allies.Where(s => s.CanMove && !s.HasMoved))
        {
            var enemy = enemies.FirstOrDefault(e => !e.IsAirborne);
            if (enemy is not null && ship.IsArmed)
            {
                var route = battle.PathToAttackPosition(ship.Id, enemy.Id);
                if (route.Count > 1)
                {
                    var next = battle.AffordableDestination(ship.Id, route);
                    if (next != ship.Position)
                        return battle.Move(side, ship.Id, next);
                }
            }

            var available = battle.Reachable(ship.Id);
            var frontier = available.Keys.Where(p => p != ship.Position).OrderBy(p => battle.Vision.LastSeen(side, p)).ThenByDescending(p => available[p]).FirstOrDefault(ship.Position);
            if (frontier != ship.Position)
                return battle.Move(side, ship.Id, frontier);
        }

        return battle.EndTurn(side);
    }
}
