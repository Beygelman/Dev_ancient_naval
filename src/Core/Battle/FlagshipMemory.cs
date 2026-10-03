using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public sealed record FlagshipSighting(Side Owner, GridPosition Position, int Turn);
public sealed record SavedFlagshipSighting(Side Observer, Side Owner, GridPosition Position, int Turn);

/// <summary>Only a lookout's recorded enemy flagship position is retained.
/// Hidden moves/deaths cannot update or erase it. Seeing the recorded tile
/// empty invalidates the old objective; human God's eye is not a lookout.</summary>
public sealed partial class BattleState
{
    private readonly Dictionary<(Side Observer, Side Owner), FlagshipSighting> _flagshipSightings = new();

    public IReadOnlyList<FlagshipSighting> KnownFlagships(Side observer) => _flagshipSightings
        .Where(entry => entry.Key.Observer == observer).OrderBy(entry => entry.Key.Owner)
        .Select(entry => entry.Value).ToArray();

    private void ObserveFlagships()
    {
        foreach (var observer in _factions)
        {
            foreach (var record in _flagshipSightings.Where(entry => entry.Key.Observer == observer).ToArray())
                if (Vision.IsOpticallyVisible(observer, record.Value.Position)
                    && !_ships.Any(ship => ship.IsMothership && ship.Owner == record.Value.Owner
                        && ship.Position == record.Value.Position))
                    _flagshipSightings.Remove(record.Key);
            foreach (var ship in _ships.Where(ship => ship.IsMothership && ship.Owner != observer
                && Vision.IsOpticallyVisible(observer, ship.Position)))
                _flagshipSightings[(observer, ship.Owner)] = new(ship.Owner, ship.Position, TurnSerial);
        }
    }

    private SavedFlagshipSighting[] SaveFlagshipSightings() => _flagshipSightings
        .OrderBy(entry => entry.Key.Observer).ThenBy(entry => entry.Key.Owner)
        .Select(entry => new SavedFlagshipSighting(entry.Key.Observer, entry.Key.Owner,
            entry.Value.Position, entry.Value.Turn)).ToArray();

    private void RestoreFlagshipSightings(IEnumerable<SavedFlagshipSighting> saved)
    {
        _flagshipSightings.Clear();
        foreach (var record in saved)
            _flagshipSightings.Add((record.Observer, record.Owner), new(record.Owner, record.Position, record.Turn));
    }
}
