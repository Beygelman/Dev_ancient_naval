namespace DevAncientNaval.Core.Grid;

/// <summary>Stable cell address. On generated maps it is an ID; use GameBoard
/// for neighbors, distance and geometry. Rectangular fixtures retain XY rules.</summary>
public readonly record struct GridPosition(int X, int Y)
{
    public IEnumerable<GridPosition> OrthogonalNeighbors()
    {
        yield return new(X + 1, Y);
        yield return new(X, Y + 1);
        yield return new(X - 1, Y);
        yield return new(X, Y - 1);
    }
}
