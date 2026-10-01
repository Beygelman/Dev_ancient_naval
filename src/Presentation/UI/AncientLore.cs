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
        if (ship.HasRadar)
            text.Append($" · Radar {ship.RadarRange}");
        if (!ship.IsStructure)
            text.Append($"\nMovement {ship.MovementRemaining:0.#}/{ship.MovementAllowance} tiles this turn");
        if (ship.IsAirborne)
        {
            var bomb = battle.Rules.Balloon;
            text.Append($"\nBomb: {bomb.BombDamage} to its target, {bomb.SplashDamage} to adjacent tiles, including allies. Recharge: {bomb.CooldownTurns} turns.");
            text.Append($"\nFly first, then release the bomb. A Mothership may strike this balloon within {battle.Rules.Balloon.AntiAirRange} tiles.");
            if (ship.BombCooldown > 0)
                text.Append($"\nReady in {ship.BombCooldown} turn(s).");
        }
        else if (ship.IsArmed)
        {
            if (!ship.HasMortar || ship.IsMothership)
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

        if (ship.Definition.IncomePerTurn > 0)
            text.Append($"\nIncome: +{ship.Definition.IncomePerTurn} Thors each turn.");
        if (ship.Definition.CollectionRange > 0)
            text.Append(battle.Rules.Economy.AdjacentCollectionOnly ? "\nCollect resources from an adjacent tile, or raise a Fishing Dock on a shoal." : $"\nCollection reach: {ship.Definition.CollectionRange} tiles. Raise a Fishing Dock on a shoal.");
        if (ship.CanRepair || !ship.IsAirborne && ship.Definition.Class != ShipClass.AncientGun)
            text.Append($"\nRepair: +{battle.Rules.RepairAmount} HP actively; +{battle.Rules.AutoRepairAmount} HP automatically after a turn without an active attack.");
        if (ship.IsMothership)
        {
            text.Append($"\nIncome: +{battle.Rules.IncomePerMothership + battle.Rules.Economy.MothershipIncomePerLevel * (ship.Level - 1) + (ship.IncomeUpgrade ? 1 : 0)} Thors each turn.");
            text.Append($"\nLevel {ship.Level}/5 · Resources {ship.Resources}/{ship.ResourcesRequired}. The fourth level grants one further movement tile.");
        }

        if (ship.Definition.Class == ShipClass.PirateSchooner)
            text.Append($"\nBounty: {battle.Rules.PirateCurrencyReward} Thors and {battle.Rules.PirateResourceReward} Mothership resource.");
        if (ship.CanEarnVeterancy)
            text.Append(ship.IsVeteran ? "\nVeteran: greater health and damage, earned at sea." : $"\nVeterancy: {ship.Kills}/3 ships sunk.");
        return text.ToString();
    }

    public static string Village(BattleState battle, Village village)
    {
        string text = $"A town outlives its broken walls. Lower its defenses to 0, keep a warship alongside through one further turn, then claim its flag. The claiming crew spends all remaining actions.\n\nLevel {village.Level}/5 · Health {village.Health:0.##}/{village.MaxHealth:0.##} · Sight {village.VisualRange}\nIncome: +{battle.VillageIncome(village)} Thors each turn while owned defenses stand.\nGrowth: one level every 2 owned turns; each level adds 5 HP.\nShipyard: Fishing Schooner at level 1, Brig at 2, Galleon at 3, Kolonel at 4, Granado at 5.\nRepair: +{battle.Rules.RepairAmount} HP actively or after a turn without an active attack.";
        return text + (battle.Rules.VillageCombat.AutomaticAttack ? $"\nOutpost: automatic fire at the end of the owner turn, from level 2, range {battle.Rules.VillageCombat.AttackRange}; weakest visible enemy first. Damage {battle.VillageAttackDamage(village)} now (2/3/4/5 by level); counterattack {battle.VillageCounterDamage(village)} at range {battle.Rules.VillageCombat.CounterRange}. Villages cannot build towers.\n" : "") + $"\nFortification: {battle.FortificationPrice(Side.Player)} Thors; 25% damage resistance, {(battle.Rules.VillageCombat.AutomaticAttack ? "level-scaled guns" : "counterattack 3 at range 3")}, and 2 additional sight tiles.";
    }

    public static (string Title, string Text) Cell(BattleState battle, GridPosition cell)
    {
        if (battle.IsForbidden(cell))
            return ("Whirlpool", "The sea has opened its mouth. No keel may cross these nine cursed tiles; the vessel that woke it was claimed at once.");
        bool visible = battle.Vision.IsVisible(Side.Player, cell);
        if (visible && battle.TreasuryAt(cell)is not null)
        {
            var rule = battle.Rules.Treasury;
            return ("Ancient treasury", $"Old riches wait for a patient crew. Place a warship here and let one turn pass before plundering.\n\nAncient Mortar Tower: {rule.AncientGunWeight}%\n{rule.CurrencyReward} Thors: {rule.CurrencyWeight}%\nAncient Balloon: {rule.AncientBalloonWeight}%\n{rule.ResourceReward} Mothership resources: {rule.ResourcesWeight}%\nDeadly whirlpool: {rule.WhirlpoolWeight}% — the claiming ship is lost and nine tiles become impassable.");
        }

        if (visible && battle.Shoals.Contains(cell))
            return ("Fishing shoal", $"Build where the sea provides, and return to the same shoal should the dock fall.\n\nFishing Dock: {battle.DockPrice(Side.Player)} Thors · {battle.Rules.Get(ShipClass.FishingDock).MaxHealth} HP\nIncome: +{battle.Rules.Get(ShipClass.FishingDock).IncomePerTurn} each turn\nMothership reward: {battle.Rules.DockResourceReward} resources\nKeep a collecting ship {(battle.Rules.Economy.AdjacentCollectionOnly ? "on an adjacent tile" : "within collection reach")}; a level 2 Mothership is required.");
        if (visible && battle.FishSpots.Contains(cell))
            return ("Resource shoal", $"Bring a collecting ship {(battle.Rules.Economy.AdjacentCollectionOnly ? "alongside" : "within reach")}, for the sea grants nothing to a distant hand.\n\nCollection: {battle.CollectionCost(Side.Player)} Thors\nReward: 1 Mothership resource\nThe shoal is spent after collection.");
        return battle.Board.GetTile(cell).Terrain == DevAncientNaval.Core.World.TerrainType.Land ? ("Island", "Stone and trees mark land no ship can cross. Seek the white shore for a sheltered approach; hidden water must first be charted.") : ("Sea", "A captain reads the water before choosing a course. Friendly ships permit passage; an enemy blocks its own tile and doubles movement cost on the neighboring tiles.");
    }
}
