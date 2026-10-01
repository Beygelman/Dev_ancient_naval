namespace DevAncientNaval.Core.Battle;
public sealed class PortRules
{
    public int Price { get; init; } = 6;
    public int Income { get; init; } = 1;
    public double Discount { get; init; } = .20;
    public int MinimumBonus { get; init; } = 2;
    public double MovementBonus { get; init; } = .20;

    public void Validate()
    {
        if (Price < 0 || Income < 0 || !double.IsFinite(Discount) || Discount is < 0 or >= 1 || MinimumBonus < 1 || !double.IsFinite(MovementBonus) || MovementBonus is < 0 or > 1)
            throw new ArgumentException("Invalid port rules.");
    }
}
