using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

internal static class AINavigationRecovery
{
    internal static CommandResult Step(BattleState battle)
    {
        var side = battle.ActiveSide;
        foreach (var ship in battle.OwnShips(side).Where(s=>s.CanMove && !s.HasMoved)
            .OrderBy(s=>s.IsMothership).ThenBy(s=>s.Id))
        {
            var alternatives = battle.Reachable(ship.Id).Where(p=>p.Key!=ship.Position)
                // Prefer already charted nearby cells for recovery, while the
                // actual command still revalidates hidden occupancy and terrain.
                .OrderByDescending(p=>battle.Vision.IsExplored(side,p.Key)).ThenBy(p=>p.Value)
                .ThenBy(p=>p.Key.Y).ThenBy(p=>p.Key.X).Take(8).ToArray();
            foreach (var alternate in alternatives)
            {
                var result = battle.Move(side,ship.Id,alternate.Key);
                if (result.Success) return result;
            }
        }
        return battle.EndTurn(side);
    }
}
