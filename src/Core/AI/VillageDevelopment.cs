using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>Spend earned money to unlock shipyards and capacity instead of assuming
/// towns will mature automatically. Reserve replacement hulls during nearby combat.</summary>
internal static class VillageDevelopment
{
    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> observedEnemies)
    {
        if (StrategicPlanning.Enabled(battle) && battle.Rules.ExperimentalEconomy)
            foreach (var town in battle.Villages.Where(v => v.Owner == battle.ActiveSide && v.UpgradeWorkRemaining > 0))
            {
                bool threatened = observedEnemies.Any(e => e.IsArmed
                    && battle.Board.Distance(town.Position, e.Position) <= e.MovementAllowance + e.AttackRange);
                if (battle.CanSetVillageDevelopmentPaused(battle.ActiveSide, town.Id, threatened))
                    return battle.SetVillageDevelopmentPaused(battle.ActiveSide, town.Id, threatened);
            }
        if (CityInfrastructureInvestment.Step(battle, observedEnemies) is { } infrastructure) return infrastructure;
        if (!battle.Rules.PaidVillageUpgrades)
            return null;
        var side = battle.ActiveSide;
        int escorts = battle.OwnShips(side).Count(s => s.CountsTowardFleet && !s.IsMothership);
        bool full = battle.FleetUsed(side) >= battle.FleetCapacity(side);
        var allies = battle.OwnShips(side).ToArray();
        foreach (var town in battle.Villages.Where(v => v.Owner == side)
            .OrderBy(v => StrategicPlanning.Enabled(battle) && v.Id == battle.AiPlan(side)?.TargetId ? 0 : 1)
            .ThenBy(v => v.Level).ThenBy(v => v.Id))
        {
            if (!battle.CanUpgradeVillage(side, town.Id))
                continue;
            bool threatened = observedEnemies.Any(enemy => enemy.IsArmed
                && battle.Board.InRadius(town.Position, enemy.Position, 4));
            if (threatened && (escorts < 2 || !town.IsFortified || town.Health < town.MaxHealth * .5))
                continue;
            int reserve = town.Level == 1 || full ? 0 : battle.BuildPrice(side, ShipClass.Garrison);
            reserve = Math.Max(reserve, StrategicPlanning.Reserve(battle, allies, observedEnemies));
            // Every personality mobilizes before upgrading under actual pressure.
            if (StrategicPlanning.Enabled(battle) && battle.AiPlan(side)?.Goal == AiGoalKind.Defend && threatened)
                continue;
            if (battle.Credits(side) < (long)battle.VillageUpgradePrice(side, town.Id) + reserve)
                continue;
            if (town.Level >= 3 && !full && escorts < 2)
                continue;
            return battle.UpgradeVillage(side, town.Id);
        }
        return null;
    }
}

