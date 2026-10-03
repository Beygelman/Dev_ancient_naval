using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.Navigation;

/// <summary>Immutable routes for one observed battle state. A caller may reuse
/// this across cursor positions, but must discard it after gameplay or fog changes.</summary>
public sealed class MovementPreview
{
    public IReadOnlyDictionary<GridPosition, int> Costs { get; }
    private readonly IReadOnlyDictionary<GridPosition, GridPosition> _previous;
    private readonly GridPosition _origin;
<<<<<<< Updated upstream
    private readonly Func<GridPosition, IReadOnlyList<GridPosition>>? _paths;
    internal MovementPreview(NavigationRoutes routes)
    {
        Costs = new System.Collections.ObjectModel.ReadOnlyDictionary<GridPosition, int>(routes.Costs);
        _previous = new Dictionary<GridPosition, GridPosition>();
        _paths = routes.PathTo;
    }
=======
>>>>>>> Stashed changes
    internal MovementPreview(GridPosition origin, Dictionary<GridPosition, int> costs,
        Dictionary<GridPosition, GridPosition> previous)
    {
        _origin = origin;
        Costs = new System.Collections.ObjectModel.ReadOnlyDictionary<GridPosition, int>(costs);
        _previous = new System.Collections.ObjectModel.ReadOnlyDictionary<GridPosition, GridPosition>(previous);
    }
    public IReadOnlyList<GridPosition> PathTo(GridPosition destination) => Costs.ContainsKey(destination)
<<<<<<< Updated upstream
        ? _paths?.Invoke(destination) ?? PathSearch.Reconstruct(_origin, destination, _previous) : Array.Empty<GridPosition>();
=======
        ? PathSearch.Reconstruct(_origin, destination, _previous) : Array.Empty<GridPosition>();
>>>>>>> Stashed changes
    internal static MovementPreview Empty { get; } = new(default, new(), new());
}
