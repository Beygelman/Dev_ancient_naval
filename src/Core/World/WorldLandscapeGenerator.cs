using System.Numerics;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>World-space coast shapes and edge-connected channels on the shared cell graph.</summary>
internal static class WorldLandscapeGenerator
{
    internal static GameBoard Create(OrganicMesh mesh, int seed, int factionCount, WorldKind kind, MapSize? mapSize = null, int? terrainSeed = null)
    {
        var empty = new GameBoard(mesh.Width, mesh.Height, _ => TerrainType.Water, seed,
            mesh.Faces.ContainsKey, mesh.Boundary, mesh, kind, mapSize);
        var layout = new Landscape(empty, terrainSeed ?? seed, factionCount);
        var land = kind switch
        {
            WorldKind.SeaWorld => layout.SmallIslands(),
            WorldKind.Continents => layout.Continents(),
            WorldKind.Pangaea => layout.Pangaea(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        ConnectWater(mesh, land);
        return new GameBoard(mesh.Width, mesh.Height,
            p => land.Contains(p) ? TerrainType.Land : TerrainType.Water, seed,
            mesh.Faces.ContainsKey, mesh.Boundary, mesh, kind, mapSize);
    }

    private sealed class Landscape
    {
        private readonly GameBoard _board;
        private readonly OrganicMesh _mesh;
        private readonly Random _random;
        private readonly int _factions;
        private readonly Vector2 _center;
        private readonly Vector2[] _anchors;
        private readonly GridPosition[] _candidates;
        private readonly HashSet<GridPosition> _allowed;

        internal Landscape(GameBoard board, int seed, int factions)
        {
            _board = board;
            _mesh = board.Mesh!;
            _random = new Random(seed ^ 0x517A03);
            _factions = factions;
            _center = _mesh.Boundary.Aggregate(Vector2.Zero, (sum, p) => sum + p) / 6;
            _anchors = Enumerable.Range(0, factions).Select(i => board.Center(board.FleetAnchor(i, factions))).ToArray();
            var border = _mesh.Faces.Keys.Where(p => _mesh.Neighbors(p).Count < _mesh.Faces[p].Count).ToHashSet();
            _candidates = _mesh.Faces.Keys.Where(p => !border.Contains(p) &&
                !_mesh.Neighbors(p, true).Any(border.Contains) &&
                _anchors.All(a => Vector2.DistanceSquared(a, _mesh.Centers[p]) >= 4.6f * 4.6f)).ToArray();
            _allowed = _candidates.ToHashSet();
        }

        internal HashSet<GridPosition> SmallIslands()
        {
            var land = new HashSet<GridPosition>();
            // Strung along a few sinuous directions, with water gaps between the islets.
            // The surrounding-vertex exclusion also prevents diagonal land contacts.
            for (int index = 0; index < _factions * 8; index++)
            {
                int territory = index % _factions;
                double phase = territory * Math.Tau / _factions + _random.NextDouble() * .8;
                var axis = new Vector2((float)Math.Cos(phase), (float)Math.Sin(phase));
                var available = _candidates.Where(p => _board.StartingTerritory(p, _factions) == territory &&
                    !Touches(p, land)).ToArray();
                if (available.Length == 0)
                    continue;
                var target = _mesh.Centers[available[_random.Next(available.Length)]];
                var origin = available.MinBy(p => Math.Abs(Vector2.Dot(_mesh.Centers[p] - target,
                    new Vector2(-axis.Y, axis.X))) + Vector2.Distance(_mesh.Centers[p], target) * .14f);
                AddIslet(origin, 1 + _random.Next(3), land, axis);
            }
            return land;
        }

        internal HashSet<GridPosition> Continents()
        {
            var land = new HashSet<GridPosition>();
            for (int index = 0; index < _factions * 2; index++)
            {
                int territory = index % _factions;
                var origins = _candidates.Where(p => _board.StartingTerritory(p, _factions) == territory &&
                    !Touches(p, land)).OrderBy(_ => _random.Next()).ToArray();
                foreach (var origin in origins)
                {
                    var island = Grow(origin, 28 + _random.Next(3), land);
                    if (island.Count < 20)
                        continue;
                    // A winding shallow channel runs inland from the sea; wider basins
                    // create deltas/lakes without imposing rectangular address geometry.
                    CutRiver(island, _mesh.Centers[origin], _random.NextDouble() * Math.Tau, 1);
                    if (island.Count < 15)
                        continue;
                    land.UnionWith(island);
                    break;
                }
            }
            AddIslandChains(land, _factions * 3);
            return land;
        }

        internal HashSet<GridPosition> Pangaea()
        {
            double phase = _random.NextDouble() * Math.Tau;
            var land = _candidates.Where(p =>
            {
                var delta = _mesh.Centers[p] - _center;
                double angle = Math.Atan2(delta.Y, delta.X);
                var axis = Vector2.Normalize(delta.LengthSquared() < .001f ? Vector2.UnitX : delta);
                double extent = _mesh.Boundary.Max(v => Vector2.Dot(v - _center, axis));
                double shape = .66 + .055 * Math.Sin(3 * angle + phase) + .035 * Math.Cos(5 * angle - phase);
                return delta.Length() < extent * shape;
            }).ToHashSet();
            float radius = _mesh.Boundary.Min(p => Vector2.Distance(p, _center));
            int count = Math.Max(4, _factions);
            var lakes = new Vector2[count];
            CarveLake(land, _center, 1.6f);
            for (int i = 0; i < count; i++)
            {
                double angle = phase + i * Math.Tau / count;
                var axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                lakes[i] = _center + axis * radius * (.27f + (float)_random.NextDouble() * .1f);
                CarveLake(land, lakes[i], 1.4f + (float)_random.NextDouble() * .65f);
                CarveMeander(land, _center, lakes[i], .8f);
                // Tributaries end inland; separate mouths connect the network to the sea.
                CarveMeander(land, lakes[i], lakes[i] + new Vector2(-axis.Y, axis.X) * radius * .12f, .45f);
                if (i % 2 == 0 || i == count - 1)
                    CarveMeander(land, lakes[i], _center + axis * radius * .83f, 1.1f);
            }
            // Narrow winding bypasses join neighboring basins and create route choices.
            for (int i = 0; i < count; i += 2)
                CarveMeander(land, lakes[i], lakes[(i + 1) % count], 1.2f);
            AddIslandChains(land, _factions * 2);
            return land;
        }

        private void CarveLake(HashSet<GridPosition> land, Vector2 center, float radius)
        {
            double phase = _random.NextDouble() * Math.Tau;
            foreach (var p in _candidates)
            {
                var delta = _mesh.Centers[p] - center;
                double edge = radius * (1 + .18 * Math.Sin(Math.Atan2(delta.Y, delta.X) * 3 + phase));
                if (delta.Length() < edge) land.Remove(p);
            }
        }

        private void CarveMeander(HashSet<GridPosition> land, Vector2 from, Vector2 to, float bend)
        {
            var across = Vector2.Normalize(to - from);
            across = new Vector2(-across.Y, across.X);
            float length = Vector2.Distance(from, to);
            double phase = _random.NextDouble() * Math.Tau;
            GridPosition? previous = null;
            for (float t = 0; t <= 1.001f; t += 1 / Math.Max(2, length * 2))
            {
                var point = Vector2.Lerp(from, to, t) + across * (float)(Math.Sin(t * Math.PI) * Math.Sin(t * 7 + phase) * bend);
                var cell = _mesh.Faces.Keys.MinBy(p => Vector2.DistanceSquared(_mesh.Centers[p], point));
                foreach (var step in previous is { } last ? EdgePath(_mesh, last, cell) : new[] { cell })
                    land.Remove(step);
                previous = cell;
            }
        }

        private HashSet<GridPosition> Grow(GridPosition origin, int count, HashSet<GridPosition> existing)
        {
            var result = new HashSet<GridPosition>();
            var frontier = new HashSet<GridPosition> { origin };
            var center = _mesh.Centers[origin];
            double angle = _random.NextDouble() * Math.Tau, phase = _random.NextDouble() * Math.Tau;
            var axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            while (result.Count < count && frontier.Count > 0)
            {
                double Score(GridPosition p)
                {
                    var delta = _mesh.Centers[p] - center;
                    double u = Vector2.Dot(delta, axis), v = Vector2.Dot(delta, new Vector2(-axis.Y, axis.X));
                    double theta = Math.Atan2(v, u);
                    double coast = 1 + .14 * Math.Sin(theta * 3 + phase) + .07 * Math.Cos(theta * 5 - phase);
                    return (u * u * .8 + v * v * 1.2) / (coast * coast);
                }
                var next = frontier.MinBy(Score);
                frontier.Remove(next);
                if (!_allowed.Contains(next) || Touches(next, existing) || !result.Add(next))
                    continue;
                foreach (var neighbor in _mesh.Neighbors(next))
                    if (_allowed.Contains(neighbor) && !result.Contains(neighbor) && !Touches(neighbor, existing))
                        frontier.Add(neighbor);
            }
            return result;
        }

        private void CutRiver(HashSet<GridPosition> land, Vector2 source, double angle, int width)
        {
            var axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            var across = new Vector2(-axis.Y, axis.X);
            double length = _mesh.Boundary.Max(p => Vector2.Dot(p - source, axis));
            double phase = _random.NextDouble() * Math.Tau;
            // Sample a continuous meander, then connect successive samples by shared
            // edges; a visual river cannot jump diagonally across a closed land corner.
            var samples = new List<GridPosition>();
            for (float t = 0; t <= length + 1; t += .65f)
            {
                var point = source + axis * t + across * (float)(Math.Sin(t * .48 + phase) * .75);
                samples.Add(_mesh.Faces.Keys.MinBy(p => Vector2.DistanceSquared(_mesh.Centers[p], point)));
            }
            for (int index = 0; index < samples.Count; index++)
            {
                var channel = index == 0 ? new[] { samples[index] } : EdgePath(_mesh, samples[index - 1], samples[index]);
                foreach (var cell in channel)
                {
                    land.Remove(cell);
                    if (width > 1 && index % 3 != 0)
                    {
                        var second = _mesh.Neighbors(cell).MinBy(p => Vector2.DistanceSquared(_mesh.Centers[p],
                            _mesh.Centers[cell] + across * .6f));
                        land.Remove(second);
                    }
                }
            }
        }

        private void AddIslandChains(HashSet<GridPosition> land, int count)
        {
            for (int index = 0; index < count; index++)
            {
                int territory = index % _factions;
                var available = _candidates.Where(p => _board.StartingTerritory(p, _factions) == territory &&
                    !Touches(p, land)).ToArray();
                if (available.Length == 0)
                    continue;
                var origin = available[_random.Next(available.Length)];
                double angle = _random.NextDouble() * Math.Tau;
                AddIslet(origin, 1 + _random.Next(3), land, new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
            }
        }

        private void AddIslet(GridPosition origin, int count, HashSet<GridPosition> land, Vector2 axis)
        {
            var island = new HashSet<GridPosition> { origin };
            while (island.Count < count)
            {
                var candidates = island.SelectMany(p => _mesh.Neighbors(p)).Distinct().Where(p =>
                    _allowed.Contains(p) && !island.Contains(p) && !Touches(p, land)).ToArray();
                if (candidates.Length == 0)
                    break;
                var target = _mesh.Centers[origin] + axis * island.Count * .8f;
                island.Add(candidates.MinBy(p => Vector2.DistanceSquared(_mesh.Centers[p], target)));
            }
            land.UnionWith(island);
        }

        private bool Touches(GridPosition p, HashSet<GridPosition> land) => land.Contains(p) ||
            _mesh.Neighbors(p, true).Any(land.Contains);
    }

    private static IReadOnlyList<GridPosition> EdgePath(OrganicMesh mesh, GridPosition from, GridPosition to)
    {
        var pending = new Queue<GridPosition>();
        var previous = new Dictionary<GridPosition, GridPosition> { [from] = from };
        pending.Enqueue(from);
        while (pending.TryDequeue(out var p) && !previous.ContainsKey(to))
            foreach (var n in mesh.Neighbors(p))
                if (previous.TryAdd(n, p))
                    pending.Enqueue(n);
        var path = new List<GridPosition> { to };
        while (path[^1] != from)
            path.Add(previous[path[^1]]);
        path.Reverse();
        return path;
    }

    private static void ConnectWater(OrganicMesh mesh, HashSet<GridPosition> land)
    {
        var remaining = mesh.Faces.Keys.Where(p => !land.Contains(p)).ToHashSet();
        var components = new List<HashSet<GridPosition>>();
        while (remaining.Count > 0)
        {
            var component = new HashSet<GridPosition>();
            var pending = new Queue<GridPosition>();
            pending.Enqueue(remaining.First());
            while (pending.TryDequeue(out var p))
            {
                if (!remaining.Remove(p))
                    continue;
                component.Add(p);
                foreach (var n in mesh.Neighbors(p))
                    if (remaining.Contains(n))
                        pending.Enqueue(n);
            }
            components.Add(component);
        }
        var ocean = components.MaxBy(c => c.Count)!;
        foreach (var lake in components.Where(c => c != ocean))
        {
            var frontier = new PriorityQueue<GridPosition, int>();
            var costs = new Dictionary<GridPosition, int>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            foreach (var p in lake)
            {
                costs[p] = 0;
                frontier.Enqueue(p, 0);
            }
            GridPosition? mouth = null;
            while (frontier.TryDequeue(out var p, out int cost))
            {
                if (costs[p] != cost)
                    continue;
                if (ocean.Contains(p))
                {
                    mouth = p;
                    break;
                }
                foreach (var n in mesh.Neighbors(p))
                {
                    int next = cost + (land.Contains(n) ? 10 : 1);
                    if (costs.TryGetValue(n, out int old) && old <= next)
                        continue;
                    costs[n] = next;
                    previous[n] = p;
                    frontier.Enqueue(n, next);
                }
            }
            if (mouth is not { } cell)
                throw new InvalidOperationException("The generated world has no navigable connection to its ocean.");
            while (!lake.Contains(cell))
            {
                land.Remove(cell);
                ocean.Add(cell);
                cell = previous[cell];
            }
            ocean.UnionWith(lake);
        }
    }
}
