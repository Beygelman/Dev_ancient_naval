using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

/// <summary>Small persistent objective layer; command legality and tactical forecasts remain shared.</summary>
internal static class StrategicPlanning
{
    private sealed record Candidate(AiGoalKind Kind, int Id, GridPosition Position, double Value);
    internal static AiStrategicStyle Style(BattleState battle) => battle.AiPlan(battle.ActiveSide)?.Style ?? AiStrategicStyle.Balanced;
    internal static bool Enabled(BattleState battle) => battle.Rules.StrategicAi && battle.AiPlan(battle.ActiveSide) is not null;
    internal static bool HoldingPosition(BattleState battle, Ship ship) => Enabled(battle)
        && battle.AiPlan(battle.ActiveSide) is { Goal: AiGoalKind.Raid } plan
        && plan.AssignedShipId == ship.Id && plan.Position == ship.Position;
    internal static double Weight(AiStrategicStyle style, AiGoalKind kind) => (style, kind) switch
    {
        (AiStrategicStyle.Economist, AiGoalKind.Develop) => 1.6,
        (AiStrategicStyle.Economist, AiGoalKind.Expand) => 1.2,
        (AiStrategicStyle.Economist, AiGoalKind.Hunt or AiGoalKind.Raid) => .8,
        (AiStrategicStyle.Defender, AiGoalKind.Defend) => 1.6,
        (AiStrategicStyle.Defender, AiGoalKind.Expand or AiGoalKind.Hunt or AiGoalKind.Raid) => .8,
        (AiStrategicStyle.Aggressor, AiGoalKind.Develop) => .7,
        (AiStrategicStyle.Aggressor, AiGoalKind.Expand) => 1.4,
        (AiStrategicStyle.Aggressor, AiGoalKind.Raid) => 1.6,
        (AiStrategicStyle.Hunter, AiGoalKind.Hunt) => 1.8,
        (AiStrategicStyle.Hunter, AiGoalKind.Develop) => .8,
        _ => 1
    };
    internal static double Risk(BattleState battle) => !Enabled(battle) ? 1 : Style(battle) switch
    {
        AiStrategicStyle.Defender => .65, AiStrategicStyle.Economist => .8,
        AiStrategicStyle.Aggressor => 1.2, AiStrategicStyle.Hunter => 1.05, _ => .9
    };

    internal static int Reserve(BattleState battle, IReadOnlyList<Ship> allies, IReadOnlyList<Ship> enemies)
    {
        if (!Enabled(battle)) return 0;
        int garrison = battle.BuildPrice(battle.ActiveSide, ShipClass.Garrison);
        int mobile = allies.Count(s => s.IsArmed && !s.IsMothership && !s.IsStructure);
        bool threatened = enemies.Any(e => e.IsArmed && allies.Any(a => a.IsMothership
            && battle.Board.Distance(a.Position, e.Position) <= e.MovementAllowance + e.AttackRange + 2));
        if (mobile < 2 || threatened) return garrison;
        return Style(battle) switch
        {
            AiStrategicStyle.Defender => garrison,
            AiStrategicStyle.Aggressor or AiStrategicStyle.Hunter => garrison,
            _ => 0
        };
    }

    internal static ShipClass PreferredHull(BattleState battle, IReadOnlyList<Ship> allies, IReadOnlyList<Ship> enemies)
    {
        int Count(ShipClass kind) => allies.Count(s => s.Definition.Class == kind);
        var style = Style(battle);
        var goal = battle.AiPlan(battle.ActiveSide)?.Goal;
        if (Count(ShipClass.Garrison) == 0) return ShipClass.Garrison;
        if (goal == AiGoalKind.Expand && Count(ShipClass.Invader) == 0) return ShipClass.Invader;
        if (style == AiStrategicStyle.Hunter && Count(ShipClass.Garrison) < 2) return ShipClass.Garrison;
        if ((style == AiStrategicStyle.Defender || enemies.Any(e => e.IsAirborne)) && Count(ShipClass.Kolonel) == 0)
            return ShipClass.Kolonel;
        if (goal == AiGoalKind.Expand && Count(ShipClass.Invader) >= 1 && Count(ShipClass.Togus) == 0)
            return ShipClass.Togus;
        if (style == AiStrategicStyle.Economist && Count(ShipClass.Fishing) + Count(ShipClass.FishingDock) < 2)
            return ShipClass.Fishing;
        if (style == AiStrategicStyle.Aggressor && Count(ShipClass.Garrison) < Count(ShipClass.Invader) + 2)
            return ShipClass.Garrison;
        return Count(ShipClass.Invader) <= Count(ShipClass.Kolonel) + 1 ? ShipClass.Invader : ShipClass.Kolonel;
    }

