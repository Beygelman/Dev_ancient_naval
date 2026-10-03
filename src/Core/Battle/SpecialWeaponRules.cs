namespace DevAncientNaval.Core.Battle;
/// <summary>Defaults also apply when loading version 1 saves without this section.</summary>
public sealed record MortarRules
{
    public int? GranadoDeadZone { get; init; } // Null keeps the historical shared mortar annulus.
    public int DeadZone { get; init; } = 3;
    public bool TowerDeadZone { get; init; }
    public int PurchasePrice { get; init; } = 10;
    public int SplashDamage { get; init; } = 2;
    public int VillageDamageBonus { get; init; } = 2;

    internal void Validate()
    {
        if (GranadoDeadZone is < 0 or > 4 || DeadZone is < 0 or > 4 || PurchasePrice < 0 || SplashDamage < 0 || VillageDamageBonus < 0)
            throw new ArgumentException("Mortar prices and damage must not be negative.");
    }
}

public sealed record BalloonRules
{
    public bool KolonelAntiAir { get; init; }
    public int BombDamage { get; init; } = 6;
    public int SplashDamage { get; init; } = 2;
    public int CooldownTurns { get; init; } = 3;
    public int AntiAirRange { get; init; } = 3; // Legacy saves retain their original range.

    internal void Validate()
    {
        if (BombDamage <= 0 || SplashDamage < 0 || CooldownTurns < 1 || AntiAirRange < 1)
            throw new ArgumentException("Balloon bombs need positive damage and cooldown, and nonnegative splash damage.");
    }
}
