using System.Numerics;
using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.World;

/// <summary>World-space interior of the great land, independent of tile address shape.</summary>
public static class PangaeaWaters
{
    public static bool IsInterior(GameBoard board, GridPosition cell)
    {
        if (board.Kind != WorldKind.Pangaea || board.Mesh is null) return false;
        var center = board.Boundary.Aggregate(Vector2.Zero, (sum, p) => sum + p) / board.Boundary.Count;
        var offset = board.Center(cell) - center;
        var direction = offset.LengthSquared() < .001f ? Vector2.UnitX : Vector2.Normalize(offset);
        float extent = board.Boundary.Max(p => Vector2.Dot(p - center, direction));
        return offset.Length() < extent * .51f;
    }
}
