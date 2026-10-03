using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

public sealed class GameBoard
{
    private readonly Tile[] _tiles;
    private readonly bool[] _playable;
    public IReadOnlyList<System.Numerics.Vector2> Boundary { get; }
    public int Seed { get; }
    public WorldKind Kind { get; }
    public MapSize? MapSize { get; }
    public int Width { get; }
    public int Height { get; }
    public OrganicMesh? Mesh { get; }
    public IReadOnlyList<Tile> Tiles { get; }

    public GameBoard(int width, int height, Func<GridPosition, TerrainType> terrainAt, int seed = 0,
        Func<GridPosition, bool>? playable = null, IReadOnlyList<System.Numerics.Vector2>? boundary = null, OrganicMesh? mesh = null,
        WorldKind kind = WorldKind.Oceans, MapSize? mapSize = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(terrainAt);
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (mapSize is { } size && !Enum.IsDefined(size))
            throw new ArgumentOutOfRangeException(nameof(mapSize));
        Kind = kind;
        MapSize = mapSize;
        Seed = seed; Width = width; Mesh = mesh;
        Height = height;
        _tiles = new Tile[checked(width * height)];
        _playable = new bool[_tiles.Length];
        Boundary = boundary?.ToArray() ?? Array.Empty<System.Numerics.Vector2>();
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            _playable[y * width + x] = playable?.Invoke(new(x,y)) ?? true;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var position = new GridPosition(x, y);
            var terrain = terrainAt(position);
            if (!Enum.IsDefined(terrain))
                throw new ArgumentOutOfRangeException(nameof(terrainAt));
            _tiles[y * width + x] = new Tile(position, terrain);
        }
        // One-cell ring of shallow coastal water, including diagonal shores.
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var p = new GridPosition(x, y);
            if (Contains(p) && _tiles[y * width + x].Terrain == TerrainType.Water &&
                GetSurrounding(p).Any(n => _tiles[n.Y * width + n.X].Terrain == TerrainType.Land))
                _tiles[y * width + x] = new Tile(p, TerrainType.Coast);
        }
        Tiles = Array.AsReadOnly(_tiles.Where(t => Contains(t.Position)).ToArray());
    }

    public int StartingTerritory(GridPosition p)
    {
        return StartingTerritory(p, 2);
    }

    public int StartingTerritory(GridPosition p, int factionCount)
    {
        ValidateFactionCount(factionCount);
        var point = Center(p);
        int closest = 0;
        float distance = float.MaxValue;
        for (int index = 0; index < factionCount; index++)
        {
            float next = System.Numerics.Vector2.DistanceSquared(point, Center(FleetAnchor(index, factionCount)));
            if (next >= distance) continue;
            closest = index;
            distance = next;
        }
        return closest;
    }

    /// <summary>A face touching the outside boundary, rather than a coastal tile.</summary>
    public bool IsOuterCell(GridPosition position) => Contains(position) && (Mesh is { } mesh
        ? mesh.Neighbors(position).Count < mesh.Faces[position].Count
        : position.OrthogonalNeighbors().Any(p => !Contains(p)));

    public bool Contains(GridPosition position) =>
        position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height && _playable[position.Y * Width + position.X];

    private readonly GridPosition?[] _anchors = new GridPosition?[2];
    private readonly Dictionary<(int Index, int Count), GridPosition> _factionAnchors = new();

    public GridPosition FleetAnchor(int factionIndex, int factionCount)
    {
        ValidateFactionCount(factionCount);
        if (factionIndex < 0 || factionIndex >= factionCount)
            throw new ArgumentOutOfRangeException(nameof(factionIndex));
        if (factionCount == 2) return FleetAnchor(factionIndex == 1);
        if (_factionAnchors.TryGetValue((factionIndex, factionCount), out var existing)) return existing;
        var center = Mesh is null ? new System.Numerics.Vector2(Width / 2f, Height / 2f) :
            Boundary.Aggregate(System.Numerics.Vector2.Zero, (sum, point) => sum + point) / Boundary.Count;
        var random = new Random(Seed ^ 0x57A1);
        int first = random.Next(6);
        var baseAxis = Mesh is null ? new System.Numerics.Vector2(-1, 0) :
            System.Numerics.Vector2.Normalize((Boundary[first] + Boundary[(first + 1) % 6]) * .5f - center);
        double angle = Math.Atan2(baseAxis.Y, baseAxis.X) + Math.Tau * factionIndex / factionCount;
        var axis = new System.Numerics.Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        float extent = Mesh is null ? Math.Min(Width, Height) * .5f : Boundary.Max(point => System.Numerics.Vector2.Dot(point - center, axis));
        var target = center + axis * extent * .70f;
        var position = Tiles.Where(tile => tile.Terrain != TerrainType.Land && !IsNarrowPassage(tile.Position))
            .OrderBy(tile => System.Numerics.Vector2.DistanceSquared(Center(tile.Position), target))
            .ThenBy(tile => tile.Position.X).ThenBy(tile => tile.Position.Y).First().Position;
        _factionAnchors[(factionIndex, factionCount)] = position;
        return position;
    }

    private static void ValidateFactionCount(int factionCount)
    {
        if (factionCount is < 2 or > 5) throw new ArgumentOutOfRangeException(nameof(factionCount));
    }
    public GridPosition FleetAnchor(bool right)
    {
        int index=right?1:0;
        if(_anchors[index] is { } existing) return existing;
        var position=ComputeFleetAnchor(right); _anchors[index]=position; return position;
    }
    private GridPosition ComputeFleetAnchor(bool right)
    {
        if (Mesh is not null)
        {
            var random = new Random(Seed ^ 0x57A1);
            int first = random.Next(6), second = (first + 3) % 6;
            int side = right ? second : first;
            var center = Mesh.Boundary.Aggregate(System.Numerics.Vector2.Zero,(a,b)=>a+b)/6;
            var midpoint=(Mesh.Boundary[first]+Mesh.Boundary[(first+1)%6])*.5f;
            var axis=System.Numerics.Vector2.Normalize(midpoint-center)*(right?-1:1);
            float extent=Mesh.Boundary.Max(p=>System.Numerics.Vector2.Dot(p-center,axis));
            var target=center+axis*extent*.68f;
            return Tiles.Where(t => t.Terrain != TerrainType.Land).OrderBy(t => System.Numerics.Vector2.DistanceSquared(Mesh.Centers[t.Position], target)).First().Position;
        }
        int y = Height / 2;
        var row = Tiles.Where(t => t.Position.Y == y).Select(t => t.Position.X).ToArray();
        return new(right ? row.Max() - 3 : row.Min() + 3, y);
    }

    public Tile GetTile(GridPosition position) => Contains(position)
        ? _tiles[position.Y * Width + position.X]
        : throw new ArgumentOutOfRangeException(nameof(position), position, "Outside board.");

    public bool TryGetTile(GridPosition position, out Tile? tile)
    {
        tile = Contains(position) ? _tiles[position.Y * Width + position.X] : null;
        return tile is not null;
    }

    public IEnumerable<GridPosition> GetNeighbors(GridPosition position)
    {
        if (!Contains(position)) yield break;
        if (Mesh is not null) { foreach (var p in Mesh.Neighbors(position)) yield return p; yield break; }
        foreach (var neighbor in position.OrthogonalNeighbors())
            if (Contains(neighbor)) yield return neighbor;
    }

    public IEnumerable<GridPosition> GetSurrounding(GridPosition position)
    {
        if (Mesh is not null) { foreach (var p in Mesh.Neighbors(position, true)) yield return p; yield break; }
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var next = new GridPosition(position.X + dx, position.Y + dy);
            if ((dx != 0 || dy != 0) && Contains(next)) yield return next;
        }
    }

    public bool IsNarrowPassage(GridPosition p) => IsNarrowAt(p, cell => Contains(cell) && GetTile(cell).Terrain == TerrainType.Land);

    public bool IsNarrowAt(GridPosition p, Func<GridPosition, bool> isLand)
    {
        if (Mesh is null) return IsNarrowPassage(p, isLand);
        var banks = GetNeighbors(p).Where(isLand).ToArray();
        return banks.Any(a => banks.Any(b => a != b && System.Numerics.Vector2.Dot(
            System.Numerics.Vector2.Normalize(Mesh.Centers[a] - Mesh.Centers[p]),
            System.Numerics.Vector2.Normalize(Mesh.Centers[b] - Mesh.Centers[p])) < -.45f));
    }

    public double Distance(GridPosition a, GridPosition b) => Mesh?.Steps(a, b) ?? Math.Sqrt((double)(a.X-b.X)*(a.X-b.X)+(double)(a.Y-b.Y)*(a.Y-b.Y));
    public bool InRadius(GridPosition a, GridPosition b, int radius) => Mesh is not null ? Mesh.Steps(a, b) <= radius :
        DevAncientNaval.Core.Vision.BattleVision.InRadius(a, b, radius);
    public System.Numerics.Vector2 Center(GridPosition p) => Mesh is null ? new(p.X, p.Y) : Mesh.Centers[p];
    public GridPosition CentralCell => Tiles.MinBy(t => System.Numerics.Vector2.DistanceSquared(Center(t.Position),
        Mesh is null ? new(Width/2f, Height/2f) : Mesh.Boundary.Aggregate(System.Numerics.Vector2.Zero, (a,b) => a+b)/6))!.Position;
    public IReadOnlyList<GridPosition> HarborCells(bool right, int count = 3)
    {
        return HarborCells(right ? 1 : 0, 2, count);
    }

    public IReadOnlyList<GridPosition> HarborCells(int factionIndex, int factionCount, int count = 3)
    {
        var anchor = FleetAnchor(factionIndex, factionCount);
        return Tiles.Where(t => t.Terrain != TerrainType.Land && !IsNarrowPassage(t.Position))
            .OrderBy(t => Distance(anchor, t.Position)).ThenBy(t => System.Numerics.Vector2.DistanceSquared(Center(anchor), Center(t.Position)))
            .Take(count).Select(t => t.Position).ToArray();
    }
    public IReadOnlyCollection<GridPosition> BlastCells(GridPosition p) => Mesh is null
        ? GetSurrounding(p).Append(p).ToArray()
        : Tiles.OrderBy(t => Mesh.Steps(p, t.Position)).ThenBy(t => System.Numerics.Vector2.DistanceSquared(Center(p), Center(t.Position))).Take(9).Select(t => t.Position).ToArray();

    public static bool IsNarrowPassage(GridPosition p, Func<GridPosition, bool> isLand) =>
        (isLand(new(p.X - 1, p.Y)) && isLand(new(p.X + 1, p.Y))) ||
        (isLand(new(p.X, p.Y - 1)) && isLand(new(p.X, p.Y + 1)));
}
