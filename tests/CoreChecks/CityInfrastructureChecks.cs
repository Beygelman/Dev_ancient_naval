using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class CityInfrastructureChecks
{
    internal static int Run(BattleRules rules)
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception("City infrastructure: " + message); checks++; }
        BattleState Restore(BattleSave saved) => BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        BattleState Reset(BattleState battle)
        {
            var saved = battle.CaptureSnapshot();
            for (int i = 0; i < saved.Villages.Length; i++)
                saved.Villages[i] = saved.Villages[i] with { Produced = false, Repaired = false };
            return Restore(saved);
        }
        BattleState Fixture(int level = 5)
        {
            var cells = new[] { new GridPosition(8, 8), new GridPosition(12, 8) };
            var battle = new BattleState(new GameBoard(20, 20, p => cells.Contains(p) ? TerrainType.Land : TerrainType.Water),
                rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                    (Side.Player, ShipClass.Invader, new GridPosition(8, 9)),
                    (Side.Player, ShipClass.Invader, new GridPosition(9, 9)) },
                Array.Empty<GridPosition>(), villageSpots: cells);
            var saved = battle.CaptureSnapshot();
            for (int i = 0; i < 2; i++)
            {
                var v = saved.Villages[i];
                saved.Villages[i] = v with { Owner = Side.Player, Level = level, Health = level * 5,
                    Port = level >= 3, PortCell = level >= 3 ? new(v.Position.X, 9) : null };
                saved.Income = saved.Income.Append(new IncomeSource($"village:{v.Id}", Side.Player, level)).ToArray();
            }
            saved.Ships.Single(s => s.Id == 3).Health = 4;
            saved.Ships.Single(s => s.Id == 4).Health = 4;
            saved.Credits[0] = 200;
            return Restore(saved);
        }
        Check(rules.Infrastructure.Enabled, "current voyages enable facilities");
        foreach (int level in new[] { 1, 2, 3, 4, 5 })
        {
            var b = Fixture(level);
            Check(b.CityFacilitySlots(b.Villages[0]) == (level < 2 ? 0 : level < 4 ? 1 : 2), "slots unlock at levels 2/4");
        }
        var city = Fixture();
        int id = city.Villages[0].Id;
        int credits = city.Credits(Side.Player), initialIncome = city.GrossIncome(Side.Player);
        Check(city.ConfigureCityFacility(Side.Player, id, 0, CityFacility.TradeYard).Success, "install trade yard");
        Check(city.Credits(Side.Player) == credits - rules.Infrastructure.BuildPrice
            && city.GrossIncome(Side.Player) == initialIncome + rules.Infrastructure.TradeIncome,
            "paid yard changes credited income");
        Check(!city.ConfigureCityFacility(Side.Player, id, 1, CityFacility.NavalArsenal).Success, "one city work per turn");
        city = Reset(city);
        Check(!city.ConfigureCityFacility(Side.Player, id, 1, CityFacility.TradeYard).Success, "duplicate facility rejected");
        int baseCapacity = city.FleetCapacity(Side.Player);
        Check(city.ConfigureCityFacility(Side.Player, id, 1, CityFacility.NavalArsenal).Success
            && city.FleetCapacity(Side.Player) == baseCapacity + rules.Infrastructure.ArsenalCapacity, "arsenal expands shared fleet capacity");
        city = Reset(city);
        Check(city.ConfigureCityFacility(Side.Player, id, 0, CityFacility.TradeYard).Success
            && city.Villages[0].ImprovedFacility == CityFacility.TradeYard, "level five improves one facility");
        city = Reset(city);
        Check(!city.ConfigureCityFacility(Side.Player, id, 1, CityFacility.NavalArsenal).Success, "no second improved facility");
        int shipsBefore = city.Ships.Count;
        credits = city.Credits(Side.Player);
        Check(city.ConfigureCityFacility(Side.Player, id, 1, CityFacility.RepairDock).Success, "paid replacement");
        Check(city.FleetCapacity(Side.Player) == baseCapacity && city.Ships.Count == shipsBefore
            && city.Credits(Side.Player) == credits - rules.Infrastructure.BuildPrice - rules.Infrastructure.ReplacementSurcharge,
            "arsenal loss lowers capacity without deleting ships");
        city = Reset(city);
        double health = city.Find(3)!.Health;
        var repair = city.Repair(Side.Player, 3);
        Check(repair.Success && repair.Amount == Math.Min(10 - health, rules.RepairAmount + rules.Infrastructure.DockRepair)
            && city.Find(3)!.IsExhausted && city.Find(3)!.AttacksRemaining == 0 && !city.Find(3)!.CanMove,
            "dock repair consumes the serviced ship actions");
        Check(city.Villages[0].HasProduced && city.Villages[0].HasRepaired
            && city.ShipRepairAmount(city.Find(4)!) == rules.RepairAmount, "one dock service per city turn");

        string saved = city.SaveJson();
        Check(BattleState.LoadJson(saved).SaveJson() == saved, "facilities and improvement survive Continue");
        var isolated = city.CaptureSnapshot();
        isolated.Villages[1] = isolated.Villages[1] with { Port = false, PortCell = null };
        var alone = Restore(isolated);
        Check(alone.PortIncome(alone.Villages[0]) == 0, "isolated yard earns no trade bonus");

        void Reject(Action<BattleSave> mutate, string name)
        {
            var snapshot = city.CaptureSnapshot();
            mutate(snapshot);
            bool rejected = false;
            try { Restore(snapshot); } catch (ArgumentException) { rejected = true; }
            Check(rejected, name);
        }
        Reject(s => s.Villages[0] = s.Villages[0] with { Facility2 = CityFacility.TradeYard }, "duplicate saved facilities rejected");
        Reject(s => s.Villages[0] = s.Villages[0] with { Facility1 = (CityFacility)99 }, "unknown saved facility rejected");
        Reject(s => s.Villages[0] = s.Villages[0] with { Level = 3, Health = 15 }, "locked saved slot rejected");
        var historical = JsonNode.Parse(saved)!.AsObject();
        historical["Rules"]!.AsObject().Remove("Infrastructure");
        foreach (var town in historical["Villages"]!.AsArray())
        {
            town!.AsObject().Remove("Facility1");
            town.AsObject().Remove("Facility2");
            town.AsObject().Remove("ImprovedFacility");
        }
        var legacy = BattleState.LoadJson(historical.ToJsonString());
        Check(!legacy.Rules.Infrastructure.Enabled && legacy.Villages.All(v => v.Facility1 == CityFacility.None),
            "absent optional state/rules preserve historical economy");

        // Capacity overflow after replacing an arsenal prevents new builds, not existing hulls.
        var limitRules = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(rules))!.AsObject();
        limitRules["DynamicFleetCapacity"] = false;
        limitRules["FleetLimit"] = 4;
        var overflowSave = Fixture().CaptureSnapshot();
        overflowSave.Rules = BattleRules.FromJson(limitRules.ToJsonString());
        overflowSave.Villages[0] = overflowSave.Villages[0] with { Facility1 = CityFacility.NavalArsenal };
        var overflow = Restore(overflowSave);
        Check(overflow.CanFitFleet(Side.Player, ShipClass.Invader), "arsenal permits another two-slot hull");
        var built = overflow.BuildFromVillage(Side.Player, overflow.Villages[0].Id, ShipClass.Invader, new(7, 9));
        Check(built.Success, "build into arsenal capacity");
        overflow = Reset(overflow);
        shipsBefore = overflow.Ships.Count;
        Check(overflow.ConfigureCityFacility(Side.Player, overflow.Villages[0].Id, 0, CityFacility.RepairDock).Success,
            "replace arsenal while above resulting capacity");
        Check(overflow.Ships.Count == shipsBefore && !overflow.CanFitFleet(Side.Player, ShipClass.Garrison),
            "overcapacity only prevents further construction");
        return checks;
    }
}

