using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class GridChecks : Node
{
    public Main Game { get; set; } = null!;
    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (int seed in new[] { 42, 101, 723 })
            {
                var board = ArchipelagoGenerator.Create(seed);
                Game.LoadScenario(SkirmishSetup.Create(board, Game.Battle.Rules));
                foreach (var tile in board.Tiles) Game.Battle.Vision.RevealCombat(Side.Player, tile.Position);
                Game.Battle.Vision.Recompute(Game.Battle.Ships, 1, Game.Battle.Villages); Game.CancelOrder(); Game.Refresh();
                Game.MapCamera.FitBoard();
                GD.Print($"GRID {seed}: {board.Tiles.Count} cells, sides {string.Join(',', board.Mesh!.SideTileCounts)}, redirects {board.Mesh.RowRedirects}, triangles {board.Mesh.Faces.Values.Count(f => f.Count == 3)}, quads {board.Mesh.Faces.Values.Count(f => f.Count == 4)}, pentagons {board.Mesh.Faces.Values.Count(f => f.Count == 5)}");
                await Capture($"seed-{seed}");
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(board.CentralCell);
                Game.MapCamera.Zoom = new Vector2(.92f, .92f); Game.MapCamera.ForceUpdateScroll();
                await Capture($"detail-{seed}");
            }
            GetTree().Quit();
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
    private async Task Capture(string suffix)
    {
        string? arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) != Error.Ok) throw new Exception("Grid screenshot failed");
    }
}
