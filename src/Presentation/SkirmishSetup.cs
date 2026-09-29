using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation;

internal static class SkirmishSetup
{
    public static BattleState Create(GameBoard board, BattleRules rules) => new(board, rules,
        new (Side, ShipClass, GridPosition)[] {
            (Side.Player, ShipClass.Mothership, new(2, 9)),
            (Side.Player, ShipClass.Garrison, new(4, 9)),
            (Side.Player, ShipClass.Fishing, new(3, 11)),
            (Side.Enemy, ShipClass.Mothership, new(17, 9)),
            (Side.Enemy, ShipClass.Garrison, new(15, 9)),
            (Side.Enemy, ShipClass.Fishing, new(16, 7))
        }, resourceSeed: System.Random.Shared.Next());
}
