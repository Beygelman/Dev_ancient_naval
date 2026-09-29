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
    public bool IsMothership => Definition.Class == ShipClass.Mothership;
    public bool IsAirborne => Definition.Class == ShipClass.Balloon;
    public int Level { get; internal set; } = 1;
    public int Resources { get; internal set; }
    public int ResourcesRequired => IsMothership && Level < 4 ? Level + 1 : 0;
    public int PendingUpgradeLevel { get; internal set; }
    public bool IncomeUpgrade { get; internal set; }
    public bool MobilityUpgrade { get; internal set; }
    public bool SecondAttackUpgrade { get; internal set; }
    public bool HasRadar { get; internal set; }
    public int RadarRange => HasRadar ? Definition.RadarRange : 0;
    public int VisualRange => Definition.VisualRange;
    public static int Whole(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    public double MaxHealth => Whole((Definition.MaxHealth + (IsMothership ? (Level - 1) * 10 : 0)) * (IsVeteran ? 1.25 : 1));
    public double HealthRatio => Math.Clamp(Health / MaxHealth, 0, 1);
    public double FullDamage => Whole((Definition.Damage + (IsMothership ? (Level - 1) * 5 : 0)) * (IsVeteran ? 1.25 : 1));
    public double CurrentDamage => Whole(FullDamage * (0.5 + 0.5 * HealthRatio));
    public bool IsArmed => Definition.Damage > 0 && Definition.AttackRange > 0;
    public int MovementAllowance => Math.Max(0, Definition.Movement + (MobilityUpgrade ? 1 : 0) - (HealthRatio < 0.25 ? 1 : 0));
    public int MovementSpentUnits { get; internal set; }
    public int MovementRemainingUnits => IsExhausted ? 0 : Math.Max(0, MovementAllowance * 10 - MovementSpentUnits);
    public double MovementRemaining => MovementRemainingUnits / 10.0;
    public int AttacksUsed { get; internal set; }
    public bool HasMoved { get; internal set; }
    public bool IsExhausted { get; internal set; }
    public bool HasProduced { get; internal set; }
    public bool MovementLocked { get; internal set; }

    public bool CanMove => IsAirborne ? !IsExhausted && !HasMoved : !IsExhausted && !MovementLocked && MovementRemainingUnits >= 10 &&
        (Definition.ActionProfile switch
        {
            ActionProfile.Scout => true,
            ActionProfile.Standard => !MovementLocked,
            ActionProfile.Heavy => AttacksUsed == 0,
            _ => false
        });
    public int AttacksRemaining => IsExhausted || !IsArmed ? 0 : Math.Max(0,
        (SecondAttackUpgrade || Definition.ActionProfile == ActionProfile.Heavy && !HasMoved ? 2 : 1) - AttacksUsed);
    public bool CanRepair => !IsAirborne && !IsExhausted && !HasMoved && AttacksUsed == 0 && Health < MaxHealth;

    internal Ship(int id, Side owner, ShipDefinition definition, GridPosition position)
    {
        Id = id;
        Owner = owner;
        Definition = definition;
        Position = position;
        Health = definition.MaxHealth;
        ResetTurn();
    }

    internal void ResetTurn()
    {
        MovementSpentUnits = 0;
        AttacksUsed = 0;
        HasMoved = false;
        IsExhausted = false;
        HasProduced = false;
        MovementLocked = false;
    }
}
