using System.Numerics;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class World0202Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0, mountainCount = 0, landCount = 0, meadowCount = 0;
        double resourceRadius = 0, availableRadius = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception("World 0.20.2: " + name);
            checks++;
        }
        foreach (int seed in new[] { 19, 731, 2026 })
        foreach (int opponents in new[] { 1, 3, 4 })
        foreach (var kind in Enum.GetValues<WorldKind>())
        {
            var board = ArchipelagoGenerator.Create(seed, opponents, kind);
            var features = TerrainFeatures.For(board);
            var duplicate = TerrainFeatures.For(ArchipelagoGenerator.Create(seed, opponents, kind));
            string label = $"{kind}/{seed}/{opponents}";
            Check(ReferenceEquals(features, TerrainFeatures.For(board)), label + " feature plan retained per board");
            Check(features.MountainCells.SetEquals(duplicate.MountainCells), label + " ridge seed reproducible");
            Check(features.MountainCells.All(p => board.GetTile(p).Terrain == TerrainType.Land &&
                board.GetSurrounding(p).All(n => board.GetTile(n).Terrain == TerrainType.Land)),
                label + " every peak has an all-land ground buffer before the beach and water");
            Check(features.MountainCells.All(p => features.Footprint(p).Count >= 3 &&
                features.Footprint(p).All(v => float.IsFinite(v.X) && float.IsFinite(v.Y))),
                label + " clipped mountain footprints are finite polygons");
            mountainCount += features.MountainCells.Count;
            foreach (var tile in board.Tiles.Where(t => t.Terrain == TerrainType.Land))
            {
                landCount++;
                if (features.ForestDensity(tile.Position) == 0 && !features.MountainCells.Contains(tile.Position)) meadowCount++;
            }
            var water = board.Tiles.Where(t => t.Terrain != TerrainType.Land).ToArray();
            Check(water.All(t => features.ForestDensity(t.Position) == 0), label + " no forests at sea");
            Check(board.Tiles.Where(t => t.Terrain == TerrainType.Coast).All(t =>
                features.DistanceFromCoast(t.Position) == 1), label + " shallow shore has nearest water depth");
            Check(water.All(t => board.GetNeighbors(t.Position).Where(p =>
                board.GetTile(p).Terrain != TerrainType.Land).All(p =>
                Math.Abs(features.DistanceFromCoast(p) - features.DistanceFromCoast(t.Position)) <= 1)),
                label + " sea shading grows without depth discontinuities");

            var battle = SkirmishSetup.Create(board, rules, opponents);
            var descriptor = WorldSettlementPlacement.Describe(board, battle.Villages.Select(v => v.Position));
            var bays = battle.Villages.Where(v => v.Owner == Side.Pirates).ToArray();
            Check(descriptor.Values.Count(d => d.IsPirateBay) >= 2 && bays.Length >= 2 &&
                bays.All(v => v.Level == 3 && v.IsFortified && v.Health == v.MaxHealth),
                label + " two operational level-three fortified pirate bays");
            Check(battle.Villages.All(v => v.Level is >= 1 and <= 3 &&
                board.GetNeighbors(v.Position).Any(p => board.GetTile(p).Terrain != TerrainType.Land)),
                label + " every varied settlement has a real sea berth");
            Check(battle.Villages.Count == (opponents + 1) * 3 &&
                Enumerable.Range(0, opponents + 1).All(i => battle.Villages.Count(v =>
                    board.StartingTerritory(v.Position, opponents + 1) == i) == 3),
                label + " equal capture-site counts per starting territory");
            Check(battle.Ships.Where(s => s.IsMothership).All(m => battle.FishSpots.Any(p =>
                board.InRadius(m.Position, p, m.Definition.CollectionRange))),
                label + " every starting fleet has a first catch");
            int originalBudget = Math.Max(rules.Economy.MinimumResourceSpots,
                rules.Economy.ResourceTileInterval > 0 ? board.Tiles.Count / rules.Economy.ResourceTileInterval : 0);
            Check(battle.FishSpots.Count <= Math.Max(opponents + 1, (int)Math.Round(originalBudget * .70)) &&
                battle.Shoals.Count <= 6, label + " fish reduced at least thirty percent and rich schools bounded");
            Check(battle.FishSpots.All(p => board.GetTile(p).Terrain != TerrainType.Land && !battle.Shoals.Contains(p)),
                label + " fish and rich shoals never overlap or spawn on land");
            var center = board.Center(board.CentralCell);
            float radius = board.Tiles.Max(t => Vector2.Distance(center, board.Center(t.Position)));
            resourceRadius += battle.FishSpots.Average(p => Vector2.Distance(center, board.Center(p)) / radius);
            availableRadius += water.Average(t => Vector2.Distance(center, board.Center(t.Position)) / radius);
            var restored = BattleState.LoadJson(battle.SaveJson());
            Check(restored.FishSpots.ToHashSet().SetEquals(battle.FishSpots) &&
                restored.Shoals.ToHashSet().SetEquals(battle.Shoals) &&
                restored.Villages.Select(v => (v.Position, v.Owner, v.Level, v.IsFortified, v.Health)).SequenceEqual(
                    battle.Villages.Select(v => (v.Position, v.Owner, v.Level, v.IsFortified, v.Health))),
                label + " resource positions and pirate fortifications survive save/resume without rerolling");
            Check(TerrainFeatures.For(restored.Board).MountainCells.SetEquals(features.MountainCells),
                label + " terrain sight blockers survive exact board persistence");
        }
        Check(mountainCount > 0, "continent/island worlds contain shared mountain obstacles");
        Check(meadowCount > landCount * .25, "whole treeless plain regions remain alongside forest groves");
        Check(resourceRadius < availableRadius * .96, "resource sampling is measurably weighted toward the map interior");
        var dryBoard = new GameBoard(12, 12, _ => TerrainType.Land);
        var dryFeatures = TerrainFeatures.For(dryBoard);
        Check(dryFeatures.DistanceFromCoast(new(6, 6)) > dryFeatures.DistanceFromCoast(new(0, 6)),
            "an all-land rectangular fixture uses its boundary without missing depth records");
        Check(dryFeatures.MountainCells.All(p => dryBoard.GetNeighbors(p).Count() == 4),
            "mountain footprints cannot escape a fixture's outer boundary");
        return checks;
    }
}
