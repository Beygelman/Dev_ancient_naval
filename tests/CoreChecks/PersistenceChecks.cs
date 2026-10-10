using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class PersistenceChecks
{
    public static int Run(BattleRules rules, string? legacyFixtureDirectory = null)
    {
        int checks = AotSerializationChecks.Run(rules);
        void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("Persistence: " + name);
            checks++;
        }

        var battle = new BattleState(new GameBoard(12, 12, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(9, 9)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        string saved = battle.SaveJson();
        Check(BattleState.LoadJson(saved).SaveJson() == saved, "Exact v1 round trip");
        void Reject(Action<JsonNode> mutate, string name, string? source = null)
        {
            var node = JsonNode.Parse(source ?? saved)!;
            mutate(node);
            bool rejected = false;
            try
            {
                BattleState.LoadJson(node.ToJsonString());
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, name);
        }

        Reject(n => n["Board"] = null, "Null board rejected without indexing");
        Reject(n => n["Rules"] = null, "Null rules rejected");
        Reject(n => n["Ships"] = null, "Null fleet rejected");
        Reject(n => n["Ships"]![0] = null, "Null ship rejected");
        Reject(n => n["Ships"]![1]!["Id"] = n["Ships"]![0]!["Id"]!.GetValue<int>(), "Duplicate identity rejected");
        Reject(n => n["Ships"]![0]!["BombCooldown"] = rules.Balloon.CooldownTurns + 1, "Invalid cooldown rejected");
        Reject(n => n["Vision"]![0]!["Explored"] = null, "Null discovery collection rejected");
        Reject(n => n["Vision"] = new JsonArray(), "Missing faction discovery rejected");
        Reject(n => n["Vision"]![1]!["Side"] = n["Vision"]![0]!["Side"]!.GetValue<string>(), "Repeated vision side rejected");
        Reject(n => n["PirateHomes"] = JsonNode.Parse("[{\"Ship\":999,\"Position\":{\"X\":2,\"Y\":2}}]"), "Dangling patrol reference rejected");
        Reject(n => n["CaptureWaits"] = JsonNode.Parse("[{\"Target\":999,\"Ship\":1,\"Side\":\"Player\",\"Position\":{\"X\":2,\"Y\":2},\"Since\":0}]"), "Dangling capture target rejected");
        var organic = SkirmishSetup.Create(ArchipelagoGenerator.Create(20), rules).SaveJson();
        Reject(n => n["Board"]!["Mesh"]!["Faces"]![0]!["Vertices"]![0] = int.MaxValue, "Missing mesh vertex rejected before geometry lookup", organic);
        Reject(n => n["Board"]!["Mesh"]!["Faces"]![0]!["Vertices"] = null, "Null mesh polygon rejected", organic);
        Check(BattleState.LoadJson(organic).SaveJson() == organic, "Organic mesh retains exact geometry and discovery");
        var captured = battle.CaptureSnapshot();
        string frozen = BattleState.SerializeSnapshot(captured);
        var background = Task.Run(() =>
        {
            for (int i = 0; i < 32; i++)
                if (BattleState.SerializeSnapshot(captured) != frozen)
                    throw new Exception("Background serialization observed live battle mutations.");
        });
        battle.SetPlayerColor(FleetColor.Green);
        battle.EndTurn(Side.Player);
        battle.EndTurn(Side.Enemy);
        battle.Find(1)!.Health--;
        battle.Move(Side.Player, 1, new(3, 2));
        background.GetAwaiter().GetResult();
        Check(BattleState.SerializeSnapshot(captured) == frozen && battle.SaveJson() != frozen, "Captured save remains stable while color, currency, health, movement and fog change");
        int liveCredits = battle.Credits(Side.Player);
        captured.Credits[0] = 0;
        captured.Ships[0].Position = new(0, 0);
        Check(battle.Credits(Side.Player) == liveCredits && battle.Find(1)!.Position != new GridPosition(0, 0), "Saved currency and ship arrays are detached from live state");
        var organicBattle = BattleState.LoadJson(organic);
        var geometryCopy = organicBattle.CaptureSnapshot().Board.Mesh!;
        var face = geometryCopy.Faces[0];
        int liveVertex = organicBattle.Board.Mesh!.Faces[face.Cell][0];
        face.Vertices[0] = int.MaxValue;
        Check(organicBattle.Board.Mesh.Faces[face.Cell][0] == liveVertex, "Snapshot mesh face arrays do not alias live topology");
        if (legacyFixtureDirectory is not null)
        {
            var files = Directory.GetFiles(legacyFixtureDirectory, "baseline-menu-*.json");
            Check(files.Length >= 2, "Pre-refactor v1 fixtures available");
            foreach (string file in files)
            {
                var session = JsonNode.Parse(File.ReadAllText(file))!;
                var restored = BattleState.LoadJson(session["Battle"]!.ToJsonString());
                Check(restored.Board.Mesh is not null, "Released v1 organic map loads: " + Path.GetFileName(file));
                Check(BattleState.LoadJson(restored.SaveJson()).SaveJson() == restored.SaveJson(), "Released v1 resave is stable: " + Path.GetFileName(file));
            }
        }

        return checks;
    }
}
