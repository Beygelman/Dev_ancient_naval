using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class EffectsChecks
{
    private async Task PresentedSinking()
    {
        var fixture = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Mothership, new GridPosition(9, 8)), (Side.Enemy, ShipClass.Garrison, new GridPosition(10, 8)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var saved = fixture.CaptureSnapshot();
        saved.Ships.Single(s => s.Id == 2).Health = 1;
        Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(saved)));
        Reveal();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(9, 8));
        Game.MapCamera.Zoom = Vector2.One * 2;
        Game.MapCamera.ForceUpdateScroll();
        Game.FastChecks = false;
        Game.SelectCell(new(8, 8));
        Game.SelectCell(new(9, 8));
        var order = Game.CurrentOrder;
        Check(Game.Battle.Find(2)!.Health == 1 && Game.Battle.Find(3)is not null, "Doomed flagship and escort still exist during gun aiming");
        for (int frame = 0; frame < 240 && Game.Fleet.SinkingCount == 0; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Game.Fleet.SinkingCount == 1 && Game.Battle.Find(2)is null && Game.Battle.Find(3)is not null, "Actual impact starts only the flagship wreck, retaining its escort");
        await Wait(.55f);
        await Capture("flagship-fracture");
        for (int frame = 0; frame < 240 && Game.Battle.Find(3)is not null; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Game.Fleet.SinkingCount > 0 && Game.Battle.Find(3)is null && !order.IsCompleted, "Escort sinking starts after the flagship has submerged");
        await Capture("escort-sinking");
        await order;
        Check(Game.Fleet.SinkingCount == 0 && Game.Battle.Winner == Side.Player && Game.Battle.PendingPresentation is null, "Completed sinking leaves one resolved winning aggregate");
    }
}
