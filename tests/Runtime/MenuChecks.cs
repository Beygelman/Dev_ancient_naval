using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class MenuChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
    private void Check(bool ok, string name)
    {
        if (!ok)
            throw new Exception(name);
        _checks++;
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var n in Descendants(child))
                yield return n;
    }

    private Button Button(string name) => Descendants(Game.Home).OfType<Button>().Single(b => b.Name == name);
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task Capture(string suffix)
    {
        string? arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await Frame();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok, "Capture " + suffix);
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            Check(Game.Home.IsOpen && !Game.BoardView.Visible && !Game.Hud.Visible, "Title screen opens before battle");
            Check(Descendants(Game.Home).OfType<TextureRect>().Single(t => t.Name == "AncientNavalTitle").Texture is not null, "Generated title asset imports");
            Check(Descendants(Game.Home).OfType<TextureRect>().Single(t => t.Name == "CityShipArtwork").Texture is not null, "Static generated city-ship background imports");
            Check(!Descendants(Game.Home).OfType<DevAncientNaval.Presentation.UI.MenuHarborView>().Single().IsProcessing(), "Static menu background has no animation loop");
            await Capture("title");
            if (OS.GetCmdlineUserArgs().Contains("--resume-only"))
            {
                Check(!Button("HomeContinue").Disabled, "Saved battle is available after process restart");
                Button("HomeContinue").EmitSignal(BaseButton.SignalName.Pressed);
                await Game.CurrentOrder;
                Check(!Game.Home.IsOpen && Game.Battle.PlayerColor == FleetColor.Purple, "Continue loads in a new process");
            }
            else
            {
                Button("HomeNewGame").EmitSignal(BaseButton.SignalName.Pressed);
                await Frame();
                foreach (var color in Enum.GetValues<FleetColor>())
                {
                    Button("FleetColor" + color).EmitSignal(BaseButton.SignalName.Pressed);
                    Check(Game.Home.SelectedColor == color, "Color swatch " + color);
                }

                Button("FleetColorPurple").EmitSignal(BaseButton.SignalName.Pressed);
                await Capture("colors");
                Button("StartBattle").EmitSignal(BaseButton.SignalName.Pressed);
                await Game.CurrentOrder;
                await Frame();
                Check(!Game.Home.IsOpen && Game.BoardView.Visible && Game.Hud.Visible && Game.Battle.PlayerColor == FleetColor.Purple, "New game begins in chosen color");
                Check(Game.Saves.Exists, "New battle saved automatically");
                var mother = Game.Battle.Mothership(Side.Player)!;
                Game.SelectCell(mother.Position);
                await Frame();
                Check(Game.SelectedShipId == mother.Id, "Commands enabled after menu generation");
                var fisher = Game.Battle.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Fishing);
                var destination = Game.Battle.Reachable(fisher.Id).Keys.First(p => p != fisher.Position && !Game.Battle.CollectionCells(Side.Player).Contains(p) && !Game.Battle.DockCells(Side.Player).Contains(p));
                Game.SelectCell(fisher.Position);
                Game.SelectCell(destination);
                await Game.CurrentOrder;
                Check(fisher.Position == destination && Game.Saves.Read().Battle.Find(fisher.Id)!.Position == destination, "Move through UI persists without leaving battle");
                string saved = Game.Battle.SaveJson();
                Game.MapCamera.Zoom = Vector2.One * .7f;
                Game.ShowHome();
                await Frame();
                Check(Game.Home.IsOpen && !Button("HomeContinue").Disabled, "Save and return to title enables Continue");
                Button("HomeContinue").EmitSignal(BaseButton.SignalName.Pressed);
                await Game.CurrentOrder;
                await Frame();
                Check(Game.Battle.SaveJson() == saved && Math.Abs(Game.MapCamera.Zoom.X - .7f) < .001, "Continue restores exact battle and camera");
                Game.SaveSession();
                System.IO.File.WriteAllText(Game.Saves.Path, "interrupted write");
                Check(Game.Saves.Read().Backup, "Corrupt primary recovers complete backup");
                Game.SaveSession();
            }

            _checks += UiPapyrusChecks.Run(Game);
            foreach (var tile in Game.Battle.Board.Tiles)
                Game.Battle.Vision.RevealCombat(Side.Player, tile.Position);
            Game.Battle.Vision.Recompute(Game.Battle.Ships, Game.Battle.TurnSerial, Game.Battle.Villages);
            Game.Refresh();
            Game.MapCamera.FitBoard();
            await Capture("map");
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(Game.Battle.Mothership(Side.Player)!.Position);
            Game.MapCamera.Zoom = Vector2.One * 1.4f;
            await Capture("fleet");
            var flagship = Game.Battle.Mothership(Side.Player)!;
            Game.SelectCell(flagship.Position);
            await ToSignal(GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
            await Frame();
            await Capture("papyrus");
            Check(!Descendants(Game.Hud).OfType<DevAncientNaval.Presentation.UI.SectorButton>().Any(b => b.Name == "ActionInformation"), "Information is integrated into the selected object card.");
            void MouseClick(Vector2 point, MouseButton button)
            {
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = true }, true);
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = false }, true);
            }


            await Frame();
            Check(Game.Hud.InformationVisible, "Selecting the ship opens the integrated counsel.");
            var sections = Descendants(Game.Hud).OfType<Label>().Where(n => n.Name == "InformationSectionHeading").ToArray();
            Check(sections.Select(s => s.Text).Contains("AT A GLANCE") && sections.Select(s => s.Text).Contains("WEAPONS"), "Information has visibly separate fact groups");
            Check(Descendants(Game.Hud).OfType<Label>().Count(n => n.Name == "InformationFieldLabel") >= 8, "Facts have individual label/value rows");
            await Capture("lore");
            var infoPanel = Descendants(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "InformationScroll");
            MouseClick(infoPanel.GetGlobalRect().GetCenter(), MouseButton.Right);
            await Frame();
            Check(Game.SelectedShipId is null && Game.SelectedVillageId is null && !Game.Hud.InformationVisible, "Right click over the information panel closes it and clears selection.");
            var resourceCell = Game.Battle.CollectionCells(Side.Player).First();
            Game.SelectCell(resourceCell);
            await ToSignal(GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
            await Frame();
            var resourceAction = Descendants(Game.Hud).OfType<Button>().Single(b => b.Name == "TileResource");
            Check(resourceAction.IsVisibleInTree(), "Resource scroll is open before right-click cancellation.");
            Check(!Descendants(Game.Hud).OfType<DevAncientNaval.Presentation.UI.SectorButton>().Any(b => b.Name == "ResourceInformation"), "Resource facts need no separate information sector.");
            await Frame();
            Check(Game.Hud.InformationVisible && Game.Hud.InformationText.Contains("1 Mothership resource"), "Resource information describes the selected shoal.");
            await Frame();
            var shortContent = Descendants(infoPanel).OfType<ScrollContainer>().Single();
            Check(shortContent.Size.Y <= infoPanel.Size.Y && infoPanel.Size.Y <= 239, "Resource facts use the compact bounded reading viewport");
            await Capture("resource-lore");
            MouseClick(infoPanel.GetGlobalRect().GetCenter(), MouseButton.Right);
            await Frame();
            Check(Game.SelectedShipId is null && !Game.Hud.InformationVisible && !resourceAction.IsVisibleInTree(), "Right click closes the resource scroll and its information without purchasing.");
            // Presentation-only reveal above must not overwrite the player's saved fog.
            string? saveArgument = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--save-file="));
            if (saveArgument is not null)
                _checks += SaveRecoveryChecks.Run(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(saveArgument[12..]))!, Game.Battle);
            await CaptureInformationExamples();
            GD.Print($"PASS: {_checks} title/color/save/reload checks.");
            GetTree().Quit();
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task CaptureInformationExamples()
    {
        Game.LoadScenario(LoreChecks.Example(Game.Battle.Rules));
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(8, 10));
        Game.MapCamera.Zoom = Vector2.One * 1.5f;
        var mother = Game.Battle.Mothership(Side.Player)!;
        Game.Hud.ShowInformation(Game.Battle, mother.Position);
        await Frame();
        await Frame();
        var panel = Descendants(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "InformationScroll");
        var content = Descendants(panel).OfType<ScrollContainer>().Single();
        Check(panel.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y - 13,
            "Long information retains the compact lower-left screen margin");
        Check(content.GetVScrollBar().MaxValue > content.GetVScrollBar().Page, "Long installed-equipment card scrolls instead of overflowing");
        await Capture("upgraded-lore");
        content.ScrollVertical = 10000;
        await Frame();
        Check(content.ScrollVertical > 0, "Installed improvements can be reached by scrolling");
        await Capture("installed-lore");
        Game.Hud.UpdateBattle(Game.Battle, mother, false, OrderMode.None);
        await Frame();
        Check(content.ScrollVertical > 0, "Unchanged state retains the current reading position");
        Game.Hud.CloseMenus();
        Game.Hud.ShowInformation(Game.Battle, Game.Battle.Villages[1].Position);
        await Frame();
        await Frame();
        Check(content.ScrollVertical == 0 && Game.Hud.InformationText.Contains("Port:"), "Selecting a different object starts its card at the top");
        await Capture("city-lore");
        content.ScrollVertical = 10000;
        await Frame();
        await Capture("city-improvements");
        Game.Hud.CloseMenus();
        var radar = Game.Battle.CaptureSnapshot();
        var hidden = radar.Ships.Single(s => s.Owner == Side.Enemy);
        // The new Lighthouse sees the old eastern fixture. Put the contact
        // beyond the veteran tower's optical reach but within its radar.
        hidden.Position = new(18, 15);
        radar.GodEye = false;
        Game.LoadScenario(DevAncientNaval.Core.Battle.BattleState.LoadJson(DevAncientNaval.Core.Battle.BattleState.SerializeSnapshot(radar)));
        Check(Game.Battle.Vision.IsRadarContact(Side.Player, hidden.Position) && !Game.Battle.Vision.IsVisible(Side.Player, hidden.Position), "Radar fixture is not directly observed");
        Game.Hud.ShowInformation(Game.Battle, hidden.Position);
        await Frame();
        Check(Game.Hud.InformationVisible && !Game.Hud.InformationText.Contains("Health") && !Game.Hud.InformationText.Contains("Mothership"), "Radar information does not disclose health or identity");
        Game.Hud.CloseMenus();
    }
}
