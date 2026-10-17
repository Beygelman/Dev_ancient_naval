using System.Text.Json;
using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class V0209x2CityChecks
{
    internal static int Run(BattleRules current)
    {
        int checks = 0;
        void Check(bool value, string name)
        { if (!value) throw new Exception("0209x2 city: " + name); checks++; }
        BattleRules Policy(bool experiment)
        {
            var json = JsonNode.Parse(JsonSerializer.Serialize(current))!.AsObject();
            json["ExperimentalEconomy"] = experiment;
            json["StrategicAi"] = false;
            json["PaidVillageUpgrades"] = true;
            json["VillageCombat"]!["AutomaticAttack"] = true;
            json["Infrastructure"]!["Enabled"] = false;
            return BattleRules.FromJson(json.ToJsonString());
        }
        BattleState Restore(BattleSave saved) => BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        BattleState Fixture(int level = 2, bool fortified = false, bool experiment = true)
        {
            var cell = new GridPosition(8, 8);
            var battle = new BattleState(new GameBoard(20, 20, p => p == cell ? TerrainType.Land : TerrainType.Water),
                Policy(experiment), new[] {
                    (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                    (Side.Player, ShipClass.Invader, new GridPosition(8, 9)),
                    (Side.Enemy, ShipClass.Invader, new GridPosition(10, 8)) },
                Array.Empty<GridPosition>(), villageSpots: new[] { cell }, piratesEnabled: false);
            var saved = battle.CaptureSnapshot();
            var town = saved.Villages[0];
            saved.Villages[0] = town with { Owner = Side.Player, Level = level,
                Health = experiment ? 10 : level * 5, Fortified = fortified };
            saved.Credits[0] = 200;
            saved.Income = saved.Income.Append(new IncomeSource($"village:{town.Id}", Side.Player, 2)).ToArray();
            return Restore(saved);
        }
        void Round(BattleState battle)
        {
            Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
                "round advances through actual turn pipeline");
        }
        var battle = Fixture(fortified: true);
        var city = battle.Villages[0];
        int cityId = city.Id, capacity = battle.FleetCapacity(Side.Player);
        int money = battle.Credits(Side.Player), price = battle.VillageUpgradePrice(Side.Player, cityId);
        Check(city.MaxHealth == 15 && battle.VillageFleetCapacity(city) == 3,
            "fixed city durability and contribution are independent of level");
        Check(battle.UpgradeVillage(Side.Player, cityId).Success && city.Level == 2 && city.Health == 10
            && city.UpgradeWorkRemaining == 2 && battle.Credits(Side.Player) == money - price,
            "funding queues two works without instant level or healing");
        Check(!battle.UpgradeVillage(Side.Player, cityId).Success && battle.Credits(Side.Player) == money - price,
            "pending project cannot be purchased twice");
        var first = battle.EndTurn(Side.Player);
        Check(first.Success && city.UpgradeWorkRemaining == 1 && city.Level == 2 && city.Health == 10
            && first.OutpostShots!.Count == 0,
            "idle project consumes one work and blocks active guns and passive repair");
        Check(battle.EndTurn(Side.Enemy).Success, "enemy turn returns to owner");
        string pending = battle.SaveJson();
        battle = BattleState.LoadJson(pending);
        city = battle.Villages[0];
        Check(battle.SaveJson() == pending && city.UpgradeProjectPrice == price,
            "Continue preserves prepaid project and work progress exactly");
        Check(battle.BuildFromVillage(Side.Player, cityId, ShipClass.Garrison,
            battle.VillageSpawnCells(cityId).First()).Success, "mobilization is allowed while development is pending");
        var paused = battle.EndTurn(Side.Player);
        Check(city.Level == 2 && city.UpgradeWorkRemaining == 1 && city.Health == 10
            && paused.OutpostShots!.Count == 0,
            "production pauses development and prevents simultaneous guns or passive healing");
        battle.EndTurn(Side.Enemy);
        Round(battle);
        Check(city.Level == 3 && city.UpgradeWorkRemaining == 0 && city.UpgradeProjectPrice == 0
            && city.Health == 10 && battle.FleetCapacity(Side.Player) == capacity,
            "second idle work completes without healing or new fleet capacity");

        battle = Fixture(fortified: true); city = battle.Villages[0];
        battle.UpgradeVillage(Side.Player, city.Id);
        money = battle.Credits(Side.Player);
        Check(battle.SetVillageDevelopmentPaused(Side.Player, city.Id, true).Success
            && !battle.SetVillageDevelopmentPaused(Side.Enemy, city.Id, false).Success
            && battle.Credits(Side.Player) == money && !city.HasProduced,
            "free pause is an owner command and does not consume work or money");
        var defended = battle.EndTurn(Side.Player);
        Check(defended.OutpostShots!.Count == 1 && city.UpgradeWorkRemaining == 2 && city.Health == 10,
            "paused development leaves idle work available to the defensive gun");
        battle.EndTurn(Side.Enemy);
        battle = BattleState.LoadJson(battle.SaveJson()); city = battle.Villages[0];
        Check(city.DevelopmentPaused && city.UpgradeWorkRemaining == 2,
            "Continue preserves an explicit development pause");
        Check(battle.SetVillageDevelopmentPaused(Side.Player, city.Id, false).Success
            && !battle.SetVillageDevelopmentPaused(Side.Player, city.Id, false).Success,
            "resume is explicit and cannot be repeated without a state change");
        Round(battle);
        Check(city.UpgradeWorkRemaining == 1 && !city.DevelopmentPaused,
            "resumed project again automatically takes its idle work");

        battle = Fixture(); city = battle.Villages[0];
        Check(battle.FortifyVillage(Side.Player, city.Id).Success
            && !battle.CanRepairVillage(Side.Player, city.Id)
            && !battle.CanUpgradeVillage(Side.Player, city.Id), "fortification consumes the same city work");
        var fortified = battle.EndTurn(Side.Player);
        Check(fortified.OutpostShots!.Count == 0 && city.Health == 10,
            "new fortification cannot also fire or heal on its construction turn");
        battle.EndTurn(Side.Enemy);
        var firing = battle.EndTurn(Side.Player);
        Check(firing.OutpostShots!.Count == 1 && city.Health == 10,
            "idle fortified city fires but cannot passively heal in the same turn");
        Check(battle.VillageAttackDamage(city) == 3 && battle.VillageCounterDamage(city) == 4,
            "fixed guns and replies no longer scale with economic level");

        battle = Fixture(); city = battle.Villages[0];
        Check(battle.RepairVillage(Side.Player, city.Id).Success && city.Health == 14,
            "active repair restores four and occupies work");
        Round(battle);
        Check(city.Health == 14, "active repair does not stack with passive recovery");
        Round(battle);
        Check(city.Health == 15, "entirely idle city recovers one HP");

        battle = Fixture(3);
        var conquest = battle.CaptureSnapshot();
        cityId = conquest.Villages[0].Id;
        conquest.Villages[0] = conquest.Villages[0] with { Owner = Side.Enemy, Health = 0,
            Port = true, PortCell = new GridPosition(9, 8), UpgradeWorkRemaining = 1,
            UpgradeProjectPrice = conquest.Rules.VillageLevelPrices[2] };
        conquest.Income = conquest.Income.Where(i => i.Id != $"village:{cityId}").ToArray();
        conquest.TurnSerial = 1;
        conquest.CaptureWaits = new[] { new SavedWait(cityId, Side.Player, 3, new GridPosition(8, 9), 0) };
        battle = Restore(conquest); city = battle.Villages[0];
        Check(battle.CaptureVillage(Side.Player, cityId).Success && city.Health == 8 && city.Level == 3
            && city.HasPort && city.UpgradeWorkRemaining == 0 && city.UpgradeProjectPrice == 0
            && battle.VillageBuildBlockReason(Side.Player, cityId, ShipClass.Garrison) is not null,
            "capture retains development but cancels enemy project and consumes work");

        battle = Fixture();
        Check(battle.UpgradeVillage(Side.Player, battle.Villages[0].Id).Success,
            "development can be active when its faction is defeated");
        var defeat = battle.CaptureSnapshot();
        defeat.ActiveSide = Side.Enemy;
        defeat.Ships.Single(s => s.Id == 1).Position = new GridPosition(9, 8);
        defeat.Ships.Single(s => s.Id == 1).Health = 1;
        battle = Restore(defeat);
        Check(battle.Attack(Side.Enemy, 4, 1).Success && battle.Villages[0].Owner is null
            && battle.Villages[0].UpgradeWorkRemaining == 0,
            "faction elimination clears abandoned city projects");
        Check(BattleState.LoadJson(battle.SaveJson()).Villages[0].UpgradeProjectPrice == 0,
            "defeat during prepaid development still produces a valid Continue snapshot");

        battle = Fixture(experiment: false); city = battle.Villages[0];
        Check(city.MaxHealth == 10 && battle.UpgradeVillage(Side.Player, city.Id).Success
            && city.Level == 3 && city.Health == 15 && city.UpgradeWorkRemaining == 0,
            "legacy voyages preserve immediate upgrades and level-scaled health");
        battle = Fixture();
        foreach (bool excessiveWork in new[] { true, false })
        {
            var invalid = battle.CaptureSnapshot();
            var town = invalid.Villages[0];
            invalid.Villages[0] = excessiveWork ? town with { UpgradeWorkRemaining = 3 }
                : town with { UpgradeProjectPrice = 1 };
            bool rejected = false;
            try { Restore(invalid); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "invalid saved project is rejected");
        }
        var invalidPause = battle.CaptureSnapshot();
        invalidPause.Villages[0] = invalidPause.Villages[0] with { DevelopmentPaused = true };
        bool rejectedPause = false;
        try { Restore(invalidPause); } catch (ArgumentException) { rejectedPause = true; }
        Check(rejectedPause, "a paused project cannot exist without development work");
        return checks;
    }

}
