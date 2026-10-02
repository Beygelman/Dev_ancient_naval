using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Information reflects active rules and installed equipment, not unlock promises.</summary>
internal static class LoreChecks
{
    internal static BattleState Example(BattleRules rules)
    {
        var towns = new[] { new GridPosition(8, 5), new GridPosition(12, 5) };
        var board = new GameBoard(28, 20, p => towns.Contains(p) ? TerrainType.Land : TerrainType.Water, seed: 731);
        var ships = Enum.GetValues<ShipClass>().Select((kind, i) => (Side.Player, kind, new GridPosition(2 + i * 2, 10)))
            .Append((Side.Enemy, ShipClass.Mothership, new GridPosition(25, 17)));
        var battle = new BattleState(board, rules, ships, new[] { new GridPosition(4, 12) }, villageSpots: towns);
        var saved = battle.CaptureSnapshot();
        var mother = saved.Ships.First(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership);
        mother.Level = 5;
        mother.Health = rules.Get(ShipClass.Mothership).MaxHealth + 4 * rules.Get(ShipClass.Mothership).HealthPerLevel;
        mother.MobilityUpgrade = true;
        mother.VisionUpgrade = true;
        mother.SecondAttackUpgrade = true;
        mother.ShipwrightUpgrade = true;
        mother.HasRadar = true;
        mother.HasMortar = true;
        var veteran = saved.Ships.First(s => s.Kind == ShipClass.CannonTower);
        veteran.IsVeteran = true;
        veteran.Kills = 3;
        veteran.HasRadar = true;
        saved.Villages = saved.Villages.Select((v, i) => i == 0 ? v : v with
        {
            Level = 4, Health = 20, Owner = Side.Player, Fortified = true, Port = true
        }).ToArray();
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        battle.SetGodEye(true);
        return battle;
    }

    internal static int Run(BattleState actual)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            checks++;
        }
        string Field(LorePage page, string label) => page.Sections.SelectMany(s => s.Rows).Single(r => r.Label == label).Value;
        var rules = actual.Rules;
        var example = Example(rules);
        foreach (var ship in example.OwnShips(Side.Player))
        {
            var page = AncientLore.Ship(example, ship);
            Check(page.Summary.Length <= 100 && !page.Summary.Contains('\n'), "Counsel is one short sentence: " + ship.Name);
            Check(page.Sections.All(s => s.Rows.Count > 0 && s.Rows.All(r => r.Label.Length > 0 && r.Value.Length > 0 && !r.Value.Contains('\n'))), "Facts have separate labelled rows: " + ship.Name);
            Check(Field(page, "Health") == $"{ship.Health:0.##}/{ship.MaxHealth:0.##} HP", "Health is current: " + ship.Name);
            Check(!page.PlainText.Contains("unlocks") && !page.PlainText.Contains("at level"), "No future upgrade catalogue: " + ship.Name);
        }
        var mother = example.Mothership(Side.Player)!;
        var installed = AncientLore.Ship(example, mother).Sections.Single(s => s.Title == "Installed improvements").Rows;
        Check(installed.Select(r => r.Label).SequenceEqual(new[] { "Radar", "Mortar", "Propulsion", "Level-4 keel", "Lookout", "Extra broadside", "Shipwright" }), "Only installed Mother improvements are listed");
        Check(!installed.Any(r => r.Label is "Heavy shot" or "Reinforced hull"), "Unchosen level rewards remain absent");
        var unmodified = example.Mothership(Side.Enemy)!;
        Check(!AncientLore.Ship(example, unmodified).Sections.Any(s => s.Title == "Installed improvements"), "Base ship has no invented improvements");
        Check(AncientLore.Ship(example, unmodified).Sections.Single(s => s.Title == "Crew & economy").Rows.Single(r => r.Label == "Shipyard").Value
            == (rules.LighthousesEnabled ? "Fishing Schooner, Brig, Lighthouse" : "Fishing Schooner, Brig"), "Shipyard lists only presently available names");
        var tower = example.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.CannonTower);
        Check(AncientLore.Ship(example, tower).Sections.Single(s => s.Title == "Installed improvements").Rows.Any(r => r.Label == "Veteran"), "Tower veterancy is an installed improvement");
        var bareTown = AncientLore.Village(example, example.Villages[0]);
        Check(!bareTown.Sections.Any(s => s.Title == "Installed improvements") && !bareTown.PlainText.Contains("Port"), "Unbuilt town upgrades are not advertised");
        Check(Field(bareTown, "Income") == "+0 Thors per turn", "Unowned town has no active income");
        var city = AncientLore.Village(example, example.Villages[1]);
        var cityShipyard = Field(city, "Shipyard");
        Check(cityShipyard.Split(", ").All(name => rules.Ships.Any(definition => definition.Name == name)),
            "producer counsel lists class names without ship statistics or descriptions");
        Check(cityShipyard.Contains("Lighthouse") == rules.LighthousesEnabled, "town shipyard includes its unlocked Lighthouse");
        Check(city.Sections.Single(s => s.Title == "Installed improvements").Rows.Any(r => r.Label == "Port"), "Built port has its own row");
        Check(Field(city, "Local shipyard") == $"{rules.Ports.Discount:P0} cheaper ships", "Port discount follows voyage rules");
        Check(Field(city, "Automatic guns").StartsWith($"{example.VillageAttackDamage(example.Villages[1])} damage"), "Outpost reports this level's guns");
        var shoal = AncientLore.Cell(example, new(4, 12)).Page;
        Check(Field(shoal, "Reward") == "1 Mothership resource", "Resource reward stays concise and correct");
        var changed = example.CaptureSnapshot();
        changed.Rules = new BattleRules
        {
            StartingCredits = rules.StartingCredits, IncomePerMothership = rules.IncomePerMothership,
            RepairAmount = 7, AutoRepairAmount = 3, FleetLimit = rules.FleetLimit, Ships = rules.Ships,
            Economy = rules.Economy, Mortar = rules.Mortar, Balloon = rules.Balloon, VillageCombat = rules.VillageCombat,
            Ports = new PortRules { Price = rules.Ports.Price, Discount = .3, Income = 2,
                MinimumBonus = rules.Ports.MinimumBonus, MovementBonus = rules.Ports.MovementBonus }
        };
        var custom = BattleState.LoadJson(BattleState.SerializeSnapshot(changed));
        Check(Field(AncientLore.Ship(custom, custom.Mothership(Side.Player)!), "Passive repair").StartsWith("+3 HP"), "Saved repair balance appears in rows");
        Check(Field(AncientLore.Village(custom, custom.Villages[1]), "Local shipyard") == "30% cheaper ships", "Saved port balance appears in rows");
        Check(Field(AncientLore.Village(custom, custom.Villages[1]), "Passive repair").StartsWith("+7 HP"), "Village repair follows active rules");
        return checks;
    }
}
