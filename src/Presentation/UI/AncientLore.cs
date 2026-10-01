using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Short counsel and current facts, never a catalogue of future upgrades.</summary>
internal static class AncientLore
{
    public static LorePage Ship(BattleState battle, Ship ship)
    {
        string counsel = ship.Definition.Class switch
        {
            ShipClass.Mothership => "A city may sail; this broad keel carries the heart of your fleet.",
            ShipClass.Garrison => "The little brig guards narrow waters; haste is its shield.",
            ShipClass.Invader => "The galleon carries a patient broadside, ready when the enemy draws near.",
            ShipClass.Kolonel => "The Kolonel speaks twice with iron, though its mighty hull turns slowly.",
            ShipClass.Togus => "The Granado casts iron high; let its mortar speak before its keel moves.",
            ShipClass.Fishing => "The quiet schooner feeds a fleet more faithfully than a loud cannon.",
            ShipClass.FishingDock => "A quay draws wealth from the shoal; the fish endure when its timbers fall.",
            ShipClass.AncientGun => "The old tower watches without a keel; its stone remembers no healing.",
            ShipClass.CannonTower => "A small tower keeps an unwavering watch over the nearby sea.",
            ShipClass.PirateSchooner => "The pirate follows careless sails; its defeat leaves a modest prize.",
            _ => "A silk watcher sees beyond the waves and carries thunder beneath its basket."
        };
        var sections = new List<LoreSection>();
        var stats = new List<LoreRow>
        {
            new("Health", $"{ship.Health:0.##}/{ship.MaxHealth:0.##} HP"),
            new("Sight", $"{ship.VisualRange} tiles")
        };
        if (ship.HasRadar)
            stats.Add(new("Radar", $"{ship.RadarRange} tiles"));
        if (!ship.IsStructure)
            stats.Add(new("Movement", $"{ship.MovementRemaining:0.#}/{ship.MovementAllowance} tiles left"));
        if (ship.IsMothership)
        {
            stats.Add(new("Level", $"{ship.Level}/5"));
            if (ship.Level < 5)
                stats.Add(new("Resources", $"{ship.Resources}/{ship.ResourcesRequired}"));
        }
        if (ship.CanEarnVeterancy && !ship.IsVeteran)
            stats.Add(new("Veterancy", $"{ship.Kills}/3 ships sunk"));
        sections.Add(new("At a glance", stats));
        AddWeapons(sections, battle, ship);
        AddCrew(sections, battle, ship);
        AddImprovements(sections, ship);
        return new(counsel, sections);
    }

    private static void AddWeapons(List<LoreSection> sections, BattleState battle, Ship ship)
    {
        var rows = new List<LoreRow>();
        if (ship.IsAirborne)
        {
            var bomb = battle.Rules.Balloon;
            rows.Add(new("Bomb", $"{bomb.BombDamage} damage to target"));
            rows.Add(new("Blast", $"{bomb.SplashDamage} to adjacent tiles; allies too"));
            rows.Add(new("Recharge", $"Every {bomb.CooldownTurns} turns"));
            rows.Add(new("Readiness", ship.BombCooldown > 0 ? $"{ship.BombCooldown} turns remaining" : "Ready after flying"));
            rows.Add(new("Vulnerability", $"{(bomb.KolonelAntiAir ? "Flagship and Kolonel" : "Flagship")} guns within {bomb.AntiAirRange} tiles"));
        }
        else if (ship.IsArmed)
        {
            if (!ship.HasMortar || ship.IsMothership)
                rows.Add(new("Cannons", $"{ship.CurrentDamage + ship.ShotDamageBonus:0.##} damage · {ship.Definition.AttackRange} tiles"));
            if (ship.HasMortar)
            {
                string range = ship.Definition.Class == ShipClass.AncientGun ? $"1–{ship.MortarRange}" : $"4–{ship.MortarRange}";
                rows.Add(new("Mortar", $"{ship.CurrentMortarDamage + ship.ShotDamageBonus:0.##} damage · {range} tiles"));
                rows.Add(new("Blast", $"{battle.Rules.Mortar.SplashDamage} to adjacent enemies; allies spared"));
                rows.Add(new("Against towns", $"+{battle.Rules.Mortar.VillageDamageBonus} mortar damage"));
            }
            rows.Add(new("Shots", $"{ship.AttacksRemaining} left this turn"));
            if (ship.Definition.Class is not (ShipClass.Togus or ShipClass.AncientGun))
                rows.Add(new("Counterfire", $"{ship.CurrentDamage + ship.CounterDamageBonus:0.##} damage · {ship.Definition.AttackRange} tiles; if alive"));
            if (battle.HasAntiAir(ship))
                rows.Add(new("Anti-air", $"Balloons within {System.Math.Min(battle.Rules.Balloon.AntiAirRange, ship.Definition.AttackRange)} tiles"));
            rows.Add(new("Radar targets", "Shared contacts within weapon range"));
        }
        if (rows.Count > 0)
            sections.Add(new("Weapons", rows));
    }

