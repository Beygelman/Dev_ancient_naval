using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class V0209x2EconomyChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("v020.9x2 economy: " + message); checks++; }
        var json = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        json["ExperimentalEconomy"] = true;
        json["Infrastructure"]!["Enabled"] = false;
        json["Ports"]!["MaximumRouteLength"] = 6;
        rules = BattleRules.FromJson(json.ToJsonString());
        BattleState Restore(BattleSave save) => BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        BattleState Fixture(bool bypass = false, int level = 3, bool market = true)
        {
            var towns = new[] { new GridPosition(2, 4), new(9, 4) };
            var board = new GameBoard(16, 12, p => towns.Contains(p) ? TerrainType.Land
                : bypass || p.Y == 3 || p.Y >= 8 ? TerrainType.Water : TerrainType.Land, seed: 209202);
            var battle = new BattleState(board, rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(1, 9)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(14, 9)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(5, 3)) },
                Array.Empty<GridPosition>(), villageSpots: towns, piratesEnabled: false);
            var saved = battle.CaptureSnapshot();
            for (int i = 0; i < saved.Villages.Length; i++)
            {
                var town = saved.Villages[i];
                saved.Villages[i] = town with { Owner = i == 0 || market ? Side.Player : null,
                    Level = i == 0 ? level : 1, Health = i == 0 ? 15 : 5,
                    Port = i == 0, PortCell = i == 0 ? new GridPosition(2, 3) : null };
                if (i == 0 || market)
                    saved.Income = saved.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 999)).ToArray();
            }
            if (bypass)
            {
                saved.Ships = saved.Ships.Append(new SavedShip { Id = 100, Owner = Side.Player,
                    Kind = ShipClass.Lighthouse, Position = new GridPosition(6, 1), Health = 10, Level = 1 }).ToArray();
                saved.NextId = 101;
            }
            saved.Credits[0] = 200;
            return Restore(saved);
        }
        void Cycle(BattleState battle)
        {
            Check(battle.EndTurn(Side.Player).Success, "player turn ends");
            Check(battle.EndTurn(Side.Enemy).Success, "enemy turn ends");
        }

        foreach (int level in new[] { 3, 4, 5 })
        {
            var b = Fixture(level: level);
            var port = b.Villages[0];
            Check(b.VillageIncome(port) == 2 && b.VillageIncome(b.Villages[1]) == 2,
                "local income is independent of economic level");
            Check(!b.Villages[1].HasPort && b.PortIncome(port) == level - 1,
                "unported friendly market enables level-dependent trade");
            Check(b.GrossIncome(Side.Player) == rules.IncomePerMothership + 4 + level - 1,
                "actual income uses live formula rather than serialized stale source amount");
            Check(b.VillageBuildPrice(port.Id, ShipClass.Garrison) == b.BuildPrice(Side.Player, ShipClass.Garrison),
                "experimental port has no shipyard discount");
        }
        var marketOnly = Fixture().CaptureSnapshot();
        marketOnly.Villages[0] = marketOnly.Villages[0] with { Port = false, PortCell = null };
        var noPorts = Restore(marketOnly);
        Check(noPorts.PreviewPortBerth(noPorts.Villages[0].Id) == new GridPosition(3, 3),
            "first port chooses the shoreline toward an ordinary friendly market");
        Check(noPorts.TradeRoutes(Side.Player).IsEmpty && noPorts.PortIncome(noPorts.Villages[0]) == 0,
            "ordinary markets do not create a trade-movement network without any port");
        var relayCells = new[] { new GridPosition(2, 4), new(9, 4), new(15, 4) };
        var relayBoard = new GameBoard(24, 14, p => relayCells.Contains(p) ? TerrainType.Land : TerrainType.Water);
        var relayBattle = new BattleState(relayBoard, rules, new[] {
            (Side.Player, ShipClass.Mothership, new GridPosition(1, 11)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 11)) },
            Array.Empty<GridPosition>(), villageSpots: relayCells, piratesEnabled: false);
        var relaySave = relayBattle.CaptureSnapshot();
        for (int i = 0; i < 3; i++)
        {
            var town = relaySave.Villages[i];
            relaySave.Villages[i] = town with { Owner = Side.Player, Level = 3, Health = 15,
                Port = i != 1, PortCell = i == 0 ? new GridPosition(2, 3) : i == 2 ? new GridPosition(14, 3) : null };
            relaySave.Income = relaySave.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 3)).ToArray();
        }
        relayBattle = Restore(relaySave);
        Check(!relayBattle.TradeRoutes(Side.Player).AreConnected(new(2, 3), new(14, 3))
            && relayBattle.ConnectedPortCityCount(relayBattle.Villages[0]) == 1,
            "an ordinary receiver market cannot relay between two distant ports");
        relaySave.Villages[1] = relaySave.Villages[1] with { Port = true, PortCell = new GridPosition(8, 3) };
        relayBattle = Restore(relaySave);
        Check(relayBattle.TradeRoutes(Side.Player).AreConnected(new(2, 3), new(14, 3))
            && relayBattle.ConnectedPortCityCount(relayBattle.Villages[0]) == 2,
            "a real intermediate port can relay six-transition links");
        var isolated = Fixture(market: false);
        Check(isolated.PortIncome(isolated.Villages[0]) == 0
            && isolated.TradeStatusFor(Side.Player, isolated.Villages[0].Id) == PortTradeStatus.NoMarket,
            "a port has no profit without a friendly market");
        var blocked = Fixture();
        int portId = blocked.Villages[0].Id;
        int raiderId = blocked.Ships.Single(ship => ship.Owner == Side.Enemy && !ship.IsMothership).Id;
        var baseline = blocked.TradeRoutes(Side.Player);
        Cycle(blocked);
        Check(blocked.TradeRaidHoldEnds(Side.Enemy, raiderId) == 1 && blocked.PortIncome(blocked.Villages[0]) == 2,
            "first stationary end does not interrupt income");
        Cycle(blocked);
        Check(blocked.TradeRaidHoldEnds(Side.Enemy, raiderId) == 2
            && blocked.PortIncome(blocked.Villages[0]) == 0
            && blocked.TradeStatusFor(Side.Player, portId) == PortTradeStatus.Interrupted,
            "second stationary end closes a genuine choke and suppresses only trade");
        Check(blocked.GrossIncome(Side.Player) == rules.IncomePerMothership + 4,
            "local city income survives interception");
        Check(ReferenceEquals(baseline, blocked.TradeRoutes(Side.Player)),
            "public network geometry never exposes hidden raider mask");
        Check(blocked.TradeStatusFor(Side.Enemy, portId) == PortTradeStatus.NotOwned
            && blocked.TradeRaidHoldEnds(Side.Player, raiderId) == 0,
            "observer status does not expose foreign economic or raid state");
        string savedRaid = blocked.SaveJson();
        var loaded = BattleState.LoadJson(savedRaid);
        Check(loaded.TradeRaidHoldEnds(Side.Enemy, raiderId) == 2
            && loaded.PortIncome(loaded.Villages[0]) == 0 && loaded.SaveJson() == savedRaid,
            "Continue retains stationary history and operative interruption");
        Check(loaded.EndTurn(Side.Player).Success, "advance to raider movement");
        Check(loaded.Move(Side.Enemy, raiderId, new GridPosition(4, 3)).Success,
            "raider can leave its intercepting cell");
        Check(loaded.TradeRaidHoldEnds(Side.Enemy, raiderId) == 0 && loaded.PortIncome(loaded.Villages[0]) == 2,
            "movement immediately restores trade before the next receipt");
        Check(loaded.Move(Side.Enemy, raiderId, new GridPosition(5, 3)).Success
            && loaded.TradeRaidHoldEnds(Side.Enemy, raiderId) == 0,
            "moving away and returning never preserves an active interception");
        var withBypass = Fixture(bypass: true);
        Cycle(withBypass); Cycle(withBypass);
        Check(withBypass.PortIncome(withBypass.Villages[0]) == 2,
            "a real alternative via six-transition beacon links preserves income");
        var twoCutsSave = Fixture(bypass: true).CaptureSnapshot();
        twoCutsSave.Ships = twoCutsSave.Ships.Append(new SavedShip { Id = 101, Owner = Side.Enemy,
            Kind = ShipClass.Garrison, Position = new GridPosition(4, 1),
            Health = rules.Get(ShipClass.Garrison).MaxHealth, Level = 1 }).ToArray();
        twoCutsSave.NextId = 102;
        var twoCuts = Restore(twoCutsSave);
        var originalInk = twoCuts.TradeRoutes(Side.Player);
        Check(originalInk.Edges.Any(edge => edge.From == new GridPosition(4, 1) || edge.To == new GridPosition(4, 1))
            && !originalInk.Edges.Any(edge => edge.From == new GridPosition(5, 3) || edge.To == new GridPosition(5, 3)),
            "two-cut fixture places one raider on original ink and another exclusively on the real bypass");
        Cycle(twoCuts); Cycle(twoCuts);
        Check(twoCuts.PortIncome(twoCuts.Villages[0]) == 0
            && twoCuts.TradeStatusFor(Side.Player, twoCuts.Villages[0].Id) == PortTradeStatus.Interrupted,
            "simultaneous committed zones close both original route and off-ink bypass");
        var firstCutOnly = twoCuts.CaptureSnapshot();
        int bypassRaider = firstCutOnly.Ships.Single(ship => ship.Kind == ShipClass.Garrison
            && ship.Position == new GridPosition(5, 3)).Id;
        firstCutOnly.Ships = firstCutOnly.Ships.Where(ship => ship.Id != bypassRaider).ToArray();
        firstCutOnly.TradeRaids = firstCutOnly.TradeRaids.Where(raid => raid.Ship != bypassRaider).ToArray();
        var reopened = Restore(firstCutOnly);
        Check(reopened.PortIncome(reopened.Villages[0]) == 2,
            "removing the off-ink interceptor reopens a genuine bypass while the first cut stays held");
        var noRaider = blocked.CaptureSnapshot();
        noRaider.Ships = noRaider.Ships.Where(ship => ship.Id != raiderId).ToArray();
        noRaider.TradeRaids = Array.Empty<SavedTradeRaid>();
        Check(Restore(noRaider).PortIncome(blocked.Villages[0]) == 2,
            "destroyed raider cannot retain a blockade");
        var invalid = blocked.CaptureSnapshot();
        invalid.TradeRaids = new[] { new SavedTradeRaid(invalid.Ships.First(ship => ship.Kind == ShipClass.Mothership).Id,
            invalid.Ships.First(ship => ship.Kind == ShipClass.Mothership).Position, 2) };
        bool rejected = false;
        try { Restore(invalid); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "a saved mothership interception is rejected");
        Check(blocked.KnownTradeRaidTargets(Side.Player).Count == 0,
            "no invented enemy trade targets appear behind fog");
        return checks;
    }
}
