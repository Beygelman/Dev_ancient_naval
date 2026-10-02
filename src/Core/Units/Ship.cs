using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.Units;
public sealed class Ship
{
    public int Id { get; }
    public Side Owner { get; }
    public ShipDefinition Definition { get; }
    public GridPosition Position { get; internal set; }
    public double Health { get; internal set; }
    public int Kills { get; internal set; }
    public bool IsVeteran { get; internal set; }
    public bool CanEarnVeterancy => !IsMothership && !IsAirborne && IsArmed && Definition.Class != ShipClass.FishingDock;
    public bool IsStructure => Definition.Class is ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse;
    public bool IsAncient { get; internal set; }
    public int BombCooldown { get; internal set; }
    public bool HasRepaired { get; internal set; }
    public string Name => IsAirborne && IsAncient ? "Ancient Balloon" : Definition.Name;
    public bool IsMothership => Definition.Class == ShipClass.Mothership;
    public bool IsAirborne => Definition.Class == ShipClass.Balloon;
    public int Level { get; internal set; } = 1;
    public int Resources { get; internal set; }
    public int ResourcesRequired => IsMothership && Level < 5 ? Level + 1 + Definition.ResourceRequirementIncrease : 0;
    public int PendingUpgradeLevel { get; internal set; }
    public bool IncomeUpgrade { get; internal set; }
    public bool MobilityUpgrade { get; internal set; }
    public bool SecondAttackUpgrade { get; internal set; }
    public bool HasMortar { get; internal set; }
    public bool FortificationUpgrade { get; internal set; }
    public bool ShipwrightUpgrade { get; internal set; }
    public bool VisionUpgrade { get; internal set; }
    public bool RestorationUpgrade { get; internal set; }
    public bool FirepowerUpgrade { get; internal set; }
    public bool BombUsed => BombCooldown > 0;
    public bool HasRadar { get; internal set; }
    public int RadarRange => HasRadar ? Definition.RadarRange + (VisionUpgrade ? 2 : 0) : 0;
    public int VisualRange => Definition.VisualRange + (VisionUpgrade ? 2 : 0);
    public int AttackRange => IsArmed ? Math.Max(Definition.AttackRange, HasMortar ? MortarRange : 0) : 0;
    public int MortarRange => HasMortar ? 5 : 0;
    public double CurrentMortarDamage => Definition.Class == ShipClass.AncientGun ? Whole(Definition.Damage * (IsVeteran ? 1.25 : 1)) : Whole((8 * (IsVeteran ? 1.25 : 1)) * (0.5 + 0.5 * HealthRatio));

    public static int Whole(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    public double MaxHealth => Whole((Definition.MaxHealth + (Level - 1) * Definition.HealthPerLevel + (RestorationUpgrade ? 5 : 0)) * (IsVeteran ? 1.25 : 1));
    public double HealthRatio => Math.Clamp(Health / MaxHealth, 0, 1);
    public double FullDamage => Whole(Definition.Damage * (IsVeteran ? 1.25 : 1));
    public double CurrentDamage => Whole(FullDamage * (0.5 + 0.5 * HealthRatio));
    public int ShotDamageBonus => FirepowerUpgrade ? 3 : 0;
    public int CounterDamageBonus => FirepowerUpgrade ? 2 : 0;
    public bool CountsTowardFleet => !IsAirborne && !IsStructure && IsArmed;
    public bool IsArmed => Definition.Damage > 0 && Definition.AttackRange > 0;
    public int MovementAllowance => Math.Max(0, Definition.Movement + (MobilityUpgrade ? 1 : 0) + (IsMothership && Level >= 4 ? 1 : 0) - (HealthRatio < 0.25 ? 1 : 0));
    public int TradeStreak { get; internal set; }
    public int MovementSpentUnits { get; internal set; }
    public int MovementRemainingUnits => IsExhausted ? 0 : Math.Max(0, MovementAllowance * 10 - MovementSpentUnits);
    public double MovementRemaining => MovementRemainingUnits / 10.0;
    public int AttacksUsed { get; internal set; }
    public bool HasMoved { get; internal set; }
    public bool IsExhausted { get; internal set; }
    public bool HasProduced { get; internal set; }
    public bool MovementLocked { get; internal set; }
    public bool CanMove => IsAirborne ? !IsExhausted && MovementRemainingUnits >= 10 : !IsStructure && !IsExhausted && !MovementLocked && MovementRemainingUnits >= 1 && (Definition.ActionProfile switch
    {
        ActionProfile.Scout => true,
        ActionProfile.Standard => !MovementLocked,
        ActionProfile.Heavy => AttacksUsed == 0,
        _ => false
    });
    public int AttacksRemaining => IsExhausted || !IsArmed || HasMortar && HasMoved ? 0 : Math.Max(0, (Definition.ActionProfile == ActionProfile.Heavy ? 2 : 1) + (SecondAttackUpgrade ? 1 : 0) - AttacksUsed);
    public bool CanRepair => !IsAirborne && Definition.Class != ShipClass.AncientGun && !IsExhausted && !HasRepaired && AttacksUsed == 0 && Health < MaxHealth;

    internal Ship(int id, Side owner, ShipDefinition definition, GridPosition position)
    {
        Id = id;
        Owner = owner;
        Definition = definition;
        Position = position;
        Health = definition.MaxHealth;
        HasMortar = definition.Class is ShipClass.Togus or ShipClass.AncientGun;
        HasRadar = HasMortar;
        ResetTurn();
    }

    internal void ResetTurn()
    {
        MovementSpentUnits = 0;
        TradeStreak = 0;
        AttacksUsed = 0;
        HasMoved = false;
        IsExhausted = false;
        HasProduced = false;
        MovementLocked = false;
        HasRepaired = false;
    }
}
