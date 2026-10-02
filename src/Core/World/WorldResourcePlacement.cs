using System.Numerics;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>Seeded weighted sampling: sparse distant opportunities and denser contested interior seas.</summary>
public static class WorldResourcePlacement
{
    public static IReadOnlyList<GridPosition> Order(GameBoard board, IEnumerable<GridPosition> candidates, int seed)
    {
        var random = new Random(seed);
        var center = board.Center(board.CentralCell);
        float radius = board.Tiles.Max(t => Vector2.Distance(center, board.Center(t.Position)));
        return candidates.OrderBy(p => p.Y).ThenBy(p => p.X).Select(p =>
        {
            float normalized = Math.Clamp(Vector2.Distance(center, board.Center(p)) / Math.Max(1, radius), 0, 1);
            double weight = .6 + 3 * (1 - normalized) * (1 - normalized);
            if (board.Kind == WorldKind.Pangaea && PangaeaWaters.IsInterior(board, p)) weight *= 1.25;
            // An exponential race gives reproducible, without-replacement weighted sampling.
            double priority = -Math.Log(Math.Max(.0000001, random.NextDouble())) / weight;
            return (Cell: p, Priority: priority);
        }).OrderBy(item => item.Priority).Select(item => item.Cell).ToArray();
    }
}
