using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
<<<<<<< Updated upstream
public sealed record FactionColor(Side Side, FleetColor Color);
=======

public sealed record FactionColor(Side Side, FleetColor Color);

>>>>>>> Stashed changes
public sealed partial class BattleState
{
    public const int SideSlots = 6;
    public static IReadOnlyList<Side> PlayableSides { get; } = Array.AsReadOnly(new[] { Side.Player, Side.Enemy, Side.Enemy2, Side.Enemy3, Side.Enemy4 });
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
    private readonly List<Side> _factions = new();
    private readonly Dictionary<Side, FleetColor> _factionColors = new();
    private System.Collections.ObjectModel.ReadOnlyCollection<Side>? _factionView;
    public IReadOnlyList<Side> Factions => _factionView ??= _factions.AsReadOnly();
<<<<<<< Updated upstream
    public IEnumerable<Side> AliveFactions => _factions.Where(side => Mothership(side)is not null);
    public int OpponentCount => _factions.Count - 1;
    public bool PlayerDefeated => Mothership(Side.Player)is null;

    public FleetColor ColorFor(Side side) => side == Side.Player ? PlayerColor : _factionColors.GetValueOrDefault(side, FleetColor.Purple);
=======
    public IEnumerable<Side> AliveFactions => _factions.Where(side => Mothership(side) is not null);
    public int OpponentCount => _factions.Count - 1;
    public bool PlayerDefeated => Mothership(Side.Player) is null;
    public FleetColor ColorFor(Side side) => side == Side.Player ? PlayerColor : _factionColors.GetValueOrDefault(side, FleetColor.Purple);
    public static string FactionName(Side side) => side switch
    {
        Side.Player => "Your fleet",
        Side.Enemy => "Captain Anat",
        Side.Enemy2 => "Captain Tarek",
        Side.Enemy3 => "Captain Selene",
        Side.Enemy4 => "Captain Amun",
        _ => "Pirates"
    };

>>>>>>> Stashed changes
    private void InitializeFactions()
    {
        foreach (var side in PlayableSides)
        {
            int mothers = _ships.Count(ship => ship.Owner == side && ship.IsMothership);
            if (mothers == 0)
            {
                if (_ships.Any(ship => ship.Owner == side))
                    throw new ArgumentException("A deployed fleet needs a Mothership.");
                continue;
            }
<<<<<<< Updated upstream

            if (mothers != 1 || !Rules.DynamicFleetCapacity && _ships.Count(ship => ship.Owner == side && ship.CountsTowardFleet) > Rules.FleetLimit)
                throw new ArgumentException("Each fleet needs one Mothership and must respect the fleet limit.");
            _factions.Add(side);
        }

=======
            if (mothers != 1 || _ships.Count(ship => ship.Owner == side && ship.CountsTowardFleet) > Rules.FleetLimit)
                throw new ArgumentException("Each fleet needs one Mothership and must respect the fleet limit.");
            _factions.Add(side);
        }
>>>>>>> Stashed changes
        if (!_factions.Contains(Side.Player) || _factions.Count is < 2 or > 5)
            throw new ArgumentException("Deploy one player and between one and four rival fleets.");
        AssignFactionColors(PlayerColor);
    }

    private void AssignFactionColors(FleetColor playerColor)
    {
        _factionColors.Clear();
        _factionColors[Side.Player] = playerColor;
        var available = Enum.GetValues<FleetColor>().Where(color => color != playerColor).ToArray();
        // Cosmetic randomness must not consume discovery/pirate simulation draws.
        var random = new Random(Board.Seed ^ 0x6319);
        for (int i = available.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        int index = 0;
        foreach (var side in _factions.Where(side => side != Side.Player))
            _factionColors[side] = available[index++];
    }

    private Side NextFactionTurn(Side completed)
    {
        var order = _factions.Concat(new[] { Side.Pirates }).ToArray();
        int previous = Array.IndexOf(order, completed);
        for (int step = 1; step <= order.Length; step++)
        {
            var candidate = order[(previous + step) % order.Length];
<<<<<<< Updated upstream
            if (candidate == Side.Pirates ? OwnShips(candidate).Any() || _villages.Any(v => v.Owner == Side.Pirates && v.Health > 0) : Mothership(candidate)is not null)
                return candidate;
        }

=======
            if (candidate == Side.Pirates ? OwnShips(candidate).Any() : Mothership(candidate) is not null)
                return candidate;
        }
>>>>>>> Stashed changes
        return completed;
    }

    private void EliminateFaction(Side side)
    {
        foreach (var remaining in OwnShips(side).ToArray())
            RemoveDestroyedShip(remaining);
        foreach (var village in _villages.Where(village => village.Owner == side))
        {
            village.Owner = null;
            RegisterVillageIncome(village);
        }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        RecomputeVictory();
    }

    private void RecomputeVictory()
    {
        var surviving = AliveFactions.ToArray();
        IsDraw = surviving.Length == 0;
        Winner = surviving.Length == 1 ? surviving[0] : null;
    }
}
