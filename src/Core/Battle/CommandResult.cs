using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.Battle;

public enum CommandKind { None, Move, Attack, Repair, Build, EndTurn }

public sealed record CommandResult(bool Success, string Message, CommandKind Kind = CommandKind.None,
    int ActorId = 0, int TargetId = 0, int Amount = 0, IReadOnlyList<GridPosition>? Path = null)
{
    public static CommandResult Rejected(string message) => new(false, message);
}
