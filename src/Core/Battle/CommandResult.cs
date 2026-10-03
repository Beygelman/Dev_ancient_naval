using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
<<<<<<< Updated upstream
public enum CommandKind
=======

public enum CommandKind { None, Move, Attack, Repair, Build, EndTurn, Collect, Radar, Upgrade, Dock, Bomb, Capture, Fortify, Loot }

public sealed record CommandResult(bool Success, string Message, CommandKind Kind = CommandKind.None,
    int ActorId = 0, int TargetId = 0, double Amount = 0, IReadOnlyList<GridPosition>? Path = null,
    IReadOnlyList<CombatShot>? Shots = null, IReadOnlyList<MovementFrame>? Movement = null, StructureHit? StructureHit = null, IReadOnlyList<CombatShot>? Splash = null, IReadOnlyList<AreaHit>? AreaHits = null, IReadOnlyList<IncomeReceipt>? IncomeReceipts = null)
>>>>>>> Stashed changes
{
    None,
    Move,
    Attack,
    Repair,
    Build,
    EndTurn,
    Collect,
    Radar,
    Upgrade,
    Dock,
    Bomb,
    Capture,
    Fortify,
    Loot,
    Port,
    Scuttle,
    ClaimAward
}

public sealed record CommandResult(bool Success, string Message, CommandKind Kind = CommandKind.None, int ActorId = 0, int TargetId = 0, double Amount = 0, IReadOnlyList<GridPosition>? Path = null, IReadOnlyList<CombatShot>? Shots = null, IReadOnlyList<MovementFrame>? Movement = null, StructureHit? StructureHit = null, IReadOnlyList<CombatShot>? Splash = null, IReadOnlyList<AreaHit>? AreaHits = null, IReadOnlyList<IncomeReceipt>? IncomeReceipts = null, IReadOnlyList<OutpostShot>? OutpostShots = null, IReadOnlyList<HealingReceipt>? HealingReceipts = null, IReadOnlyList<HeavenlyReceipt>? HeavenlyReceipts = null)
{
    public int SalvoCharges { get; init; } = 1;
    public static CommandResult Rejected(string message) => new(false, message);
}

<<<<<<< Updated upstream
public sealed record HeavenlyReceipt(Side Owner, int Amount, int Beneficiaries, bool IsReligiousBlessing);
public sealed record HealingReceipt(GridPosition Position, double Amount, bool VisibleToPlayer, bool IsVillage = false);
public sealed record IncomeReceipt(string SourceId, Side Owner, GridPosition? Position, int Amount, bool IsUpkeep = false);
public sealed record ShipSnapshot(int Id, Side Owner, ShipClass Class, GridPosition Position, double Health, double MaxHealth, bool IsVeteran, bool IsExhausted, int Progress = 0, int ProgressGoal = 3, int Level = 1, bool HasMortar = false, bool IsAncient = false, int BombCooldown = 0)
{
    public static ShipSnapshot From(Ship ship) => new(ship.Id, ship.Owner, ship.Definition.Class, ship.Position, ship.Health, ship.MaxHealth, ship.IsVeteran, ship.IsExhausted, ship.IsMothership ? ship.Level == 5 ? 5 : ship.Resources : Math.Min(ship.Kills, 3), ship.IsMothership ? ship.Level == 5 ? 5 : ship.ResourcesRequired : ship.CanEarnVeterancy ? 3 : 0, ship.Level, ship.HasMortar, ship.IsAncient, ship.BombCooldown);
=======
public sealed record IncomeReceipt(string SourceId, Side Owner, GridPosition? Position, int Amount, bool IsUpkeep = false);

public sealed record ShipSnapshot(int Id, Side Owner, ShipClass Class, GridPosition Position,
    double Health, double MaxHealth, bool IsVeteran, bool IsExhausted, int Progress = 0, int ProgressGoal = 3, int Level = 1, bool HasMortar = false,
    bool IsAncient = false, int BombCooldown = 0)
{
    public static ShipSnapshot From(Ship ship) => new(ship.Id, ship.Owner, ship.Definition.Class,
        ship.Position, ship.Health, ship.MaxHealth, ship.IsVeteran, ship.IsExhausted,
        ship.IsMothership ? ship.Level == 5 ? 5 : ship.Resources : Math.Min(ship.Kills, 3), ship.IsMothership ? ship.Level == 5 ? 5 : ship.ResourcesRequired : ship.IsArmed && !ship.IsStructure ? 3 : 0, ship.Level, ship.HasMortar, ship.IsAncient, ship.BombCooldown);
>>>>>>> Stashed changes
}

public sealed record OutpostShot(int VillageId, GridPosition Origin, ShipSnapshot Target, double Damage, bool TargetSunk, bool OriginVisibleToPlayer, bool TargetVisibleToPlayer);
public sealed record CombatShot(ShipSnapshot Attacker, ShipSnapshot Target, double Damage, bool IsCounterattack, bool TargetSunk, bool Promoted, bool IsMortar = false, bool AttackerVisibleToPlayer = true, bool TargetVisibleToPlayer = true);
public sealed record MovementFrame(GridPosition Position, bool VisibleToPlayer, bool ContactToPlayer);
<<<<<<< Updated upstream
public sealed record StructureHit(ShipSnapshot Attacker, GridPosition Position, double CounterDamage, bool IsMortar, bool AttackerVisibleToPlayer, bool TargetVisibleToPlayer, int Salvos = 1);
=======
public sealed record StructureHit(ShipSnapshot Attacker, GridPosition Position, double CounterDamage,
    bool IsMortar, bool AttackerVisibleToPlayer, bool TargetVisibleToPlayer);
>>>>>>> Stashed changes
