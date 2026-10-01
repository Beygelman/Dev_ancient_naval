using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class WorldGenerationScenarios
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("World generation: " + name);
            checks++;
        }

        foreach (int seed in new[] { 19, 731 })
        foreach (int rivals in Enumerable.Range(1, 4))
        foreach (var kind in Enum.GetValues<WorldKind>())
        {
            var board = ArchipelagoGenerator.Create(seed, rivals, kind);
            var duplicate = ArchipelagoGenerator.Create(seed, rivals, kind);
            Check(board.Kind == kind && board.Mesh is not null && board.Boundary.Count == 6,
                $"{kind}/{seed}/{rivals}: organic hexagon and chosen policy retained");
            Check(board.Tiles.Select(t => (t.Position, t.Terrain)).SequenceEqual(
                duplicate.Tiles.Select(t => (t.Position, t.Terrain))) &&
                board.Mesh!.Vertices.SequenceEqual(duplicate.Mesh!.Vertices),
                $"{kind}/{seed}/{rivals}: seeded mesh/terrain are deterministic");
            if (kind == WorldKind.Oceans)
            {
                var familiar = ArchipelagoGenerator.Create(seed, rivals);
                Check(familiar.Tiles.Select(t => t.Terrain).SequenceEqual(board.Tiles.Select(t => t.Terrain)),
                    "Oceans remains the default original generator");
                continue;
            }
            var land = Components(board, true);
            var sea = Components(board, false);
            Check(sea.Count == 1, $"{kind}/{seed}/{rivals}: every river/lake connects by shared edges to ocean");
            Check(board.Tiles.Any(t => t.Terrain == TerrainType.Coast), "Navigable shallow shores exist");
            Check(board.Tiles.Where(t => t.Terrain == TerrainType.Coast).All(t =>
                board.GetSurrounding(t.Position).Any(p => board.GetTile(p).Terrain == TerrainType.Land)),
                "Shallow water follows actual land banks");
            Check(board.Tiles.Where(t => t.Terrain == TerrainType.Land).All(t =>
                board.GetNeighbors(t.Position).Count() == board.Mesh!.Faces[t.Position].Count),
                "The six boundary sides remain navigable water");
            if (kind == WorldKind.SeaWorld)
                Check(land.Count >= rivals + 1 && land.All(c => c.Count is >= 1 and <= 3),
                    "Sea World has only separated one-to-three-cell islets, including corner contacts");
            if (kind == WorldKind.Continents)
                Check(land.Any(c => c.Count >= 15) && land.All(c => c.Count <= 30),
                    "Continents differ from tiny islets without becoming one central great land");
            if (kind == WorldKind.Pangaea)
            {
                Check(land.Sum(c => c.Count) > board.Tiles.Count * .22 && land.Max(c => c.Count) >= 35,
                    "Pangaea has a broad central land envelope and large river-cut regions");
                Check(board.GetTile(board.CentralCell).Terrain != TerrainType.Land,
                    "Pangaea central river/lake remains navigable");
            }

            var battle = SkirmishSetup.Create(board, rules, rivals);
            int factions = rivals + 1;
            var fleet = battle.Ships.Where(s => s.Owner != Side.Pirates).ToArray();
            Check(fleet.Length == factions * 3 && fleet.Select(s => s.Position).Distinct().Count() == fleet.Length,
                "Every faction has three separate starting berths");
            Check(fleet.Where(s => s.IsMothership).All(s => !board.IsNarrowPassage(s.Position)),
                "No Mothership starts stranded in a river");
            var accessible = new HashSet<GridPosition>();
            var pending = new Queue<GridPosition>();
            pending.Enqueue(board.FleetAnchor(0, factions));
            while (pending.TryDequeue(out var cell))
            {
                if (!accessible.Add(cell))
                    continue;
                foreach (var next in board.GetNeighbors(cell))
                    if (board.GetTile(next).Terrain != TerrainType.Land && !board.IsNarrowPassage(next) &&
                        !accessible.Contains(next))
                        pending.Enqueue(next);
            }
            Check(Enumerable.Range(0, factions).All(i => accessible.Contains(board.FleetAnchor(i, factions))),
                "Ocean-side fleets can meet without forcing a Mother through a narrow river");
            var villages = Enumerable.Range(0, factions).Select(i => battle.Villages.Count(v =>
                board.StartingTerritory(v.Position, factions) == i)).ToArray();
            Check(villages.All(count => count == 3), "Each territory has three accessible coastal settlements");
            var treasures = Enumerable.Range(0, factions).Select(i => battle.Treasuries.Count(t =>
                board.StartingTerritory(t.Position, factions) == i)).ToArray();
            Check(treasures.Min() >= 1 && treasures.Distinct().Count() == 1,
                "Treasury opportunities remain equal and nonempty");
            Check(battle.OwnShips(Side.Pirates).Count() == factions, "Pirate pressure matches faction count");
            var restored = BattleState.LoadJson(battle.SaveJson());
            Check(restored.Board.Kind == kind && restored.Villages.Count == battle.Villages.Count &&
                restored.Board.Tiles.Select(t => t.Terrain).SequenceEqual(board.Tiles.Select(t => t.Terrain)),
                "Chosen world policy and exact terrain survive Continue");
        }
        return checks;
    }

    private static List<HashSet<GridPosition>> Components(GameBoard board, bool land)
    {
        var remaining = board.Tiles.Where(t => (t.Terrain == TerrainType.Land) == land)
            .Select(t => t.Position).ToHashSet();
        var result = new List<HashSet<GridPosition>>();
        while (remaining.Count > 0)
        {
            var component = new HashSet<GridPosition>();
            var pending = new Queue<GridPosition>();
            pending.Enqueue(remaining.First());
            while (pending.TryDequeue(out var p))
            {
                if (!remaining.Remove(p))
                    continue;
                component.Add(p);
                foreach (var n in land ? board.GetSurrounding(p) : board.GetNeighbors(p))
                    if (remaining.Contains(n))
                        pending.Enqueue(n);
            }
            result.Add(component);
        }
        return result;
    }
}
