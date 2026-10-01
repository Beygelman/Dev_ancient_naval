namespace DevAncientNaval.Core.Battle;
/// <summary>
/// Integer percentages keep discoveries on the original single 0–99 random draw.
/// The order is a save compatibility contract: tower, currency, balloon, resources, whirlpool.
/// </summary>
public sealed record TreasuryRules
{
    public int AncientGunWeight { get; init; } = 22;
    public int CurrencyWeight { get; init; } = 22;
    public int AncientBalloonWeight { get; init; } = 22;
    public int ResourcesWeight { get; init; } = 22;
    public int WhirlpoolWeight { get; init; } = 12;
    public int CurrencyReward { get; init; } = 5;
    public int ResourceReward { get; init; } = 2;

    internal void Validate()
    {
        if (AncientGunWeight < 0 || CurrencyWeight < 0 || AncientBalloonWeight < 0 || ResourcesWeight < 0 || WhirlpoolWeight < 0 || (long)AncientGunWeight + CurrencyWeight + AncientBalloonWeight + ResourcesWeight + WhirlpoolWeight != 100)
            throw new ArgumentException("Treasury reward weights must be nonnegative percentages totaling 100.");
        if (CurrencyReward < 0 || ResourceReward < 0)
            throw new ArgumentException("Treasury rewards must not be negative.");
    }

    public TreasuryReward RewardForRoll(int roll)
    {
        if (roll is < 0 or >= 100)
            throw new ArgumentOutOfRangeException(nameof(roll));
        int limit = AncientGunWeight;
        if (roll < limit)
            return TreasuryReward.AncientGun;
        limit += CurrencyWeight;
        if (roll < limit)
            return TreasuryReward.Currency;
        limit += AncientBalloonWeight;
        if (roll < limit)
            return TreasuryReward.AncientBalloon;
        limit += ResourcesWeight;
        return roll < limit ? TreasuryReward.Resources : TreasuryReward.Whirlpool;
    }
}
