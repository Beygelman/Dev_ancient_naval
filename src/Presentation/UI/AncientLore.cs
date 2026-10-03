<<<<<<< Updated upstream
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
            ShipClass.AncientGun => "The old tower watches without a keel; its stone shelters a distant bombard.",
            ShipClass.CannonTower => "A small tower keeps an unwavering watch over the nearby sea.",
            ShipClass.Lighthouse => "A light among the rocks joins distant ports and watches the open sea.",
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
            stats.Add(new("Progress cells", ship.Level < 5
                ? "Each resource fills one cell; complete the row to reach the next level."
                : "Maximum level; no further resource progress."));
        }
        if (ship.CanEarnVeterancy && !ship.IsVeteran)
        {
            stats.Add(new("Veterancy", $"{ship.Kills}/3 ships sunk"));
            stats.Add(new("Progress cells", "Each enemy ship sunk fills one cell. Three kills grant veteran status, full healing and +25% maximum health and weapon strength."));
            if (ship.Definition.VeteranRangeBonus > 0)
                stats.Add(new("Veteran reach", $"+{ship.Definition.VeteranRangeBonus} cannon range after three kills"));
        }
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
=======
using System.Linq;
using System.Text;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Short chart annotations, written from the current battle's rules.</summary>
internal static class AncientLore
{
    public static string Ship(BattleState battle, Ship ship)
    {
        string counsel = ship.Definition.Class switch
        {
            ShipClass.Mothership => "A city may sail, yet its captain must still choose which shore to call home.",
            ShipClass.Garrison => "Send the little brig where a heavier keel would hesitate; haste is its shield.",
            ShipClass.Invader => "The galleon carries a patient broadside. Let the enemy come within reach.",
            ShipClass.Kolonel => "The Kolonel speaks twice with iron, but a mighty hull turns slowly.",
            ShipClass.Togus => "The Granado sends its iron high. Speak with the mortar before moving your keel.",
            ShipClass.Fishing => "The quiet schooner feeds a fleet more faithfully than a loud cannon.",
            ShipClass.FishingDock => "Where the shoal endures, the quay may rise again after its timbers fall.",
            ShipClass.AncientGun => "The old tower keeps watch without a keel; its stone remembers no healing.",
            ShipClass.CannonTower => "A small tower guards a short stretch of water. Place it where that stretch matters.",
            ShipClass.PirateSchooner => "A pirate's sail follows the careless. Break it, and the sea yields a modest prize.",
            _ => "A watcher above the waves sees far; even the thinnest silk must fear the flagship's guns."
        };
        var text = new StringBuilder(counsel);
        text.Append($"\n\nHealth {ship.Health:0.##}/{ship.MaxHealth:0.##} · Sight {ship.VisualRange}");
        if (ship.HasRadar) text.Append($" · Radar {ship.RadarRange}");
        if (!ship.IsStructure) text.Append($"\nMovement {ship.MovementRemaining:0.#}/{ship.MovementAllowance} tiles this turn");
        if (ship.IsAirborne)
        {
            var bomb = battle.Rules.Balloon;
            text.Append($"\nBomb: {bomb.BombDamage} to its target, {bomb.SplashDamage} to adjacent tiles, including allies. Recharge: {bomb.CooldownTurns} turns.");
            text.Append("\nFly first, then release the bomb. A Mothership may strike this balloon within 3 tiles.");
            if (ship.BombCooldown > 0) text.Append($"\nReady in {ship.BombCooldown} turn(s).");
>>>>>>> Stashed changes
        }
        else if (ship.IsArmed)
        {
            if (!ship.HasMortar || ship.IsMothership)
<<<<<<< Updated upstream
                rows.Add(new("Cannons", $"{ship.CurrentDamage + ship.ShotDamageBonus:0.##} damage · {ship.CannonRange} tiles"));
            if (ship.HasMortar)
            {
                int minimum = ship.Definition.Class == ShipClass.AncientGun && !battle.Rules.Mortar.TowerDeadZone ? 1 : battle.Rules.Mortar.DeadZone + 1;
                string range = $"{minimum}–{ship.MortarRange}";
                rows.Add(new("Mortar", $"{ship.CurrentMortarDamage + ship.ShotDamageBonus:0.##} damage · {range} tiles"));
                rows.Add(new("Blast", $"{battle.Rules.Mortar.SplashDamage} to adjacent enemies; allies spared"));
                rows.Add(new("Against towns", $"+{battle.Rules.Mortar.VillageDamageBonus} mortar damage"));
            }
            rows.Add(new("Shots", $"{ship.AttacksRemaining} left this turn"));
            if (battle.Rules.DoubleSalvo && (ship.Definition.Class == ShipClass.Kolonel || ship.IsMothership && ship.SecondAttackUpgrade))
                rows.Add(new("Double salvo", "Select a cannon target, then choose one or two cannonballs on its parchment; two shots launch together and receive one enemy reply"));
            if (ship.Definition.Class is not (ShipClass.Togus or ShipClass.AncientGun))
                rows.Add(new("Counterfire", $"{ship.CurrentDamage + ship.CounterDamageBonus:0.##} damage · {ship.CannonRange} tiles; if alive"));
            if (battle.HasAntiAir(ship))
                rows.Add(new("Anti-air", $"Balloons within {System.Math.Min(battle.Rules.Balloon.AntiAirRange, ship.CannonRange)} tiles"));
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
        else if (ship.Definition.Class == ShipClass.AncientGun && battle.Rules.AncientAutoRepairAmount > 0)
            rows.Add(new("Passive repair", $"+{battle.Rules.AncientAutoRepairAmount} HP after a turn without an active attack"));
        if (ship.IsMothership)
            rows.Add(new("Shipyard", CurrentShipyard(battle, ship.Level, false)));
        else if (ship.Definition.Class == ShipClass.Fishing && battle.Rules.FishingLighthouses && battle.Rules.LighthousesEnabled)
            rows.Add(new("Shipyard", battle.Rules.Get(ShipClass.Lighthouse).Name));
        if (battle.Rules.SmallHullRadarStealth && (ship.Definition.Class == ShipClass.Garrison
            || ship.Definition.Class == ShipClass.Fishing && !battle.Rules.FishingRadarVisible))
            rows.Add(new("Low profile", "Hidden from radar; visible to nearby lookouts"));
        if (battle.Rules.HeavenlyAssistance && ship.IsMothership && ship.Owner == Side.Player)
            rows.Add(new("Heavenly blessing", "+2 Thors every fifth personal turn"));
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
        if (ship.IsVeteran && ship.Definition.VeteranRangeBonus > 0)
            rows.Add(new("Veteran reach", $"+{ship.Definition.VeteranRangeBonus} cannon range"));
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
                new("Progress cells", battle.Rules.PaidVillageUpgrades
                    ? "Town level grows through paid upgrades; each upgrade uses the town's construction for this turn."
                    : "A living owned town fills one cell each turn and grows after two turns, up to level 5."),
                new("Next level", village.Level >= 5 ? "Maximum level" : battle.Rules.PaidVillageUpgrades
                    ? $"{battle.VillageUpgradePrice(village.Owner ?? Side.Player, village.Id)} Thors" : "Automatic growth"),
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
        if (battle.Rules.HeavenlyAssistance && village.Owner == Side.Player && village.Health > 0)
            sections.Add(new("Faith", new LoreRow[] { new("Heavenly blessing", "+2 Thors every fifth personal turn") }));
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
        if (battle.Rules.LighthousesEnabled && (!town || !battle.Rules.FishingLighthouses) && level >= (town ? 3 : 1))
            classes = classes.Append(ShipClass.Lighthouse);
        return string.Join(", ", classes.Where(kind => (town ? BattleState.VillageRequiredLevel(kind) : BattleState.RequiredLevel(kind)) <= level)
            .Select(kind => battle.Rules.Get(kind).Name));
    }

    public static (string Title, LorePage Page)? Cell(BattleState battle, GridPosition cell)
    {
        if (battle.IsForbidden(cell))
            return ("Whirlpool", new("The sea has opened its mouth; no keel may cross these cursed waters.", new[]
            {
                new LoreSection("Hazard", new LoreRow[] { new("Area", "9 impassable tiles"), new("Passage", "Forbidden") })
            }));
=======
                text.Append($"\nCannon damage {ship.CurrentDamage + ship.ShotDamageBonus:0.##} · Range {ship.Definition.AttackRange} · Shots left {ship.AttacksRemaining}");
            if (ship.HasMortar)
            {
                string range = ship.Definition.Class == ShipClass.AncientGun ? $"1–{ship.MortarRange}" : $"4–{ship.MortarRange}";
                text.Append($"\nMortar: {ship.CurrentMortarDamage + ship.ShotDamageBonus:0.##} damage at {range} tiles; +{battle.Rules.Mortar.VillageDamageBonus} against towns.");
                text.Append($"\nSplash: {battle.Rules.Mortar.SplashDamage} to adjacent enemies; friendly ships are spared. Fire before moving.");
            }
            if (ship.Definition.Class is not (ShipClass.Togus or ShipClass.AncientGun))
                text.Append($"\nCounterattack: up to {ship.CurrentDamage + ship.CounterDamageBonus:0.##} damage within {ship.Definition.AttackRange} tiles, if the vessel survives.");
            if (!ship.IsStructure && !ship.HasMortar)
                text.Append(ship.Definition.ActionProfile switch
                {
                    ActionProfile.Heavy => "\nMovement precedes cannon fire; the heavy crew can fire twice.",
                    ActionProfile.Scout => "\nThe nimble crew may fire and keep sailing.",
                    _ => "\nOnce the crew has moved and fired, further sailing must wait."
                });
        }
        if (ship.Definition.IncomePerTurn > 0) text.Append($"\nIncome: +{ship.Definition.IncomePerTurn} Thors each turn.");
        if (ship.Definition.CollectionRange > 0)
            text.Append(battle.Rules.Economy.AdjacentCollectionOnly
                ? "\nCollect resources from an adjacent tile, or raise a Fishing Dock on a shoal."
                : $"\nCollection reach: {ship.Definition.CollectionRange} tiles. Raise a Fishing Dock on a shoal.");
        if (ship.CanRepair || !ship.IsAirborne && ship.Definition.Class != ShipClass.AncientGun)
            text.Append($"\nRepair: +{battle.Rules.RepairAmount} HP actively; +{battle.Rules.AutoRepairAmount} HP automatically after a turn without an active attack.");
        if (ship.IsMothership)
        {
            text.Append($"\nIncome: +{battle.Rules.IncomePerMothership + battle.Rules.Economy.MothershipIncomePerLevel * (ship.Level - 1) + (ship.IncomeUpgrade ? 1 : 0)} Thors each turn.");
            text.Append($"\nLevel {ship.Level}/5 · Resources {ship.Resources}/{ship.ResourcesRequired}. The fourth level grants one further movement tile.");
        }
        if (ship.Definition.Class == ShipClass.PirateSchooner)
            text.Append($"\nBounty: {battle.Rules.PirateCurrencyReward} Thors and {battle.Rules.PirateResourceReward} Mothership resource.");
        if (!ship.IsMothership && !ship.IsAirborne && !ship.IsStructure)
            text.Append(ship.IsVeteran ? "\nVeteran: greater health and damage, earned at sea." : $"\nVeterancy: {ship.Kills}/3 ships sunk.");
        return text.ToString();
    }

