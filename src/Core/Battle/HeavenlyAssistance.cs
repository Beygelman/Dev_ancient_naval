using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    // Initial player turn is start one; all other clocks begin on their first
    // actual turn. Separate counters survive eliminated factions and Continue.
    private readonly int[] _personalTurnStarts = new int[SideSlots];
    private readonly int[] _flagshipKills = new int[SideSlots];
    public int PersonalTurnStarts(Side side) => _personalTurnStarts[(int)side];
    public int FlagshipsDestroyedBy(Side side) => _flagshipKills[(int)side];

    private IReadOnlyList<HeavenlyReceipt> StartHeavenlyTurn(Side side)
    {
        if (side == Side.Pirates)
            return Array.Empty<HeavenlyReceipt>();
        int starts = ++_personalTurnStarts[(int)side];
        if (!Rules.HeavenlyAssistance || starts % 5 != 0)
            return Array.Empty<HeavenlyReceipt>();
        bool religious = side == Side.Player;
        int beneficiaries = religious
            ? _villages.Count(v => v.Owner == side && v.Health > 0) + (Mothership(side) is null ? 0 : 1)
            : _flagshipKills[(int)side];
        int amount = checked(beneficiaries * 2);
        if (amount == 0)
            return Array.Empty<HeavenlyReceipt>();
        AwardOrQueue(new($"heavenly:{(int)side}:{starts}", AwardKind.Heavenly, side, amount, TurnSerial));
        return new[] { new HeavenlyReceipt(side, amount, beneficiaries, religious) };
    }

    private void InitializeSettlementLevels()
    {
        var starts = WorldSettlementPlacement.Describe(Board, _villages.Select(v => v.Position), Rules.FrozenUnownedVillages);
        foreach (var village in _villages)
        {
            var start = starts[village.Position];
            village.Level = start.Level;
            village.Health = village.MaxHealth;
            village.IsFortified = start.IsPirateBay;
            village.Owner = start.IsPirateBay ? Side.Pirates : null;
            RegisterVillageIncome(village);
        }
    }
}
