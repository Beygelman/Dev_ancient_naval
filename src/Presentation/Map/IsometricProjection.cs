using System;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Grid integers describe diamond centers. Shared edges belong to the
/// cell on the positive side; no physics colliders or screen-space rounding.</summary>
public sealed class IsometricProjection
{
    public float TileWidth { get; }
    public float TileHeight { get; }

    public IsometricProjection(float tileWidth = 96, float tileHeight = 48)
    {
        if (!float.IsFinite(tileWidth) || tileWidth <= 0) throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (!float.IsFinite(tileHeight) || tileHeight <= 0) throw new ArgumentOutOfRangeException(nameof(tileHeight));
        TileWidth = tileWidth;
        TileHeight = tileHeight;
    }

    public Vector2 GridToWorld(GridPosition cell) =>
        new((cell.X - cell.Y) * TileWidth / 2, (cell.X + cell.Y) * TileHeight / 2);

    public GridPosition WorldToGrid(Vector2 point) => new(
        (int)MathF.Floor(point.X / TileWidth + point.Y / TileHeight + 0.5f),
        (int)MathF.Floor(point.Y / TileHeight - point.X / TileWidth + 0.5f));

    public Vector2[] Diamond(GridPosition cell)
    {
        var center = GridToWorld(cell);
        return new[] { center + new Vector2(0, -TileHeight / 2),
            center + new Vector2(TileWidth / 2, 0), center + new Vector2(0, TileHeight / 2),
            center + new Vector2(-TileWidth / 2, 0) };
    }

    public Rect2 BoardBounds(int width, int height) => new(
        -height * TileWidth / 2, -TileHeight / 2,
        (width + height) * TileWidth / 2, (width + height) * TileHeight / 2);
}
