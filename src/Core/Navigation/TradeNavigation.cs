using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Navigation;
/// <summary>A route remembers its lane run, because progressive speed is not a
/// property of a cell alone. Ordinary voyages keep the original fast search.</summary>
internal sealed class NavigationRoutes
{
    internal Dictionary<GridPosition, int> Costs { get; }

    private readonly Func<GridPosition, IReadOnlyList<GridPosition>> _path;
    internal NavigationRoutes(Dictionary<GridPosition, int> costs, Func<GridPosition, IReadOnlyList<GridPosition>> path)
    {
        Costs = costs;
        _path = path;
    }

    internal IReadOnlyList<GridPosition> PathTo(GridPosition cell) => Costs.ContainsKey(cell) ? _path(cell) : Array.Empty<GridPosition>();
}

internal sealed class TradeNavigation
{
    private readonly TradeNetwork _network;
    private readonly int _speed, _period;
    internal TradeNavigation(TradeNetwork network, Ship ship, PortRules rules)
    {
        _network = network;
        _speed = ship.MovementAllowance;
        _period = _speed + Math.Max(rules.MinimumBonus, (int)Math.Ceiling(_speed * rules.MovementBonus));
    }

    // Cumulative rounding prevents a sequence of individually rounded discounts
    // from losing the promised two tiles. The first two edges accelerate gently.
    private int Cumulative(int run) => (int)Math.Ceiling((_speed * 10.0 + 4) * run / _period + (run == 1 ? 1.5 : run == 2 ? 1 : 0) - 1e-9);
    internal (int Cost, int Run) Step(GridPosition from, GridPosition to, int raw, int run)
    {
        if (!_network.Contains(from, to))
            return (raw, 0);
        int cost = raw - 10 + Cumulative(run + 1) - Cumulative(run);
        int next = run + 1;
        if (next >= 3 + _period)
            next -= _period;
        return (Math.Max(1, cost), next);
    }

    private readonly record struct State(GridPosition Cell, int Run);
    internal NavigationRoutes Find(GameBoard board, Ship ship, int budget, NavalNavigationQuery query)
    {
        var origin = new State(ship.Position, ship.TradeStreak);
        var costs = new Dictionary<State, int>
        {
            [origin] = 0
        };
        var previous = new Dictionary<State, State>();
        var best = new Dictionary<GridPosition, State>
        {
            [origin.Cell] = origin
        };
        var flatCosts = new Dictionary<GridPosition, int>
        {
            [origin.Cell] = 0
        };
        var pending = new PriorityQueue<State, (int Cost, int Order)>();
        int order = 0;
        pending.Enqueue(origin, (0, order++));
        while (pending.TryDequeue(out var current, out var priority))
        {
            if (priority.Cost != costs[current])
                continue;
            foreach (var cell in board.GetSurrounding(current.Cell))
            {
                if (query.StepCost(current.Cell, cell)is not { } raw)
                    continue;
                var step = Step(current.Cell, cell, raw, current.Run);
                int cost = priority.Cost + step.Cost;
                var next = new State(cell, step.Run);
                if (cost > budget || costs.TryGetValue(next, out int old) && old <= cost)
                    continue;
                costs[next] = cost;
                previous[next] = current;
                pending.Enqueue(next, (cost, order++));
                if (!flatCosts.TryGetValue(cell, out int flat) || cost < flat)
                {
                    flatCosts[cell] = cost;
                    best[cell] = next;
                }
            }
        }

        IReadOnlyList<GridPosition> Path(GridPosition cell)
        {
            var state = best[cell];
            var path = new List<GridPosition>
            {
                state.Cell
            };
            while (state != origin)
            {
                state = previous[state];
                path.Add(state.Cell);
            }

            path.Reverse();
            return path;
        }

        return new(flatCosts, Path);
    }
}
