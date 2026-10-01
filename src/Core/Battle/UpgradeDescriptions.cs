using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
public sealed record LevelUpgrades(int Level, IReadOnlyList<UpgradeChoice> Choices);
public static class UpgradeDescriptions
{
    public static string Title(UpgradeChoice choice) => choice switch
    {
        UpgradeChoice.Mobility => "Improved propulsion",
        UpgradeChoice.FishingBoat => "Fishing expedition",
        UpgradeChoice.Vision => "Lookout tower",
        UpgradeChoice.Restoration => "Reinforced hull",
        UpgradeChoice.Balloon => "Observation balloon",
        UpgradeChoice.SecondAttack => "Extra broadside",
        UpgradeChoice.Shipwright => "Master shipwright",
        UpgradeChoice.Firepower => "Heavy ammunition",
        _ => choice.ToString()
    };
    public static string Description(UpgradeChoice choice, BattleRules? rules = null) => choice switch
    {
        UpgradeChoice.Mobility => "+1 movement",
        UpgradeChoice.FishingBoat => "Receive one free Fishing Schooner",
        UpgradeChoice.Vision => "+2 vision and +2 radar range, including future radar",
        UpgradeChoice.Restoration => "+5 maximum HP and repair 2 HP at the start of each turn",
        UpgradeChoice.Balloon => rules is null ? "Persistent balloon: vision 8, bomb 6 + 2 splash every 3 turns" : $"Persistent balloon: vision {rules.Get(ShipClass.Balloon).VisualRange}, bomb {rules.Balloon.BombDamage} + {rules.Balloon.SplashDamage} splash every {rules.Balloon.CooldownTurns} turns",
        UpgradeChoice.SecondAttack => "+1 attack per turn",
        UpgradeChoice.Shipwright => "Ship construction costs reduced by 25%",
        UpgradeChoice.Firepower => "+3 shot damage and +2 counterattack damage",
        _ => Title(choice)
    };
}
