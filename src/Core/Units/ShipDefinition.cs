namespace DevAncientNaval.Core.Units;

// Released saves store the first three numeric identities. Never reorder them.
public enum Side { Player = 0, Enemy = 1, Pirates = 2, Enemy2 = 3, Enemy3 = 4, Enemy4 = 5 }
<<<<<<< Updated upstream
public enum ShipClass { Mothership, Garrison, Invader, Kolonel, Fishing, Balloon, Togus, FishingDock, AncientGun, PirateSchooner, CannonTower, Lighthouse }
=======
public enum ShipClass { Mothership, Garrison, Invader, Kolonel, Fishing, Balloon, Togus, FishingDock, AncientGun, PirateSchooner, CannonTower }
>>>>>>> Stashed changes
public enum UpgradeChoice { Income, Mobility, SecondAttack, Balloon, Fortification, Shipwright, FishingBoat, Vision, Restoration, Firepower }
public enum ActionProfile { Scout, Standard, Heavy }

public sealed record ShipDefinition(ShipClass Class, string Name, int MaxHealth,
    int Armor, int Damage, int Movement, int VisualRange, int RadarRange,
    int AttackRange, int Price, ActionProfile ActionProfile,
    double CoastMovementCost = 1, double NarrowMovementCost = 1, int IncomePerTurn = 0,
<<<<<<< Updated upstream
    int RadarPrice = 0, int CollectionRange = 0, int HealthPerLevel = 0, int ResourceRequirementIncrease = 0, int VeteranRangeBonus = 0);
=======
    int RadarPrice = 0, int CollectionRange = 0, int HealthPerLevel = 0);
>>>>>>> Stashed changes
