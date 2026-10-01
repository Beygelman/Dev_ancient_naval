using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed record AreaHit(GridPosition Position, double Damage, bool VisibleToPlayer);
public sealed partial class BattleState
{
    private CombatShot[] MortarSplash(Ship attacker, GridPosition center, int directTarget = 0)
    {
        var ring = Board.GetSurrounding(center).ToHashSet();
        // Materialize targets before Fire can remove destroyed ships from the fleet.
        return _ships.Where(s => s.Id != directTarget && s.Owner != attacker.Owner && !s.IsAirborne && ring.Contains(s.Position)).ToArray().Select(s => Fire(attacker, s, false, Rules.Mortar.SplashDamage)).ToArray();
    }

    private AreaHit[] MortarVillageSplash(Ship attacker, GridPosition center, int directVillage = 0)
    {
        var ring = Board.GetSurrounding(center).ToHashSet();
        var hits = new List<AreaHit>();
        foreach (var town in _villages.Where(v => v.Id != directVillage && v.Owner != attacker.Owner && v.Health > 0 && ring.Contains(v.Position)))
        {
            double damage = Math.Min(town.Health, Rules.Mortar.SplashDamage * (town.IsFortified ? .75 : 1));
            town.Health -= damage;
            RegisterVillageIncome(town);
            hits.Add(new(town.Position, damage, Vision.IsVisible(Side.Player, town.Position)));
        }

        return hits.ToArray();
    }

    private void RemoveDestroyedShip(Ship ship)
    {
        _ships.Remove(ship);
        RemoveIncomeSource($"ship:{ship.Id}");
        InvalidateWaiting(ship.Id);
        _pirateHomes.Remove(ship.Id);
        if (ship.Definition.Class == ShipClass.FishingDock)
            _shoals.Add(ship.Position);
        if (ship.IsMothership)
            EliminateFaction(ship.Owner);
    }
}
