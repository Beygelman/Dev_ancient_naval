using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

public sealed record SettlementStart(int Level, bool IsPirateBay);

/// <summary>Fair near-home access, with the remaining coastal settlements drawn toward the center.</summary>
public static class WorldSettlementPlacement
{
    public static IReadOnlyList<GridPosition> Create(GameBoard board, int factionCount, bool requireLandNeighbor = false)
    {
        const int perFaction = 3;
        var chosen = new List<GridPosition>();
        if (!board.Tiles.Any(t => t.Terrain == TerrainType.Land)) return chosen;
        var center = board.Center(board.CentralCell);
        var coastByFaction = Enumerable.Range(0, factionCount).Select(faction => board.Tiles
            .Where(t => t.Terrain == TerrainType.Land && board.StartingTerritory(t.Position, factionCount) == faction &&
                board.GetNeighbors(t.Position).Any(p => board.GetTile(p).Terrain != TerrainType.Land) &&
                (!requireLandNeighbor || board.GetNeighbors(t.Position).Any(p => board.GetTile(p).Terrain == TerrainType.Land)))
            .Select(t => t.Position).OrderBy(p => p.Y).ThenBy(p => p.X).ToArray()).ToArray();
        if (coastByFaction.Any(coast => coast.Length < perFaction))
            throw new InvalidOperationException("The generated territory has too few coastal settlement sites.");
        var local = Enumerable.Range(0, factionCount).Select(_ => new List<GridPosition>()).ToArray();
        int interiorQuota = board.Kind == WorldKind.Pangaea ? factionCount * 2 : 0;
        var interiorCoasts = coastByFaction.Select(coast => coast.Where(p => PangaeaWaters.IsInterior(board, p)).ToHashSet()).ToArray();
        var near = new Dictionary<GridPosition, HashSet<GridPosition>>();
        HashSet<GridPosition> Exclusion(GridPosition cell)
        {
            if (near.TryGetValue(cell, out var cached)) return cached;
            // Local radius traversal does not populate a whole-map distance
            // table for every rejected candidate on a large organic board.
            return near[cell] = board.Mesh is { } mesh ? mesh.Within(cell, 3).ToHashSet() :
                board.Tiles.Where(t => board.InRadius(cell, t.Position, 3)).Select(t => t.Position).ToHashSet();
        }
        bool Separated(GridPosition cell) => chosen.All(existing => !Exclusion(existing).Contains(cell));
        int attempts = 0;
        bool Place()
        {
            int interiorChosen = chosen.Count(p => PangaeaWaters.IsInterior(board, p));
            if (chosen.Count == factionCount * perFaction) return interiorChosen >= interiorQuota;
            if (++attempts > 50_000) return false;
            if (interiorQuota > 0 && interiorChosen + Enumerable.Range(0, factionCount).Sum(f =>
                Math.Min(perFaction - local[f].Count, interiorCoasts[f].Count(Separated))) < interiorQuota) return false;
            // Satisfy the least flexible territory first. Backtracking can
            // relocate a convenient shore instead of weakening the clearance.
            var next = Enumerable.Range(0, factionCount).Where(f => local[f].Count < perFaction)
                .Select(f => (Faction: f, Pool: coastByFaction[f].Where(Separated).ToArray()))
                .OrderBy(p => p.Pool.Length - (perFaction - local[p.Faction].Count)).ThenBy(p => p.Faction).First();
            int faction = next.Faction;
            if (next.Pool.Length < perFaction - local[faction].Count) return false;
            var anchor = board.FleetAnchor(faction, factionCount);
            double Score(GridPosition cell)
            {
                double preference = board.Kind == WorldKind.Pangaea &&
                    PangaeaWaters.IsInterior(board, cell) != (interiorChosen < interiorQuota) ? 1000 : 0;
                if (local[faction].Count == 0) return preference + board.Distance(anchor, cell);
                return preference + System.Numerics.Vector2.Distance(board.Center(cell), center) +
                    5 / (1 + local[faction].Min(q => System.Numerics.Vector2.Distance(board.Center(cell), board.Center(q))));
            }
            foreach (var cell in next.Pool.OrderBy(p => board.MapSize is not null && board.GetNeighbors(p).Count(n => board.GetTile(n).Terrain == TerrainType.Land) < 3 ? 1 : 0).ThenBy(Score).ThenBy(p => p.Y).ThenBy(p => p.X))
            {
                chosen.Add(cell);
                local[faction].Add(cell);
                if (Place()) return true;
                local[faction].RemoveAt(local[faction].Count - 1);
                chosen.RemoveAt(chosen.Count - 1);
                if (attempts > 50_000) break;
            }
            return false;
        }
        bool placed = Place();
        if (!placed && board.Kind == WorldKind.Pangaea)
        {
            // Some territory borders make two inland towns in every fleet's
            // area impossible with the new clearance. Keep a global majority
            // while preserving exactly three sites in every territory.
            attempts = 0;
            interiorQuota = factionCount * perFaction / 2 + 1;
            placed = Place();
        }
        if (!placed)
            throw new InvalidOperationException("Cannot place equal coastal settlements more than three tiles apart.");
        return local.SelectMany(cells => cells).ToArray();
    }

