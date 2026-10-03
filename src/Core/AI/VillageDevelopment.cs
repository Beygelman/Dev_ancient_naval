using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>Spend earned money to unlock shipyards and capacity instead of assuming
/// towns will mature automatically. Reserve replacement hulls during nearby combat.</summary>
internal static class VillageDevelopment
{
    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> observedEnemies)
    {
        if (!battle.Rules.PaidVillageUpgrades)
            return null;
        var side = battle.ActiveSide;
        int escorts = battle.OwnShips(side).Count(s => s.CountsTowardFleet && !s.IsMothership);
        bool full = battle.FleetUsed(side) >= battle.FleetCapacity(side);
        foreach (var town in battle.Villages.Where(v => v.Owner == side).OrderBy(v => v.Level).ThenBy(v => v.Id))
        {
            if (!battle.CanUpgradeVillage(side, town.Id))
                continue;
            bool threatened = observedEnemies.Any(enemy => enemy.IsArmed
                && battle.Board.InRadius(town.Position, enemy.Position, 4));
            if (threatened && (escorts < 2 || !town.IsFortified || town.Health < town.MaxHealth * .5))
                continue;
            int reserve = town.Level == 1 || full ? 0 : battle.BuildPrice(side, ShipClass.Garrison);
            if (battle.Credits(side) < (long)battle.VillageUpgradePrice(side, town.Id) + reserve)
                continue;
            if (town.Level >= 3 && !full && escorts < 2)
                continue;
            return battle.UpgradeVillage(side, town.Id);
        }
        return null;
    }
}
