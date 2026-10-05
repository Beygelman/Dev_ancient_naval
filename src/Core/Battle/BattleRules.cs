using System.Text.Json;
using System.Text.Json.Serialization;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed class BattleRules
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    public int StartingCredits { get; init; }
    public bool ConstructionClearsRuins { get; init; }
    public bool EmptyOuterRim { get; init; }
    public bool SeparatedStartingEscorts { get; init; }
    public bool MapSizePirateSettlements { get; init; }
    public bool EqualDoubleSalvoDamage { get; init; }
    public bool FreeCoastalNavigation { get; init; } // Missing in old saves: retain their coastal rules.
    public bool DoubleSalvo { get; init; }
    public bool MountainSightShadows { get; init; }
    public bool SmallHullRadarStealth { get; init; }
    public bool HeavenlyAssistance { get; init; }
    public bool ThemedWorldNames { get; init; }
    public bool LighthousesEnabled { get; init; } // Missing in old voyages: their construction menu is preserved.
    public bool DynamicFleetCapacity { get; init; }
    public bool WeightedFleetCapacity { get; init; }
    public bool DiagonalVillageBerths { get; init; }
    public double ScuttleRefundFraction { get; init; }
    public bool DeferredRewards { get; init; }
    public bool FishingRadarVisible { get; init; }
    public bool FishingLighthouses { get; init; }
    public bool FishingCannonTowers { get; init; } // Missing in old voyages: preserve schooner construction limits.
    public bool FrozenUnownedVillages { get; init; }
    public bool PersistTreasuryRuins { get; init; }
    public bool PaidVillageUpgrades { get; init; } // Missing in old saves: retain automatic town growth.
    public IReadOnlyList<int> VillageLevelPrices { get; init; } = new[] { 5, 8, 12, 16 };
    public int AncientAutoRepairAmount { get; init; }
    public int EncounterCurrencyReward { get; init; }
    public IReadOnlyList<int> LevelCurrencyRewards { get; init; } = new[] { 0, 0, 0, 0 };
    public int IncomePerMothership { get; init; }
    public int RepairAmount { get; init; }
    public int AutoRepairAmount { get; init; } = 2;
    public int? VillageAutoRepairAmount { get; init; } // Null retains the older town passive-heal rule.
    public int FleetLimit { get; init; }
    public int VillageFortificationPrice { get; init; } = 5;
    public int DockResourceReward { get; init; } = 2;
    public int PirateCurrencyReward { get; init; } = 2;
    public int PirateResourceReward { get; init; } = 1;
    public MortarRules Mortar { get; init; } = new();
    public BalloonRules Balloon { get; init; } = new();
    public TreasuryRules Treasury { get; init; } = new();
    public EconomyRules Economy { get; init; } = new();
    public PortRules Ports { get; init; } = new();
    public VillageCombatRules VillageCombat { get; init; } = new();
    public IReadOnlyList<LevelUpgrades> Upgrades { get; init; } = new[]
    {
        new LevelUpgrades(2, new[] { UpgradeChoice.Mobility, UpgradeChoice.FishingBoat }),
        new LevelUpgrades(3, new[] { UpgradeChoice.Vision, UpgradeChoice.Restoration }),
        new LevelUpgrades(4, new[] { UpgradeChoice.Balloon, UpgradeChoice.SecondAttack }),
        new LevelUpgrades(5, new[] { UpgradeChoice.Shipwright, UpgradeChoice.Firepower })
    };
    public IReadOnlyList<ShipDefinition> Ships { get; init; } = Array.Empty<ShipDefinition>();
    public static ShipDefinition DefaultCannonTower { get; } = new(ShipClass.CannonTower, "Cannon Tower", 10, 0, 3, 0, 3, 0, 2, 8, ActionProfile.Scout);
    public static ShipDefinition DefaultLighthouse { get; } = new(ShipClass.Lighthouse, "Lighthouse", 10, 0, 0, 0, 4, 5, 0, 6, ActionProfile.Scout, RadarPrice: 2);

    public ShipDefinition Get(ShipClass shipClass) => Ships.FirstOrDefault(s => s.Class == shipClass) ?? (shipClass switch
    {
        ShipClass.CannonTower => DefaultCannonTower,
        ShipClass.Lighthouse => DefaultLighthouse,
        _ => throw new ArgumentException("Unknown ship class.")
    });
    public static BattleRules FromJson(string json)
    {
        var rules = JsonSerializer.Deserialize<BattleRules>(json, JsonOptions) ?? throw new ArgumentException("Missing battle rules.", nameof(json));
        rules.Validate();
        return rules;
    }

    public void Validate()
    {
        if (!double.IsFinite(ScuttleRefundFraction) || ScuttleRefundFraction is < 0 or > 1)
            throw new ArgumentException("Invalid ship dismantling refund.");
        if (VillageAutoRepairAmount is < 0 || StartingCredits < 0 || IncomePerMothership < 0 || RepairAmount <= 0 || AutoRepairAmount < 0 || AncientAutoRepairAmount < 0 || FleetLimit < 4 || VillageFortificationPrice < 0 || DockResourceReward < 0 || PirateCurrencyReward < 0 || PirateResourceReward < 0)
            throw new ArgumentException("Invalid economy rules.");
        if (EncounterCurrencyReward < 0 || LevelCurrencyRewards is null || LevelCurrencyRewards.Count != 4 || LevelCurrencyRewards.Any(r => r < 0))
            throw new ArgumentException("Invalid voyage reward rules.");
        if (VillageLevelPrices is null || VillageLevelPrices.Count != 4 || VillageLevelPrices.Any(price => price < 0))
            throw new ArgumentException("Village upgrades need four nonnegative level prices.");
        if (Mortar is null || Balloon is null || Treasury is null || Economy is null || VillageCombat is null || Ports is null)
            throw new ArgumentException("Special weapon and treasury rules cannot be null.");
        Mortar.Validate();
        Balloon.Validate();
        Treasury.Validate();
        Economy.Validate();
        VillageCombat.Validate();
        Ports.Validate();
        ValidateUpgrades();
        ValidateShips();
    }

    private void ValidateUpgrades()
    {
        if (Upgrades is null || Upgrades.Any(u => u is null || u.Choices is null))
            throw new ArgumentException("Mothership upgrade choices cannot be null.");
        if (Upgrades.Count != 4 || Upgrades.Select(u => u.Level).Order().SequenceEqual(new[] { 2, 3, 4, 5 }) == false || Upgrades.Any(u => u.Choices.Count != 2 || u.Choices.Distinct().Count() != 2 || u.Choices.Any(c => !Enum.IsDefined(c) || c is UpgradeChoice.Income or UpgradeChoice.Fortification)))
            throw new ArgumentException("Every Mothership level needs two supported upgrade choices.");
    }

    private void ValidateShips()
    {
        if (Ships is null || Ships.Any(s => s is null))
            throw new ArgumentException("Ship definitions cannot be null.");
        // Added structures are optional in old snapshots; defaults are supplied by Get.
        if (Ships.Count < Enum.GetValues<ShipClass>().Length - 2 || Ships.Count > Enum.GetValues<ShipClass>().Length || Ships.Select(s => s.Class).Distinct().Count() != Ships.Count || Enum.GetValues<ShipClass>().Except(new[] { ShipClass.CannonTower, ShipClass.Lighthouse }).Any(kind => !Ships.Any(s => s.Class == kind)))
            throw new ArgumentException("Every ship class must have exactly one definition.");
        foreach (var ship in Ships)
        {
            if (!Enum.IsDefined(ship.Class) || !Enum.IsDefined(ship.ActionProfile) || string.IsNullOrWhiteSpace(ship.Name) || ship.MaxHealth <= 0 || ship.HealthPerLevel < 0 || ship.VeteranRangeBonus is < 0 or > 2 || ship.ResourceRequirementIncrease is < 0 or > 10 || ship.Movement < 0 || (ship.Movement == 0 && ship.Class is not (ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)) || ship.Damage < 0 || ship.Armor < 0 || ship.AttackRange < 0 || !double.IsFinite(ship.CoastMovementCost) || !double.IsFinite(ship.NarrowMovementCost) || ship.CoastMovementCost < 1 || ship.NarrowMovementCost < 1 || ship.IncomePerTurn < 0 || ship.VisualRange < 0 || ship.RadarRange < 0 || ship.RadarPrice < 0 || ship.CollectionRange < 0 || ship.Price < 0 || (ship.Class is not (ShipClass.Mothership or ShipClass.Balloon or ShipClass.AncientGun or ShipClass.PirateSchooner) && ship.Price == 0) || (ship.Class is ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock && (ship.Damage != 0 || ship.AttackRange != 0 || ship.RadarRange != 0)) || (ship.Class is not (ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock or ShipClass.Lighthouse) && (ship.Damage == 0 || ship.AttackRange == 0)))
                throw new ArgumentException($"Invalid ship definition: {ship.Name}.");
            if (ship.Class == ShipClass.Lighthouse && (ship.Movement != 0 || ship.Damage != 0 || ship.AttackRange != 0 ||
                ship.VisualRange != 4 || ship.RadarRange < 1 || ship.RadarPrice < 1))
                throw new ArgumentException("A Lighthouse must be stationary and unarmed, with four-tile sight and purchasable radar.");
        }
    }
}
