using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0204Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("v020.4 rules: " + message);
            checks++;
        }
        BattleState Create(bool town = false, params (Side Owner, ShipClass Class, GridPosition Position)[] extra) => new(
            new GameBoard(32, 32, p => town && p == new GridPosition(15, 15) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(28, 28)) }.Concat(extra),
            Array.Empty<GridPosition>(), villageSpots: town ? new[] { new GridPosition(15, 15) } : Array.Empty<GridPosition>());
        BattleState Money(BattleState source, Side side, int amount)
        {
            var snapshot = source.CaptureSnapshot();
            snapshot.Credits[(int)side] = amount;
            return BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        }
        void NextPlayerTurn(BattleState battle)
        {
            Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
                "advance to next legal player construction turn");
        }
        Check(rules.PaidVillageUpgrades && rules.VillageLevelPrices.Count == 4 && rules.VillageLevelPrices.All(price => price > 0)
            && rules.LevelCurrencyRewards.SequenceEqual(new[] { 2, 2, 2, 2 }), "new-voyage paid growth and equal flagship rewards");

        var development = Create(true);
        development.Villages.Single().Owner = Side.Player;
        development = Money(development, Side.Player, 100);
        int townId = development.Villages.Single().Id;
        for (int turns = 0; turns < 3; turns++) NextPlayerTurn(development);
        Check(development.Villages.Single().Level == 1 && development.Villages.Single().Health == 5,
            "waiting several personal turns does not grow a paid village");
        for (int level = 1; level < 5; level++)
        {
            var town = development.Villages.Single();
            int money = development.Credits(Side.Player);
            int capacity = development.FleetCapacity(Side.Player);
            int price = rules.VillageLevelPrices[level - 1];
            Check(development.VillageUpgradePrice(Side.Player, townId) == price
                && development.CanUpgradeVillage(Side.Player, townId), "next paid level is available at its configured cost");
            var upgraded = development.UpgradeVillage(Side.Player, townId);
            Check(upgraded.Success && upgraded.Kind == CommandKind.Upgrade && upgraded.ActorId == townId
                && upgraded.TargetId == townId && town.Level == level + 1 && town.Health == town.MaxHealth
                && development.Credits(Side.Player) == money - price && development.FleetCapacity(Side.Player) == capacity + 1,
                "upgrade spends once, raises health and dynamic capacity, and identifies the town");
            Check(!development.UpgradeVillage(Side.Player, townId).Success
                && !development.BuildFromVillage(Side.Player, townId, ShipClass.Fishing, new(15, 14)).Success,
                "paid level uses this turn's construction action");
            development = BattleState.LoadJson(development.SaveJson());
            Check(development.Villages.Single().Level == level + 1 && development.Villages.Single().HasProduced
                && !development.CanUpgradeVillage(Side.Player, townId), "paid level and spent action survive Continue");
            NextPlayerTurn(development);
            var unlocked = (level + 1) switch
            {
                2 => ShipClass.Garrison,
                3 => ShipClass.Invader,
                4 => ShipClass.Kolonel,
                _ => ShipClass.Togus
            };
            Check(development.VillageBuildBlockReason(Side.Player, townId, unlocked) is null,
                "paid town level unlocks its matching shipyard class on the fresh turn");
            Check(development.VillageIncome(development.Villages.Single()) == rules.Economy.VillageIncomeBonus + (level + 2) / 2,
                "village income refresh follows the upgraded level");
        }
        Check(!development.CanUpgradeVillage(Side.Player, townId) && development.VillageUpgradePrice(Side.Player, townId) == 0,
            "maximum town level cannot be bought again");

        var poor = Create(true);
        poor.Villages.Single().Owner = Side.Player;
        poor = Money(poor, Side.Player, 4);
        townId = poor.Villages.Single().Id;
        Check(!poor.CanUpgradeVillage(Side.Player, townId) && !poor.UpgradeVillage(Side.Player, townId).Success
            && poor.Villages.Single().Level == 1 && poor.Credits(Side.Player) == 4, "insufficient funds cause no upgrade mutation");
        Check(!poor.UpgradeVillage(Side.Enemy, townId).Success, "another nation cannot buy this town's level");
        poor.Villages.Single().Health = 0;
        poor.SetCreative(true);
        Check(!poor.CanUpgradeVillage(Side.Player, townId), "defeated village cannot upgrade even in Creative");
        poor.Villages.Single().Health = 5;
        Check(poor.VillageUpgradePrice(Side.Player, townId) == 0 && poor.UpgradeVillage(Side.Player, townId).Success
            && poor.Credits(Side.Player) == 4, "Creative construction removes level cost but retains legal state checks");
        NextPlayerTurn(poor);
        poor.Villages.Single().Owner = null;
        Check(!poor.CanUpgradeVillage(Side.Player, townId), "neutral village cannot be upgraded before capture");

        var legacyJson = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        legacyJson.Remove("PaidVillageUpgrades");
        legacyJson.Remove("VillageLevelPrices");
        legacyJson["LevelCurrencyRewards"] = JsonSerializer.SerializeToNode(new[] { 2, 4, 5, 7 });
        var legacyRules = BattleRules.FromJson(legacyJson.ToJsonString());
        var legacy = new BattleState(new GameBoard(32, 32, p => p == new GridPosition(15, 15) ? TerrainType.Land : TerrainType.Water),
            legacyRules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(28, 28)) },
            Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(15, 15) });
        legacy.Villages.Single().Owner = Side.Player;
        NextPlayerTurn(legacy);
        legacy = BattleState.LoadJson(legacy.SaveJson());
        NextPlayerTurn(legacy);
        Check(!legacy.Rules.PaidVillageUpgrades && legacy.Villages.Single().Level == 2
            && !legacy.CanUpgradeVillage(Side.Player, legacy.Villages.Single().Id)
            && legacy.Rules.LevelCurrencyRewards.SequenceEqual(new[] { 2, 4, 5, 7 }),
            "historic missing policy retains automatic growth and its own reward schedule across Continue");
        foreach (JsonNode? invalidPrices in new JsonNode?[] { null, JsonSerializer.SerializeToNode(new[] { 5, 8, 12 }),
            JsonSerializer.SerializeToNode(new[] { 5, -8, 12, 16 }) })
        {
            var malformed = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
            malformed["VillageLevelPrices"] = invalidPrices;
            bool rejected = false;
            try { BattleRules.FromJson(malformed.ToJsonString()); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "malformed village price schedules are rejected before indexing");
        }

        var flagship = Money(Create(), Side.Player, 100);
        for (int level = 2; level <= 5; level++)
        {
            var snapshot = flagship.CaptureSnapshot();
            snapshot.Ships.Single(s => s.Id == 1).Resources = flagship.Find(1)!.ResourcesRequired - 1;
            snapshot.Fish = new[] { new GridPosition(1, 2) };
            snapshot.Shoals = Array.Empty<GridPosition>();
            flagship = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
            int money = flagship.Credits(Side.Player);
            long lifetime = flagship.Statistics.CurrencyEarned;
            Check(flagship.Collect(Side.Player, 1, new(1, 2)).Success && flagship.Find(1)!.Level == level
                && flagship.Credits(Side.Player) == money - flagship.CollectionCost(Side.Player) + 2
                && flagship.Statistics.CurrencyEarned == lifetime + 2, "each new flagship level earns exactly two coins once");
            flagship = BattleState.LoadJson(flagship.SaveJson());
            var choice = level switch { 2 => UpgradeChoice.Mobility, 3 => UpgradeChoice.Restoration,
                4 => UpgradeChoice.SecondAttack, _ => UpgradeChoice.Firepower };
            money = flagship.Credits(Side.Player);
            Check(flagship.ChooseUpgrade(Side.Player, 1, choice).Success
                && flagship.Credits(Side.Player) == money && !flagship.ChooseUpgrade(Side.Player, 1, choice).Success,
                "choosing or retrying the saved flagship reward cannot pay again");
        }

        var repair = Create(false, (Side.Enemy, ShipClass.Garrison, new(2, 2)));
        var repairSnapshot = repair.CaptureSnapshot();
        repairSnapshot.Ships.Single(s => s.Id == 1).Level = 2;
        repairSnapshot.Ships.Single(s => s.Id == 1).Health = 10;
        repairSnapshot.Fish = new[] { new GridPosition(2, 1) };
        repairSnapshot.Shoals = new[] { new GridPosition(1, 2) };
        repairSnapshot.Credits[(int)Side.Player] = 50;
        repair = BattleState.LoadJson(BattleState.SerializeSnapshot(repairSnapshot));
        Check(repair.CollectionCells(1).Contains(new(2, 1)) && repair.DockCells(1).Contains(new(1, 2))
            && repair.CanAttack(1, 3) && repair.BuildBlockReason(Side.Player, 1, ShipClass.Garrison) is null,
            "damaged flagship fixture has valid combat, collection and building before repair");
        Check(repair.Repair(Side.Player, 1).Success && !repair.Find(1)!.CanMove
            && repair.Find(1)!.AttacksRemaining == 0 && repair.Find(1)!.MovementLocked,
            "active repair consumes movement and all active shots");
        Check(!repair.Move(Side.Player, 1, new(1, 2)).Success && !repair.Attack(Side.Player, 1, 3).Success
            && !repair.Build(Side.Player, 1, ShipClass.Garrison, new(2, 1)).Success
            && !repair.BuildLighthouse(Side.Player, 1, new(2, 1)).Success
            && !repair.BuyRadar(Side.Player, 1).Success, "all ship action facades reject after repair");
        Check(repair.CollectionCells(1).Count == 0 && repair.DockCells(1).Count == 0
            && !repair.Collect(Side.Player, 1, new(2, 1)).Success && !repair.Collect(Side.Player, new(2, 1)).Success
            && !repair.BuildDock(Side.Player, 1, new(1, 2)).Success && !repair.BuildDock(Side.Player, new(1, 2)).Success,
            "direct and side-selected resource/dock orders cannot bypass repaired flagship");
        repair = BattleState.LoadJson(repair.SaveJson());
        Check(!repair.Find(1)!.CanMove && repair.Find(1)!.AttacksRemaining == 0
            && repair.BuildBlockReason(Side.Player, 1, ShipClass.Garrison) is not null,
            "repair exhaustion and construction block survive Continue");
        NextPlayerTurn(repair);
        Check(repair.Find(1)!.CanMove && repair.Find(1)!.AttacksRemaining == 1
            && repair.CollectionCells(1).Count > 0, "next personal start restores action budget after active repair");

        var fisher = Create(false, (Side.Player, ShipClass.Fishing, new(4, 4)));
        var fishingSnapshot = fisher.CaptureSnapshot();
        fishingSnapshot.Ships.Single(s => s.Id == 1).Level = 2;
        fishingSnapshot.Ships.Single(s => s.Id == 3).Health = 1;
        fishingSnapshot.Fish = new[] { new GridPosition(4, 5) };
        fishingSnapshot.Shoals = new[] { new GridPosition(4, 3) };
        fishingSnapshot.Credits[(int)Side.Player] = 50;
        fisher = BattleState.LoadJson(BattleState.SerializeSnapshot(fishingSnapshot));
        Check(fisher.LighthouseBlockReason(Side.Player, 3) is null && fisher.CollectionCells(3).Count == 1
            && fisher.DockCells(3).Count == 1, "damaged fishing producer has valid work before repair");
        Check(fisher.Repair(Side.Player, 3).Success && !fisher.BuildLighthouse(Side.Player, 3, new(5, 4)).Success
            && !fisher.BuildDock(Side.Player, new(4, 3)).Success && !fisher.Collect(Side.Player, new(4, 5)).Success,
            "side-selected collection, dock and fishing lighthouse all respect active repair");
        var mortarRepair = Money(Create(), Side.Player, 50);
        mortarRepair.Find(1)!.Level = 5;
        mortarRepair.Find(1)!.Health = 10;
        mortarRepair.Find(1)!.HasRadar = true;
        Check(mortarRepair.MortarBlockReason(Side.Player, 1) is null && mortarRepair.Repair(Side.Player, 1).Success
            && !mortarRepair.BuyMortar(Side.Player, 1).Success, "active repair also blocks an otherwise unlocked mortar upgrade");
        var rewardAfterRepair = Money(Create(), Side.Player, 50);
        rewardAfterRepair.Find(1)!.Level = 2;
        rewardAfterRepair.Find(1)!.Health = 10;
        Check(rewardAfterRepair.Repair(Side.Player, 1).Success, "repair before an externally earned level choice");
        rewardAfterRepair.Find(1)!.PendingUpgradeLevel = 2;
        Check(!rewardAfterRepair.ChooseUpgrade(Side.Player, 1, UpgradeChoice.FishingBoat).Success
            && rewardAfterRepair.FleetUsed(Side.Player) == 1 && rewardAfterRepair.Find(1)!.PendingUpgradeLevel == 2,
            "upgrade gift cannot bypass a repaired producer's construction lock");

        var townRepair = Create(true);
        var repairTown = townRepair.Villages.Single();
        repairTown.Owner = Side.Player;
        repairTown.Level = 3;
        repairTown.Health = 5;
        townRepair = Money(townRepair, Side.Player, 50);
        townId = townRepair.Villages.Single().Id;
        Check(townRepair.CanRepairVillage(Side.Player, townId) && townRepair.CanUpgradeVillage(Side.Player, townId)
            && townRepair.PortBlockReason(Side.Player, townId) is null
            && townRepair.FortifyBlockReason(Side.Player, townId) is null, "damaged city can work before repair");
        Check(townRepair.RepairVillage(Side.Player, townId).Success
            && !townRepair.BuildFromVillage(Side.Player, townId, ShipClass.Fishing, new(15, 14)).Success
            && !townRepair.UpgradeVillage(Side.Player, townId).Success
            && !townRepair.BuildPort(Side.Player, townId).Success
            && !townRepair.FortifyVillage(Side.Player, townId).Success,
            "repaired city cannot construct, improve, fortify or open a port");
        townRepair = BattleState.LoadJson(townRepair.SaveJson());
        Check(!townRepair.CanUpgradeVillage(Side.Player, townId), "city repair locks persist across Continue");
        NextPlayerTurn(townRepair);
        Check(townRepair.CanUpgradeVillage(Side.Player, townId), "city work resumes on the next personal start");

        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var opponent = Create(true);
            opponent.Villages.Single().Owner = Side.Enemy;
            opponent = Money(opponent, Side.Enemy, 20);
            opponent.SetDifficulty(difficulty);
            Check(opponent.EndTurn(Side.Player).Success, "start paid-town AI turn");
            int cash = opponent.Credits(Side.Enemy);
            var order = SimpleOpponent.Step(opponent);
            Check(order.Success && order.Kind == CommandKind.Upgrade
                && order.ActorId == opponent.Villages.Single().Id && opponent.Villages.Single().Level == 2
                && opponent.Credits(Side.Enemy) == cash - 5,
                difficulty + " invests in a legal paid level rather than waiting for automatic growth");
            var resumed = BattleState.LoadJson(opponent.SaveJson());
            Check(resumed.Villages.Single().Level == 2 && resumed.Villages.Single().HasProduced,
                difficulty + " paid AI development survives Continue");
        }
        return checks;
    }
}