    public static string Village(BattleState battle, Village village)
    {
        string text = $"A town outlives its broken walls. Lower its defenses to 0, keep a warship alongside through one further turn, then claim its flag. The claiming crew spends all remaining actions.\n\nLevel {village.Level}/5 · Health {village.Health:0.##}/{village.MaxHealth:0.##} · Sight {village.VisualRange}\nIncome: +{battle.VillageIncome(village)} Thors each turn while owned defenses stand.\nGrowth: one level every 2 owned turns; each level adds 5 HP.\nShipyard: Fishing Schooner at level 1, Brig and Cannon Tower at 2, Galleon at 3, Kolonel at 4, Granado at 5.\nRepair: +{battle.Rules.RepairAmount} HP actively or after a turn without an active attack.";
        return text + $"\nFortification: {battle.FortificationPrice(Side.Player)} Thors; 25% damage resistance, counterattack 3 at range 3, and 2 additional sight tiles.";
    }

    public static (string Title, string Text) Cell(BattleState battle, GridPosition cell)
    {
        if (battle.IsForbidden(cell))
            return ("Whirlpool", "The sea has opened its mouth. No keel may cross these nine cursed tiles; the vessel that woke it was claimed at once.");
>>>>>>> Stashed changes
        bool visible = battle.Vision.IsVisible(Side.Player, cell);
        if (visible && battle.TreasuryAt(cell) is not null)
        {
            var rule = battle.Rules.Treasury;
<<<<<<< Updated upstream
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
        return null;
=======
            return ("Ancient treasury", $"Old riches wait for a patient crew. Place a warship here and let one turn pass before plundering.\n\nAncient Mortar Tower: {rule.AncientGunWeight}%\n{rule.CurrencyReward} Thors: {rule.CurrencyWeight}%\nAncient Balloon: {rule.AncientBalloonWeight}%\n{rule.ResourceReward} Mothership resources: {rule.ResourcesWeight}%\nDeadly whirlpool: {rule.WhirlpoolWeight}% — the claiming ship is lost and nine tiles become impassable.");
        }
        if (visible && battle.Shoals.Contains(cell))
            return ("Fishing shoal", $"Build where the sea provides, and return to the same shoal should the dock fall.\n\nFishing Dock: {battle.DockPrice(Side.Player)} Thors · {battle.Rules.Get(ShipClass.FishingDock).MaxHealth} HP\nIncome: +{battle.Rules.Get(ShipClass.FishingDock).IncomePerTurn} each turn\nMothership reward: {battle.Rules.DockResourceReward} resources\nKeep a collecting ship {(battle.Rules.Economy.AdjacentCollectionOnly ? "on an adjacent tile" : "within collection reach")}; a level 2 Mothership is required.");
        if (visible && battle.FishSpots.Contains(cell))
            return ("Resource shoal", $"Bring a collecting ship {(battle.Rules.Economy.AdjacentCollectionOnly ? "alongside" : "within reach")}, for the sea grants nothing to a distant hand.\n\nCollection: {battle.CollectionCost(Side.Player)} Thors\nReward: 1 Mothership resource\nThe shoal is spent after collection.");
        return battle.Board.GetTile(cell).Terrain == DevAncientNaval.Core.World.TerrainType.Land
            ? ("Island", "Stone and trees mark land no ship can cross. Seek the white shore for a sheltered approach; hidden water must first be charted.")
            : ("Sea", "A captain reads the water before choosing a course. Friendly ships permit passage; an enemy blocks its own tile and doubles movement cost on the neighboring tiles.");
>>>>>>> Stashed changes
    }
}
