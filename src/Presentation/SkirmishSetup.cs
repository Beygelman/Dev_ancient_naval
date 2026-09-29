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
        }, resourceSeed: board.Seed);
    public static BattleState Create(GameBoard board, BattleRules rules)
    {
        var a=board.FleetAnchor(false); var b=board.FleetAnchor(true);
        return new(board,rules,new (Side,ShipClass,GridPosition)[] {
            (Side.Player,ShipClass.Mothership,a),(Side.Player,ShipClass.Garrison,new(a.X+2,a.Y)),
            (Side.Player,ShipClass.Fishing,new(a.X+1,a.Y+2)),
            (Side.Enemy,ShipClass.Mothership,b),(Side.Enemy,ShipClass.Garrison,new(b.X-2,b.Y)),
            (Side.Enemy,ShipClass.Fishing,new(b.X-1,b.Y-2))
        },resourceSeed:board.Seed);
    }
}
