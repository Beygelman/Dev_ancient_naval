using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public enum AwardKind { Nation, Heavenly }
public sealed record PendingAward(string Id, AwardKind Kind, Side Owner, int Amount, int Turn, Side? Nation = null, GridPosition? Position = null);
public sealed partial class BattleState
{
    private readonly List<PendingAward> _pendingAwards = new();
    public IReadOnlyList<PendingAward> PendingAwards => _pendingAwards.AsReadOnly();
    private void AwardOrQueue(PendingAward award)
    {
        if (Rules.DeferredRewards && award.Owner == Side.Player)
        {
            if (!_pendingAwards.Any(a => a.Id == award.Id)) _pendingAwards.Add(award);
        }
        else
        {
            _credits[(int)award.Owner] = checked(_credits[(int)award.Owner] + award.Amount);
            RecordCurrencyReceipt(award.Owner, award.Amount);
        }
    }
    public CommandResult ClaimAward(Side side, string id)
    {
        if (PendingPresentation is not null || IsOver || ActiveSide != side) return CommandResult.Rejected("Cannot claim this reward now.");
        var award = _pendingAwards.FirstOrDefault(a => a.Id == id && a.Owner == side);
        if (award is null) return CommandResult.Rejected("This reward has already been claimed.");
        _credits[(int)side] = checked(_credits[(int)side] + award.Amount);
        RecordCurrencyReceipt(side, award.Amount);
        _pendingAwards.Remove(award);
        return new(true, $"Reward accepted: +{award.Amount} Thors.", CommandKind.ClaimAward, Amount: award.Amount);
    }
}
