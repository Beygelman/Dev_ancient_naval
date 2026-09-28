namespace DevAncientNaval.Core.Units;

public enum Side { Player, Enemy }
public enum ShipClass { Mothership, Garrison, Invader, Kolonel }
public enum ActionProfile { Scout, Standard, Heavy }

public sealed record ShipDefinition(ShipClass Class, string Name, int MaxHealth,
    int Armor, int Damage, int Movement, int VisualRange, int RadarRange,
    int AttackRange, int Price, ActionProfile ActionProfile);
