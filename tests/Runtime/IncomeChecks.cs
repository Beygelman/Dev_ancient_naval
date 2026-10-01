using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class IncomeChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
    private void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
        _checks++;
    }

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var battle = new BattleState(new GameBoard(12, 12, _ => TerrainType.Water), Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)), (Side.Enemy, ShipClass.Mothership, new GridPosition(10, 10)), (Side.Player, ShipClass.Fishing, new GridPosition(4, 3)), (Side.Player, ShipClass.Garrison, new GridPosition(3, 4)), (Side.Player, ShipClass.Garrison, new GridPosition(4, 4)), (Side.Player, ShipClass.Garrison, new GridPosition(5, 4)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            Game.LoadScenario(battle);
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(4, 4));
            Game.MapCamera.Zoom = Vector2.One * 2;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await Game.Fleet.Animate(battle.EndTurn(Side.Player));
            Check(Game.Fleet.IncomeLabelCount == 0, "Private rival income never creates a popup.");
            int credits = battle.Credits(Side.Player);
            int income = battle.Income(Side.Player);
            var next = battle.EndTurn(Side.Enemy);
            Check(next.Success && battle.Credits(Side.Player) == credits + income, "Popup turn receives actual net income.");
            Check(next.IncomeReceipts!.Sum(r => r.Amount) == income, "Source receipts and upkeep reconcile with net income.");
            await Game.Fleet.Animate(next);
            Check(Game.Fleet.IncomeLabelCount == 3, "Mothership, fishing and upkeep appear at their own sources.");
            var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "Income screenshot saved.");
            }

            await ToSignal(GetTree().CreateTimer(2.3), SceneTreeTimer.SignalName.Timeout);
            Check(Game.Fleet.IncomeLabelCount == 0, "Income labels expire without retaining an unbounded history.");
            GD.Print($"PASS: {_checks} source-income animation checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
