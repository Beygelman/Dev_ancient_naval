using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using System.Diagnostics;
using System.Text.Json.Nodes;

internal static partial class BattleScenarios
{
    private static void PortsAndDifficulty()
    {
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")))!;
        document["startingCredits"] = 100;
        var rules = BattleRules.FromJson(document.ToJsonString());
        Check(rules.Get(ShipClass.Mothership).MaxHealth == 15 && rules.Get(ShipClass.Fishing).Price == 4 && rules.Get(ShipClass.FishingDock).Price == 8, "Requested lower HP/fishing prices");
        var towns = new[]
        {
            new GridPosition(3, 3),
            new GridPosition(19, 3),
            new GridPosition(26, 3)
        };
        var board = new GameBoard(32, 14, p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water);
        BattleState Create() => new(board, rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 10)), (Side.Enemy, ShipClass.Mothership, new GridPosition(30, 10)), (Side.Player, ShipClass.Garrison, new GridPosition(3, 2)) }, Array.Empty<GridPosition>(), villageSpots: towns);
        var battle = Create();
        battle.SetGodEye(true);
        Check(board.Tiles.All(t => battle.Vision.IsVisible(Side.Player, t.Position)) && !battle.Vision.IsVisible(Side.Enemy, new(1, 1)), "God's eye is human-only");
        battle.SetDifficulty(AiDifficulty.Admiral);
        battle = BattleState.LoadJson(battle.SaveJson());
        Check(battle.GodEye && battle.Difficulty == AiDifficulty.Admiral && battle.Vision.IsVisible(Side.Player, new(31, 0)), "Voyage settings survive resume");
        battle.SetGodEye(false);
        Check(!battle.Vision.IsExplored(Side.Player, new(31, 0)), "Turning off God's eye restores real exploration memory");
        var ordinaryPreview = battle.PreviewMovement(3);
        var ordinaryGoal = ordinaryPreview.Costs.Keys.First(p => p != battle.Find(3)!.Position);
        var ordinaryPath = ordinaryPreview.PathTo(ordinaryGoal).ToArray();
        Check(battle.Move(Side.Player, 3, ordinaryGoal).Success && ordinaryPreview.PathTo(ordinaryGoal).SequenceEqual(ordinaryPath), "Ordinary preview also remains immutable after movement");
        battle.Find(3)!.ResetTurn();
        battle.Find(3)!.Position = new(3, 2);
        var town = battle.Villages[0];
        town.Owner = Side.Player;
        Check(battle.PortBlockReason(Side.Player, town.Id)is not null, "Ports are locked before city level");
        town.Level = 3;
        town.Health = 15;
        int funds = battle.Credits(Side.Player), income = battle.GrossIncome(Side.Player);
        Check(battle.BuildPort(Side.Player, town.Id).Success && battle.Credits(Side.Player) == funds - 6 && town.HasProduced, "Port purchase consumes currency and shipyard work");
        Check(battle.GrossIncome(Side.Player) == income + battle.VillageIncome(town) + 1, "Port adds exactly one town income");
        Check(battle.VillageBuildPrice(town.Id, ShipClass.Invader) == 8 && battle.BuildPrice(Side.Player, ShipClass.Invader) == 10, "Discount belongs only to the port's shipyard");
        town.HasProduced = false;
        funds = battle.Credits(Side.Player);
        var built = battle.BuildFromVillage(Side.Player, town.Id, ShipClass.Invader, battle.VillageSpawnCells(town.Id).First());
        Check(built.Success && battle.Credits(Side.Player) == funds - 8, "Displayed port price equals actual debit");
        foreach (var next in battle.Villages.Skip(1))
        {
            next.Owner = Side.Player;
            next.Level = 3;
            next.Health = 15;
            Check(battle.BuildPort(Side.Player, next.Id).Success, "More owned ports link");
        }

        var lanes = battle.TradeRoutes(Side.Player);
        Check(lanes.Routes.Count == 3 && lanes.Routes.All(r => r.All(p => board.GetTile(p).Terrain != TerrainType.Land)), "All port pairs have shortest water routes");
        Check(ReferenceEquals(lanes, battle.TradeRoutes(Side.Player)), "Trade topology reuses a cache");
        var brig = battle.Find(3)!;
        var route = lanes.Routes[0];
        brig.Position = route[0];
        // Clear the new launch from the route without changing the network.
        battle.Find(built.TargetId)!.Position = new(2, 10);
        battle.SetGodEye(true);
        for (int speed = 1; speed <= 8; speed++)
        {
            var boosted = document.DeepClone();
            foreach (var definition in boosted["ships"]!.AsArray())
                if (definition!["class"]!.GetValue<string>() == "Garrison")
                    definition["movement"] = speed;
            var save = battle.CaptureSnapshot();
            save.Rules = BattleRules.FromJson(boosted.ToJsonString());
            var sample = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            var target = route[speed + Math.Max(2, (int)Math.Ceiling(speed * .2))];
            var preview = sample.PreviewMovement(brig.Id);
            Check(preview.Costs.ContainsKey(target), "Every speed receives minimum +2 or 20% on uninterrupted lanes");
            var predicted = preview.PathTo(target);
            Check(sample.PathTo(brig.Id, target).SequenceEqual(predicted), "Trade preview equals command route");
            var moved = sample.Move(Side.Player, brig.Id, target);
            Check(moved.Success && moved.Path!.SequenceEqual(predicted), "Actual trade travel equals preview");
            Check(preview.PathTo(target).SequenceEqual(predicted), "Preview retains its original state after the ship moves");
        }

        var firstLeg = battle.Move(Side.Player, brig.Id, route[2]);
        Check(firstLeg.Success && brig.TradeStreak > 0, "Continuous lane run persists between orders");
        var loaded = BattleState.LoadJson(battle.SaveJson());
        Check(loaded.Find(brig.Id)!.TradeStreak == brig.TradeStreak && loaded.PathTo(brig.Id, route[5]).SequenceEqual(battle.PathTo(brig.Id, route[5])), "Resume preserves acceleration and remaining route");
        battle.Villages[1].Owner = Side.Enemy;
        Check(battle.TradeRoutes(Side.Player).Routes.Count == 1 && battle.TradeRoutes(Side.Enemy).Routes.Count == 0, "Captured enemy ports cannot lend player speed");
        battle.Villages[2].Health = 0;
        Check(battle.TradeRoutes(Side.Player).IsEmpty, "Defeated port suspends the lane");
        var taken = battle.Villages[1];
        taken.Health = 0;
        brig.Position = battle.PortBerth(taken);
        brig.ResetTurn();
        Check(!battle.CanCaptureVillage(Side.Player, taken.Id), "A defeated port still requires the capture wait");
        Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success, "Capture wait advances a complete round");
        Check(battle.CaptureVillage(Side.Player, taken.Id).Success && taken.HasPort && taken.Owner == Side.Player && brig.IsExhausted, "Actual delayed capture transfers a port and spends the crew");
        Check(battle.TradeRoutes(Side.Player).Routes.Count == 1, "Captured port rejoins only the new owner's network");

        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.CannonTower, new GridPosition(5, 6)), (Side.Enemy, ShipClass.Garrison, new GridPosition(9, 5)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        battle.Find(1)!.HasRadar = true;
        battle.Vision.Recompute(battle.Ships, 1);
        Check(!battle.Find(3)!.HasRadar && battle.FindObserved(Side.Player, 4)is null && battle.CanAttack(3, 4) && battle.TargetCells(3).Contains(new(9, 5)), "Shared radar permits an armed tower without revealing enemy stats");
        var radarShot = battle.AttackAt(Side.Player, 3, new(9, 5));
        Check(radarShot.Success && radarShot.Shots![0].TargetVisibleToPlayer == false, "Anonymous radar shot keeps impact privacy");
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(10, 8)), (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)), (Side.Player, ShipClass.Garrison, new GridPosition(3, 2)), (Side.Enemy, ShipClass.Garrison, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Invader, new GridPosition(8, 9)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        battle.SetDifficulty(AiDifficulty.Admiral);
        battle.Find(5)!.Health = 1;
        var decision = SimpleOpponent.Step(battle);
        Check(decision.Kind == CommandKind.Attack && decision.TargetId == 5, "Admiral finishes weak targets before healthy flagships");
        battle.Find(1)!.Position = new(7, 8);
        battle.Find(1)!.Health = 5;
        battle.Vision.Recompute(battle.Ships, 1);
        decision = SimpleOpponent.Step(battle);
        Check(decision.Kind is CommandKind.Move or CommandKind.Repair && decision.ActorId == 1, "Wounded flagship escapes before ordinary fleet actions");
        // Endgame full visibility must survive a save without enabling AI omniscience.
        battle = Create();
        battle.Find(2)!.Position = new(4, 2);
        battle.Find(2)!.Health = 1;
        battle.Vision.Recompute(battle.Ships, 1);
        Check(battle.Attack(Side.Player, 3, 2).Success && battle.Winner == Side.Player && battle.FullMapVisible && board.Tiles.All(t => battle.Vision.IsVisible(Side.Player, t.Position)), "Human victory removes fog");
        Check(BattleState.LoadJson(battle.SaveJson()).FullMapVisible, "Victory exploration resumes without a manual toggle");
        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var sample = SkirmishSetup.Create(ArchipelagoGenerator.Create(97, 1), rules, 1);
            sample.SetDifficulty(difficulty);
            var clock = Stopwatch.StartNew();
            for (int orders = 0; orders < 300 && !sample.IsOver; orders++)
                Check(SimpleOpponent.Step(sample).Success, "Difficulty issues only legal observed orders");
            Check(BattleState.LoadJson(sample.SaveJson()).Difficulty == difficulty, "Difficulty remains stable after AI turns");
            Console.WriteLine($"DIFFICULTY {difficulty} round={sample.Round} ships={sample.Ships.Count} elapsed_ms={clock.Elapsed.TotalMilliseconds:F1}");
        }

        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var normal = BattleRules.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")));
            var sample = SkirmishSetup.Create(ArchipelagoGenerator.Create(101, 1), normal, 1);
            sample.SetDifficulty(difficulty);
            int orders = 0;
            while (!sample.IsOver && sample.Round < 160 && orders < 5000)
            {
                Check(SimpleOpponent.Step(sample).Success, "Whole-game difficulty orders remain legal");
                orders++;
            }

            Console.WriteLine($"COMPLETE_AI {difficulty} winner={sample.Winner} round={sample.Round} orders={orders}");
            Check(sample.IsOver, "Difficulty finishes a complete voyage rather than a permanent stalemate");
        }
    }
}