    private static void AddCrew(List<LoreSection> sections, BattleState battle, Ship ship)
    {
        var rows = new List<LoreRow>();
        if (ship.HasMortar && !ship.IsStructure)
            rows.Add(new("Order", "Fire before moving"));
        else if (ship.IsArmed && !ship.IsStructure)
            rows.Add(new("Order", ship.Definition.ActionProfile switch
            {
                ActionProfile.Heavy => "Move, then fire; no sailing after firing",
                ActionProfile.Scout => "Fire and keep sailing",
                _ => "No further sailing after moving and firing"
            }));
        int income = ship.IsMothership ? battle.Rules.IncomePerMothership
            + battle.Rules.Economy.MothershipIncomePerLevel * (ship.Level - 1)
            + (ship.IncomeUpgrade ? 1 : 0) : ship.Definition.IncomePerTurn;
        if (income > 0)
            rows.Add(new("Income", $"+{income} Thors per turn"));
        if (ship.Definition.CollectionRange > 0)
            rows.Add(new("Collection", battle.Rules.Economy.AdjacentCollectionOnly
                ? "Same or adjacent tile" : $"Within {ship.Definition.CollectionRange} tiles"));
        if (!ship.IsAirborne && ship.Definition.Class != ShipClass.AncientGun)
        {
            rows.Add(new("Repair", $"+{battle.Rules.RepairAmount} HP; spends actions"));
            rows.Add(new("Passive repair", $"+{battle.Rules.AutoRepairAmount} HP after a turn without an active attack"));
        }
        if (ship.IsMothership)
            rows.Add(new("Shipyard", CurrentShipyard(battle, ship.Level, false)));
        if (ship.Definition.Class == ShipClass.PirateSchooner)
            rows.Add(new("Bounty", $"{battle.Rules.PirateCurrencyReward} Thors · {battle.Rules.PirateResourceReward} resource"));
        if (rows.Count > 0)
            sections.Add(new("Crew & economy", rows));
    }

    private static void AddImprovements(List<LoreSection> sections, Ship ship)
    {
        var rows = new List<LoreRow>();
        if (ship.IsVeteran)
            rows.Add(new("Veteran", "+25% hull health and weapon strength"));
        if (ship.HasRadar && ship.Definition.Class is ShipClass.Mothership or ShipClass.Kolonel or ShipClass.CannonTower)
            rows.Add(new("Radar", $"Installed · {ship.RadarRange} tiles"));
        if (ship.IsMothership && ship.HasMortar)
            rows.Add(new("Mortar", "Installed; cannons cover close targets"));
        if (ship.MobilityUpgrade)
            rows.Add(new("Propulsion", "+1 movement"));
        if (ship.IsMothership && ship.Level >= 4)
            rows.Add(new("Level-4 keel", "+1 movement"));
        if (ship.VisionUpgrade)
            rows.Add(new("Lookout", "+2 sight · +2 radar reach"));
        if (ship.RestorationUpgrade)
            rows.Add(new("Reinforced hull", "+5 HP · +2 HP at turn start"));
        if (ship.SecondAttackUpgrade)
            rows.Add(new("Extra broadside", "+1 attack per turn"));
        if (ship.ShipwrightUpgrade)
            rows.Add(new("Shipwright", "25% cheaper ships"));
        if (ship.FirepowerUpgrade)
            rows.Add(new("Heavy shot", "+3 shot damage · +2 counterfire"));
        if (ship.IncomeUpgrade)
            rows.Add(new("Trade stores", "+1 Thor per turn"));
        if (rows.Count > 0)
            sections.Add(new("Installed improvements", rows));
    }

    public static LorePage Village(BattleState battle, Village village)
    {
        var sections = new List<LoreSection>
        {
            new("At a glance", new LoreRow[]
            {
                new("Level", $"{village.Level}/5"),
                new("Health", $"{village.Health:0.##}/{village.MaxHealth:0.##} HP"),
                new("Sight", $"{village.VisualRange} tiles")
            }),
            new("Town life", new LoreRow[]
            {
                new("Income", $"+{(village.Owner is null || village.Health <= 0 ? 0 : battle.VillageIncome(village) + (village.HasPort ? battle.Rules.Ports.Income : 0))} Thors per turn"),
                new("Shipyard", CurrentShipyard(battle, village.Level, true)),
                new("Repair", $"+{battle.Rules.RepairAmount} HP; replaces production and fire"),
                new("Passive repair", $"+{battle.Rules.RepairAmount} HP after a turn without an active attack")
            }),
            new("Capture", new LoreRow[]
            {
                new("1 · Defeat", "Reduce defenses to 0 HP"),
                new("2 · Hold", "Keep a warship alongside until its next turn"),
                new("3 · Claim", "Use the flag; the crew spends all actions")
            })
        };
        var improvements = new List<LoreRow>();
        if (village.IsFortified)
        {
            improvements.Add(new("Outpost", "25% damage resistance · +2 sight"));
            bool gunsActive = !battle.Rules.VillageCombat.AutomaticAttack || village.Level >= 2;
            if (gunsActive)
            {
                if (battle.Rules.VillageCombat.AutomaticAttack)
                    improvements.Add(new("Automatic guns", $"{battle.VillageAttackDamage(village)} damage · {battle.Rules.VillageCombat.AttackRange} tiles; weakest enemy first"));
                improvements.Add(new("Counterfire", $"{battle.VillageCounterDamage(village)} damage · {battle.Rules.VillageCombat.CounterRange} tiles"));
            }
        }
        if (village.HasPort)
        {
            improvements.Add(new("Port", $"+{battle.Rules.Ports.Income} Thor per turn"));
            improvements.Add(new("Local shipyard", $"{battle.Rules.Ports.Discount:P0} cheaper ships"));
            improvements.Add(new("Trade lanes", $"At least +{battle.Rules.Ports.MinimumBonus} tiles or +{battle.Rules.Ports.MovementBonus:P0}; stay on the lane"));
        }
        if (improvements.Count > 0)
            sections.Add(new("Installed improvements", improvements));
        return new("A town outlives its broken walls; its harbor shelters the fleet that holds its flag.", sections);
    }

