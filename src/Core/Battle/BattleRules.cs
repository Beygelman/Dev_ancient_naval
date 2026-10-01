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
    public bool FreeCoastalNavigation { get; init; } // Missing in old saves: retain their coastal rules.
    public int IncomePerMothership { get; init; }
    public int RepairAmount { get; init; }
    public int AutoRepairAmount { get; init; } = 2;
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

    public ShipDefinition Get(ShipClass shipClass) => Ships.FirstOrDefault(s => s.Class == shipClass) ?? (shipClass == ShipClass.CannonTower ? DefaultCannonTower : throw new ArgumentException("Unknown ship class."));
    public static BattleRules FromJson(string json)
    {
        var rules = JsonSerializer.Deserialize<BattleRules>(json, JsonOptions) ?? throw new ArgumentException("Missing battle rules.", nameof(json));
        rules.Validate();
        return rules;
    }

    public void Validate()
    {
        if (StartingCredits < 0 || IncomePerMothership < 0 || RepairAmount <= 0 || AutoRepairAmount < 0 || FleetLimit < 4 || VillageFortificationPrice < 0 || DockResourceReward < 0 || PirateCurrencyReward < 0 || PirateResourceReward < 0)
            throw new ArgumentException("Invalid economy rules.");
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
        // CannonTower is optional in old snapshots; its default is supplied by Get.
        if (Ships.Count < Enum.GetValues<ShipClass>().Length - 1 || Ships.Count > Enum.GetValues<ShipClass>().Length || Ships.Select(s => s.Class).Distinct().Count() != Ships.Count || Enum.GetValues<ShipClass>().Except(new[] { ShipClass.CannonTower }).Any(kind => !Ships.Any(s => s.Class == kind)))
            throw new ArgumentException("Every ship class must have exactly one definition.");
        foreach (var ship in Ships)
            if (!Enum.IsDefined(ship.Class) || !Enum.IsDefined(ship.ActionProfile) || string.IsNullOrWhiteSpace(ship.Name) || ship.MaxHealth <= 0 || ship.HealthPerLevel < 0 || ship.Movement < 0 || (ship.Movement == 0 && ship.Class is not (ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower)) || ship.Damage < 0 || ship.Armor < 0 || ship.AttackRange < 0 || !double.IsFinite(ship.CoastMovementCost) || !double.IsFinite(ship.NarrowMovementCost) || ship.CoastMovementCost < 1 || ship.NarrowMovementCost < 1 || ship.IncomePerTurn < 0 || ship.VisualRange < 1 || ship.RadarRange < 0 || ship.RadarPrice < 0 || ship.CollectionRange < 0 || ship.Price < 0 || (ship.Class is not (ShipClass.Mothership or ShipClass.Balloon or ShipClass.AncientGun or ShipClass.PirateSchooner) && ship.Price == 0) || (ship.Class is ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock && (ship.Damage != 0 || ship.AttackRange != 0 || ship.RadarRange != 0)) || (ship.Class is not (ShipClass.Fishing or ShipClass.Balloon or ShipClass.FishingDock) && (ship.Damage == 0 || ship.AttackRange == 0)))
                throw new ArgumentException($"Invalid ship definition: {ship.Name}.");
    }
}
