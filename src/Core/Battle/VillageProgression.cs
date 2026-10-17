using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    public int VillageUpgradePrice(Side side, int villageId)
    {
        var village = _villages.FirstOrDefault(v => v.Id == villageId);
        if (village is null || village.Level >= 5 || Creative && side == Side.Player)
            return 0;
        return Rules.VillageLevelPrices[village.Level - 1];
    }

    public string? VillageUpgradeBlockReason(Side side, int villageId)
    {
        var error = ValidateVillage(side, villageId, out var village);
        if (error is not null)
            return error;
        if (!Rules.PaidVillageUpgrades)
            return "Village upgrades are automatic in this voyage.";
        if (village!.Level >= 5)
            return "This village is already at maximum level.";
        if (Rules.ExperimentalEconomy && village.UpgradeWorkRemaining > 0)
            return "This city already has a development project.";
        if (village.HasProduced)
            return "This village has already worked this turn.";
        return Credits(side) < VillageUpgradePrice(side, villageId) ? "Not enough Thors." : null;
    }

    public bool CanUpgradeVillage(Side side, int villageId) => VillageUpgradeBlockReason(side, villageId) is null;

    public CommandResult UpgradeVillage(Side side, int villageId)
    {
        var error = VillageUpgradeBlockReason(side, villageId);
        if (error is not null)
            return CommandResult.Rejected(error);
        var village = _villages.First(v => v.Id == villageId);
        int price = VillageUpgradePrice(side, villageId);
        _credits[(int)side] -= price;
        if (Rules.ExperimentalEconomy)
        {
            village.UpgradeWorkRemaining = 2;
            village.UpgradeProjectPrice = price;
            return new(true, "Development funded. Two idle city works are required.", CommandKind.Upgrade,
                ActorId: villageId, TargetId: villageId, Amount: village.Level + 1);
        }
        village.Level++;
        village.Health += 5;
        village.HasProduced = true;
        RegisterVillageIncome(village);
        UpdateVision();
        return new(true, $"Village upgraded to level {village.Level}.", CommandKind.Upgrade,
            ActorId: villageId, TargetId: villageId, Amount: village.Level);
    }
}
