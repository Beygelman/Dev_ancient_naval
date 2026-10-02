using System;
using System.Collections.Generic;
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

public partial class VictoryChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException("Victory: " + name);
        _checks++;
    }

    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }

    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var descendant in Nodes(child))
                yield return descendant;
    }

    public override async void _Ready()
    {
        try
        {
            Check(OS.GetCmdlineUserArgs().Any(arg => arg.StartsWith("--save-file=")),
                "persistence check requires an explicit disposable save path");
            await Frame();
            var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules,
                new[]
                {
                    (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(9, 6)),
                    (Side.Player, ShipClass.AncientGun, new GridPosition(6, 6)),
                    (Side.Enemy, ShipClass.Garrison, new GridPosition(13, 10))
                }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            var snapshot = battle.CaptureSnapshot();
            snapshot.Ships.Single(ship => ship.Id == 2).Health = 1;
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
            battle.SetGodEye(true);
            Game.LoadScenario(battle);
            Game.FastChecks = false;
            Game.SelectCell(new(6, 6));
            Game.SelectCell(new(9, 6));
            Check(Game.Busy && Game.Victory?.IsOpen != true,
                "victory stays closed while the shell flies");
            bool sawImpactBeforeScreen = false, sawFlagshipSinking = false, sawFollowerSinking = false;
            for (int frame = 0; !Game.CurrentOrder.IsCompleted && frame < 1500; frame++)
            {
                if (battle.Winner == Side.Player)
                {
                    sawImpactBeforeScreen = true;
                    Check(Game.Victory?.IsOpen != true,
                        "the result cannot interrupt impact and sinking animations");
                    if (Game.Fleet.SinkingCount > 0)
                    {
                        if (battle.Find(4) is not null)
                            sawFlagshipSinking = true;
                        else
                            sawFollowerSinking = true;
                    }
                }
                await Frame();
            }
            await Game.CurrentOrder;
            await Frame();
            Check(sawImpactBeforeScreen && sawFlagshipSinking && sawFollowerSinking,
                "the actual shell, flagship wreck and following fleet resolve in order");
            var victory = Game.Victory!;
            Check(victory.IsOpen && Game.Fleet.SinkingCount == 0 && !Game.Busy
                && battle.PendingPresentation is null, "the modal result opens only after the completed command");
            Check(victory.Layer > Game.Hud.Layer && victory.StatisticsText == "5 Thors|0|1|1",
                "foreground result uses actual direct combat totals");
            Check(!Game.MapInput.IsProcessingInput() && !Game.MapInput.IsProcessingUnhandledInput()
                && !Game.Hud.Visible, "map gestures and underlying actions are blocked");
            int shown = victory.ShowCount;
            for (int i = 0; i < 12; i++)
            {
                Game.Refresh();
                await Frame();
            }
            Check(victory.ShowCount == shown, "routine refresh never restarts fireworks or result totals");
            Input.ParseInputEvent(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            await Frame();
            Check(victory.IsOpen && !Game.Hud.MenuVisible, "Escape does not open a second menu beneath the result");
            await ToSignal(GetTree().CreateTimer(.8), SceneTreeTimer.SignalName.Timeout);
            Check(victory.ActiveSparkCount is > 0 and <= 240, "celebration has a bounded visible particle population");
            var capture = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await Frame();
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "victory screenshot saved");
            }
            await ToSignal(GetTree().CreateTimer(8), SceneTreeTimer.SignalName.Timeout);
            Check(victory.ActiveSparkCount == 0 && !victory.IsProcessing(),
                "the finite celebration stops processing after its final spark");

            Game.Saves.Write(battle, Game.MapCamera.Position, Game.MapCamera.Zoom.X);
            Nodes(victory).OfType<Button>().Single(button => button.Name == "VictoryHome").EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(Game.Home.IsOpen && !victory.IsOpen && !victory.IsProcessing(),
                "Return to menu closes the result and all hidden celebration processing");
            await Game.ContinueSession();
            await Frame();
            Check(victory.IsOpen && victory.ShowCount == shown + 1
                && Game.Battle.Statistics == battle.Statistics,
                "Continue reopens a won voyage without recounting combat");
            Check(Nodes(victory).OfType<Button>().Single(button => button.Name == "VictoryExit").Text == "Exit game",
                "result supplies the second requested exit action");
            Nodes(victory).OfType<Button>().Single(button => button.Name == "VictoryHome").EmitSignal(Button.SignalName.Pressed);
            await Frame();
            Check(Game.Home.IsOpen && !victory.IsOpen, "a continued victory can return to menu again");
            GD.Print($"PASS: {_checks} staged victory/statistics/modal checks ({DisplayServer.GetName()}).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
