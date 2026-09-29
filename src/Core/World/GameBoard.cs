using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

public sealed class GameBoard
{
    private readonly Tile[] _tiles;
    public int Seed { get; }
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Tile> Tiles { get; }

    public GameBoard(int width, int height, Func<GridPosition, TerrainType> terrainAt, int seed = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(terrainAt);
        Seed = seed; Width = width;
        Height = height;
        _tiles = new Tile[checked(width * height)];
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
            if (_tiles[y * width + x].Terrain == TerrainType.Water &&
                GetSurrounding(p).Any(n => _tiles[n.Y * width + n.X].Terrain == TerrainType.Land))
                _tiles[y * width + x] = new Tile(p, TerrainType.Coast);
        }
        Tiles = Array.AsReadOnly(_tiles);
    }

    public bool Contains(GridPosition position) =>
        position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;

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
        foreach (var neighbor in position.OrthogonalNeighbors())
            if (Contains(neighbor)) yield return neighbor;
    }

    public IEnumerable<GridPosition> GetSurrounding(GridPosition position)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var next = new GridPosition(position.X + dx, position.Y + dy);
            if ((dx != 0 || dy != 0) && Contains(next)) yield return next;
        }
    }

    public bool IsNarrowPassage(GridPosition p) => IsNarrowPassage(p,
        cell => Contains(cell) && GetTile(cell).Terrain == TerrainType.Land);

    public static bool IsNarrowPassage(GridPosition p, Func<GridPosition, bool> isLand) =>
        (isLand(new(p.X - 1, p.Y)) && isLand(new(p.X + 1, p.Y))) ||
        (isLand(new(p.X, p.Y - 1)) && isLand(new(p.X, p.Y + 1)));
}
