using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;

/// <summary>Fixed visual test fixture; deliberately not a procedural generator.</summary>
internal static class PrototypeBoard
{
    public static GameBoard Create() => new(20, 20, p =>
        (p.X >= 4 && p.X <= 7 && p.Y >= 5 && p.Y <= 7) ||
        (p.X >= 12 && p.X <= 14 && p.Y >= 11 && p.Y <= 15) ||
        (p.X == 11 && p.Y >= 12 && p.Y <= 14)
            ? TerrainType.Land : TerrainType.Water);
}
