using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Native selector, new-voyage persistence and actual renderer coverage for all terrain policies.</summary>
public partial class WorldModeChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("World modes: " + message);
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

    private Button Button(string name) => Nodes(Game.Home).OfType<Button>().Single(button => button.Name == name);

    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(value => value.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "saved " + suffix + " screenshot");
    }

    private async Task OpenSetup()
    {
        Game.ShowHome();
        Button("HomeNewGame").EmitSignal(BaseButton.SignalName.Pressed);
        await Frame();
        await Frame();
        Check(Game.Home.IsOpen && !Game.BoardView.Visible, "world setup is shown over the title background");
    }

    private void CheckSetupFits()
    {
        var viewport = GetViewport().GetVisibleRect();
        Check(viewport.Size.X >= 1280 && viewport.Size.Y >= 720, "run setup layout check at 1280×720 or larger");
        foreach (var control in Nodes(Game.Home).OfType<Control>().Where(c => c.IsVisibleInTree()))
        {
            var rect = control.GetGlobalRect();
            Check(rect.Position.X >= -1 && rect.Position.Y >= -1 && rect.End.X <= viewport.End.X + 1 &&
                rect.End.Y <= viewport.End.Y + 1, "setup control stays inside the viewport: " + control.Name);
        }
        var select = Nodes(Game.Home).OfType<OptionButton>().Single(button => button.Name == "WorldGeneration");
        Check(select.GetGlobalRect().End.Y < Button("StartBattle").GetGlobalRect().Position.Y,
            "world selector precedes the sail action without overlap");
        Check(Button("CancelColor").GetGlobalRect().End.Y < viewport.End.Y - 32,
            "setup keeps the footer clear");
    }

    private void SelectWorld(OptionButton selector, WorldKind kind)
    {
        int index = Enumerable.Range(0, selector.ItemCount).Single(i => selector.GetItemId(i) == (int)kind);
        selector.Select(index);
        selector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
        Check(Game.Home.WorldKind == kind && selector.GetSelectedId() == (int)kind,
            "native choice selects " + kind);
    }

    public override async void _Ready()
    {
        try
        {
            Check(OS.GetCmdlineUserArgs().Any(arg => arg.StartsWith("--save-file=")),
                "persistence test requires an explicit disposable save path");
            await Frame();
            Game.FastChecks = true;
            await OpenSetup();
            var selector = Nodes(Game.Home).OfType<OptionButton>().Single(button => button.Name == "WorldGeneration");
            Check(Game.Home.WorldKind == WorldKind.Oceans && selector.GetSelectedId() == (int)WorldKind.Oceans,
                "Oceans is the familiar default");
            Check(selector.ItemCount == 4 && Enumerable.Range(0, 4).Select(selector.GetItemText).SequenceEqual(
                new[] { "Sea World", "Oceans", "Continents", "Pangaea" }), "all four English policies have stable choices");
            foreach (var kind in Enum.GetValues<WorldKind>())
                SelectWorld(selector, kind);
            CheckSetupFits();
            await Capture("setup");

            foreach (var kind in Enum.GetValues<WorldKind>())
            {
                await OpenSetup();
                SelectWorld(selector, kind);
                Button("OpponentCount3").EmitSignal(BaseButton.SignalName.Pressed);
                Button("FleetColorGreen").EmitSignal(BaseButton.SignalName.Pressed);
                Button("StartBattle").EmitSignal(BaseButton.SignalName.Pressed);
                await Game.CurrentOrder;
                await Frame();
                Check(!Game.Home.IsOpen && Game.BoardView.Visible && Game.Battle.Board.Kind == kind,
                    "new game starts with selected world " + kind);
                GD.Print($"WORLD MODE {kind}: seed={Game.Battle.Board.Seed}, cells={Game.Battle.Board.Tiles.Count}");
                Check(Game.Saves.Exists && Game.Saves.Read().Battle.Board.Kind == kind,
                    "automatic disposable save retains selected world " + kind);
                Check(Game.Battle.PlayerColor == FleetColor.Green && !Game.Battle.GodEye,
                    "world choice retains chosen color and ordinary fog");
                string before = Game.Battle.SaveJson();
                GD.Print($"WORLD MODE {kind}: Continue begins");
                await Game.ContinueSession();
                GD.Print($"WORLD MODE {kind}: Continue complete");
                Check(Game.Battle.SaveJson() == before && Game.Battle.Board.Kind == kind,
                    "Continue retains exact generated terrain and world policy " + kind);

                Game.Battle.SetGodEye(true); // Only the disposable on-screen capture uses full sight.
                Game.Refresh();
                Game.CancelOrder();
                Game.Hud.HideOpponentTurn(); // Keep the temporary start banner out of terrain screenshots.
                Game.MapCamera.FitBoard();
                GD.Print($"WORLD MODE {kind}: full chart begins");
                for (int frame = 0; frame < 8; frame++)
                    await Frame();
                Check(Game.BoardView.TerrainTextureIdle, "full chart raster settles for " + kind);
                Check(Game.BoardView.IslandContours.Count > 0 && Game.BoardView.IslandContours.All(contour =>
                    contour.All(point => point.IsFinite()) && Geometry2D.TriangulatePolygon(contour).Length >= 3),
                    "rounded shared shores triangulate for " + kind);
                Check(Game.Ambience.GetNodeOrNull<Node2D>("HighClouds") is { ZIndex: >= 3 },
                    "cloud volume stays above the surface layer");
                await Capture(kind.ToString().ToLowerInvariant() + "-map");
                GD.Print($"WORLD MODE {kind}: full chart complete");
                if (kind == WorldKind.Continents)
                {
                    var rivers = Game.Battle.Board.Tiles.Where(tile => tile.Terrain == TerrainType.Coast &&
                        Game.Battle.Board.IsNarrowPassage(tile.Position)).ToArray();
                    Check(rivers.Length > 0, "continents include shallow channels between land banks");
                    var center = rivers.MinBy(tile => Game.Battle.Board.Distance(tile.Position, Game.Battle.Board.CentralCell))!;
                    Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(center.Position);
                    Game.MapCamera.Zoom = Vector2.One * 1.4f;
                    Game.MapCamera.ForceUpdateScroll();
                    for (int frame = 0; frame < 8; frame++)
                        await Frame();
                    await Capture("continents-river-close");
                }
                Game.Battle.SetGodEye(false);
                Game.Refresh();
                Check(Game.Battle.SaveJson() == before, "capture leaves genuine fog and persistent state unchanged " + kind);
                Check(!Game.Saves.Read().Battle.GodEye, "capture reveal was never written to the save " + kind);
            }
            GD.Print($"PASS: {_checks} world selector/generation/save/native rendering checks ({DisplayServer.GetName()}).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
