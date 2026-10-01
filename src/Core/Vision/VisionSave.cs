using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Vision;
public sealed record SeenCell(GridPosition Cell, int Turn);
public sealed record SavedVision(Side Side, GridPosition[] Explored, GridPosition[] Flashes, SeenCell[] LastSeen);
public sealed partial class BattleVision
{
    internal SavedVision[] Save() => Enum.GetValues<Side>().Select(side =>
    {
        int index = (int)side;
        return new SavedVision(side, _explored[index].ToArray(), _flashes[index].ToArray(), _lastSeen[index].Select(e => new SeenCell(e.Key, e.Value)).ToArray());
    }).ToArray();
    internal void Restore(IEnumerable<SavedVision> data, IEnumerable<Side> factions)
    {
        var restoredSides = new HashSet<Side>();
        foreach (var item in data)
        {
            if (item is null || !Enum.IsDefined(item.Side) || !restoredSides.Add(item.Side) || item.Explored is null || item.Flashes is null || item.LastSeen is null || item.Explored.Any(p => !_board.Contains(p)) || item.Flashes.Any(p => !_board.Contains(p)) || item.LastSeen.Any(c => c is null || !_board.Contains(c.Cell) || c.Turn < 0))
                throw new ArgumentException("Invalid saved vision.");
            int index = (int)item.Side;
            _explored[index].Clear();
            _explored[index].UnionWith(item.Explored);
            _flashes[index].Clear();
            _flashes[index].UnionWith(item.Flashes);
            _lastSeen[index].Clear();
            foreach (var cell in item.LastSeen)
                _lastSeen[index][cell.Cell] = cell.Turn;
        }

        if (factions.Append(Side.Pirates).Any(side => !restoredSides.Contains(side)))
            throw new ArgumentException("Saved vision is missing a faction.");
    }
}
