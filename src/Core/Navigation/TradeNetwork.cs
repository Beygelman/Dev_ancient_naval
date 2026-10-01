using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Navigation;
/// <summary>Shortest connected sea lanes, independent of temporary ship occupancy.</summary>
public sealed class TradeNetwork
{
    private readonly HashSet<(GridPosition, GridPosition)> _edges = new();
    private readonly List<IReadOnlyList<GridPosition>> _routes = new();
    public IReadOnlyList<IReadOnlyList<GridPosition>> Routes => _routes;

    public bool Contains(GridPosition from, GridPosition to) => _edges.Contains((from, to));
    public bool IsEmpty => _edges.Count == 0;

    internal static TradeNetwork Create(GameBoard board, GridPosition[] ports, IReadOnlySet<GridPosition> forbidden)
    {
        var result = new TradeNetwork();
        bool Sea(GridPosition p) => board.GetTile(p).Terrain != TerrainType.Land && !forbidden.Contains(p);
        int? Cost(GridPosition from, GridPosition to)
        {
            if (!Sea(to))
                return null;
            if (!board.GetNeighbors(from).Contains(to))
            {
                var corners = board.Mesh is null ? new[]
                {
                    new GridPosition(from.X, to.Y),
                    new GridPosition(to.X, from.Y)
                }

                : board.GetNeighbors(from).Intersect(board.GetNeighbors(to));
                if (!corners.All(Sea))
                    return null;
            }

            return 1;
        }

        for (int i = 0; i < ports.Length; i++)
        {
            if (!Sea(ports[i]))
                continue;
            var search = PathSearch.Find(ports[i], board.Tiles.Count, board.GetSurrounding, Cost);
            for (int j = i + 1; j < ports.Length; j++)
            {
                if (!search.Costs.ContainsKey(ports[j]))
                    continue;
                var route = PathSearch.Reconstruct(ports[i], ports[j], search.Previous);
                result._routes.Add(route);
                for (int step = 1; step < route.Count; step++)
                {
                    result._edges.Add((route[step - 1], route[step]));
                    result._edges.Add((route[step], route[step - 1]));
                }
            }
        }

        return result;
    }
}
