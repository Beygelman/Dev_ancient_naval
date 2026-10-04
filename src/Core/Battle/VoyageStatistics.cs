using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

/// <summary>Player-attributed voyage totals, independent of the current treasury
/// and fleet. Gross receipts include upkeep subsequently paid from that income.
/// Construction counts shipyard hulls, including fishing boats, but excludes
/// buildings, balloons and ships granted by discoveries or level rewards.
/// Enemy losses count direct player combat kills, not another captain's kills
/// or the automatic scuttling of a defeated nation's remaining fleet.</summary>
public sealed record VoyageStatistics(
    long CurrencyEarned = 0,
    long ShipsBuilt = 0,
    long EnemyShipsDestroyed = 0,
    int NationsDefeated = 0,
    long StructuresBuilt = 0,
    long SeaStrongholdsBuilt = 0)
{
    public static VoyageStatistics Empty { get; } = new();

    public void Validate()
    {
        if (CurrencyEarned < 0 || ShipsBuilt < 0 || EnemyShipsDestroyed < 0 || StructuresBuilt < 0
            || SeaStrongholdsBuilt < 0 || SeaStrongholdsBuilt > StructuresBuilt
            || NationsDefeated is < 0 or > 4)
            throw new ArgumentException("Invalid saved voyage statistics.");
    }
}

public sealed partial class BattleState
{
    public VoyageStatistics Statistics { get; private set; } = VoyageStatistics.Empty;
    private readonly long[] _directShipKills = new long[SideSlots];
    /// <summary>Direct opposing hull/balloon kills; excludes demolition, buildings and nation collapse.</summary>
    public long DirectShipKills(Side side) => _directShipKills[(int)side];

    private void RecordCurrencyReceipt(Side owner, int amount)
    {
        if (owner == Side.Player && amount > 0)
            Statistics = Statistics with
            {
                CurrencyEarned = checked(Statistics.CurrencyEarned + amount)
            };
    }

    private void RecordShipConstruction(Ship ship)
    {
        if (ship.Owner == Side.Player && ship.IsStructure)
            Statistics = Statistics with { StructuresBuilt = checked(Statistics.StructuresBuilt + 1) };
        if (ship.Owner == Side.Player && ship.Definition.Class is ShipClass.CannonTower or ShipClass.Lighthouse)
            Statistics = Statistics with { SeaStrongholdsBuilt = checked(Statistics.SeaStrongholdsBuilt + 1) };
        if (ship.Owner == Side.Player && !ship.IsStructure && !ship.IsAirborne)
            Statistics = Statistics with
            {
                ShipsBuilt = checked(Statistics.ShipsBuilt + 1)
            };
    }

    private void RecordEnemyLoss(Side attacker, Ship target)
    {
        if (target.Health <= 0 && _ships.Contains(target) && attacker != target.Owner && !target.IsStructure)
            _directShipKills[(int)attacker] = checked(_directShipKills[(int)attacker] + 1);
        if (target.IsMothership && target.Health <= 0 && _ships.Contains(target)
            && attacker != target.Owner && PlayableSides.Contains(attacker) && PlayableSides.Contains(target.Owner))
            _flagshipKills[(int)attacker]++;
        if (attacker != Side.Player || target.Owner == Side.Player
            || target.Health > 0 || !_ships.Contains(target))
            return;

        if (!target.IsStructure)
            Statistics = Statistics with
            {
                EnemyShipsDestroyed = checked(Statistics.EnemyShipsDestroyed + 1)
            };
        if (target.IsMothership)
            Statistics = Statistics with
            {
                NationsDefeated = Statistics.NationsDefeated + 1
            };
    }
}
