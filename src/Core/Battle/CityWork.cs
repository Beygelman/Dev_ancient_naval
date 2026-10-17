using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    public string? CityDevelopmentPauseBlockReason(Side side, int villageId, bool paused)
    {
        var error = ValidateVillage(side, villageId, out var town, requireWork: false);
        if (error is not null) return error;
        if (!Rules.ExperimentalEconomy || town!.UpgradeWorkRemaining == 0)
            return "This city has no development project.";
        return town.DevelopmentPaused == paused ? "This development state is already selected." : null;
    }

    public bool CanSetVillageDevelopmentPaused(Side side, int villageId, bool paused) =>
        CityDevelopmentPauseBlockReason(side, villageId, paused) is null;

    public CommandResult SetVillageDevelopmentPaused(Side side, int villageId, bool paused)
    {
        var error = CityDevelopmentPauseBlockReason(side, villageId, paused);
        if (error is not null) return CommandResult.Rejected(error);
        var town = _villages.First(v => v.Id == villageId);
        town.DevelopmentPaused = paused;
        return new(true, paused ? "City development paused. Idle work is available for defense."
            : "City development resumed. Idle work continues automatically.", CommandKind.Upgrade,
            ActorId: villageId, TargetId: villageId, Amount: town.UpgradeWorkRemaining);
    }

    /// <summary>Explicit orders get first choice; an idle project has priority over automatic guns.</summary>
    private void EndVillageWork(Side side)
    {
        if (!Rules.ExperimentalEconomy) return;
        foreach (var town in _villages.Where(v => v.Owner == side && v.Health > 0
                     && v.UpgradeWorkRemaining > 0 && !v.DevelopmentPaused
                     && !v.HasProduced && !v.HasRepaired && !v.HasAttacked))
        {
            town.HasProduced = true;
            town.UpgradeWorkRemaining--;
            if (town.UpgradeWorkRemaining != 0) continue;
            town.Level++;
            town.UpgradeProjectPrice = 0;
            town.DevelopmentPaused = false;
            RegisterVillageIncome(town);
        }
    }
}
