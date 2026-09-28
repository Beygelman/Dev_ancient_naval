namespace DevAncientNaval.Core.Grid;

/// <summary>Logical square-grid coordinates, independent of the renderer.</summary>
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