    private static string CurrentShipyard(BattleState battle, int level, bool town)
    {
        var classes = new[] { ShipClass.Fishing, ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel, ShipClass.Togus }
            .Concat(town ? System.Array.Empty<ShipClass>() : new[] { ShipClass.CannonTower });
        return string.Join(", ", classes.Where(kind => (town ? BattleState.VillageRequiredLevel(kind) : BattleState.RequiredLevel(kind)) <= level)
            .Select(kind => battle.Rules.Get(kind).Name));
    }

    public static (string Title, LorePage Page) Cell(BattleState battle, GridPosition cell)
    {
        if (battle.IsForbidden(cell))
            return ("Whirlpool", new("The sea has opened its mouth; no keel may cross these cursed waters.", new[]
            {
                new LoreSection("Hazard", new LoreRow[] { new("Area", "9 impassable tiles"), new("Passage", "Forbidden") })
            }));
        bool visible = battle.Vision.IsVisible(Side.Player, cell);
        if (visible && battle.TreasuryAt(cell) is not null)
        {
            var rule = battle.Rules.Treasury;
            return ("Ancient treasury", new("Old riches wait for a patient crew; the sea keeps one dark bargain among its gifts.", new[]
            {
                new LoreSection("Plunder", new LoreRow[] { new("Crew", "A warship on this tile"), new("Waiting", "One turn before claiming") }),
                new LoreSection("Possible spoils", new LoreRow[]
                {
                    new("Mortar tower", $"{rule.AncientGunWeight}%"),
                    new($"{rule.CurrencyReward} Thors", $"{rule.CurrencyWeight}%"),
                    new("Ancient Balloon", $"{rule.AncientBalloonWeight}%"),
                    new($"{rule.ResourceReward} resources", $"{rule.ResourcesWeight}%"),
                    new("Whirlpool", $"{rule.WhirlpoolWeight}%; loses crew and blocks 9 tiles")
                })
            }));
        }
        if (visible && battle.Shoals.Contains(cell))
            return ("Fishing shoal", new("The reef feeds a lasting quay; its fish remain when a fallen dock must rise again.", new[]
            {
                new LoreSection("Shoal", new LoreRow[]
                {
                    new("Fishing Dock", $"{battle.DockPrice(Side.Player)} Thors · {battle.Rules.Get(ShipClass.FishingDock).MaxHealth} HP"),
                    new("Income", $"+{battle.Rules.Get(ShipClass.FishingDock).IncomePerTurn} Thors per turn"),
                    new("Reward", $"{battle.Rules.DockResourceReward} Mothership resources"),
                    new("Crew", battle.Rules.Economy.AdjacentCollectionOnly ? "Same or adjacent tile" : "Within collection reach"),
                    new("Requirement", "Level-2 Mothership")
                })
            }));
        if (visible && battle.FishSpots.Contains(cell))
            return ("Resource shoal", new("Bring a collecting keel alongside; the sea grants nothing to a distant hand.", new[]
            {
                new LoreSection("Collection", new LoreRow[]
                {
                    new("Cost", $"{battle.CollectionCost(Side.Player)} Thors"),
                    new("Reward", "1 Mothership resource"),
                    new("Reach", battle.Rules.Economy.AdjacentCollectionOnly ? "Same or adjacent tile" : "Within collection reach"),
                    new("Afterward", "Shoal is spent")
                })
            }));
        if (battle.Board.GetTile(cell).Terrain == TerrainType.Land)
            return ("Island", new("Stone and trees keep this shore beyond the reach of any keel.", new[]
            {
                new LoreSection("Terrain", new LoreRow[] { new("Passage", "Ships cannot cross land") })
            }));
        return ("Sea", new("Read the water before choosing a course; each wake shares the same sea.", new[]
        {
            new LoreSection("Navigation", new LoreRow[]
            {
                new("Friendly ships", "Allow passage"),
                new("Enemy ships", "Block their own tile"),
                new("Enemy waters", "Adjacent tiles cost twice the movement")
            })
        }));
    }
}
