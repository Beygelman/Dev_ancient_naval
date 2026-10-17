using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class V0209x2AiChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool valid, string message)
        {
            if (!valid) throw new Exception("v020.9x2 AI: " + message);
            checks++;
        }
        BattleRules NoCash()
        {
            var json = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
            json["StartingCredits"] = 0;
            json["IncomePerMothership"] = 0;
            json["HeavenlyAssistance"] = false;
            return BattleRules.FromJson(json.ToJsonString());
        }
        var poor = NoCash();
        BattleState CaptureArena(AiDifficulty difficulty, AiStrategicStyle style)
        {
            var coast = new GridPosition(9, 8);
            var battle = new BattleState(new GameBoard(22, 20, p => p == coast ? TerrainType.Land : TerrainType.Water, seed: 37),
                poor, new[]
                {
                    (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(20, 18)),
                    (Side.Player, ShipClass.Garrison, new GridPosition(8, 8)),
                    (Side.Enemy, ShipClass.Fishing, new GridPosition(10, 8))
                }, Array.Empty<GridPosition>(), villageSpots: new[] { coast }, difficulty: difficulty, piratesEnabled: false);
            battle.Villages.Single().Health = 0;
            return WithStyle(battle, style);
        }
        foreach (var style in Enum.GetValues<AiStrategicStyle>())
        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var ready = CaptureArena(difficulty, style);
            ready.EndTurn(Side.Player); ready.EndTurn(Side.Enemy);
            Check(ready.CanCaptureVillage(Side.Player, ready.Villages.Single().Id), "fixture has a legal waiting crew");
            var command = SimpleOpponent.Step(ready);
            Check(command.Success && command.Kind == CommandKind.Capture,
                $"{style}/{difficulty} captures before a tempting fishing target");

            var wait = CaptureArena(difficulty, style);
            bool ended = false;
            for (int order = 0; order < 40 && !ended; order++)
            {
                command = SimpleOpponent.Step(wait);
                Check(command.Success, "pre-capture waiting orders stay legal");
                Check(wait.Find(3)!.Position == new GridPosition(8, 8), "exploration does not pull the designated crew away");
                ended = command.Kind == CommandKind.EndTurn;
            }
            Check(ended, "waiting for a capture ends the turn without oscillation");
            wait.EndTurn(Side.Enemy);
            Check(SimpleOpponent.Step(wait).Kind == CommandKind.Capture, "held station becomes next-turn capture");
        }

        var saved = CaptureArena(AiDifficulty.Admiral, AiStrategicStyle.Hunter);
        var restored = BattleState.LoadJson(saved.SaveJson());
        Check(restored.AiPlan(Side.Player) == saved.AiPlan(Side.Player), "profile is embedded before the first AI decision");
        var originalOrder = SimpleOpponent.Step(saved);
        var restoredOrder = SimpleOpponent.Step(restored);
        Check(Signature(originalOrder) == Signature(restoredOrder)
            && saved.AiPlan(Side.Player) == restored.AiPlan(Side.Player), "save/resume preserves objective and next decision");
        var legacy = saved.CaptureSnapshot(); legacy.AiPlans = Array.Empty<SavedAiPlan>();
        Check(BattleState.LoadJson(BattleState.SerializeSnapshot(legacy)).AiPlan(Side.Player) is null,
            "missing old-save AI records never reroll on Continue");
        var invalid = saved.CaptureSnapshot();
        invalid.AiPlans[0] = invalid.AiPlans[0] with { Style = (AiStrategicStyle)999 };
        bool rejected = false;
        try { BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "invalid serialized personality is rejected");

        var hidden = new BattleState(new GameBoard(28, 24, _ => TerrainType.Water, seed: 11), poor,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(25, 21)),
                (Side.Player, ShipClass.Garrison, new GridPosition(4, 4)) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>(), difficulty: AiDifficulty.Admiral, piratesEnabled: false);
        var alternate = hidden.CaptureSnapshot(); alternate.Ships.Single(s => s.Owner == Side.Enemy).Position = new(22, 20);
        alternate.Ships.Single(s => s.Owner == Side.Enemy).Health = 1;
        var otherHidden = BattleState.LoadJson(BattleState.SerializeSnapshot(alternate));
        Check(Signature(SimpleOpponent.Step(hidden)) == Signature(SimpleOpponent.Step(otherHidden)),
            "unseen enemy HP and location do not change orders");

        var spending = hidden.CaptureSnapshot();
        spending.Credits[(int)Side.Player] = 8;
        spending.Ships.Single(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership).HasRadar = true;
        foreach (var s in spending.Ships.Where(s => s.Owner == Side.Player)) { s.HasMoved = false; s.MovementSpentUnits = 0; }
        spending.ActiveSide = Side.Player;
        var economist = WithStyle(BattleState.LoadJson(BattleState.SerializeSnapshot(spending)), AiStrategicStyle.Economist);
        var hunter = WithStyle(BattleState.LoadJson(BattleState.SerializeSnapshot(spending)), AiStrategicStyle.Hunter);
        var economyOrder = SimpleOpponent.Step(economist);
        var huntOrder = SimpleOpponent.Step(hunter);
        Check(economyOrder.Kind == CommandKind.Build && economist.Find(economyOrder.TargetId)?.Definition.Class == ShipClass.Fishing,
            "safe Economist funds an economic hull");
        Check(huntOrder.Kind == CommandKind.Build && hunter.Find(huntOrder.TargetId)?.Definition.Class == ShipClass.Garrison,
            "Hunter buys an additional mobile scout at the same prices");
        Check(economist.CaptureSnapshot().EventDraws == hunter.CaptureSnapshot().EventDraws,
            "strategic style does not draw from simulation event RNG");
        return checks;
    }

    /// <summary>Actual-rule deterministic skirmishes, bounded and explicitly reported if unresolved.
    /// These are planner integration measurements, not proof of balanced human play.</summary>
    internal static int Simulate(BattleRules rules)
    {
        int matches = 0;
        foreach (var style in Enum.GetValues<AiStrategicStyle>())
        {
            int seed = 1700 + (int)style;
            var towns = new[] { new GridPosition(7, 5), new GridPosition(7, 12), new GridPosition(16, 5), new GridPosition(16, 12) };
            var board = new GameBoard(24, 18, p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water, seed);
            var game = new BattleState(board, rules, new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(3, 8)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(20, 8)),
                (Side.Player, ShipClass.Garrison, new GridPosition(6, 8)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(17, 8)),
                (Side.Player, ShipClass.Fishing, new GridPosition(3, 10)),
                (Side.Enemy, ShipClass.Fishing, new GridPosition(20, 10))
            }, new[] { new GridPosition(4, 10), new GridPosition(19, 10) },
                resourceSeed: seed, villageSpots: towns, difficulty: AiDifficulty.Admiral, piratesEnabled: false);
            game = WithStyle(game, style);
            var clock = Stopwatch.StartNew();
            int orders = 0, turnOrders = 0, captures = 0, builds = 0, attacks = 0;
            bool resumeChecked = false;
            while (!game.IsOver && game.Round <= 100 && orders < 10000)
            {
                if (!resumeChecked && orders >= 30)
                {
                    var resumed = BattleState.LoadJson(game.SaveJson());
                    var a = game.Prepare(SimpleOpponent.Step); var b = resumed.Prepare(SimpleOpponent.Step);
                    a.Finish(); b.Finish();
                    if (Signature(a.Result) != Signature(b.Result) || game.SaveJson() != resumed.SaveJson())
                        throw new Exception("v020.9x2 simulation lost deterministic resume.");
                    if (!b.Result.Success) throw new Exception("v020.9x2 resumed simulation rejected an order.");
                    game = resumed;
                    resumeChecked = true; orders++; turnOrders++;
                    if (b.Result.Kind == CommandKind.Capture) captures++;
                    if (b.Result.Kind == CommandKind.Build) builds++;
                    if (b.Result.Kind == CommandKind.Attack) attacks++;
                    if (b.Result.Kind == CommandKind.EndTurn) turnOrders = 0;
                    continue;
                }
                var command = SimpleOpponent.Step(game);
                if (!command.Success) throw new Exception($"v020.9x2 simulation rejected {style}: {command.Message}");
                orders++; turnOrders++;
                if (command.Kind == CommandKind.Capture) captures++;
                if (command.Kind == CommandKind.Build) builds++;
                if (command.Kind == CommandKind.Attack) attacks++;
                if (command.Kind == CommandKind.EndTurn) turnOrders = 0;
                if (turnOrders > 200) throw new Exception("v020.9x2 AI failed to finish a bounded turn.");
            }
            if (!game.IsOver && game.Round <= 100) throw new Exception("v020.9x2 AI exhausted the order budget before the round horizon.");
            Console.WriteLine($"AI0209X2 seed={seed} player={style} enemy={game.AiPlan(Side.Enemy)?.Style} rounds={game.Round} orders={orders} winner={game.Winner?.ToString() ?? (game.IsDraw ? "draw" : "unresolved_horizon")} captures={captures} builds={builds} attacks={attacks} elapsed_ms={clock.Elapsed.TotalMilliseconds:F0}");
            matches++;
        }
        return matches;
    }

    private static BattleState WithStyle(BattleState battle, AiStrategicStyle style)
    {
        var state = battle.CaptureSnapshot();
        state.AiPlans = state.AiPlans.Select(p => p.Side == Side.Player ? p with { Style = style } : p).ToArray();
        return BattleState.LoadJson(BattleState.SerializeSnapshot(state));
    }
    private static string Signature(CommandResult command) => $"{command.Kind}/{command.ActorId}/{command.TargetId}/" +
        string.Join(';', command.Path?.Select(p => $"{p.X},{p.Y}") ?? Array.Empty<string>());
}
