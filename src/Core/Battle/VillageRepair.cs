using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    public bool CanRepairVillage(Side side, int id) => ValidateVillage(side, id, out var v) is null &&
        v!.Health < v.MaxHealth && !v.HasAttacked && !v.HasRepaired && !v.HasProduced;
    public CommandResult RepairVillage(Side side, int id)
    {
        if (!CanRepairVillage(side, id)) return CommandResult.Rejected("A living damaged village can repair before production; only once per turn.");
        var village = _villages.First(v => v.Id == id);
        double amount = Math.Min(Rules.RepairAmount, village.MaxHealth - village.Health);
        village.Health += amount; village.HasRepaired = true; village.HasProduced = true;
        return new(true, $"Village repaired +{amount:0.##} HP.", CommandKind.Repair, TargetId: id, Amount: amount);
    }
}
