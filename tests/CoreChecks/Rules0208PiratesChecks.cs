using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Rules0208PiratesChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("v020.8 pirates: " + message);
            checks++;
        }
        void Reject(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { checks++; return; }
            throw new Exception("v020.8 pirates: " + message);
        }

        Check(rules.PirateDifficultyScaling, "new voyages enable initial pirate pressure by difficulty");
        var land = new HashSet<GridPosition>();
        var villages = new List<GridPosition>();
        for (int y = 12; y <= 36; y += 8)
        for (int x = 12; x <= 36; x += 8)
        {
            villages.Add(new(x, y));
            land.Add(new(x, y));
            land.Add(new(x + 1, y));
        }
        BattleState Fixture(GameBoard board, AiDifficulty difficulty, bool includePirates = true,
            BattleRules? selectedRules = null) => new(board, selectedRules ?? rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(44, 44)) },
            resourceSeed: board.Seed, villageSpots: villages,
            seaEvents: true, generatedSettlements: true,
            difficulty: difficulty, piratesEnabled: includePirates);
        string Fleet(BattleState battle) => JsonSerializer.Serialize(battle.CaptureSnapshot().Ships);
        string Towns(BattleState battle) => JsonSerializer.Serialize(battle.CaptureSnapshot().Villages);

        foreach (var size in Enum.GetValues<MapSize>())
        {
            var board = new GameBoard(48, 48, p => land.Contains(p) ? TerrainType.Land : TerrainType.Water,
                seed: 20804 + (int)size, mapSize: size);
            var matches = Enum.GetValues<AiDifficulty>().Select(d => Fixture(board, d)).ToArray();
            var bays = matches.Select(b => b.Villages.Count(v => v.Owner == Side.Pirates)).ToArray();
            var patrols = matches.Select(b => b.OwnShips(Side.Pirates).Count()).ToArray();
            Check(bays[0] < bays[1] && bays[1] < bays[2], size + " increases pirate settlements at each difficulty");
            Check(patrols[0] < patrols[1] && patrols[1] < patrols[2], size + " increases patrols at each difficulty");
            Check(matches.All(b => b.Villages.Count(v => v.Owner is null) >= 2), "pirate pressure leaves neutral towns");
            foreach (var match in matches)
            {
                Check(match.PiratesEnabled && match.Difficulty == (AiDifficulty)Array.IndexOf(matches, match),
                    "difficulty is applied before generation and belongs to the voyage");
                Check(match.Villages.Count(v => v.Owner == Side.Pirates && v.Level == 3 && v.IsFortified) >= 2
                    && match.Villages.Where(v => v.Owner == Side.Pirates).All(v => v.Level < 5),
                    "fortified level-three bays remain present and pirates never start at level five");
                Check(match.Factions.Select(side => board.StartingTerritory(match.Mothership(side)!.Position,
                    match.Factions.Count)).Distinct().Count() == match.Factions.Count, "fixture covers distinct territories");
                var territoryCounts = match.OwnShips(Side.Pirates)
                    .GroupBy(s => board.StartingTerritory(s.Position, match.Factions.Count)).Select(g => g.Count()).ToArray();
                Check(territoryCounts.Length == match.Factions.Count && territoryCounts.Distinct().Count() == 1,
                    "each fleet receives equal initial patrol pressure");
                Check(match.OwnShips(Side.Pirates).All(s => s.Health == rules.Get(ShipClass.PirateSchooner).MaxHealth
                    && s.Definition == rules.Get(ShipClass.PirateSchooner)), "difficulty never inflates pirate statistics");
                Check(match.OwnShips(Side.Pirates).All(s => board.GetTile(s.Position).Terrain == TerrainType.Water
                    && !board.IsOuterCell(s.Position) && match.Factions.All(f =>
                        board.Distance(s.Position, match.Mothership(f)!.Position) > 4)), "patrols start away from home fleets and land");
                Check(BattleState.LoadJson(match.SaveJson()).SaveJson() == match.SaveJson(),
                    "enabled preference, levels, patrol homes and event stream resume exactly");
            }
            var noPirates = Fixture(board, AiDifficulty.Admiral, includePirates: false);
            Check(!noPirates.PiratesEnabled && !noPirates.OwnShips(Side.Pirates).Any()
                && noPirates.Villages.All(v => v.Owner is null) && noPirates.CaptureSnapshot().PirateHomes.Length == 0,
                "unchecked pirate choice removes all generated bays and patrols");
            Check(noPirates.Villages.Select(v => v.Position).SequenceEqual(matches[2].Villages.Select(v => v.Position))
                && noPirates.FishSpots.SequenceEqual(matches[2].FishSpots)
                && noPirates.Shoals.SequenceEqual(matches[2].Shoals)
                && noPirates.TreasuryRuins.SequenceEqual(matches[2].TreasuryRuins),
                "pirate-free voyages retain town sites, fish, shoals and fair treasuries");
            Check(noPirates.CaptureSnapshot().EventDraws == matches[2].CaptureSnapshot().EventDraws,
                "the pirate toggle does not consume treasury reward randomness");
            Check(BattleState.LoadJson(noPirates.SaveJson()).SaveJson() == noPirates.SaveJson(),
                "pirate-free choice persists without regenerating objects");
            var duplicate = Fixture(board, AiDifficulty.Admiral);
            Check(duplicate.SaveJson() == matches[2].SaveJson(), "same seed and settings produce identical initial worlds");
            var changed = matches[0];
            string fleetBefore = Fleet(changed), townsBefore = Towns(changed);
            changed.SetDifficulty(AiDifficulty.Admiral);
            Check(Fleet(changed) == fleetBefore && Towns(changed) == townsBefore,
                "changing tactical difficulty in an existing world does not reroll its pirates");
            var resumed = BattleState.LoadJson(changed.SaveJson());
            Check(Fleet(resumed) == fleetBefore && Towns(resumed) == townsBefore,
                "restoring an existing world never applies a new initial count");
            var frozen = noPirates.Villages.Select(v => v.Level).ToArray();
            for (int turn = 0; turn < 10; turn++)
                Check(noPirates.EndTurn(noPirates.ActiveSide).Success, "pirate-free factions can advance turns");
            Check(noPirates.ActiveSide != Side.Pirates && noPirates.Villages.Select(v => v.Level).SequenceEqual(frozen),
                "disabled pirates receive no turns and neutral towns do not grow");
        }

        var legacyRuleJson = JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        legacyRuleJson.Remove("PirateDifficultyScaling");
        var legacyRules = BattleRules.FromJson(legacyRuleJson.ToJsonString());
        var legacyBoard = new GameBoard(48, 48,
            p => land.Contains(p) ? TerrainType.Land : TerrainType.Water, seed: 20815, mapSize: MapSize.Ocean);
        var legacy = Fixture(legacyBoard, AiDifficulty.Admiral, selectedRules: legacyRules);
        Check(!legacy.Rules.PirateDifficultyScaling && legacy.Villages.Count(v => v.Owner == Side.Pirates) == 5
            && legacy.OwnShips(Side.Pirates).Count() == legacy.Factions.Count,
            "absent optional rule preserves released map-size bays and one patrol per territory");
        var legacySave = JsonNode.Parse(legacy.SaveJson())!.AsObject();
        legacySave.Remove("PiratesEnabled");
        var old = BattleState.LoadJson(legacySave.ToJsonString());
        Check(old.PiratesEnabled && Fleet(old) == Fleet(legacy) && Towns(old) == Towns(legacy)
            && old.CaptureSnapshot().PirateHomes.SequenceEqual(legacy.CaptureSnapshot().PirateHomes),
            "missing old-save preference defaults to enabled and preserves every existing pirate");
        var invalid = legacy.CaptureSnapshot();
        invalid.PiratesEnabled = false;
        Reject(() => BattleState.LoadJson(BattleState.SerializeSnapshot(invalid)),
            "inconsistent pirate-free snapshots are rejected, never silently pruned");
        var peacefulLegacy = Fixture(legacyBoard, AiDifficulty.Admiral, includePirates: false, selectedRules: legacyRules);
        var contradictoryWinner = peacefulLegacy.CaptureSnapshot();
        contradictoryWinner.Winner = Side.Pirates;
        Reject(() => BattleState.LoadJson(BattleState.SerializeSnapshot(contradictoryWinner)),
            "a disabled pirate nation cannot be the saved winner");
        var contradictoryIncome = peacefulLegacy.CaptureSnapshot();
        contradictoryIncome.Income = contradictoryIncome.Income.Append(
            new DevAncientNaval.Core.Economy.IncomeSource("invalid-pirate-income", Side.Pirates, 1)).ToArray();
        Reject(() => BattleState.LoadJson(BattleState.SerializeSnapshot(contradictoryIncome)),
            "disabled pirates cannot retain an unbound income stream");
        Reject(() => Fixture(legacyBoard, (AiDifficulty)50), "invalid initial difficulty is rejected");

        // Exercise the real organic setup facade, including fairness and unchanged
        // coast/navigation geometry, rather than only custom rectangular fixtures.
        foreach (var kind in Enum.GetValues<WorldKind>())
        {
            var board = ArchipelagoGenerator.Create(2086, 1, kind, MapSize.Lake);
            var normal = SkirmishSetup.Create(board, rules, 1, AiDifficulty.Captain);
            var peaceful = SkirmishSetup.Create(board, rules, 1, AiDifficulty.Captain, piratesEnabled: false);
            Check(ReferenceEquals(normal.Board, peaceful.Board) && peaceful.Villages.Count == 6
                && peaceful.OwnShips(Side.Pirates).Count() == 0 && peaceful.Villages.All(v => v.Owner is null),
                kind + " organic pirate-free setup preserves six fair coast settlements and mesh");
            Check(normal.FishSpots.SequenceEqual(peaceful.FishSpots)
                && normal.TreasuryRuins.SequenceEqual(peaceful.TreasuryRuins),
                kind + " organic pirate toggle retains resources and sea routes");
            var prepared = peaceful.Prepare(battle => battle.EndTurn(Side.Player));
            Check(prepared.Result.Success && !peaceful.PiratesEnabled,
                "presented commands preserve the pirate-free preference in their simulation snapshot");
            prepared.Finish();
        }
        return checks;
    }
}
