namespace DevAncientNaval.Core.Battle;
/// <summary>Optional rules preserve the passive fortifications in released saves.</summary>
public sealed record VillageCombatRules
{
    public bool AutomaticAttack { get; init; }
    public int AttackRange { get; init; } = 2;
    public int CounterRange { get; init; } = 3;
    public int CounterBonus { get; init; } = 1;

    internal void Validate()
    {
        if (AttackRange < 1 || CounterRange < 1 || CounterBonus < 0)
            throw new ArgumentException("Outpost ranges must be positive and counter bonus nonnegative.");
    }
}
