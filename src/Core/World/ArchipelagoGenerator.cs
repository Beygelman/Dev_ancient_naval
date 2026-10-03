using DevAncientNaval.Core.Grid;
using System.Numerics;

namespace DevAncientNaval.Core.World;
/// <summary>Seeded islands on a boundary-fitted hexagonal cell complex.</summary>
public static class ArchipelagoGenerator
{
    public static GameBoard Create(int seed, int opponentCount = 3, WorldKind kind = WorldKind.Oceans, MapSize? mapSize = null)
    {
        if (opponentCount is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(opponentCount));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (mapSize is { } requestedSize && !Enum.IsDefined(requestedSize))
            throw new ArgumentOutOfRangeException(nameof(mapSize));
        float scale = mapSize switch
        {
            MapSize.Lake => .87f, MapSize.Bay => 1f, MapSize.Sea => 1.12f, MapSize.Ocean => 1.25f,
            _ => MathF.Sqrt((opponentCount + 1) / 4f)
        };
        var mesh = OrganicMesh.Create(seed, scale);
        if (mapSize is null)
            return kind == WorldKind.Oceans
                ? OceanBoard(mesh, seed, opponentCount, scale, mapSize, seed)
                : WorldLandscapeGenerator.Create(mesh, seed, opponentCount + 1, kind);
        // A chosen area must serve up to five fleets without reducing coastal-town
        // quotas or clearance. Retry only the landscape, keeping this exact mesh,
        // original world seed and map-size metadata stable.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            int terrainSeed = unchecked(seed + attempt * 104729);
            var board = kind == WorldKind.Oceans
                ? OceanBoard(mesh, seed, opponentCount, scale, mapSize, terrainSeed)
                : WorldLandscapeGenerator.Create(mesh, seed, opponentCount + 1, kind, mapSize, terrainSeed);
            try
            {
                if (WorldSettlementPlacement.Create(board, opponentCount + 1).Count == (opponentCount + 1) * 3)
                    return board;
            }
            catch (InvalidOperationException)
            {
                // The terrain candidate has too little separated coastline.
            }
        }
        throw new InvalidOperationException("Cannot generate a fair coastline for this map size.");
    }

    private static GameBoard OceanBoard(OrganicMesh mesh, int seed, int opponentCount,
        float scale, MapSize? mapSize, int terrainSeed)
    {
        var random = new Random(terrainSeed ^ 91417);
        GameBoard Board(HashSet<GridPosition> land) => new(mesh.Width, mesh.Height, p => land.Contains(p) ? TerrainType.Land : TerrainType.Water, seed, mesh.Faces.ContainsKey, mesh.Boundary, mesh, WorldKind.Oceans, mapSize);
        var water = Board(new());
        var anchors = Enumerable.Range(0, opponentCount + 1).Select(index => water.Center(water.FleetAnchor(index, opponentCount + 1))).ToArray();
        var center = mesh.Boundary.Aggregate(Vector2.Zero, (a, b) => a + b) / 6;
        var land = new HashSet<GridPosition>();
        var border = mesh.Faces.Keys.Where(p => mesh.Neighbors(p).Count < mesh.Faces[p].Count).ToHashSet();
        var candidates = mesh.Faces.Keys.Where(p => !border.Contains(p) && !mesh.Neighbors(p, true).Any(border.Contains)).ToArray();
        int factionCount = opponentCount + 1;
        var territoryCandidates = Enumerable.Range(0, factionCount).Select(index => candidates.Where(position => water.StartingTerritory(position, factionCount) == index).ToArray()).ToArray();
        bool Reserved(Vector2 p) => anchors.Any(anchor => Vector2.Distance(p, anchor) < 4.6f) || opponentCount == 1 && Math.Abs(Vector2.Dot(p - center, new Vector2(-(anchors[1] - anchors[0]).Y, (anchors[1] - anchors[0]).X)) / Vector2.Distance(anchors[0], anchors[1])) < 1.5f;
        int accepted = 0, desired = (int)Math.Round((16 + random.Next(3)) * scale * scale);
        for (int attempt = 0; attempt < 700 && accepted < desired; attempt++)
        {
            var territory = territoryCandidates[accepted % factionCount];
            if (territory.Length == 0)
                territory = candidates;
            var origin = mesh.Centers[territory[random.Next(territory.Length)]];
            double size = accepted < factionCount ? 3.4 + random.NextDouble() * 1.8 : accepted < factionCount * 2 ? 2.2 + random.NextDouble() * 1.5 : .9 + random.NextDouble();
            double ry = size * (.7 + random.NextDouble() * .5), angle = random.NextDouble() * Math.PI, phase = random.NextDouble() * 6;
            var remaining = new HashSet<GridPosition>();
            foreach (var p in candidates)
            {
                var c = mesh.Centers[p];
                if (Reserved(c))
                    continue;
                double dx = c.X - origin.X, dy = c.Y - origin.Y;
                double u = (dx * Math.Cos(angle) + dy * Math.Sin(angle)) / size, v = (-dx * Math.Sin(angle) + dy * Math.Cos(angle)) / ry;
                double a = Math.Atan2(v, u), coast = 1 + .22 * Math.Sin(3 * a + phase) + .12 * Math.Cos(5 * a - phase);
                if (u * u + v * v + .09 * Math.Sin(dx * .55 + phase) * Math.Cos(dy * .5 - phase) < coast * coast)
                    remaining.Add(p);
            }

            var largest = new HashSet<GridPosition>();
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

                if (component.Count > largest.Count)
                    largest = component;
            }

            if (largest.Count == 0 || accepted < factionCount && largest.Count < 8)
                continue;
            if (largest.Any(p => mesh.Neighbors(p, true).Any(n => land.Contains(n) || mesh.Neighbors(n, true).Any(land.Contains))))
                continue;
            land.UnionWith(largest);
            accepted++;
        }

        bool widened;
        do
        {
            widened = false;
            foreach (var p in mesh.Faces.Keys)
            {
                if (land.Contains(p) || !water.IsNarrowAt(p, land.Contains))
                    continue;
                var bank = mesh.Neighbors(p).Where(land.Contains).MinBy(q => mesh.Neighbors(q).Count(land.Contains));
                land.Remove(bank);
                widened = true;
            }
        }
        while (widened);
        return Board(land);
    }
}
