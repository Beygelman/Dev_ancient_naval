using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.Navigation;
/// <summary>Stable Dijkstra traversal. Costs and neighbor order are supplied by
/// the caller; this class has no knowledge of ships, fog or turn state.</summary>
internal static class PathSearch
{
    public static (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) Find(GridPosition origin, int budget, Func<GridPosition, IEnumerable<GridPosition>> neighbors, Func<GridPosition, GridPosition, int?> stepCost)
    {
        var costs = new Dictionary<GridPosition, int>
        {
            [origin] = 0
        };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new PriorityQueue<GridPosition, (int Cost, int Order)>();
        int order = 0;
        queue.Enqueue(origin, (0, order++));
        while (queue.TryDequeue(out var current, out var priority))
        {
            if (priority.Cost != costs[current])
                continue;
            foreach (var next in neighbors(current))
            {
                var step = stepCost(current, next);
                if (step is null)
                    continue;
                int cost = priority.Cost + step.Value;
                if (cost > budget || costs.TryGetValue(next, out int old) && old <= cost)
                    continue;
                costs[next] = cost;
                previous[next] = current;
                queue.Enqueue(next, (cost, order++));
            }
        }

        return (costs, previous);
    }

    public static IReadOnlyList<GridPosition> Reconstruct(GridPosition origin, GridPosition destination, IReadOnlyDictionary<GridPosition, GridPosition> previous)
    {
        var path = new List<GridPosition>
        {
            destination
        };
        while (path[^1] != origin)
            path.Add(previous[path[^1]]);
        path.Reverse();
        return path.AsReadOnly();
    }
}
