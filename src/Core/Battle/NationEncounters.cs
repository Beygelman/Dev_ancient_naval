using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed record NationEncounter(Side Side, GridPosition Position);

public sealed partial class BattleState
{
    private readonly Dictionary<Side, NationEncounter> _encounters = new();
    public IReadOnlyCollection<NationEncounter> Encounters => _encounters.Values.ToArray();
    public bool HasMet(Side side) => side == Side.Player || _encounters.ContainsKey(side);

    private void DiscoverNations()
    {
        // Real optical sight only: radar, God's eye and the victory reveal are not encounters.
        foreach (var ship in _ships.OrderBy(s => s.Id))
        {
            if (ship.Owner is Side.Player or Side.Pirates || HasMet(ship.Owner)
                || !Vision.IsOpticallyVisible(Side.Player, ship.Position)) continue;
            _encounters.Add(ship.Owner, new(ship.Owner, ship.Position));
            _credits[(int)Side.Player] += Rules.EncounterCurrencyReward;
            RecordCurrencyReceipt(Side.Player, Rules.EncounterCurrencyReward);
        }
    }
}
