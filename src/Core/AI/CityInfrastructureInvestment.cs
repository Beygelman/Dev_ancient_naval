using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.AI;

internal static class CityInfrastructureInvestment
{
    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> observedEnemies)
    {
        if (!battle.Rules.Infrastructure.Enabled) return null;
        var side = battle.ActiveSide;
        int reserve = battle.BuildPrice(side, ShipClass.Garrison);
        foreach (var town in battle.Villages.Where(v => v.Owner == side && v.Health > 0).OrderBy(v => v.Id))
        {
            bool threatened = observedEnemies.Any(e => e.IsArmed && battle.Board.InRadius(town.Position, e.Position, 4));
            bool damaged = battle.OwnShips(side).Any(s => s.Health < s.MaxHealth && !s.HasMoved
                && battle.Board.GetSurrounding(town.Position).Contains(s.Position));
            var kind = damaged ? CityFacility.RepairDock
                : battle.FleetUsed(side) >= battle.FleetCapacity(side) || threatened ? CityFacility.NavalArsenal
                : town.HasPort && battle.ConnectedPortCityCount(town) > 0 ? CityFacility.TradeYard : CityFacility.None;
            if (kind == CityFacility.None) continue;
            int slot = Enumerable.Range(0, battle.CityFacilitySlots(town))
                .FirstOrDefault(i => BattleState.CityFacilityAt(town, i) == CityFacility.None, -1);
            if (town.Facility1 == kind || town.Facility2 == kind)
                slot = town.Facility1 == kind ? 0 : 1; // Improve at level five; never churn replacements.
            if (slot < 0 || battle.CityFacilityBlockReason(side, town.Id, slot, kind) is not null) continue;
            int price = battle.CityFacilityPrice(side, town.Id, slot, kind);
            if (battle.Credits(side) < (long)price + reserve) continue;
            return battle.ConfigureCityFacility(side, town.Id, slot, kind);
        }
        return null;
    }
}

