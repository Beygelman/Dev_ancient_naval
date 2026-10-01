namespace DevAncientNaval.Core.Battle;
/// <summary>Missing settings preserve the economy of released version-one saves.</summary>
public sealed class EconomyRules
{
    public int MothershipIncomePerLevel { get; init; } = 2;
    public int VillageLevelsPerIncome { get; init; } = 1;
    public int FreeCombatShips { get; init; } = 2;
    public int CombatShipsPerUpkeep { get; init; }
    public bool AdjacentCollectionOnly { get; init; }
    public int MinimumResourceSpots { get; init; } = 16;
    public int ResourceTileInterval { get; init; }

    internal void Validate()
    {
        if (MothershipIncomePerLevel < 0 || VillageLevelsPerIncome < 1 || FreeCombatShips < 0 || CombatShipsPerUpkeep < 0 || MinimumResourceSpots < 0 || MinimumResourceSpots > 20_000 || ResourceTileInterval < 0)
            throw new ArgumentException("Invalid economy settings.");
    }
}
