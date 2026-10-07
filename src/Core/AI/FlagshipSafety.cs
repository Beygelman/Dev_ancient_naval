using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;
internal static class FlagshipSafety
{
    // Only observed enemies enter this policy: the computer cannot see through fog.
    internal static bool IsCautious(BattleState battle, Ship mother, IReadOnlyList<Ship> enemies) => mother.HealthRatio < .5
        && enemies.Any(e => Threatens(battle, e, mother.Position) || (e.IsArmed || e.IsAirborne)
            && battle.Board.InRadius(e.Position, mother.Position, e.MovementAllowance + e.AttackRange + 2));
    private static bool Threatens(BattleState battle, Ship enemy, GridPosition cell) => enemy.IsAirborne
        ? enemy.BombCooldown <= 1 && battle.Board.InRadius(enemy.Position, cell, enemy.MovementAllowance + 1)
        : enemy.IsArmed && (battle.WeaponCovers(enemy, cell) || !enemy.IsStructure && !enemy.HasMortar
            && battle.Board.InRadius(enemy.Position, cell, enemy.MovementAllowance + enemy.AttackRange));
    internal static CommandResult? Retreat(BattleState battle, Ship mother, IReadOnlyList<Ship> enemies)
    {
        if (!IsCautious(battle, mother, enemies))
            return null;
        if (mother.CanMove && !mother.HasMoved)
        {
            int Danger(GridPosition cell) => enemies.Count(e => Threatens(battle, e, cell));
            double Exposure(GridPosition cell) => enemies.Where(e => Threatens(battle, e, cell)).Sum(e => e.CurrentDamage + e.ShotDamageBonus);
            double Clearance(GridPosition cell) => enemies.Where(e => e.IsArmed || e.IsAirborne).Select(e => battle.Board.Distance(e.Position, cell)).DefaultIfEmpty(99).Min();
            var bases = battle.Villages.Where(v => v.Owner == mother.Owner && v.Health > v.MaxHealth * .5)
                .OrderByDescending(v => v.IsFortified).ThenByDescending(v => v.HasPort).ThenByDescending(v => v.Level)
                .ThenBy(v => enemies.Count(e => Threatens(battle, e, v.Position))).ThenBy(v => v.Id).Take(3).ToArray();
            double Refuge(GridPosition cell) => bases.Select(v => battle.Board.Distance(cell, v.Position))
                .DefaultIfEmpty(0).Min();
            var choices = battle.Reachable(mother.Id).Keys
                .Where(p => p != mother.Position)
                .Select(p => new { Cell = p, Path = battle.PathTo(mother.Id, p) })
                .Where(p => p.Path.Skip(1).All(c => Danger(c) <= Danger(mother.Position)))
                .OrderBy(p => Exposure(p.Cell)).ThenBy(p => Danger(p.Cell))
                .ThenBy(p => Refuge(p.Cell))
                .ThenByDescending(p => Clearance(p.Cell))
                .ThenBy(p => battle.Board.Distance(mother.Position, p.Cell)).ToArray();
            var best = choices.FirstOrDefault();
            if (best is not null && (Exposure(best.Cell) < Exposure(mother.Position) || Clearance(best.Cell) > Clearance(mother.Position)
                || Refuge(best.Cell) < Refuge(mother.Position)))
                return battle.Move(mother.Owner, mother.Id, best.Cell);
        }

        return !mother.HasMoved && mother.CanRepair ? battle.Repair(mother.Owner, mother.Id) : null;
    }
}
