using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class AotSerializationChecks
{
    public static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("AOT serialization: " + name);
            checks++;
        }

        // Compare generated metadata against the previous desktop serializer,
        // including property order, indentation, numeric representation and enums.
        var legacySaveOptions = new JsonSerializerOptions { WriteIndented = true };
        legacySaveOptions.Converters.Add(new JsonStringEnumConverter());
        foreach (WorldKind kind in Enum.GetValues<WorldKind>())
        {
            var battle = SkirmishSetup.Create(ArchipelagoGenerator.Create(73, kind: kind), rules);
            battle.SetPlayerColor(FleetColor.Purple);
            var snapshot = battle.CaptureSnapshot();
            string generated = BattleState.SerializeSnapshot(snapshot);
            string previous = JsonSerializer.Serialize(snapshot, legacySaveOptions);
            Check(generated == previous, kind + " retains the exact previous JSON contract");
            Check(BattleState.LoadJson(previous).SaveJson() == generated,
                kind + " loads previous JSON without altering geometry, identity, RNG or rules");
            Check(BattleState.LoadJson(generated).SaveJson() == generated,
                kind + " generated metadata has an exact stable round trip");
        }

        var originalRules = JsonSerializer.Serialize(rules, legacySaveOptions);
        var mixedCase = originalRules.Replace("\"StartingCredits\"", "\"startingcredits\"", StringComparison.Ordinal)
            .Replace("\"Ships\"", "\"sHiPs\"", StringComparison.Ordinal)
            .Replace("\"MaxHealth\"", "\"maxhealth\"", StringComparison.Ordinal);
        Check(JsonSerializer.Serialize(BattleRules.FromJson(mixedCase), legacySaveOptions) == originalRules,
            "balance files remain case-insensitive at root and nested ship definitions");

        var futureRules = JsonNode.Parse(originalRules)!.AsObject();
        futureRules["UnknownFuturePolicy"] = new JsonObject { ["UnknownValue"] = 1 };
        futureRules["Ports"]!.AsObject()["UnknownFuturePortField"] = true;
        Check(JsonSerializer.Serialize(BattleRules.FromJson(futureRules.ToJsonString()), legacySaveOptions) == originalRules,
            "unknown root and nested properties remain safely ignored");

        var numericEnums = JsonNode.Parse(originalRules)!.AsObject();
        foreach (JsonNode? node in numericEnums["Ships"]!.AsArray())
        {
            var ship = node!.AsObject();
            ship["Class"] = (int)Enum.Parse<ShipClass>(ship["Class"]!.GetValue<string>());
            ship["ActionProfile"] = (int)Enum.Parse<ActionProfile>(ship["ActionProfile"]!.GetValue<string>());
        }
        Check(JsonSerializer.Serialize(BattleRules.FromJson(numericEnums.ToJsonString()), legacySaveOptions) == originalRules,
            "released numeric enum values remain accepted as well as string names");

        var oldRules = JsonNode.Parse(originalRules)!.AsObject();
        oldRules.Remove("PirateCautiousRounds");
        oldRules.Remove("CannonTowersIgnoreWalls");
        oldRules.Remove("AutoRepairAmount");
        oldRules.Remove("VillageAutoRepairAmount");
        oldRules["Ports"]!.AsObject().Remove("ConnectedCityIncome");
        var restoredRules = BattleRules.FromJson(oldRules.ToJsonString());
        Check(restoredRules.PirateCautiousRounds == 0 && !restoredRules.CannonTowersIgnoreWalls &&
            restoredRules.AutoRepairAmount == 2 && restoredRules.VillageAutoRepairAmount is null &&
            !restoredRules.Ports.ConnectedCityIncome,
            "absent historical policies retain their initializer defaults");

        var sea = new BattleState(new GameBoard(12, 12, _ => TerrainType.Water), rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(9, 9)) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var save = JsonNode.Parse(sea.SaveJson())!.AsObject();
        save.Remove("PiratesEnabled");
        Check(BattleState.LoadJson(save.ToJsonString()).PiratesEnabled,
            "absent saved pirate choice retains true historical default");
        save["ships"] = save["Ships"]!.DeepClone();
        save.Remove("Ships");
        bool rejected = false;
        try { BattleState.LoadJson(save.ToJsonString()); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "save envelope remains case-sensitive rather than inheriting balance read options");
        return checks;
    }
}
