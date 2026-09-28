using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>One legal command at a time, so presentation can animate each action.
/// This open-information skirmish has no fog; observation filtering comes with vision.</summary>
public static class SimpleOpponent
{
    public static CommandResult Step(BattleState battle)
    {
        if (battle.IsOver) return CommandResult.Rejected("Бой завершён.");
        var side = battle.ActiveSide;
        var allies = battle.Ships.Where(s => s.Owner == side).ToArray();
        var enemies = battle.Ships.Where(s => s.Owner != side).ToArray();
        foreach (var ship in allies)
        {
            var target = enemies.Where(t => battle.CanAttack(ship.Id, t.Id))
                .OrderBy(t => t.Health <= battle.Damage(ship, t) ? 0 : 1)
                .ThenBy(t => t.Definition.Class == ShipClass.Mothership ? 0 : 1).ThenBy(t => t.Health).FirstOrDefault();
            if (target is not null) return battle.Attack(side, ship.Id, target.Id);
        }
        foreach (var mother in allies.Where(s => s.Definition.Class == ShipClass.Mothership))
        {
            var preferred = allies.Length % 3 == 0 ? ShipClass.Kolonel : ShipClass.Invader;
            foreach (var type in new[] { preferred, ShipClass.Garrison }.Distinct())
            {
                if (battle.BuildBlockReason(side, mother.Id, type) is not null) continue;
                var spawn = battle.SpawnCells(mother.Id).OrderBy(p => enemies.Min(e => BattleState.Distance(p, e.Position))).First();
                return battle.Build(side, mother.Id, type, spawn);
            }
        }
        foreach (var ship in allies)
        {
            if (ship.CanRepair && ship.Health <= ship.Definition.MaxHealth / 2)
                return battle.Repair(side, ship.Id);
            if (!ship.CanMove) continue;
            var route = enemies.Select(e => battle.PathToAttackPosition(ship.Id, e.Id))
                .Where(p => p.Count > 1).OrderBy(p => p.Count).FirstOrDefault();
            if (route is null) continue;
            var destination = route[Math.Min(ship.MovementRemaining, route.Count - 1)];
            return battle.Move(side, ship.Id, destination);
        }
        return battle.EndTurn(side);
    }
}
