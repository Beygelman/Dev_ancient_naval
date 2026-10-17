using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public enum AiStrategicStyle { Economist, Defender, Balanced, Aggressor, Hunter }
public enum AiGoalKind { None, Develop, Defend, Expand, Raid, Hunt }
/// <summary>Intent, not private knowledge. Positions are observed objectives or saved sightings.</summary>
public sealed record SavedAiPlan(Side Side, AiStrategicStyle Style, AiGoalKind Goal = AiGoalKind.None,
    int TargetId = 0, GridPosition? Position = null, int AssignedShipId = 0,
    int SinceRound = 1, int LastProgressRound = 1, int LastDistance = int.MaxValue,
    GridPosition? AvoidPosition = null, int AvoidUntilRound = 0);

public sealed partial class BattleState
{
    private readonly Dictionary<Side, SavedAiPlan> _aiPlans = new();
    public SavedAiPlan? AiPlan(Side side) => _aiPlans.GetValueOrDefault(side);
    internal void SetAiPlan(SavedAiPlan plan) => _aiPlans[plan.Side] = plan;
    private void InitializeAiPlans()
    {
        if (!Rules.StrategicAi) return;
        foreach (var side in _factions)
        {
            // An independent integer mix never advances simulation/event RNG.
            uint value = unchecked((uint)Board.Seed ^ ((uint)(int)side + 1) * 0x9e3779b9u ^ 0xa17d31u);
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15;
            _aiPlans[side] = new(side, (AiStrategicStyle)(value % 5));
        }
    }
    private SavedAiPlan[] CaptureAiPlans() => _aiPlans.Values.OrderBy(p => p.Side).ToArray();
    private void RestoreAiPlans(SavedAiPlan[] plans)
    {
        _aiPlans.Clear();
        foreach (var plan in plans) _aiPlans[plan.Side] = plan;
        // Missing records retain legacy policy. Continue never invents or rerolls a style.
    }

    /// <summary>Reserve one real combat crew per defeated visible town, not the entire fleet.</summary>
    internal bool IsAiCaptureCrew(Side side, int shipId)
    {
        foreach (var town in ObservedVillages(side).Where(t => t.Owner != side && t.Health <= 0))
        {
            var crew = OwnShips(side).Where(s => IsCapturingShip(s) && IsAdjacent(s, town))
                .OrderByDescending(s => _captureWaits.ContainsKey((town.Id, side, s.Id)))
                .ThenBy(s => s.IsMothership).ThenByDescending(s => s.Health).ThenBy(s => s.Id).FirstOrDefault();
            if (crew?.Id == shipId) return true;
        }
        return false;
    }

    internal static void ValidateAiPlans(BattleSave saved, GameBoard board)
    {
        if (saved.AiPlans is null || saved.AiPlans.Length > 5
            || saved.AiPlans.Any(p => p is null || !PlayableSides.Contains(p.Side)
                || !Enum.IsDefined(p.Style) || !Enum.IsDefined(p.Goal) || p.TargetId < 0 || p.AssignedShipId < 0
                || p.Position is { } position && !board.Contains(position)
                || p.AvoidPosition is { } avoided && !board.Contains(avoided) || p.AvoidUntilRound < 0
                || p.SinceRound < 1 || p.SinceRound > saved.Round || p.LastProgressRound < p.SinceRound
                || p.LastProgressRound > saved.Round || p.LastDistance < 0
                || p.Goal != AiGoalKind.None && p.Position is null)
            || saved.AiPlans.Select(p => p.Side).Distinct().Count() != saved.AiPlans.Length
            || saved.AiPlans.Any(p => saved.Factions.Length > 0 && !saved.Factions.Contains(p.Side)))
            throw new ArgumentException("Invalid saved strategic AI intent.");
        // Targets/assigned crews may have died since the last decision; the planner revalidates them.
    }
}
