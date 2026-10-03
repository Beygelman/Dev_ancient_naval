using System.Text.Json.Nodes;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static partial class BattleScenarios
{
    private static void DataDrivenRules()
    {
        var original = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "balance-v0202.json"));
        var legacy = JsonNode.Parse(original)!.AsObject();
        foreach (var key in new[]
        {
            "mortar",
            "balloon",
            "treasury",
            "dockResourceReward",
            "pirateCurrencyReward",
            "pirateResourceReward"
        }

        )
            legacy.Remove(key);
        var defaults = BattleRules.FromJson(legacy.ToJsonString());
        Check(defaults.Mortar.PurchasePrice == 10 && defaults.Mortar.SplashDamage == 2 && defaults.Mortar.VillageDamageBonus == 2, "Old balance documents retain mortar defaults");
        Check(defaults.Balloon.BombDamage == 6 && defaults.Balloon.SplashDamage == 2 && defaults.Balloon.CooldownTurns == 3, "Old balance documents retain balloon defaults");
        Check(defaults.Treasury.CurrencyReward == 5 && defaults.Treasury.ResourceReward == 2 && defaults.DockResourceReward == 2 && defaults.PirateCurrencyReward == 2 && defaults.PirateResourceReward == 1, "Old balance documents retain economic rewards");
        for (int roll = 0; roll < 100; roll++)
            Check(defaults.Treasury.RewardForRoll(roll) == BattleState.RewardForRoll(roll), "Default weighted rewards preserve each original random draw");
        foreach (var invalid in new[]
        {
            ("mortar", "splashDamage", -1),
            ("mortar", "purchasePrice", -1),
            ("mortar", "villageDamageBonus", -1),
            ("balloon", "cooldownTurns", 0),
            ("balloon", "bombDamage", 0),
            ("balloon", "splashDamage", -1),
            ("balloon", "antiAirRange", 0),
            ("villageCombat", "attackRange", 0),
            ("villageCombat", "counterRange", 0),
            ("villageCombat", "counterBonus", -1),
            ("economy", "minimumResourceSpots", -1),
            ("economy", "resourceTileInterval", -1),
            ("treasury", "whirlpoolWeight", 13),
            ("treasury", "currencyWeight", -1),
            ("treasury", "resourceReward", -1)
        }

        )
        {
            var document = JsonNode.Parse(original)!;
            document[invalid.Item1]![invalid.Item2] = invalid.Item3;
            bool rejected = false;
            try
            {
                BattleRules.FromJson(document.ToJsonString());
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, $"Reject invalid {invalid.Item1}.{invalid.Item2}");
        }

        foreach (var section in new[]
        {
            "mortar",
            "balloon",
            "treasury",
            "villageCombat",
            "ships",
            "upgrades"
        }

        )
        {
            var document = JsonNode.Parse(original)!;
            document[section] = null;
            bool rejected = false;
            try
            {
                BattleRules.FromJson(document.ToJsonString());
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, $"Reject explicit null {section} with a configuration error");
        }

        var custom = JsonNode.Parse(original)!;
        custom["startingCredits"] = 30;
        custom["mortar"]!["purchasePrice"] = 7;
        custom["mortar"]!["splashDamage"] = 1;
        custom["balloon"]!["bombDamage"] = 7;
        custom["balloon"]!["splashDamage"] = 1;
        custom["balloon"]!["cooldownTurns"] = 4;
        custom["treasury"]!["ancientGunWeight"] = 0;
        custom["treasury"]!["currencyWeight"] = 100;
        custom["treasury"]!["ancientBalloonWeight"] = 0;
        custom["treasury"]!["resourcesWeight"] = 0;
        custom["treasury"]!["whirlpoolWeight"] = 0;
        custom["treasury"]!["currencyReward"] = 9;
        var rules = BattleRules.FromJson(custom.ToJsonString());
        Check(Enumerable.Range(0, 100).All(roll => rules.Treasury.RewardForRoll(roll) == TreasuryReward.Currency), "Custom reward weights handle zero-width outcomes");
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(8, 9)), (Side.Player, ShipClass.Balloon, new GridPosition(8, 8)) }, Array.Empty<GridPosition>());
        battle.Find(5)!.HasMoved = true;
        Check(battle.DropBomb(Side.Player, 5).Success && battle.Find(3)!.Health == 8 && battle.Find(4)!.Health == 4 && battle.Find(5)!.BombCooldown == 4, "Balloon commands consume configured damage, splash and cooldown");
        var restored = BattleState.LoadJson(battle.SaveJson());
        Check(restored.Rules.Balloon == rules.Balloon && restored.Find(5)!.BombCooldown == 4, "Saved rules preserve a nondefault bomb cooldown");
        var mother = battle.Find(1)!;
        mother.Level = 5;
        mother.Health = mother.MaxHealth;
        battle.BuyRadar(Side.Player, mother.Id);
        int funds = battle.Credits(Side.Player);
        Check(battle.BuyMortar(Side.Player, mother.Id).Success && battle.Credits(Side.Player) == funds - 7, "Mothership mortar purchase uses configured price");
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Togus, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(12, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(12, 9)), (Side.Player, ShipClass.Garrison, new GridPosition(11, 9)) }, Array.Empty<GridPosition>());
        var volley = battle.Attack(Side.Player, 3, 4);
        Check(volley.Success && volley.Amount == 8 && battle.Find(5)!.Health == 4 && battle.Find(6)!.Health == 5, "Configured mortar splash preserves direct damage and excludes allies");
        battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Garrison, new GridPosition(2, 2)) }, Array.Empty<GridPosition>(), seaEvents: true);
        battle.Find(3)!.Position = battle.Treasuries.First().Position;
        battle.EndTurn(Side.Player);
        battle.EndTurn(Side.Enemy);
        battle.EndTurn(Side.Pirates);
        funds = battle.Credits(Side.Player);
        restored = BattleState.LoadJson(battle.SaveJson());
        Check(battle.LootTreasury(Side.Player, 3).Success && battle.Credits(Side.Player) == funds + 9, "Loot uses the configured weighted table and currency reward");
        Check(restored.LootTreasury(Side.Player, 3).Success && restored.SaveJson() == battle.SaveJson(), "Custom reward rules retain deterministic save and replay");
        var oldSave = JsonNode.Parse(Fixture().SaveJson())!;
        foreach (var key in new[]
        {
            "Mortar",
            "Balloon",
            "Treasury",
            "DockResourceReward",
            "PirateCurrencyReward",
            "PirateResourceReward"
        }

        )
            oldSave["Rules"]!.AsObject().Remove(key);
        restored = BattleState.LoadJson(oldSave.ToJsonString());
        Check(restored.Rules.Mortar == defaults.Mortar && restored.Rules.Balloon == defaults.Balloon && restored.Rules.Treasury == defaults.Treasury, "Version 1 saves without rule sections restore exact historical defaults");
    }
}