    internal static void Update(BattleState battle)
    {
        if (!Enabled(battle)) return;
        var side = battle.ActiveSide;
        var plan = battle.AiPlan(side)!;
        var allies = battle.OwnShips(side).ToArray();
        var mobile = allies.Where(s => s.IsArmed && !s.IsMothership && !s.IsStructure && !s.IsAirborne).ToArray();
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side).ToArray();
        var towns = battle.ObservedVillages(side).ToArray();
        var mother = battle.Mothership(side)!;
        var candidates = new List<Candidate>();
        int Distance(GridPosition p) => (int)Math.Ceiling(mobile.Select(s => battle.Board.Distance(s.Position, p)).DefaultIfEmpty(battle.Board.Distance(mother.Position, p)).Min());
        void Add(AiGoalKind kind, int id, GridPosition p, double value) => candidates.Add(new(kind, id, p,
            value * Weight(plan.Style, kind) / (1 + Distance(p) * .12)));
        foreach (var town in towns)
        {
            if (town.Owner != side)
            {
                Add(AiGoalKind.Expand, town.Id, town.Position, town.Health <= 0 ? 100 : 28 + (town.HasPort ? 8 : 0));
                continue;
            }
            var approaching = enemies.Where(e => e.IsArmed && battle.Board.Distance(town.Position, e.Position)
                <= e.MovementAllowance + e.AttackRange + 1).ToArray();
            if (approaching.Length > 0) Add(AiGoalKind.Defend, town.Id, town.Position, 45 + approaching.Length * 8);
            if (town.Health > 0 && town.Level < 5)
                Add(AiGoalKind.Develop, town.Id, town.Position, 20 + (town.HasPort ? 5 : 0));
        }
        foreach (var enemy in enemies.Where(e => e.IsMothership))
            Add(AiGoalKind.Hunt, enemy.Id, enemy.Position, 32 + (1 - enemy.HealthRatio) * 40);
        foreach (var raider in enemies.Where(e => e.IsArmed && !e.IsMothership && !e.IsAirborne
                     && allies.Any(a => a.Definition.Class == ShipClass.Lighthouse
                         && battle.Board.Distance(a.Position, e.Position) <= 2)))
            Add(AiGoalKind.Defend, raider.Id, raider.Position, 55);
        foreach (var sighting in battle.KnownFlagships(side).Where(s => battle.TurnSerial - s.Turn <= 12))
            if (!enemies.Any(e => e.IsMothership && e.Owner == sighting.Owner))
                Add(AiGoalKind.Hunt, 0, sighting.Position, 18);
        if (battle.Rules.ExperimentalEconomy)
            foreach (var p in battle.KnownTradeRaidTargets(side).Take(8)) Add(AiGoalKind.Raid, 0, p, 25);

