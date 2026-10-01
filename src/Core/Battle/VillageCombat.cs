using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public int VillageAttackDamage(Village village) => village.Level >= 2 ? village.Level : 0;
    public int VillageCounterDamage(Village village) => Rules.VillageCombat.AutomaticAttack ? VillageAttackDamage(village) + Rules.VillageCombat.CounterBonus : 3;
    private IReadOnlyList<OutpostShot> FireOutposts(Side side)
    {
        if (!Rules.VillageCombat.AutomaticAttack)
            return Array.Empty<OutpostShot>();
        var shots = new List<OutpostShot>();
        foreach (var village in _villages.Where(v => v.Owner == side && v.Health > 0 && v.IsFortified && v.Level >= 2 && !v.HasRepaired && !v.HasAttacked).OrderBy(v => v.Id))
        {
            if (IsOver)
                break;
            var target = _ships.Where(s => s.Owner != side && !s.IsAirborne && !s.IsStructure && Vision.IsVisible(side, s.Position) && Board.InRadius(village.Position, s.Position, Rules.VillageCombat.AttackRange)).OrderBy(s => s.Health).ThenBy(s => s.Id).FirstOrDefault();
            if (target is null)
                continue;
            var before = ShipSnapshot.From(target);
            double damage = Math.Min(target.Health, Math.Max(1, VillageAttackDamage(village) - target.Definition.Armor));
            bool originVisible = side == Side.Player || Vision.IsVisible(Side.Player, village.Position);
            bool targetVisible = target.Owner == Side.Player || Vision.IsVisible(Side.Player, target.Position);
            target.Health = Ship.Whole(target.Health - damage);
            village.HasAttacked = true; // An automatic shot counts as the town's active attack, unlike replies.
            bool sunk = target.Health <= 0;
            if (sunk)
            {
                RewardPirateDefeat(side, target);
                RecordEnemyLoss(side, target);
                RemoveDestroyedShip(target);
            }

            RecordImpact("outpost:" + village.Id);
            shots.Add(new(village.Id, village.Position, before, damage, sunk, originVisible, targetVisible));
        }

        UpdateVision();
        return shots;
    }
}
