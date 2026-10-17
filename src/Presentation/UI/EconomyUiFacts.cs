using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Player-facing descriptions of Core decisions, without disclosing hidden trade geometry.</summary>
internal static class EconomyUiFacts
{
    internal static string TradeStatus(BattleState battle, Village town) => battle.TradeStatusFor(Side.Player, town.Id) switch
    {
        PortTradeStatus.Connected => "Trade connected · alternative markets preserve income",
        PortTradeStatus.Interrupted => "Trade interrupted · clear the interception or connect another market",
        PortTradeStatus.NoMarket => "No reachable friendly market · local income remains",
        PortTradeStatus.NoPort => "No port · local income only",
        _ => "Trade income is not observed"
    };

    internal static string CityWork(Village town) => town.HasProduced || town.HasRepaired || town.HasAttacked
        ? "City work spent"
        : town.UpgradeWorkRemaining > 0 ? town.DevelopmentPaused
            ? "Development paused · idle work available for defense"
            : "Idle work continues development before firing" : "City work available";

    internal static string ShipRole(Ship ship) => ship.Definition.Class switch
    {
        ShipClass.Garrison => "Scout and raider · fast, fragile; at most 1 remaining movement after firing",
        ShipClass.Invader => "Mobile line and escort · captures towns and intercepts raiders",
        ShipClass.Kolonel => "Position holder · two shots and anti-air; slow, uses 4 fleet capacity",
        ShipClass.Togus => "Siege artillery · needs scouting and escort; cannot move and fire in one turn",
        ShipClass.Fishing => "Logistics · gathers resources and builds forward structures; needs protection",
        _ => ""
    };
}
