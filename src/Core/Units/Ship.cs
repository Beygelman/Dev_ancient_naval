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
    public double MaxHealth => Definition.MaxHealth * (IsVeteran ? 1.25 : 1);
    public double HealthRatio => Math.Clamp(Health / MaxHealth, 0, 1);
    public double FullDamage => Definition.Damage * (IsVeteran ? 1.25 : 1);
    public double CurrentDamage => FullDamage * (0.5 + 0.5 * HealthRatio);
    public bool IsArmed => Definition.Damage > 0 && Definition.AttackRange > 0;
    public int MovementAllowance => Math.Max(0, Definition.Movement - (HealthRatio < 0.25 ? 1 : 0));
    public int MovementSpentUnits { get; internal set; }
    public int MovementRemainingUnits => IsExhausted ? 0 : Math.Max(0, MovementAllowance * 10 - MovementSpentUnits);
    public double MovementRemaining => MovementRemainingUnits / 10.0;
    public int AttacksUsed { get; internal set; }
    public bool HasMoved { get; internal set; }
    public bool IsExhausted { get; internal set; }
    public bool HasProduced { get; internal set; }
    public bool MovementLocked { get; internal set; }

    public bool CanMove => !IsExhausted && !MovementLocked && MovementRemainingUnits >= 10 &&
        (Definition.ActionProfile switch
        {
            ActionProfile.Scout => true,
            ActionProfile.Standard => !MovementLocked,
            ActionProfile.Heavy => AttacksUsed == 0,
            _ => false
        });
    public int AttacksRemaining => IsExhausted || !IsArmed ? 0 : Math.Max(0,
        (Definition.ActionProfile == ActionProfile.Heavy && !HasMoved ? 2 : 1) - AttacksUsed);
    public bool CanRepair => !IsExhausted && !HasMoved && AttacksUsed == 0 && Health < MaxHealth;

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
