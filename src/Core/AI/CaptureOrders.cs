using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

internal static class CaptureOrders
{
    internal static bool Holding(BattleState battle, Ship ship) => battle.IsAiCaptureCrew(ship.Owner, ship.Id);
    internal static CommandResult? Ready(BattleState battle)
    {
        var side = battle.ActiveSide;
        var ready = battle.ObservedVillages(side).Where(t => battle.CanCaptureVillage(side, t.Id))
            .OrderByDescending(t => t.HasPort).ThenByDescending(t => t.Level).ThenBy(t => t.Id).FirstOrDefault();
        if (ready is null) return null;
        var mother = battle.Mothership(side)!;
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side).ToArray();
        var threats = new AdmiralThreats(battle, enemies);
        // Only an immediate potentially fatal exposure can preempt an available capture.
        if (threats.At(mother, mother.Position) >= mother.Health)
        {
            if (FlagshipSafety.Retreat(battle, mother, enemies, emergency: true) is { } escape) return escape;
            if (FlagshipEscort.Step(battle, mother, battle.OwnShips(side).ToArray(), enemies, threats.At) is { } screen) return screen;
        }
        return battle.CaptureVillage(side, ready.Id);
    }
}
