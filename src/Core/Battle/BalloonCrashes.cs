using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

/// <summary>A separate crash boundary follows the shot or dismantling boundary.
/// Presentation may play the falling wreck before committing these simultaneous hits.</summary>
public sealed record BalloonCrash(ShipSnapshot Balloon, IReadOnlyList<CombatShot> Ships,
    IReadOnlyList<AreaHit> Villages)
{
    public string ImpactKey => "balloon-crash:" + Balloon.Id;
}

public sealed partial class BattleState
{
    private readonly List<(ShipSnapshot Balloon, Side Source)> _pendingBalloonCrashes = new();
    private readonly List<BalloonCrash> _balloonCrashes = new();

    private void ClearBalloonCrashes()
    {
        _pendingBalloonCrashes.Clear();
        _balloonCrashes.Clear();
    }

    private void QueueBalloonCrash(Ship ship, Side source)
    {
        if (ship.IsAirborne && Rules.Balloon.CrashDamage > 0)
            _pendingBalloonCrashes.Add((ShipSnapshot.From(ship), source));
    }

    private void ResolveBalloonCrashes()
    {
        foreach (var (balloon, source) in _pendingBalloonCrashes)
        {
            var cells = Board.BlastCells(balloon.Position).ToHashSet();
            var targets = _ships.Where(s => !s.IsAirborne && cells.Contains(s.Position))
                .OrderBy(s => s.Id).Select(s => (Ship: s, Before: ShipSnapshot.From(s))).ToArray();
            var shots = new List<CombatShot>();
            bool originVisible = balloon.Owner == Side.Player || Vision.IsVisible(Side.Player, balloon.Position);
            // Damage every victim before a destroyed flagship can collapse its surviving fleet.
            foreach (var (ship, before) in targets)
            {
                double damage = Math.Min(ship.Health, Rules.Balloon.CrashDamage);
                ship.Health = Math.Max(0, ship.Health - damage);
                shots.Add(new(balloon, before, damage, false, ship.Health <= 0, false,
                    AttackerVisibleToPlayer: originVisible,
                    TargetVisibleToPlayer: ship.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position)));
            }
            foreach (var (ship, _) in targets.Where(t => t.Ship.Health <= 0))
                RecordEnemyLoss(source, ship);
            foreach (var (ship, _) in targets.Where(t => t.Ship.Health <= 0))
                if (_ships.Contains(ship)) RemoveDestroyedShip(ship);
            var towns = new List<AreaHit>();
            foreach (var town in _villages.Where(v => v.Health > 0 && cells.Contains(v.Position)).OrderBy(v => v.Id))
            {
                double damage = Math.Min(town.Health, Rules.Balloon.CrashDamage);
                town.Health -= damage;
                RegisterVillageIncome(town);
                towns.Add(new(town.Position, damage, town.Owner == Side.Player || Vision.IsVisible(Side.Player, town.Position)));
            }
            var crash = new BalloonCrash(balloon, shots.AsReadOnly(), towns.AsReadOnly());
            _balloonCrashes.Add(crash);
            RecomputeVictory();
            RecordImpact(crash.ImpactKey);
        }
        _pendingBalloonCrashes.Clear();
    }
}
