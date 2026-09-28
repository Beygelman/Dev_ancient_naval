using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

public sealed class GameBoard
{
    private readonly Tile[] _tiles;
    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Tile> Tiles { get; }

    public GameBoard(int width, int height, Func<GridPosition, TerrainType> terrainAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(terrainAt);
        Width = width;
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
}