    public static IReadOnlyDictionary<GridPosition, SettlementStart> Describe(GameBoard board,
        IEnumerable<GridPosition> positions, bool variedPirates = false, bool mapSizePirates = false,
        bool piratesEnabled = true, AiDifficulty? difficulty = null)
    {
        var cells = positions.Distinct().OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
        var result = cells.ToDictionary(p => p, _ => new SettlementStart(1, false));
        var random = new Random(board.Seed ^ 0x4217C);
        foreach (var cell in cells)
        {
            double roll = random.NextDouble();
            if (roll < .06) result[cell] = new(3, false);
            else if (roll < .18) result[cell] = new(2, false);
            else if (piratesEnabled && difficulty is null && variedPirates && !mapSizePirates && roll < .28)
                result[cell] = new(random.Next(1, 5), true);
        }
        if (!piratesEnabled) return result;
        var center = board.Center(board.CentralCell);
        if (difficulty is { } selectedDifficulty)
        {
            int desired = PiratePopulation.SettlementCount(board, selectedDifficulty, cells.Length);
            var bays = cells.OrderBy(p => System.Numerics.Vector2.DistanceSquared(board.Center(p), center))
                .ThenBy(p => p.Y).ThenBy(p => p.X).Take(desired).ToArray();
            for (int index = 0; index < bays.Length; index++)
                result[bays[index]] = new(index < 2 ? 3 : random.Next(1, 5), true);
            return result;
        }
        var available = cells.OrderBy(p => System.Numerics.Vector2.DistanceSquared(board.Center(p), center)).ToList();
        // The two fortified bays are guaranteed in generated worlds, including tiny-island maps.
        if (available.Count > 0)
        {
            var first = available[0];
            result[first] = new(3, true);
            available.Remove(first);
            if (available.Count > 0)
            {
                var second = available.MinBy(p =>
                    System.Numerics.Vector2.Distance(board.Center(p), center) +
                    12 / (1 + System.Numerics.Vector2.Distance(board.Center(p), board.Center(first))));
                result[second] = new(3, true);
            }
        }
        if (mapSizePirates)
        {
            int desired = board.MapSize switch
            {
                MapSize.Lake => 2, MapSize.Bay => 3, MapSize.Sea => 4, MapSize.Ocean => 5,
                _ => Math.Clamp(board.Tiles.Count / 300, 2, 5)
            };
            // An exact size quota replaces the old per-settlement random chance:
            // adding rivals cannot itself create additional pirate bays.
            foreach (var cell in result.Where(entry => entry.Value.IsPirateBay).Select(entry => entry.Key).ToArray())
                result[cell] = new(3, false);
            var pirates = cells.OrderBy(p => System.Numerics.Vector2.DistanceSquared(board.Center(p), center))
                .ThenBy(p => p.Y).ThenBy(p => p.X).Take(Math.Min(desired, cells.Length)).ToArray();
            for (int index = 0; index < pirates.Length; index++)
                result[pirates[index]] = new(index < 2 ? 3 : random.Next(1, 5), true);
        }
        return result;
    }
}
