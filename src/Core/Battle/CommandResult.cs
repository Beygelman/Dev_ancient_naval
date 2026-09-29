using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public enum CommandKind { None, Move, Attack, Repair, Build, EndTurn, Collect, Radar, Upgrade, Dock }

public sealed record CommandResult(bool Success, string Message, CommandKind Kind = CommandKind.None,
    int ActorId = 0, int TargetId = 0, double Amount = 0, IReadOnlyList<GridPosition>? Path = null,
    IReadOnlyList<CombatShot>? Shots = null, IReadOnlyList<MovementFrame>? Movement = null)
{
    public static CommandResult Rejected(string message) => new(false, message);
}

public sealed record ShipSnapshot(int Id, Side Owner, ShipClass Class, GridPosition Position,
    double Health, double MaxHealth, bool IsVeteran, bool IsExhausted, int Progress = 0, int ProgressGoal = 3, int Level = 1, bool HasMortar = false)
{
    public static ShipSnapshot From(Ship ship) => new(ship.Id, ship.Owner, ship.Definition.Class,
        ship.Position, ship.Health, ship.MaxHealth, ship.IsVeteran, ship.IsExhausted,
        ship.IsMothership ? ship.Level == 4 ? 4 : ship.Resources : Math.Min(ship.Kills, 3), ship.IsMothership ? ship.Level == 4 ? 4 : ship.ResourcesRequired : ship.IsArmed ? 3 : 0, ship.Level,ship.HasMortar);
        ship.IsMothership ? ship.Level == 5 ? 5 : ship.Resources : Math.Min(ship.Kills, 3), ship.IsMothership ? ship.Level == 5 ? 5 : ship.ResourcesRequired : ship.IsArmed ? 3 : 0, ship.Level,ship.HasMortar);
}
public sealed record CombatShot(ShipSnapshot Attacker, ShipSnapshot Target, double Damage,
    bool IsCounterattack, bool TargetSunk, bool Promoted, bool IsMortar = false);
    bool IsCounterattack, bool TargetSunk, bool Promoted, bool IsMortar = false,
    bool AttackerVisibleToPlayer = true, bool TargetVisibleToPlayer = true);
public sealed record MovementFrame(GridPosition Position, bool VisibleToPlayer, bool ContactToPlayer);
