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

public partial class Action0209Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        _checks++;
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private void Fixture()
    {
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water),
            Game.Battle.Rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                (Side.Player, ShipClass.Invader, new GridPosition(8, 8)),
                (Side.Enemy, ShipClass.Fishing, new GridPosition(9, 8)),
                (Side.Player, ShipClass.Garrison, new GridPosition(6, 8))
            }, Array.Empty<GridPosition>());
        foreach (var tile in battle.Board.Tiles)
            battle.Vision.RevealCombat(Side.Player, tile.Position);
        battle.Vision.Recompute(battle.Ships, 1, battle.Villages);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        Game.LoadScenario(battle);
        Game.Refresh();
        Game.FastChecks = false;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(7, 8));
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.ForceUpdateScroll();
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Fixture();
            var camera = Game.MapCamera.Position;
            Game.SelectCell(new(8, 8));
            Game.SelectCell(new(9, 8));
            var attack = Game.CurrentOrder;
            Check(Game.Battle.Find(4) is null && Game.Battle.PendingPresentation is null,
                "Player sinking commits immediately and leaves a stable save boundary");
            Check(!Game.Busy && !attack.IsCompleted, "Own effects play with commands unlocked");
            var saved = Game.Battle.SaveJson();
            for (int i = 0; i < 8; i++) await Frame();
            Check(Game.MapCamera.Position == camera, "Own attack never auto-focuses");
            Game.SelectCell(new(6, 8));
            Game.SelectCell(new(6, 9));
            var movement = Game.CurrentOrder;
            Check(Game.Battle.Find(5)!.Position == new GridPosition(6, 9),
                "Next movement executes while the previous attack was still animating");
            Check(attack.IsCompletedSuccessfully, "Next order settles the previous visual awaiter");
            Game.Fleet.FinishAnimation();
            await movement;
            Check(Game.Battle.Find(4) is null, "Skipping visuals never resurrects a sunk target");
            var continued = BattleState.LoadJson(saved);
            Check(continued.Find(4) is null && continued.Find(3)!.AttacksUsed == 1,
                "Continue retains one committed attack");
            Check(Game.Fleet.ActiveProjectileCount == 0 && Game.Fleet.SinkingCount == 0,
                "Settled effects leave no shells or wrecks");

            // Camera permission is explicit; own effects and replay share a renderer.
            int focuses = 0;
            var focus = Game.Fleet.FocusTarget;
            Game.Fleet.FocusTarget = _ => { focuses++; return Task.CompletedTask; };
            foreach (bool replay in new[] { false, true })
            {
                var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water),
                    Game.Battle.Rules, new[] {
                        (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                        (Side.Enemy, ShipClass.Mothership, new GridPosition(10, 8)),
                        (Side.Player, ShipClass.Invader, new GridPosition(8, 8))
                    }, Array.Empty<GridPosition>());
                battle.EndTurn(Side.Player);
                foreach (var tile in battle.Board.Tiles)
                    battle.Vision.RevealCombat(Side.Player, tile.Position);
                battle.Vision.Recompute(battle.Ships, 1, battle.Villages);
                Game.LoadScenario(battle);
                focuses = 0;
                var command = battle.Prepare(b => b.Attack(Side.Enemy, 2, 3));
                Check(command.Result.Success, "Opponent replay fixture");
                var animation = Game.Fleet.Animate(command.Result, presentation: command, opponentReplay: replay);
                Check(battle.Find(3)!.Health == 10 && battle.PendingPresentation is not null,
                    "Staged replay damage waits for its visual impact");
                Check(replay ? focuses > 0 : focuses == 0,
                    "Only explicitly marked opponent replay requests camera focus");
                Game.Fleet.FinishAnimation();
                await animation;
                Check(command.Complete && battle.Find(3)!.Health < 10,
                    "Settling replay commits exactly once");
            }
            Game.Fleet.FocusTarget = focus;

            Fixture();
            Game.SelectCell(new(8, 8));
            Game.SelectCell(new(9, 8));
            var old = Game.CurrentOrder;
            var previous = Game.Battle;
            Fixture();
            Check(old.IsCompletedSuccessfully && !ReferenceEquals(previous, Game.Battle),
                "World replacement settles the old order before changing references");
            Check(Game.Battle.Find(4) is not null && Game.Battle.Find(4)!.Health == 5,
                "Old visual continuations cannot damage the replacement world");
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "Capture");
            }
            GD.Print($"PASS: {_checks} action0209 camera, unlocked orders, save and replacement checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}

