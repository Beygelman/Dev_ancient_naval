using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>Equal settlement opportunities even when a continent spans several territories.</summary>
internal static class WorldSettlementPlacement
{
    internal static IReadOnlyList<GridPosition> Create(GameBoard board, int factionCount)
    {
        const int perFaction = 3;
        var chosen = new List<GridPosition>();
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
                var cell = local.Count == 0 ? pool[0] : pool.MaxBy(p => local.Min(q => board.Distance(p, q)));
                coast.Remove(cell);
                local.Add(cell);
            }
            if (local.Count != perFaction)
                throw new InvalidOperationException("The generated territory has too few coastal settlement sites.");
            chosen.AddRange(local);
        }
        return chosen;
    }
}
