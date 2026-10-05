using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class PortEconomyCorrectionsChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("connected-port economy: " + message);
            checks++;
        }
        BattleState Restore(BattleSave saved) => BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        Check(rules.Ports.ConnectedCityIncome && rules.Ports.Income == 0,
            "new voyages replace the former fixed port income");
        Check(!new PortRules().ConnectedCityIncome && new PortRules().Income == 1,
            "historical optional-field defaults preserve the fixed one-Thor port");

        var townCells = new[] { new GridPosition(4, 14), new(22, 14), new(13, 4), new(13, 22) };
        var berths = new[] { new GridPosition(4, 15), new(22, 15), new(13, 5), new(13, 21) };
        var beacons = new[] { new GridPosition(10, 15), new(16, 15), new(13, 11) };
        var board = new GameBoard(32, 32, p => townCells.Contains(p) ? TerrainType.Land : TerrainType.Water,
            seed: 207106);
        BattleState Fixture(int count, int relayCount = 3)
        {
            var battle = new BattleState(board, rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 27)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(29, 2)),
                (Side.Player, ShipClass.Garrison, new GridPosition(23, 15)),
                (Side.Enemy, ShipClass.Kolonel, new GridPosition(22, 13)) },
                Array.Empty<GridPosition>(), villageSpots: townCells.Take(count), piratesEnabled: true);
            var saved = battle.CaptureSnapshot();
            for (int index = 0; index < saved.Villages.Length; index++)
            {
                var town = saved.Villages[index];
                saved.Villages[index] = town with { Owner = Side.Player, Level = 3, Health = 15,
                    Port = true, PortCell = berths[index] };
                saved.Income = saved.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 3)).ToArray();
            }
            for (int index = 0; index < relayCount; index++)
                saved.Ships = saved.Ships.Append(new SavedShip { Id = 100 + index, Owner = Side.Player,
                    Kind = ShipClass.Lighthouse, Position = beacons[index], Health = 10, Level = 1,
                    ConstructionPrice = 0 }).ToArray();
            saved.NextId = 103;
            saved.Credits[(int)Side.Player] = 200;
            return Restore(saved);
        }
        void EveryPort(BattleState battle, int amount, string name)
        {
            foreach (var town in battle.Villages.Where(v => v.Owner == Side.Player && v.Health > 0))
                Check(battle.PortIncome(town) == amount && battle.ConnectedPortCityCount(town) == amount,
                    name + ": " + town.Id);
        }

        var single = Fixture(1);
        Check(single.PortIncome(single.Villages[0]) == 0,
            "a lone port earns no trade income even with three beacons");
        Check(single.GrossIncome(Side.Player) == rules.IncomePerMothership + 3,
            "ordinary settlement income is independent from the disconnected port");
        EveryPort(Fixture(4, 0), 0, "disconnected cities earn zero");
        EveryPort(Fixture(2), 1, "two connected cities earn one each");
        EveryPort(Fixture(3), 2, "three connected cities earn two each");
        var four = Fixture(4);
        EveryPort(four, 3, "four connected cities earn three each");
        var sharedLand = new[] { new GridPosition(8, 8), new GridPosition(10, 8) };
        var shared = new BattleState(new GameBoard(18, 18,
            p => sharedLand.Contains(p) ? TerrainType.Land : TerrainType.Water), rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(15, 15)) },
            Array.Empty<GridPosition>(), villageSpots: sharedLand);
        var sharedSave = shared.CaptureSnapshot();
        for (int index = 0; index < sharedSave.Villages.Length; index++)
        {
            var town = sharedSave.Villages[index];
            sharedSave.Villages[index] = town with { Owner = Side.Player, Level = 3, Health = 15,
                Port = true, PortCell = new(9, 9) };
            sharedSave.Income = sharedSave.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 3)).ToArray();
        }
        shared = Restore(sharedSave);
        EveryPort(shared, 1, "cities sharing a valid berth count each other once");
        Check(shared.TradeRoutes(Side.Player).IsEmpty
            && shared.GrossIncome(Side.Player) == rules.IncomePerMothership + 8,
            "a shared berth needs no phantom lane or duplicate income edge");
        var network = four.TradeRoutes(Side.Player);
        Check(network.AreConnected(berths[0], berths[1]) && network.AreConnected(berths[2], berths[3])
            && !network.Routes.Any(route => route.First() == berths[0] && route.Last() == berths[1]
                || route.First() == berths[1] && route.Last() == berths[0]),
            "city connectivity traverses multiple six-cell links instead of requiring a direct route");
        Check(four.GrossIncome(Side.Player) == rules.IncomePerMothership + 4 * (3 + 3),
            "four trade bonuses enter the actual fleet gross income");
        string beforeQueries = four.SaveJson();
        for (int query = 0; query < 100; query++)
            Check(ReferenceEquals(network, four.TradeRoutes(Side.Player))
                && four.PortIncome(four.Villages[0]) == 3, "unchanged topology reuses the lane network");
        Check(four.SaveJson() == beforeQueries,
            "income and connectivity queries never rewrite state, receipts or RNG");
        var resumed = BattleState.LoadJson(beforeQueries);
        Check(resumed.SaveJson() == beforeQueries && resumed.GrossIncome(Side.Player) == four.GrossIncome(Side.Player),
            "Continue preserves generated routes, policy, balances and deterministic state exactly");
        var unbuilt = Fixture(2).CaptureSnapshot();
        for (int index = 0; index < unbuilt.Villages.Length; index++)
            unbuilt.Villages[index] = unbuilt.Villages[index] with { Port = false, PortCell = null };
        var construction = Restore(unbuilt);
        int baseIncome = construction.GrossIncome(Side.Player);
        var firstPort = construction.BuildPort(Side.Player, construction.Villages[0].Id);
        Check(firstPort.Success && firstPort.Message.Contains("+0 income")
            && construction.GrossIncome(Side.Player) == baseIncome,
            "constructing the first port gives no hidden fixed bonus");
        var secondPort = construction.BuildPort(Side.Player, construction.Villages[1].Id);
        Check(secondPort.Success && secondPort.Message.Contains("+1 income")
            && construction.GrossIncome(Side.Player) == baseIncome + 2,
            "constructing a connected second port updates both cities and its acknowledgement");

        var foreign = Fixture(4).CaptureSnapshot();
        foreign.Villages[1] = foreign.Villages[1] with { Owner = Side.Enemy };
        foreign.Income = foreign.Income.Select(source => source.Id == $"village:{foreign.Villages[1].Id}"
            ? source with { Owner = Side.Enemy } : source).ToArray();
        var divided = Restore(foreign);
        EveryPort(divided, 2, "foreign cities cannot contribute to a friendly component");
        Check(divided.PortIncome(divided.Villages[1]) == 0,
            "the enemy's solitary port does not borrow player lighthouses");
        var foreignRelays = Fixture(2).CaptureSnapshot();
        foreach (var ship in foreignRelays.Ships.Where(s => s.Kind == ShipClass.Lighthouse)) ship.Owner = Side.Enemy;
        EveryPort(Restore(foreignRelays), 0, "foreign lighthouse chains never relay friendly trade");

        var blockedSave = Fixture(4).CaptureSnapshot();
        blockedSave.Whirlpools = new[] { new Whirlpool(beacons[2], board.BlastCells(beacons[2])) };
        var blocked = Restore(blockedSave);
        Check(blocked.PortIncome(blocked.Villages[2]) == 0 && blocked.PortIncome(blocked.Villages[0]) == 2,
            "a forbidden relay suspends only the cities cut from the surviving network");
        blockedSave = blocked.CaptureSnapshot();
        blockedSave.Whirlpools = Array.Empty<Whirlpool>();
        EveryPort(Restore(blockedSave), 3, "restoring a navigable relay restores the derived income");

        var relayLoss = Fixture(3);
        Check(relayLoss.Scuttle(Side.Player, 102).Success, "a real lighthouse removal succeeds");
        Check(relayLoss.PortIncome(relayLoss.Villages[2]) == 0
            && relayLoss.PortIncome(relayLoss.Villages[0]) == 1
            && relayLoss.PortIncome(relayLoss.Villages[1]) == 1,
            "destroying a relay recomputes both connected and stranded city income");

        var claimSave = Fixture(4).CaptureSnapshot();
        var claimedId = claimSave.Villages[1].Id;
        claimSave.Villages[1] = claimSave.Villages[1] with { Owner = Side.Enemy, Health = 0 };
        claimSave.Income = claimSave.Income.Where(source => source.Id != $"village:{claimedId}").ToArray();
        claimSave.TurnSerial = 1;
        claimSave.CaptureWaits = new[] { new SavedWait(claimedId, Side.Player, 3, new(23, 15), 0) };
        var claimed = Restore(claimSave);
        EveryPort(claimed, 2, "a defeated enemy harbor is not an income endpoint");
        Check(claimed.CaptureVillage(Side.Player, claimedId).Success && claimed.Find(3)!.IsExhausted,
            "the real delayed capture transfers a saved port and consumes its crew");
        EveryPort(claimed, 3, "capturing a port updates every connected friendly city");
        int claimIncome = claimed.GrossIncome(Side.Player);
        Check(!claimed.CaptureVillage(Side.Player, claimedId).Success && claimed.GrossIncome(Side.Player) == claimIncome,
            "duplicate claims cannot add a second connection or income source");

        var hitSave = Fixture(4).CaptureSnapshot();
        hitSave.ActiveSide = Side.Enemy;
        hitSave.Villages[1] = hitSave.Villages[1] with { Health = 1 };
        var attacked = Restore(hitSave);
        var hit = attacked.Prepare(b => b.AttackVillage(Side.Enemy, 4, attacked.Villages[1].Id));
        Check(hit.Result.Success && attacked.PendingPresentation is not null
            && attacked.PortIncome(attacked.Villages[0]) == 3,
            "prepared shells do not reduce trade income before impact");
        hit.Impact("village"); hit.Impact("village");
        Check(attacked.Villages[1].Health == 0 && attacked.PortIncome(attacked.Villages[1]) == 0
            && attacked.PortIncome(attacked.Villages[0]) == 2,
            "the exact-once impact removes the defeated city's connection");
        hit.Finish(); hit.Finish();
        Check(attacked.PendingPresentation is null && attacked.PortIncome(attacked.Villages[0]) == 2,
            "staged restoration and repeated finish cannot resurrect a lost trade bonus");
        int bank = attacked.Credits(Side.Player);
        long lifetime = attacked.Statistics.CurrencyEarned;
        int net = attacked.Income(Side.Player), gross = attacked.GrossIncome(Side.Player);
        var incomeOrder = attacked.Prepare(b => b.EndTurn(Side.Enemy));
        incomeOrder.Finish(); incomeOrder.Finish();
        Check(incomeOrder.Result.Success && incomeOrder.Result.IncomeReceipts!.Sum(receipt => receipt.Amount) == net
            && attacked.Credits(Side.Player) == bank + net && attacked.Statistics.CurrencyEarned == lifetime + gross,
            "staged income receipts reconcile with the exact-once bank and lifetime ledger");
        Check(incomeOrder.Result.IncomeReceipts!.Where(receipt => receipt.SourceId.StartsWith("village:"))
            .All(receipt => receipt.Amount == 5),
            "each surviving level-three city receipt includes its base three and connected two");
        Check(BattleState.LoadJson(attacked.SaveJson()).SaveJson() == attacked.SaveJson(),
            "post-impact income and receipt state resume exactly");

        var legacySave = Fixture(1).CaptureSnapshot();
        var oldRules = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        oldRules["Ports"]!.AsObject().Remove("ConnectedCityIncome");
        oldRules["Ports"]!["Income"] = 1;
        legacySave.Rules = BattleRules.FromJson(oldRules.ToJsonString());
        legacySave.Income = legacySave.Income.Select(source => source.Id.StartsWith("village:")
            ? source with { Amount = source.Amount + 1 } : source).ToArray();
        var legacy = Restore(legacySave);
        Check(!legacy.Rules.Ports.ConnectedCityIncome && legacy.PortIncome(legacy.Villages[0]) == 1
            && legacy.ConnectedPortCityCount(legacy.Villages[0]) == 0,
            "a missing optional rule retains the old disconnected one-Thor port");
        Check(legacy.GrossIncome(Side.Player) == rules.IncomePerMothership + 4,
            "legacy stored income remains unchanged rather than being silently migrated");
        var oldDefeatSave = Fixture(2).CaptureSnapshot();
        oldDefeatSave.Rules = legacy.Rules;
        oldDefeatSave.Income = oldDefeatSave.Income.Select(source => source.Id.StartsWith("village:")
            ? source with { Amount = source.Amount + 1 } : source).ToArray();
        oldDefeatSave.ActiveSide = Side.Enemy;
        oldDefeatSave.Villages[1] = oldDefeatSave.Villages[1] with { Health = 1 };
        var oldDefeat = Restore(oldDefeatSave);
        Check(oldDefeat.GrossIncome(Side.Player) == rules.IncomePerMothership + 8,
            "legacy connected ports retain their stored fixed contributions before a hit");
        Check(oldDefeat.AttackVillage(Side.Enemy, 4, oldDefeat.Villages[1].Id).Success
            && oldDefeat.Villages[1].Health == 0 && oldDefeat.PortIncome(oldDefeat.Villages[1]) == 0
            && oldDefeat.PortIncome(oldDefeat.Villages[0]) == 1
            && oldDefeat.GrossIncome(Side.Player) == rules.IncomePerMothership + 4
            && oldDefeat.IncomeSources.All(source => source.Id != $"village:{oldDefeat.Villages[1].Id}"),
            "an actual legacy defeat removes ordinary and fixed port income without migrating survivors");
        Check(BattleState.LoadJson(oldDefeat.SaveJson()).SaveJson() == oldDefeat.SaveJson(),
            "a defeated legacy port resumes without restoring its removed income source");
        string legacyJson = legacy.SaveJson();
        var missing = JsonNode.Parse(legacyJson)!.AsObject();
        missing["Rules"]!["Ports"]!.AsObject().Remove("ConnectedCityIncome");
        Check(BattleState.LoadJson(missing.ToJsonString()).SaveJson() == legacyJson,
            "restoring a missing flag normalizes only its historical false default");
        Check(new BattleState(board, legacy.Rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 27)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(29, 2)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>()).Rules.Ports.Income == 1,
            "constructing from old rules never replaces them with current data");
        return checks;
    }
}
