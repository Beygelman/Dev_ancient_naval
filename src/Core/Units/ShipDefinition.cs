namespace DevAncientNaval.Core.Units;

public enum Side { Player, Enemy }
public enum ShipClass { Mothership, Garrison, Invader, Kolonel, Fishing, Balloon }
public enum UpgradeChoice { Income, Mobility, SecondAttack, Balloon }
public enum ActionProfile { Scout, Standard, Heavy }

public sealed record ShipDefinition(ShipClass Class, string Name, int MaxHealth,
    int Armor, int Damage, int Movement, int VisualRange, int RadarRange,
    int AttackRange, int Price, ActionProfile ActionProfile,
    int CoastMovementCost = 1, int NarrowMovementCost = 1, int IncomePerTurn = 0,
    int RadarPrice = 0, int CollectionRange = 0);
