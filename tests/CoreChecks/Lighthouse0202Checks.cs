using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Lighthouse0202Checks
{
    internal static int Run(BattleRules currentRules)
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("Lighthouse: " + name);
            checks++;
        }
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json")))!;
        document["startingCredits"] = 100;
        document["lighthousesEnabled"] = true;
        var rules = BattleRules.FromJson(document.ToJsonString());
        var towns = new[] { new GridPosition(8, 8), new GridPosition(25, 8), new GridPosition(34, 8) };
        BattleState Fixture() => new(new GameBoard(40, 20, p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water),
            rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(4, 10)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(38, 18)),
                (Side.Player, ShipClass.Garrison, new GridPosition(3, 10)),
                (Side.Enemy, ShipClass.Invader, new GridPosition(10, 10)) },
            Array.Empty<GridPosition>(), villageSpots: towns);
        var battle = Fixture();
        var mother = battle.Mothership(Side.Player)!;
        var sea = new GridPosition(5, 10);
        int money = battle.Credits(Side.Player);
        Check(!battle.BuildLighthouse(Side.Player, mother.Id, new(14, 10)).Success &&
            battle.Credits(Side.Player) == money && !mother.HasProduced, "a distant construction cannot spend currency or production");
        Check(battle.LighthouseBuildCells(mother.Id).Contains(sea) &&
            battle.LighthouseBlockReason(Side.Player, mother.Id) is null, "a level-one mother offers adjacent sea construction");
        var built = battle.BuildLighthouse(Side.Player, mother.Id, sea);
        var light = battle.Find(built.TargetId)!;
        Check(built.Success && light.Definition.Class == ShipClass.Lighthouse && light.IsStructure &&
            !light.IsArmed && !light.CountsTowardFleet && !light.CanEarnVeterancy,
            "the completed lighthouse is a noncombat building, not a fleet hull");
        Check(battle.Credits(Side.Player) == money - battle.LighthousePrice(Side.Player) &&
            mother.HasProduced && mother.MovementLocked && !battle.BuildLighthouse(Side.Player, mother.Id, new(4, 9)).Success,
            "purchase locks the mother's production and movement once");
        Check(light.Health == 10 && light.VisualRange == 4 && !light.CanMove && light.AttacksRemaining == 0 &&
            battle.PathTo(light.Id, new(6, 10)).Count == 0, "fixed four-tile lookout cannot move or shoot");
        Check(battle.Vision.IsVisible(Side.Player, new(9, 10)) && !battle.Vision.IsVisible(Side.Player, new(10, 10)),
            "lookout reveals its four-tile radius without exposing more distant targets");
        money = battle.Credits(Side.Player);
        Check(!light.HasRadar && battle.BuyRadar(Side.Player, light.Id).Success && light.RadarRange == 5 &&
            battle.Credits(Side.Player) == money - light.Definition.RadarPrice,
            "radar is bought separately and debited exactly once");
        Check(battle.Vision.IsRadarContact(Side.Player, new(10, 10)) && battle.FindObserved(Side.Player, 4) is null &&
            !battle.CanAttack(light.Id, 4), "its radar gives anonymous contacts while its own cannons remain unavailable");

        foreach (var town in battle.Villages)
        {
            town.Owner = Side.Player;
            town.Level = 3;
            town.Health = 15;
            town.HasPort = true;
        }
        var network = battle.TradeRoutes(Side.Player);
        Check(network.Routes.Count == 6 && network.Routes.Any(r => r[0] == sea || r[^1] == sea),
            "a lighthouse joins all three living owned ports as a fourth trade endpoint");
        Check(ReferenceEquals(network, battle.TradeRoutes(Side.Player)), "unchanged trade topology is retained");
        static (GridPosition, GridPosition) Edge(GridPosition a, GridPosition b) => a.Y < b.Y || a.Y == b.Y && a.X < b.X ? (a, b) : (b, a);
        var logical = network.Routes.SelectMany(r => r.Zip(r.Skip(1), Edge)).ToArray();
        var drawn = network.RenderRoutes.SelectMany(r => r.Zip(r.Skip(1), Edge)).ToArray();
        Check(logical.Length > logical.Distinct().Count(), "fixture contains genuinely shared logical route stretches");
        Check(drawn.Length == drawn.Distinct().Count() && drawn.ToHashSet().SetEquals(logical) &&
            drawn.ToHashSet().SetEquals(network.Edges.Select(e => Edge(e.From, e.To))),
            "every logical water edge is drawn exactly once, including overlaps and junctions");
        Check(network.RenderRoutes.All(r => r.Count >= 2), "drawn route chains never contain zero-length segments");
        var loaded = BattleState.LoadJson(battle.SaveJson());
        var resumed = loaded.Find(light.Id)!;
        Check(resumed.Definition.Class == ShipClass.Lighthouse && resumed.HasRadar && resumed.Health == light.Health &&
            resumed.IsStructure && loaded.TradeRoutes(Side.Player).Edges.SequenceEqual(network.Edges),
            "building health, radar, occupancy and rebuilt topology survive Continue");

        light.Health = 3;
        battle.Find(4)!.Position = new(6, 10);
        battle.Vision.Recompute(battle.Ships, battle.TurnSerial, battle.Villages);
        Check(battle.EndTurn(Side.Player).Success && battle.ActiveSide == Side.Enemy, "hostile turn starts normally with the new building");
        var shot = battle.Attack(Side.Enemy, 4, light.Id);
        Check(shot.Success && battle.Find(light.Id) is null && shot.Shots is { Count: 1 },
            "enemy destroys the lighthouse without receiving a counterattack");
        var after = battle.TradeRoutes(Side.Player);
        Check(!ReferenceEquals(after, network) && after.Routes.Count == 3 &&
            !after.Routes.Any(r => r[0] == sea || r[^1] == sea), "destruction removes the endpoint and invalidates its topology");

        battle = Fixture();
        var village = battle.Villages[0];
        village.Owner = Side.Player;
        village.Level = 2;
        village.Health = 10;
        Check(battle.LighthouseBlockReason(Side.Player, village.Id) is not null, "village lighthouse construction unlocks only at level three");
        village.Level = 3;
        village.Health = 15;
        var villageBuild = battle.BuildLighthouse(Side.Player, village.Id, battle.LighthouseBuildCells(village.Id).First());
        Check(villageBuild.Success && village.HasProduced && battle.Find(villageBuild.TargetId)!.IsExhausted &&
            battle.LighthouseBlockReason(Side.Player, village.Id) is not null,
            "town construction consumes its production and readies the building next turn");

        var oldDocument = document.DeepClone();
        oldDocument.AsObject().Remove("lighthousesEnabled");
        var oldShips = oldDocument["ships"]!.AsArray();
        foreach (var definition in oldShips.Where(s => s!["class"]!.GetValue<string>() == "Lighthouse").ToArray()) oldShips.Remove(definition);
        var oldRules = BattleRules.FromJson(oldDocument.ToJsonString());
        Check(!oldRules.LighthousesEnabled && oldRules.Get(ShipClass.Lighthouse) == BattleRules.DefaultLighthouse,
            "old rules without the optional class keep their construction policy and load a safe fallback");
        var oldBattle = new BattleState(battle.Board, oldRules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(4, 10)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(38, 18)) }, Array.Empty<GridPosition>(), villageSpots: towns);
        Check(oldBattle.LighthouseBlockReason(Side.Player, oldBattle.Mothership(Side.Player)!.Id) is not null &&
            BattleState.LoadJson(oldBattle.SaveJson()).Rules.LighthousesEnabled == false,
            "legacy construction remains closed after save/resume");
        return checks;
    }
}
