using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.World;
public static class SkirmishSetup
{
    public static BattleState Create(GameBoard board, BattleRules rules, int opponentCount = 1,
        AiDifficulty difficulty = AiDifficulty.Captain, bool piratesEnabled = true)
    {
        if (opponentCount is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(opponentCount), "Choose one to four rival fleets.");
        int factionCount = opponentCount + 1;
        var deployment = new List<(Side, ShipClass, GridPosition)>();
        for (int index = 0; index < factionCount; index++)
        {
            var side = BattleState.PlayableSides[index];
            var cells = board.HarborCells(index, factionCount);
            if (cells.Count < 3)
                throw new ArgumentException("A starting territory has fewer than three safe berths.");
            if (rules.SeparatedStartingEscorts)
            {
                var mother = cells[0];
                if (rules.EmptyOuterRim && board.IsOuterCell(mother))
                    mother = board.Tiles.Where(t => t.Terrain != TerrainType.Land && !board.IsOuterCell(t.Position))
                        .OrderBy(t => board.Distance(cells[0], t.Position)).First().Position;
                var escorts = board.Tiles.Where(t => t.Terrain != TerrainType.Land && !board.IsNarrowPassage(t.Position)
                    && (!rules.EmptyOuterRim || !board.IsOuterCell(t.Position)) && t.Position != mother
                    && !board.GetSurrounding(mother).Contains(t.Position)
                    && deployment.All(existing => existing.Item3 != t.Position))
                    .OrderBy(t => board.Distance(mother, t.Position))
                    .ThenBy(t => System.Numerics.Vector2.DistanceSquared(board.Center(mother), board.Center(t.Position)))
                    .ThenBy(t => t.Position.Y).ThenBy(t => t.Position.X).Take(2).Select(t => t.Position).ToArray();
                if (escorts.Length < 2) throw new ArgumentException("The starting fleet needs separated safe berths.");
                cells = new[] { mother, escorts[0], escorts[1] };
            }
            deployment.Add((side, ShipClass.Mothership, cells[0]));
            deployment.Add((side, ShipClass.Garrison, cells[1]));
            deployment.Add((side, ShipClass.Fishing, cells[2]));
        }

        var settlements = WorldSettlementPlacement.Create(board, factionCount,
            requireLandNeighbor: rules.MapSizePirateSettlements && board.MapSize is not null);
        return new(board, rules, deployment, resourceSeed: board.Seed, villageSpots: settlements,
            seaEvents: board.Mesh is not null, generatedSettlements: true,
            difficulty: difficulty, piratesEnabled: piratesEnabled);
    }
}
