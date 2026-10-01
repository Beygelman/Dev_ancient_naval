using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public enum FleetColor
{
    Blue,
    Green,
    Yellow,
    Purple,
    White
}

public sealed record SavedBoard(int Width, int Height, int Seed, GridPosition[] Land, SavedMesh? Mesh);
public sealed record SavedVillage(int Id, GridPosition Position, Side? Owner, int Level, double Health, int TurnsOwned, bool Fortified, bool Produced, bool Repaired, bool Attacked, string Name = "");
public sealed record SavedWait(int Target, Side Side, int Ship, GridPosition Position, int Since);
public sealed record SavedHome(int Ship, GridPosition Position);
public sealed record SavedOutcome(int Treasury, TreasuryReward Reward);
/// <summary>Version-one storage contract. Keep names and defaults compatible with released saves.</summary>
public sealed class BattleSave
{
    public int Version { get; set; } = 1;
    public SavedBoard Board { get; set; } = null !;
    public BattleRules Rules { get; set; } = null !;
    public SavedShip[] Ships { get; set; } = Array.Empty<SavedShip>();
    public SavedVillage[] Villages { get; set; } = Array.Empty<SavedVillage>();
    public GridPosition[] Fish { get; set; } = Array.Empty<GridPosition>();
    public GridPosition[] Shoals { get; set; } = Array.Empty<GridPosition>();
    public Treasury[] Treasuries { get; set; } = Array.Empty<Treasury>();
    public Whirlpool[] Whirlpools { get; set; } = Array.Empty<Whirlpool>();
    public SavedWait[] CaptureWaits { get; set; } = Array.Empty<SavedWait>();
    public SavedWait[] TreasuryWaits { get; set; } = Array.Empty<SavedWait>();
    public SavedHome[] PirateHomes { get; set; } = Array.Empty<SavedHome>();
    public SavedOutcome[] Outcomes { get; set; } = Array.Empty<SavedOutcome>();
    public IncomeSource[] Income { get; set; } = Array.Empty<IncomeSource>();
    public SavedVision[] Vision { get; set; } = Array.Empty<SavedVision>();
    public int[] Credits { get; set; } = Array.Empty<int>();
    public bool[] EverProduced { get; set; } = Array.Empty<bool>();
    public int NextId { get; set; }
    public int Round { get; set; }
    public int TurnSerial { get; set; }
    public Side ActiveSide { get; set; }
    public Side? Winner { get; set; }
    public bool IsDraw { get; set; }
    public bool Creative { get; set; }
    public FleetColor Color { get; set; }
    public Side[] Factions { get; set; } = Array.Empty<Side>();
    public FactionIdentity[] FactionNames { get; set; } = Array.Empty<FactionIdentity>();
    public FactionColor[] FactionColors { get; set; } = Array.Empty<FactionColor>();
    public int EventSeed { get; set; }
    public int EventDraws { get; set; }
    public TreasuryReward? LastReward { get; set; }
}
