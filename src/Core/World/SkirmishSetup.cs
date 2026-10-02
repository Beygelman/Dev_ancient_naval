using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.World;
public static class SkirmishSetup
{
    public static BattleState Create(GameBoard board, BattleRules rules, int opponentCount = 1)
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
            deployment.Add((side, ShipClass.Mothership, cells[0]));
            deployment.Add((side, ShipClass.Garrison, cells[1]));
            deployment.Add((side, ShipClass.Fishing, cells[2]));
        }

        var settlements = WorldSettlementPlacement.Create(board, factionCount);
        return new(board, rules, deployment, resourceSeed: board.Seed, villageSpots: settlements,
            seaEvents: board.Mesh is not null, generatedSettlements: true);
    }
}
