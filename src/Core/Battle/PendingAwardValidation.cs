using System.Globalization;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;

/// <summary>Checks queued receipts against their persisted discovery or personal clock.
/// Ownership at claim time may differ from ownership when a blessing was earned.</summary>
internal static class PendingAwardValidation
{
    internal static void Validate(BattleSave saved, GameBoard board, BattleRules rules)
    {
        foreach (var award in saved.PendingAwards)
        {
            if (!rules.DeferredRewards || award.Owner != Side.Player
                || award.Position is { } position && !board.Contains(position))
                Reject();
            switch (award.Kind)
            {
                case AwardKind.Nation:
                    var encounter = saved.Encounters.FirstOrDefault(e => e.Side == award.Nation);
                    if (encounter is null || award.Id != $"nation:{(int)encounter.Side}"
                        || award.Position != encounter.Position || award.Amount != rules.EncounterCurrencyReward)
                        Reject();
                    break;
                case AwardKind.Heavenly:
                    var components = award.Id.Split(':');
                    if (!rules.HeavenlyAssistance || award.Nation is not null || award.Position is not null
                        || components.Length != 3 || components[0] != "heavenly"
                        || !int.TryParse(components[1], NumberStyles.None, CultureInfo.InvariantCulture, out int owner)
                        || owner != (int)Side.Player
                        || !int.TryParse(components[2], NumberStyles.None, CultureInfo.InvariantCulture, out int starts)
                        || starts < 5 || starts % 5 != 0
                        || award.Id != $"heavenly:{owner}:{starts}"
                        || saved.PersonalTurnStarts.Length != BattleState.SideSlots
                        || starts > saved.PersonalTurnStarts[owner] || starts > (long)award.Turn + 1
                        || award.Amount <= 0 || award.Amount % 2 != 0
                        || award.Amount > 2L * (saved.Villages.Length + 1))
                        Reject();
                    break;
                default:
                    Reject();
                    break;
            }
        }
    }

    private static void Reject() => throw new ArgumentException("Pending rewards do not match this voyage.");
}