        int distance = plan.Position is { } oldPosition ? Distance(oldPosition) : int.MaxValue;
        bool raidHeld = plan.Goal == AiGoalKind.Raid && allies.Any(s => s.Id == plan.AssignedShipId && s.Position == plan.Position);
        int lastProgress = distance < plan.LastDistance || raidHeld ? battle.Round : plan.LastProgressRound;
        bool stalled = plan.Goal != AiGoalKind.None && plan.Goal != AiGoalKind.Develop && battle.Round - lastProgress >= 3;
        var avoidPosition = stalled ? plan.Position : plan.AvoidPosition;
        int avoidUntil = stalled ? battle.Round + 2 : plan.AvoidUntilRound;
        var best = candidates.Where(c => avoidUntil < battle.Round || c.Position != avoidPosition || c.Kind == AiGoalKind.Defend
                || c.Kind == AiGoalKind.Expand && towns.Any(t => t.Id == c.Id && t.Health <= 0))
            .OrderByDescending(c => c.Value).ThenBy(c => c.Kind).ThenBy(c => c.Id)
            .ThenBy(c => c.Position.Y).ThenBy(c => c.Position.X).FirstOrDefault();
        if (best is null)
        {
            battle.SetAiPlan(plan with { Goal = AiGoalKind.None, Position = null, TargetId = 0, AssignedShipId = 0,
                AvoidPosition = avoidPosition, AvoidUntilRound = avoidUntil });
            return;
        }
        var retained = candidates.FirstOrDefault(c => c.Kind == plan.Goal && c.Id == plan.TargetId && c.Position == plan.Position);
        bool emergency = best.Kind == AiGoalKind.Defend && plan.Goal != AiGoalKind.Defend;
        bool retain = retained is not null && !emergency && !stalled
            && (battle.Round - plan.SinceRound < 3 || best.Value < retained.Value * 1.3);
        if (retain)
        {
            int assigned = mobile.Any(s => s.Id == plan.AssignedShipId) ? plan.AssignedShipId
                : Crew(retained!).Id;
            battle.SetAiPlan(plan with { LastDistance = distance, LastProgressRound = lastProgress, AssignedShipId = assigned,
                AvoidPosition = avoidPosition, AvoidUntilRound = avoidUntil });
        }
        else
        {
            var crew = Crew(best);
            battle.SetAiPlan(new(side, plan.Style, best.Kind, best.Id, best.Position, crew.Id,
                battle.Round, battle.Round, Distance(best.Position), avoidPosition, avoidUntil));
        }
        (int Id, int Distance) Crew(Candidate candidate)
        {
            var ship = mobile.Where(s => !CaptureOrders.Holding(battle, s) || candidate.Kind == AiGoalKind.Expand
                    && battle.Board.GetSurrounding(candidate.Position).Contains(s.Position))
                .OrderBy(s => candidate.Kind == AiGoalKind.Raid && s.Definition.Class != ShipClass.Garrison ? 1 : 0)
                .ThenBy(s => s.HasMortar && candidate.Kind == AiGoalKind.Expand ? 1 : 0)
                .ThenBy(s => battle.Board.Distance(s.Position, candidate.Position)).ThenBy(s => s.Id).FirstOrDefault();
            return (ship?.Id ?? 0, ship is null ? int.MaxValue : (int)Math.Ceiling(battle.Board.Distance(ship.Position, candidate.Position)));
        }
    }

    internal static CommandResult? Move(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        if (!Enabled(battle) || battle.AiPlan(battle.ActiveSide) is not { Position: { } position } plan
            || plan.Goal is AiGoalKind.None or AiGoalKind.Develop) return null;
        var group = allies.Where(s => s.IsArmed && !s.IsMothership && !s.IsAirborne && !s.IsStructure
                && s.CanMove && !s.HasMoved && !CaptureOrders.Holding(battle, s))
            .OrderBy(s => s.Id == plan.AssignedShipId ? 0 : 1).ThenBy(s => battle.Board.Distance(s.Position, position)).ThenBy(s => s.Id);
        foreach (var ship in group.Take(plan.Goal == AiGoalKind.Raid ? 1 : 3))
        {
            if (plan.Goal == AiGoalKind.Raid && ship.Position == position) continue;
            int range = plan.Goal == AiGoalKind.Raid ? 0 : plan.Goal == AiGoalKind.Expand
                ? ship.HasMortar && ship.Id != plan.AssignedShipId ? Math.Max(1, ship.AttackRange) : 1
                : Math.Max(1, ship.AttackRange);
            var route = battle.RouteToward(ship.Id, position, range);
            if (route.Count < 2) continue;
            var next = battle.AffordableDestination(ship.Id, route);
            if (next == ship.Position) continue;
            double support = allies.Where(a => a.IsArmed && !a.IsMothership
                && battle.Board.Distance(a.Position, next) <= a.MovementAllowance + a.AttackRange).Sum(a => a.Health);
            double exposure = danger(ship, next);
            if (exposure < ship.Health * Risk(battle) || plan.Goal == AiGoalKind.Defend && support > exposure * 1.5)
                return battle.Move(battle.ActiveSide, ship.Id, next);
        }
        return null;
    }
}
