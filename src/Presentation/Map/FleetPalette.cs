using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side=DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public static class FleetPalette
{
    public static Color Color(FleetColor choice)=>new(choice switch
    {FleetColor.Green=>"83cf98",FleetColor.Yellow=>"ead27b",FleetColor.Purple=>"ba9cdf",FleetColor.White=>"e9eee6",FleetColor.Red=>"d85d69",_=>"71bdeb"});
    public static Color For(BattleState battle,Side? side)=>side switch
    {Side.Pirates=>new("393e43"),null=>new("e6d2a1"),_=>Color(battle.ColorFor(side.Value))};
}
