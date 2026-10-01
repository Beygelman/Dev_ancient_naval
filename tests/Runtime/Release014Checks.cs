using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class BattleChecks
{
    private async Task Release014Battle(BattleRules rules)
    {
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(8,8)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Enemy,ShipClass.Balloon,new GridPosition(10,8)),(Side.Enemy,ShipClass.Fishing,new GridPosition(10,8))},Array.Empty<GridPosition>()));
        Game.FastChecks=false;Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(9,8));Game.MapCamera.Zoom=Vector2.One*1.3f;Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(8,8));await Frame();
        var balloon=GetViewport().GetCanvasTransform()*(Game.BoardView.Projection.GridToWorld(new(10,8))+new Vector2(0,-62));
        Tap(balloon);await Game.CurrentOrder;await Frame();
        Check(Game.Battle.Find(3) is null&&Game.Battle.Find(4)!.Health==5,"Elevated balloon click targets air unit without hitting ship below");
        Check(Game.Fleet.CompletedSalvos.Count==1&&Game.Fleet.CompletedSalvos[0]==(2,2),"Anti-air shot uses two Mothership cannonballs");
        await Capture("-antiair");
        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[]{
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Togus,new GridPosition(8,8)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(12,8)),
            (Side.Enemy,ShipClass.Garrison,new GridPosition(12,9)),(Side.Player,ShipClass.Garrison,new GridPosition(11,9))},Array.Empty<GridPosition>()));
        foreach(var tile in Game.Battle.Board.Tiles)Game.Battle.Vision.RevealCombat(Side.Player,tile.Position);
        Game.Battle.Vision.Recompute(Game.Battle.Ships,1,Game.Battle.Villages);Game.Refresh();
        Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(10,8));Game.MapCamera.Zoom=Vector2.One*1.1f;Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(8,8));Game.SelectCell(new(12,8));await Game.CurrentOrder;
        Check(Game.Battle.Find(4)!.Health==7&&Game.Battle.Find(5)!.Health==3&&Game.Battle.Find(6)!.Health==5,"Animated mortar applies direct and enemy-only splash damage");
        Check(Game.Fleet.CompletedSalvos.Count==1&&Game.Fleet.CompletedSalvos[0]==(1,1),"Splash does not create extra salvos");
        await Capture("-mortar-splash");Game.FastChecks=true;
    }
}
