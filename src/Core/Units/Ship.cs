using DevAncientNaval.Core.Grid;

namespace DevAncientNaval.Core.Units;

public sealed class Ship
{
    public int Id { get; }
    public Side Owner { get; }
    public ShipDefinition Definition { get; }
    public GridPosition Position { get; internal set; }
    public int Health { get; internal set; }
    public int MovementRemaining { get; internal set; }
    public int AttacksUsed { get; internal set; }
    public bool HasMoved { get; internal set; }
    public bool IsExhausted { get; internal set; }
    public bool HasProduced { get; internal set; }
    public bool MovementLocked { get; internal set; }

    public bool CanMove => !IsExhausted && MovementRemaining > 0 &&
        (Definition.ActionProfile switch
        {
            ActionProfile.Scout => true,
            ActionProfile.Standard => !MovementLocked,
            ActionProfile.Heavy => AttacksUsed == 0,
            _ => false
        });
    public int AttacksRemaining => IsExhausted ? 0 : Math.Max(0,
        (Definition.ActionProfile == ActionProfile.Heavy && !HasMoved ? 2 : 1) - AttacksUsed);
    public bool CanRepair => !IsExhausted && !HasMoved && AttacksUsed == 0 && Health < Definition.MaxHealth;

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
        MovementRemaining = Definition.Movement;
        AttacksUsed = 0;
        HasMoved = false;
        IsExhausted = false;
        HasProduced = false;
        MovementLocked = false;
    }
}
