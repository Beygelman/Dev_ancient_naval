using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>Emergency escort orders after the flagship's escape. Own hulls
/// occupy approaching sea cells and inflict hostile-adjacency navigation cost;
/// there is no invented damage absorption or private visibility advantage.</summary>
internal static class FlagshipEscort
{
    internal static CommandResult? Step(BattleState battle, Ship mother, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        if (!FlagshipSafety.IsCautious(battle, mother, enemies)) return null;
        var threats = enemies.Where(e => e.IsArmed && !e.IsAirborne
            && battle.Board.InRadius(e.Position, mother.Position, e.MovementAllowance + e.AttackRange))
            .OrderBy(e => e.Health).ThenBy(e => battle.Board.Distance(e.Position, mother.Position))
            .ThenByDescending(e => e.CurrentDamage).ThenBy(e => e.Id).ToArray();
        foreach (var enemy in threats)
        {
            var killer = allies.Where(s => !s.IsMothership && battle.CanAttack(s.Id, enemy.Id))
                .FirstOrDefault(s => battle.Damage(s, enemy) * (battle.CanDoubleSalvo(s.Id, enemy.Position) ? 2 : 1) >= enemy.Health);
            if (killer is not null)
                return battle.Attack(battle.ActiveSide, killer.Id, enemy.Id,
                    enemy.Health > battle.Damage(killer, enemy) && battle.CanDoubleSalvo(killer.Id, enemy.Position));
        }
        foreach (var escort in allies.Where(s => !s.IsMothership && !s.IsAirborne && !s.IsStructure
            && s.CanMove && !s.HasMoved).OrderByDescending(s => s.Health).ThenBy(s => s.Id))
        {
            var position = battle.Reachable(escort.Id).Keys.Where(p => p != escort.Position
                && battle.Board.Distance(p, mother.Position) <= 2
                && threats.Any(e => battle.Board.Distance(p, e.Position) < battle.Board.Distance(mother.Position, e.Position)
                    && battle.Board.Distance(p, e.Position) + battle.Board.Distance(p, mother.Position)
                        <= battle.Board.Distance(mother.Position, e.Position) + 1))
                .OrderBy(p => danger(escort, p)).ThenBy(p => battle.Board.Distance(p, mother.Position))
                .ThenBy(p => p.Y).ThenBy(p => p.X).FirstOrDefault(escort.Position);
            if (position != escort.Position)
                return battle.Move(battle.ActiveSide, escort.Id, position);
        }
        return null;
    }
}
