using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

public sealed record SettlementStart(int Level, bool IsPirateBay);

/// <summary>Fair near-home access, with the remaining coastal settlements drawn toward the center.</summary>
public static class WorldSettlementPlacement
{
    public static IReadOnlyList<GridPosition> Create(GameBoard board, int factionCount)
    {
        const int perFaction = 3;
        var chosen = new List<GridPosition>();
        if (!board.Tiles.Any(t => t.Terrain == TerrainType.Land)) return chosen;
        for (int faction = 0; faction < factionCount; faction++)
        {
            var anchor = board.FleetAnchor(faction, factionCount);
            var coast = board.Tiles.Where(t => t.Terrain == TerrainType.Land &&
                board.StartingTerritory(t.Position, factionCount) == faction &&
                board.GetNeighbors(t.Position).Any(p => board.GetTile(p).Terrain != TerrainType.Land))
                .Select(t => t.Position).OrderBy(p => board.Distance(anchor, p)).ThenBy(p => p.Y).ThenBy(p => p.X).ToList();
            var local = new List<GridPosition>();
            while (local.Count < perFaction && coast.Count > 0)
            {
                // First coast is reachable from the local fleet; later sites spread out.
                var pool = coast;
                if (board.Kind == WorldKind.Pangaea)
                {
                    // Two river/lake towns and one outer coast town in each territory.
                    bool interior = local.Count < 2;
                    var preferred = coast.Where(p => PangaeaWaters.IsInterior(board, p) == interior).ToList();
                    if (preferred.Count > 0) pool = preferred;
                }
                var center = board.Center(board.CentralCell);
                // Keep one convenient starting town; later towns favor the contested interior.
                // Distance-based spacing avoids piling all three onto the same lake mouth.
                var cell = local.Count == 0 ? pool[0] : pool.MinBy(p =>
                    System.Numerics.Vector2.Distance(board.Center(p), center) +
                    5 / (1 + local.Min(q => System.Numerics.Vector2.Distance(board.Center(p), board.Center(q)))));
                coast.Remove(cell);
                local.Add(cell);
            }
            if (local.Count != perFaction)
                throw new InvalidOperationException("The generated territory has too few coastal settlement sites.");
            chosen.AddRange(local);
        }
        return chosen;
    }

    public static IReadOnlyDictionary<GridPosition, SettlementStart> Describe(GameBoard board,
        IEnumerable<GridPosition> positions)
    {
        var cells = positions.Distinct().OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
        var result = cells.ToDictionary(p => p, _ => new SettlementStart(1, false));
        var random = new Random(board.Seed ^ 0x4217C);
        foreach (var cell in cells)
        {
            double roll = random.NextDouble();
            if (roll < .06) result[cell] = new(3, false);
            else if (roll < .18) result[cell] = new(2, false);
        }
        var center = board.Center(board.CentralCell);
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
        return result;
    }
}
