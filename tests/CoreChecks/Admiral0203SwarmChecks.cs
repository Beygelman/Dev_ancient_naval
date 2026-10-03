using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Admiral0203SwarmChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool value, string description)
        {
            if (!value) throw new Exception("v020.3 fleet operations: " + description);
            checks++;
        }
        var json = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        json["StartingCredits"] = 0;
        json["IncomePerMothership"] = 0;
        json["EncounterCurrencyReward"] = 0;
        foreach (var node in json["Ships"]!.AsArray())
        {
            var definition = node!.AsObject();
            var kind = (ShipClass)definition["Class"]!.GetValue<int>();
            definition["IncomePerTurn"] = 0;
            if (kind == ShipClass.Mothership)
            {
                definition["MaxHealth"] = 15;
                definition["Damage"] = 3;
                definition["AttackRange"] = 3;
                definition["VisualRange"] = 1;
            }
            if (kind == ShipClass.Kolonel)
            {
                definition["MaxHealth"] = 15;
                definition["Damage"] = 4;
                definition["AttackRange"] = 3;
                definition["VisualRange"] = 3;
                definition["Movement"] = 3;
            }
            if (kind == ShipClass.Garrison)
            {
                definition["MaxHealth"] = 5;
                definition["Damage"] = 3;
                definition["AttackRange"] = 2;
                definition["VisualRange"] = 2;
                definition["Movement"] = 5;
            }
        }
        var tactical = BattleRules.FromJson(json.ToJsonString());
        BattleState Create(params (Side Owner, ShipClass Class, GridPosition Position)[] deployment)
        {
            var battle = new BattleState(new GameBoard(40, 40, _ => TerrainType.Water), tactical,
                deployment, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            battle.SetDifficulty(AiDifficulty.Admiral);
            return battle;
        }

        var swarm = Create((Side.Player, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Mothership, new(12, 10)),
            (Side.Player, ShipClass.Garrison, new(10, 9)),
            (Side.Player, ShipClass.Garrison, new(10, 10)),
            (Side.Player, ShipClass.Garrison, new(10, 11)),
            (Side.Player, ShipClass.Garrison, new(11, 8)),
            (Side.Player, ShipClass.Garrison, new(9, 10)));
        int attacks = 0;
        for (int order = 0; order < 16 && swarm.Find(2) is not null; order++)
        {
            var command = SimpleOpponent.Step(swarm);
            Check(command.Success, "swarm order is legal");
            Check(command.Kind != CommandKind.EndTurn, "combined lethal volley completes before ending the turn");
            if (command.Kind == CommandKind.Attack && command.TargetId == 2) attacks++;
        }
        Check(swarm.Find(2) is null && attacks == 5,
            "five small hulls concentrate their shots into a same-turn flagship kill");
        Check(swarm.OwnShips(Side.Player).Count(s => s.Definition.Class == ShipClass.Garrison) == 5,
            "swarm forecast preserves all escorts through scaled counterfire");

        var priority = Create((Side.Player, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Mothership, new(12, 10)),
            (Side.Player, ShipClass.Garrison, new(10, 10)),
            (Side.Player, ShipClass.Garrison, new(10, 9)),
            (Side.Enemy, ShipClass.Fishing, new(10, 11)));
        priority.Find(2)!.Health = 6;
        priority.Find(5)!.Health = 1;
        var decisive = SimpleOpponent.Step(priority);
        Check(decisive.Kind == CommandKind.Attack && decisive.TargetId == 2,
            "a decisive flagship objective outranks a cheaper one-shot fishing target");

        var assembly = Create((Side.Player, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Mothership, new(10, 10)),
            (Side.Player, ShipClass.Kolonel, new(7, 10)),
            (Side.Player, ShipClass.Kolonel, new(6, 12)));
        assembly.Find(3)!.AttacksUsed = 2;
        var gather = SimpleOpponent.Step(assembly);
        Check(gather.Success && gather.Kind == CommandKind.Move && gather.ActorId == 4,
            "a spent ready gun and an arriving gun assemble instead of taking a nondecisive solo broadside");
        Check(assembly.WeaponCovers(assembly.Find(4)!, assembly.Find(2)!.Position),
            "the arriving hull is prepared to contribute on the fresh turn");
        Check(assembly.EndTurn(Side.Player).Success && assembly.EndTurn(Side.Enemy).Success,
            "assembly fixture advances to the next personal turn");
        for (int order = 0; order < 4 && assembly.Find(2) is not null; order++)
            Check(SimpleOpponent.Step(assembly).Success, "assembled volley order is legal");
        Check(assembly.Find(2) is null, "assembled guns produce the forecast next-turn flagship kill");

        var observed = Create((Side.Player, ShipClass.Mothership, new(12, 10)),
            (Side.Enemy, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Garrison, new(10, 10)));
        Check(observed.EndTurn(Side.Player).Success, "start the observer nation's turn");
        Check(observed.KnownFlagships(Side.Enemy).Any(s => s.Owner == Side.Player && s.Position == new GridPosition(12, 10)),
            "an optical encounter records the flagship position");
        var snapshot = observed.CaptureSnapshot();
        snapshot.Ships.Single(s => s.Owner == Side.Player).Position = new(30, 30);
        snapshot.Ships.Single(s => s.Id == 3).Position = new(2, 15);
        var hunt = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        Check(!hunt.Vision.IsVisible(Side.Enemy, new(30, 30)), "the real moved flagship is outside the observer's sight");
        Check(hunt.KnownFlagships(Side.Enemy).Any(s => s.Position == new GridPosition(12, 10)),
            "a hidden move retains the saved sighting without tracking its actual destination");
        var resumed = BattleState.LoadJson(hunt.SaveJson());
        double before = hunt.Board.Distance(hunt.Find(3)!.Position, new(12, 10));
        var pursuit = SimpleOpponent.Step(hunt);
        Check(pursuit.Success && pursuit.Kind == CommandKind.Move && pursuit.ActorId == 3
            && hunt.Board.Distance(hunt.Find(3)!.Position, new(12, 10)) < before,
            "the scout crosses the map toward the last observed position");
        Check(Signature(SimpleOpponent.Step(resumed)) == Signature(pursuit),
            "Continue preserves strategic pursuit decisions");
        snapshot.Ships.Single(s => s.Owner == Side.Player).Position = new(32, 2);
        snapshot.Ships.Single(s => s.Owner == Side.Player).Health = 1;
        var elsewhere = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        Check(Signature(SimpleOpponent.Step(elsewhere)) == Signature(pursuit),
            "unseen physical location and health cannot redirect a saved pursuit");
        var emptied = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        var returnRoute = emptied.RouteToward(3, new(12, 10), 1);
        var inspect = emptied.AffordableDestination(3, returnRoute);
        Check(emptied.Move(Side.Enemy, 3, inspect).Success, "a saved sighting can be inspected through a legal route");
        // Several scout orders may be required across the map; no synthetic
        // optically visible flag is introduced just to invalidate the memory.
        for (int turn = 0; turn < 5 && emptied.KnownFlagships(Side.Enemy).Count > 0; turn++)
        {
            Check(emptied.EndTurn(Side.Enemy).Success && emptied.EndTurn(Side.Player).Success,
                "inspection advances personal turns legally");
            var path = emptied.RouteToward(3, new(12, 10), 1);
            var destination = emptied.AffordableDestination(3, path);
            if (destination != emptied.Find(3)!.Position)
                Check(emptied.Move(Side.Enemy, 3, destination).Success, "inspection advances toward the recorded tile");
        }
        Check(emptied.KnownFlagships(Side.Enemy).Count == 0,
            "seeing the old tile empty clears the remembered objective without tracking the hidden new position");
        void RejectMemory(Action<JsonObject> mutate, string description)
        {
            var broken = JsonNode.Parse(resumed.SaveJson())!.AsObject();
            mutate(broken);
            bool rejected = false;
            try { BattleState.LoadJson(broken.ToJsonString()); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, description);
        }
        RejectMemory(save => save["FlagshipSightings"] = null, "null memory collection is rejected");
        RejectMemory(save => save["FlagshipSightings"]!.AsArray().Add(save["FlagshipSightings"]![0]!.DeepClone()),
            "duplicate observer/owner memory is rejected");
        RejectMemory(save => save["FlagshipSightings"]![0]!["Turn"] = int.MaxValue,
            "future-dated memory is rejected");
        RejectMemory(save => save["FlagshipSightings"]![0]!["Position"] = JsonSerializer.SerializeToNode(new GridPosition(39, 39)),
            "a fabricated unobserved objective is rejected");

        var unexplored = Create((Side.Player, ShipClass.Mothership, new(35, 35)),
            (Side.Enemy, ShipClass.Mothership, new(1, 1)),
            (Side.Enemy, ShipClass.Garrison, new(3, 3)),
            (Side.Enemy, ShipClass.Garrison, new(5, 4)));
        Check(unexplored.EndTurn(Side.Player).Success, "start a nation with no flagship encounter");
        Check(unexplored.KnownFlagships(Side.Enemy).Count == 0, "an unseen nation has no manufactured sighting");
        var fogSave = unexplored.CaptureSnapshot();
        int exploredBefore = unexplored.Vision.ExploredCount(Side.Enemy);
        var explore = SimpleOpponent.Step(unexplored);
        Check(explore.Success && explore.Kind == CommandKind.Move
            && unexplored.Vision.ExploredCount(Side.Enemy) > exploredBefore,
            "scouts reveal a real frontier while searching for the unseen flagship");
        fogSave.Ships.Single(s => s.Owner == Side.Player).Position = new(35, 1);
        fogSave.Ships.Single(s => s.Owner == Side.Player).Health = 1;
        var hidden = BattleState.LoadJson(BattleState.SerializeSnapshot(fogSave));
        hidden.SetGodEye(true);
        Check(Signature(SimpleOpponent.Step(hidden)) == Signature(explore),
            "hidden mother relocation and the human God's eye do not change enemy scouting");
        return checks;
    }

    private static string Signature(CommandResult command) =>
        $"{command.Kind}/{command.ActorId}/{command.TargetId}/" +
        string.Join(';', command.Path?.Select(p => $"{p.X},{p.Y}") ?? Array.Empty<string>());
}
