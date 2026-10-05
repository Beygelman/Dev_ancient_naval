using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>Initial pirate pressure, without changing their combat statistics or later turn rules.</summary>
public static class PiratePopulation
{
    public static int SettlementCount(GameBoard board, AiDifficulty difficulty, int availableSites)
    {
        ValidateDifficulty(difficulty);
        if (availableSites <= 0) return 0;
        int sizeBase = EffectiveSize(board) switch
        {
            MapSize.Lake or MapSize.Bay => 2,
            MapSize.Sea => 3,
            _ => 4
        };
        // Reserve neutral settlements and room for all three difficulty tiers.
        // Small maps cannot acquire more pirate bays merely by choosing a larger
        // area preset when their coastline has too few separated town sites.
        int maximum = Math.Max(0, availableSites - 2);
        int minimum = Math.Min(2, maximum);
        int baseCount = Math.Clamp(sizeBase, minimum, Math.Max(minimum, maximum - 2));
        return Math.Min(maximum, baseCount + (int)difficulty);
    }

    public static int PatrolsPerTerritory(GameBoard board, AiDifficulty difficulty)
    {
        ValidateDifficulty(difficulty);
        int sizeBonus = EffectiveSize(board) is MapSize.Sea or MapSize.Ocean ? 1 : 0;
        return 1 + sizeBonus + (int)difficulty;
    }

    private static MapSize EffectiveSize(GameBoard board) => board.MapSize ?? (board.Tiles.Count switch
    {
        < 500 => MapSize.Lake,
        < 900 => MapSize.Bay,
        < 1400 => MapSize.Sea,
        _ => MapSize.Ocean
    });

    private static void ValidateDifficulty(AiDifficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
            throw new ArgumentOutOfRangeException(nameof(difficulty));
    }
}
