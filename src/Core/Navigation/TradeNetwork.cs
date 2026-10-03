using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Navigation;
/// <summary>Shortest connected sea lanes, independent of temporary ship occupancy.</summary>
public sealed class TradeNetwork
{
    private readonly HashSet<(GridPosition, GridPosition)> _edges = new();
    private readonly List<IReadOnlyList<GridPosition>> _routes = new();
    public IReadOnlyList<IReadOnlyList<GridPosition>> Routes => _routes;
    /// <summary>Each undirected water edge appears once, even when several shortest port routes share it.</summary>
    public IReadOnlyList<(GridPosition From, GridPosition To)> Edges { get; private set; } = Array.Empty<(GridPosition, GridPosition)>();
    /// <summary>Unique edge chains split at junctions and endpoint berths, for one layer of drawn route ink.</summary>
    public IReadOnlyList<IReadOnlyList<GridPosition>> RenderRoutes { get; private set; } = Array.Empty<IReadOnlyList<GridPosition>>();

    public bool Contains(GridPosition from, GridPosition to) => _edges.Contains((from, to));
    public bool IsEmpty => _edges.Count == 0;

    internal static TradeNetwork Create(GameBoard board, GridPosition[] ports, IReadOnlySet<GridPosition> forbidden, int maximumRouteLength = 0)
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
            var search = PathSearch.Find(ports[i], maximumRouteLength > 0 ? maximumRouteLength : board.Tiles.Count, board.GetSurrounding, Cost);
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

        result.BuildRenderRoutes(ports);
        return result;
    }

    private static int Compare(GridPosition a, GridPosition b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
    private static (GridPosition, GridPosition) Edge(GridPosition a, GridPosition b) => Compare(a, b) < 0 ? (a, b) : (b, a);

    private void BuildRenderRoutes(IEnumerable<GridPosition> endpoints)
    {
        Edges = _edges.Where(e => Compare(e.Item1, e.Item2) < 0).Select(e => (From: e.Item1, To: e.Item2))
            .OrderBy(e => e.From.Y).ThenBy(e => e.From.X).ThenBy(e => e.To.Y).ThenBy(e => e.To.X).ToArray();
        var adjacency = new Dictionary<GridPosition, List<GridPosition>>();
        foreach (var (from, to) in Edges)
        {
            if (!adjacency.TryGetValue(from, out var first)) adjacency[from] = first = new();
            if (!adjacency.TryGetValue(to, out var second)) adjacency[to] = second = new();
            first.Add(to);
            second.Add(from);
        }
        foreach (var neighbors in adjacency.Values) neighbors.Sort(Compare);
        var stops = adjacency.Where(e => e.Value.Count != 2).Select(e => e.Key).Concat(endpoints).ToHashSet();
        var used = new HashSet<(GridPosition, GridPosition)>();
        var paths = new List<IReadOnlyList<GridPosition>>();
        void Trace(GridPosition from, GridPosition to)
        {
            if (!used.Add(Edge(from, to))) return;
            var path = new List<GridPosition> { from, to };
            var previous = from;
            var current = to;
            while (!stops.Contains(current))
            {
                var next = adjacency[current].First(p => p != previous);
                if (!used.Add(Edge(current, next))) break;
                path.Add(next);
                previous = current;
                current = next;
            }
            paths.Add(path.AsReadOnly());
        }
        foreach (var node in stops.Where(adjacency.ContainsKey).OrderBy(p => p.Y).ThenBy(p => p.X))
            foreach (var next in adjacency[node]) Trace(node, next);
        // A closed sea loop with no port/junction must also draw every edge once.
        foreach (var (from, to) in Edges) Trace(from, to);
        RenderRoutes = paths.AsReadOnly();
    }
}
