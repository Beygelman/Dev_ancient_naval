using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0208bChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception("0208b: " + name); checks++; }
        Check(rules.Get(ShipClass.Togus).Movement == 3 && rules.PirateCautiousRounds == 4,
            "new voyages use requested Granado speed and restrained opening patrols");
        BattleState Fixture(params (Side Owner, ShipClass Class, GridPosition Position)[] extra) =>
            new(new GameBoard(30, 30, _ => TerrainType.Water), rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(27, 27)) }.Concat(extra),
                Array.Empty<GridPosition>(), difficulty: AiDifficulty.Admiral);

        var pirate = Fixture((Side.Pirates, ShipClass.PirateSchooner, new(8, 8)),
            (Side.Player, ShipClass.Fishing, new(9, 8)));
        Check(pirate.EndTurn(Side.Player).Success && pirate.EndTurn(Side.Enemy).Success,
            "fixture reaches a pirate turn");
        var snapshot = pirate.CaptureSnapshot();
        var cautious = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        var opening = cautious.PirateStep();
        Check(opening.Success && opening.Kind != CommandKind.Attack,
            "opening pirate does not attack an unarmed passing support hull");
        if (opening.Kind == CommandKind.Move)
            Check(cautious.Board.InRadius(new(8, 8), cautious.Find(3)!.Position, 2),
                "opening patrol remains near its saved home");
        var resumed = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        var repeated = resumed.PirateStep();
        Check(Signature(opening) == Signature(repeated) && cautious.SaveJson() == resumed.SaveJson(),
            "opening patrol decisions and RNG survive Continue deterministically");
        var legacyNode = JsonNode.Parse(BattleState.SerializeSnapshot(snapshot))!.AsObject();
        var oldRules = legacyNode["Rules"]!.AsObject();
        oldRules.Remove("PirateCautiousRounds");
        foreach (var ship in oldRules["Ships"]!.AsArray())
            if (ship!["Class"]!.GetValue<string>() == "Togus") ship["Movement"] = 4;
        var historical = BattleState.LoadJson(legacyNode.ToJsonString());
        Check(historical.Rules.PirateCautiousRounds == 0 && historical.Rules.Get(ShipClass.Togus).Movement == 4,
            "absent opening policy retains historical aggression and saved Granado catalog");
        var oldOrder = historical.PirateStep();
        Check(oldOrder.Success && oldOrder.Kind == CommandKind.Attack,
            "historical pirate still attacks the passing support hull");
        snapshot.Round = 5;
        var grown = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        Check(grown.PirateStep().Kind == CommandKind.Attack,
            "pirates regain their established offensive policy after the opening");
        var defense = Fixture((Side.Pirates, ShipClass.PirateSchooner, new(8, 8)),
            (Side.Player, ShipClass.Garrison, new(9, 8)));
        defense.EndTurn(Side.Player); defense.EndTurn(Side.Enemy);
        Check(defense.PirateStep() is { Success: true, Kind: CommandKind.Attack },
            "opening pirates retain self-defense against an armed intruder beside their home");
        var invalid = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        invalid["PirateCautiousRounds"] = -1;
        bool rejected = false;
        try { BattleRules.FromJson(invalid.ToJsonString()); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "invalid serialized opening duration is rejected");

        var towns = new[] { new GridPosition(6, 6), new GridPosition(10, 6) };
        BattleState Towns(int level, int credits)
        {
            var state = new BattleState(new GameBoard(30, 30,
                p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water), rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 6)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(27, 27)),
                    (Side.Player, ShipClass.Garrison, new GridPosition(5, 5)),
                    (Side.Player, ShipClass.Garrison, new GridPosition(7, 5)) },
                Array.Empty<GridPosition>(), villageSpots: towns, difficulty: AiDifficulty.Admiral);
            var save = state.CaptureSnapshot();
            for (int index = 0; index < save.Villages.Length; index++)
                save.Villages[index] = save.Villages[index] with { Owner = Side.Player, Level = level, Health = level * 5 };
            save.Credits[0] = credits;
            return BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        }
        var cities = Towns(3, 30);
        Check(SimpleOpponent.Step(cities) is { Success: true, Kind: CommandKind.Port },
            "Admiral invests in a port before another discretionary hull");
        Check(SimpleOpponent.Step(cities) is { Success: true, Kind: CommandKind.Port }
            && cities.Villages.All(v => cities.PortIncome(v) == 1),
            "paired city ports create real connected income");
        var savings = Towns(2, 6);
        var saveOrder = SimpleOpponent.Step(savings);
        Check(saveOrder.Success && saveOrder.Kind != CommandKind.Build && savings.Credits(Side.Player) >= 2,
            "spare escorts do not consume the town-development budget on endless cheap hulls");

        var fort = Fixture((Side.Player, ShipClass.Fishing, new(10, 10)),
            (Side.Enemy, ShipClass.Garrison, new(12, 10)),
            (Side.Player, ShipClass.Garrison, new(3, 4)),
            (Side.Player, ShipClass.Garrison, new(4, 3)));
        var fortSave = fort.CaptureSnapshot();
        fortSave.Ships.Single(s => s.Id == 1).Level = 2;
        fortSave.Shoals = Array.Empty<GridPosition>();
        fortSave.Fish = Array.Empty<GridPosition>();
        fortSave.Credits[0] = 40;
        fort = BattleState.LoadJson(BattleState.SerializeSnapshot(fortSave));
        var fortOrder = SimpleOpponent.Step(fort);
        Check(fortOrder.Success && fortOrder.Kind == CommandKind.Build
            && fort.Find(fortOrder.TargetId)!.Definition.Class == ShipClass.CannonTower,
            "support builder creates a legal defensive battery against observed pressure: " + fortOrder.Message);

        var relayTowns = new[] { new GridPosition(4, 14), new GridPosition(22, 14) };
        var relayState = new BattleState(new GameBoard(32, 32,
            p => relayTowns.Contains(p) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 27)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(29, 2)),
                (Side.Player, ShipClass.Fishing, new GridPosition(10, 15)),
                (Side.Player, ShipClass.Garrison, new GridPosition(5, 20)),
                (Side.Player, ShipClass.Garrison, new GridPosition(6, 20)) },
            Array.Empty<GridPosition>(), villageSpots: relayTowns, difficulty: AiDifficulty.Admiral);
        var relaySave = relayState.CaptureSnapshot();
        for (int index = 0; index < relaySave.Villages.Length; index++)
            relaySave.Villages[index] = relaySave.Villages[index] with { Owner = Side.Player, Level = 3,
                Health = 15, Port = true, PortCell = new GridPosition(relayTowns[index].X, 15) };
        relaySave.Credits[0] = 100;
        relaySave.Fish = Array.Empty<GridPosition>(); relaySave.Shoals = Array.Empty<GridPosition>();
        relayState = BattleState.LoadJson(BattleState.SerializeSnapshot(relaySave));
        var relayOrder = SimpleOpponent.Step(relayState);
        Check(relayOrder.Success && relayOrder.Kind == CommandKind.Build
            && relayState.Find(relayOrder.TargetId)!.Definition.Class == ShipClass.Lighthouse,
            "Admiral extends a known port relay chain even when more than one beacon is required: " + relayOrder.Message);
        Check(relayState.Villages.All(v => relayState.PortIncome(v) == 0),
            "an incomplete relay is an investment, not invented city income");

        // Nearby already-connected ports must not consume all three bounded
        // relay-search slots while a third distant city waits for a builder.
        var threeTowns = new[] { new GridPosition(4, 14), new GridPosition(8, 14), new GridPosition(24, 14) };
        var thirdCity = new BattleState(new GameBoard(32, 32,
            p => threeTowns.Contains(p) ? TerrainType.Land : TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 25)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(29, 29)),
                (Side.Player, ShipClass.Fishing, new GridPosition(6, 18)),
                (Side.Player, ShipClass.Garrison, new GridPosition(2, 26)),
                (Side.Player, ShipClass.Garrison, new GridPosition(3, 26)),
                (Side.Player, ShipClass.Lighthouse, new GridPosition(6, 15)) },
            Array.Empty<GridPosition>(), villageSpots: threeTowns, difficulty: AiDifficulty.Admiral);
        var thirdSave = thirdCity.CaptureSnapshot();
        for (int index = 0; index < thirdSave.Villages.Length; index++)
            thirdSave.Villages[index] = thirdSave.Villages[index] with { Owner = Side.Player, Level = 3,
                Health = 15, Port = true, PortCell = new GridPosition(threeTowns[index].X, 15), Produced = true };
        var restingMother = thirdSave.Ships.Single(s => s.Id == 1);
        restingMother.HasProduced = true; restingMother.MovementLocked = true; restingMother.HasRadar = true;
        thirdSave.Credits[0] = 6;
        thirdSave.Fish = Array.Empty<GridPosition>(); thirdSave.Shoals = Array.Empty<GridPosition>();
        thirdCity = BattleState.LoadJson(BattleState.SerializeSnapshot(thirdSave));
        foreach (var tile in thirdCity.Board.Tiles) thirdCity.Vision.RevealCombat(Side.Player, tile.Position);
        thirdCity.Vision.Recompute(thirdCity.Ships, thirdCity.TurnSerial, thirdCity.Villages);
        Check(thirdCity.PortIncome(thirdCity.Villages[0]) == 1
            && thirdCity.PortIncome(thirdCity.Villages[2]) == 0,
            "three-city relay fixture has a connected pair and one isolated port");
        var approach = SimpleOpponent.Step(thirdCity);
        Check(approach.Success && approach.Kind == CommandKind.Move && approach.ActorId == 3
            && thirdCity.Board.Distance(thirdCity.Find(3)!.Position, new(24, 15)) < 18,
            "relay builder advances toward the third city instead of rechecking connected short links: " + approach.Message);

        var escape = Fixture((Side.Enemy, ShipClass.Kolonel, new(8, 3)),
            (Side.Player, ShipClass.Garrison, new(4, 3)),
            (Side.Player, ShipClass.Fishing, new(6, 3)));
        var fleeSave = escape.CaptureSnapshot();
        fleeSave.Ships.Single(s => s.Id == 1).Health = 5;
        fleeSave.Credits[0] = 0;
        escape = BattleState.LoadJson(BattleState.SerializeSnapshot(fleeSave));
        var flee = SimpleOpponent.Step(escape);
        Check(flee.Success && flee.Kind == CommandKind.Move && flee.ActorId == 1,
            "damaged flagship anticipates a visible mobile gun's next-turn reach");
        var shield = Fixture((Side.Enemy, ShipClass.Kolonel, new(9, 6)),
            (Side.Player, ShipClass.Garrison, new(7, 8)),
            (Side.Player, ShipClass.Fishing, new(8, 5)));
        var shieldSave = shield.CaptureSnapshot();
        var escapedMother = shieldSave.Ships.Single(s => s.Id == 1);
        escapedMother.Position = new(6, 6); escapedMother.Health = 5;
        escapedMother.HasMoved = true; escapedMother.MovementSpentUnits = 10;
        shieldSave.Credits[0] = 0;
        shield = BattleState.LoadJson(BattleState.SerializeSnapshot(shieldSave));
        var screen = SimpleOpponent.Step(shield);
        Check(screen.Success && screen.Kind == CommandKind.Move && screen.ActorId == 4
            && shield.Board.Distance(shield.Find(4)!.Position, shield.Find(1)!.Position) <= 2,
            "an escort occupies the approaching sea after the damaged flagship has escaped");
        shieldSave.Difficulty = AiDifficulty.Captain;
        var captainShield = BattleState.LoadJson(BattleState.SerializeSnapshot(shieldSave));
        var captainScreen = SimpleOpponent.Step(captainShield);
        Check(captainScreen.Success && captainScreen.Kind == CommandKind.Move && captainScreen.ActorId == 4
            && captainShield.Board.Distance(captainShield.Find(4)!.Position, captainShield.Find(1)!.Position) <= 2,
            "Captain also screens an escaped damaged flagship using observed threats");
        var fallen = Fixture((Side.Enemy2, ShipClass.Mothership, new(27, 3)),
            (Side.Enemy, ShipClass.Invader, new(4, 3)));
        var fallenSave = fallen.CaptureSnapshot(); fallenSave.Ships.Single(s => s.Id == 1).Health = 1;
        fallen = BattleState.LoadJson(BattleState.SerializeSnapshot(fallenSave));
        Check(fallen.Attack(Side.Player, 1, 4).Success && fallen.Mothership(Side.Player) is null && !fallen.IsOver,
            "a fatal counterattack removes the active captain while other nations survive");
        Check(SimpleOpponent.Step(fallen) is { Success: true, Kind: CommandKind.EndTurn }
            && fallen.ActiveSide != Side.Player,
            "a defeated active captain advances safely instead of planning investments without a flagship");
        return checks;
    }

    private static string Signature(CommandResult command) => $"{command.Kind}/{command.ActorId}/{command.TargetId}/"
        + string.Join(';', command.Path?.Select(p => $"{p.X},{p.Y}") ?? Array.Empty<string>());
}
