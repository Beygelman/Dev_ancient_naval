using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0205Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception("v020.5 rules: " + message);
            checks++;
        }
        void Reject(Action action, string message)
        {
            bool rejected = false;
            try { action(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, message);
        }
        BattleState Fixture(bool town = false) => new(new GameBoard(30, 30,
            p => town && p == new GridPosition(15, 15) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(27, 27)),
                (Side.Player, ShipClass.Fishing, new GridPosition(3, 3)) },
            new[] { new GridPosition(3, 4) }, villageSpots: town ? new[] { new GridPosition(15, 15) } : Array.Empty<GridPosition>());
        BattleState Rich(BattleState source, int level = 2)
        {
            var saved = source.CaptureSnapshot();
            saved.Credits[(int)Side.Player] = 100;
            saved.Ships.Single(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership).Level = level;
            return BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        }
        Check(rules.RepairAmount == 3 && rules.AutoRepairAmount == 4 && rules.VillageAutoRepairAmount == 4,
            "active repair is three while personal-turn passive healing remains four");
        Check(rules.VillageLevelPrices.SequenceEqual(new[] { 5, 7, 10, 15 }), "new town prices");
        Check(rules.Get(ShipClass.Fishing).Name == "Support Brig" && rules.Get(ShipClass.Fishing).IncomePerTurn == 0
            && rules.Get(ShipClass.FishingDock).IncomePerTurn == 2, "support hull does not pay income; docks pay two");
        var build = Rich(Fixture());
        int support = build.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        Check(build.GrossIncome(Side.Player) == rules.IncomePerMothership, "no hidden support income source");
        Check(build.CollectionCells(support).Contains(new(3, 4)), "support hull retains resource collection");
        Check(build.BuildBlockReason(Side.Player, support, ShipClass.CannonTower) is null
            && build.BuildBlockReason(Side.Player, support, ShipClass.Lighthouse) is null,
            "support hull can construct both structure classes");
        Check(build.BuildBlockReason(Side.Player, support, ShipClass.Garrison) is not null,
            "support hull cannot manufacture combat vessels");
        int money = build.Credits(Side.Player);
        var tower = build.Build(Side.Player, support, ShipClass.CannonTower, new(4, 3));
        Check(tower.Success && build.At(new(4, 3))?.Definition.Class == ShipClass.CannonTower
            && build.Credits(Side.Player) == money - rules.Get(ShipClass.CannonTower).Price,
            "tower construction has one charge and creates the requested unit");
        Check(!build.Build(Side.Player, support, ShipClass.Lighthouse, new(3, 2)).Success,
            "support construction consumes its one production action");
        build = BattleState.LoadJson(build.SaveJson());
        Check(build.Find(support)!.Definition.Class == ShipClass.Fishing && build.Find(support)!.HasProduced,
            "legacy hull identity and spent construction survive save/resume");
        var dockBuild = Rich(Fixture());
        support = dockBuild.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        var dockSave = dockBuild.CaptureSnapshot();
        dockSave.Fish = Array.Empty<GridPosition>();
        dockSave.Shoals = new[] { new GridPosition(3, 4) };
        dockBuild = BattleState.LoadJson(BattleState.SerializeSnapshot(dockSave));
        int resources = dockBuild.Mothership(Side.Player)!.Resources;
        money = dockBuild.Credits(Side.Player);
        var dock = dockBuild.BuildDock(Side.Player, support, new(3, 4));
        Check(dock.Success && dockBuild.At(new(3, 4))?.Definition.Class == ShipClass.FishingDock
            && dockBuild.Mothership(Side.Player)!.Resources == resources + rules.DockResourceReward
            && dockBuild.Credits(Side.Player) == money - rules.Get(ShipClass.FishingDock).Price
            && dockBuild.GrossIncome(Side.Player) == rules.IncomePerMothership + 2,
            "support dock pays once, grants two resources and contributes two income");
        var beaconBuild = Rich(Fixture());
        support = beaconBuild.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        Check(beaconBuild.Build(Side.Player, support, ShipClass.Lighthouse, new(4, 3)).Success
            && beaconBuild.At(new(4, 3))?.Definition.Class == ShipClass.Lighthouse, "support beacon is a real legal construction");
        var starting = Rich(Fixture(), 1);
        support = starting.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        Check(starting.BuildBlockReason(Side.Player, support, ShipClass.CannonTower) is not null,
            "support tower construction follows the flagship level-two unlock");
        var locked = Rich(Fixture());
        support = locked.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        locked.Find(support)!.Health = 1;
        var repair = locked.Repair(Side.Player, support);
        Check(repair.Success && repair.Amount == 3 && locked.Find(support)!.Health == 4, "repair heals exactly three");
        Check(!locked.Move(Side.Player, support, new(3, 2)).Success
            && !locked.Build(Side.Player, support, ShipClass.CannonTower, new(4, 3)).Success
            && !locked.Collect(Side.Player, support, new(3, 4)).Success, "repair blocks travel, structures and resource collection");
        locked = BattleState.LoadJson(locked.SaveJson());
        Check(locked.Find(support)!.HasRepaired && !locked.Build(Side.Player, support, ShipClass.Lighthouse, new(3, 2)).Success,
            "repair lock survives Continue");
        var passive = Rich(Fixture(true));
        passive.Mothership(Side.Player)!.Health = 10;
        var village = passive.Villages.Single();
        village.Owner = Side.Player; village.Level = 3; village.Health = 10;
        Check(passive.EndTurn(Side.Player).Success && passive.Mothership(Side.Player)!.Health == 14
            && village.Health == 14, "unused ships and towns retain four-point passive heal");
        var town = Rich(Fixture(true));
        town.Villages.Single().Owner = Side.Player;
        for (int level = 1; level < 5; level++)
        {
            var target = town.Villages.Single();
            int price = new[] { 5, 7, 10, 15 }[level - 1];
            money = town.Credits(Side.Player);
            Check(town.VillageUpgradePrice(Side.Player, target.Id) == price
                && town.UpgradeVillage(Side.Player, target.Id).Success
                && town.Credits(Side.Player) == money - price, "each paid town level uses its new configured price");
            Check(town.EndTurn(Side.Player).Success && town.EndTurn(Side.Enemy).Success, "advance next construction turn");
        }
        var water = new GameBoard(12, 5, _ => TerrainType.Water);
        var six = TradeNetwork.Create(water, new[] { new GridPosition(1, 2), new GridPosition(7, 2) }, new HashSet<GridPosition>(), 6);
        var seven = TradeNetwork.Create(water, new[] { new GridPosition(1, 2), new GridPosition(8, 2) }, new HashSet<GridPosition>(), 6);
        Check(six.Routes.Single().Count == 7 && seven.IsEmpty, "six transitions accepted, seven rejected");
        var barrier = new GameBoard(12, 5, p => p.X == 4 && p.Y < 4 ? TerrainType.Land : TerrainType.Water);
        Check(TradeNetwork.Create(barrier, new[] { new GridPosition(1, 1), new GridPosition(6, 1) }, new HashSet<GridPosition>(), 6).IsEmpty,
            "nearby ports with a long coastal detour are not linked");
        Check(!TradeNetwork.Create(water, new[] { new GridPosition(1, 2), new GridPosition(8, 2) }, new HashSet<GridPosition>()).IsEmpty,
            "missing old maximum retains unlimited routes");
        var old = JsonNode.Parse(Rich(Fixture()).SaveJson())!.AsObject();
        old["Rules"]!.AsObject().Remove("FishingCannonTowers");
        old["Rules"]!["Ports"]!.AsObject().Remove("MaximumRouteLength");
        old["Rules"]!.AsObject().Remove("VillageAutoRepairAmount");
        old["Board"]!.AsObject().Remove("MapSize");
        var legacy = BattleState.LoadJson(old.ToJsonString());
        support = legacy.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Fishing).Id;
        Check(legacy.Board.MapSize is null && legacy.Rules.Ports.MaximumRouteLength == 0
            && legacy.Rules.VillageAutoRepairAmount is null
            && legacy.BuildBlockReason(Side.Player, support, ShipClass.CannonTower) is not null,
            "omitted optional fields restore original rules and construction limits");
        int lastArea = 0;
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var board = ArchipelagoGenerator.Create(205, 1, WorldKind.Oceans, size);
            var rivals = ArchipelagoGenerator.Create(205, 4, WorldKind.Oceans, size);
            Check(board.Tiles.Count > lastArea && board.Mesh!.SideTileCounts.SequenceEqual(rivals.Mesh!.SideTileCounts),
                "four independent sizes increase area without opponent-driven resize");
            lastArea = board.Tiles.Count;
            var battle = new BattleState(board, rules, new[] {
                (Side.Player, ShipClass.Mothership, board.FleetAnchor(0, 2)),
                (Side.Enemy, ShipClass.Mothership, board.FleetAnchor(1, 2)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            var restored = BattleState.LoadJson(battle.SaveJson());
            Check(restored.Board.MapSize == size && restored.Board.Tiles.Count == board.Tiles.Count
                && restored.Board.Kind == WorldKind.Oceans, "size metadata and exact geometry survive save/resume");
        }
        foreach (int seed in new[] { 205, 1709, 8071 })
        foreach (var kind in Enum.GetValues<WorldKind>())
        {
            Console.WriteLine($"SIZED_WORLD seed={seed} size=Lake rivals=4 kind={kind}");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var board = ArchipelagoGenerator.Create(seed, 4, kind, MapSize.Lake);
            Console.WriteLine($"SIZED_WORLD generation_ms={clock.Elapsed.TotalMilliseconds:0.0}");
            Check(board.Kind == kind && board.MapSize == MapSize.Lake
                && Enumerable.Range(0, 5).All(i => board.GetTile(board.FleetAnchor(i, 5)).Terrain != TerrainType.Land),
                "small maps preserve legal five-fleet anchors for every world policy");
            var match = SkirmishSetup.Create(board, rules, 4);
            Check(BattleState.PlayableSides.Take(5).All(side => match.OwnShips(side).Count() == 3)
                && match.Villages.Count == 15, "all small-world layouts support complete starting fleets and settlement allocation");
            Check(match.Villages.SelectMany(a => match.Villages.Where(b => a.Id != b.Id)
                .Select(b => board.Distance(a.Position, b.Position))).All(distance => distance > 3),
                "fair-map retry preserves every pair's town spacing instead of weakening the rule");
            var repeated = ArchipelagoGenerator.Create(seed, 4, kind, MapSize.Lake);
            Check(repeated.Tiles.SequenceEqual(board.Tiles)
                && repeated.Mesh!.Vertices.SequenceEqual(board.Mesh!.Vertices), "retried landscapes remain seeded and deterministic");
        }
        var original = ArchipelagoGenerator.Create(205, 3);
        var explicitLegacy = ArchipelagoGenerator.Create(205, 3, WorldKind.Oceans, null);
        Check(original.Tiles.SequenceEqual(explicitLegacy.Tiles)
            && original.Mesh!.Vertices.SequenceEqual(explicitLegacy.Mesh!.Vertices), "null map size preserves exact old seeded generation");
        Reject(() => ArchipelagoGenerator.Create(205, 1, WorldKind.Oceans, (MapSize)42), "invalid map size rejected at generator boundary");
        old["Board"]!["MapSize"] = 42;
        Reject(() => BattleState.LoadJson(old.ToJsonString()), "invalid saved map size rejected before geometry restore");
        var badRules = JsonNode.Parse(Rich(Fixture()).SaveJson())!["Rules"]!.AsObject();
        badRules["Ports"]!["MaximumRouteLength"] = -1;
        Reject(() => BattleRules.FromJson(badRules.ToJsonString()), "negative route limit rejected");
        Console.WriteLine($"v020.5 rules: {checks} assertions passed");
        return checks;
    }
}
