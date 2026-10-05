using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class Information0208Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("Information0208: " + name);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node node in Nodes(child)) yield return node;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Wheel(Control control)
    {
        var p = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = p, GlobalPosition = p }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p,
                ButtonIndex = MouseButton.WheelDown, Pressed = down }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native card capture " + suffix);
        await Frames(1);
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); Game.FastChecks = true;
            var rules = Game.Battle.Rules;
            var board = new GameBoard(18, 18, p => p == new GridPosition(7, 7) ? TerrainType.Land : TerrainType.Water);
            var battle = new BattleState(board, rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(4, 4)),
                (Side.Player, ShipClass.Kolonel, new GridPosition(5, 4)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(13, 13)) },
                fishSpots: Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(7, 7) });
            Game.LoadScenario(battle); Game.Refresh();
            var mother = battle.Mothership(Side.Player)!;
            Game.SelectCell(mother.Position); await Frames();
            var card = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "InformationScroll");
            var scroll = Nodes(card).OfType<ScrollContainer>().Single();
            Check(Game.Hud.InformationVisible, "selection automatically shows complete counsel");
            await ToSignal(GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
            var commands = Nodes(Game.Hud).OfType<SectorButton>().Where(n => n.IsVisibleInTree()).ToArray();
            Check(!Nodes(Game.Hud).OfType<SectorButton>().Any(n => n.Name == "ActionInformation" || n.Name == "ResourceInformation")
                && commands.All(n => n._HasPoint(n.IconCenter)),
                "automatic counsel replaces obsolete Info commands without losing real action hit regions");
            Check(Nodes(card).OfType<Label>().Any(n => n.Name == "InformationFieldLabel" && n.Text == "Health"),
                "requested health fact accompanies the world badge");
            Check(Nodes(card).OfType<GridContainer>().All(n => n.Columns == 2), "details use two equal fact columns");
            Check(Nodes(card).OfType<Label>().Where(n => n.Name == "InformationFieldValue").All(n => n.GetThemeFontSize("font_size") <= 12),
                "detail typography is compact");
            var seal = Nodes(card).OfType<ObjectWaxSeal>().Single();
            foreach (FleetColor faction in Enum.GetValues<FleetColor>())
            {
                battle.SetPlayerColor(faction); Game.Refresh(); await Frames();
                Check(seal.Wax == FleetPalette.Color(faction), "wax reflects selected object's nation " + faction);
                Check(Nodes(card).OfType<ObjectCardOrnament>().Single().Nation == faction, "vertical ornament follows object nation " + faction);
            }
            foreach (float scale in new[] { .8f, 1.25f })
            {
                UiScale.Set(scale, persist: false);
                foreach (string locale in new[] { "en", "uk", "nl" })
                {
                    Language.Set(locale, persist: false); Game.Refresh(); await Frames();
                    var viewport = UiScale.LogicalViewport(this);
                    Check(Math.Abs(card.Position.X - 14) < 1 && Math.Abs(viewport.Y - card.Position.Y - card.Size.Y - 14) < 1,
                        "card stays in lower-left at " + locale);
                    Check(card.Size.X <= 353 && card.Size.Y <= 287, "card has a readable bounded footprint " + locale);
                    var zoom = Game.MapCamera.Zoom;
                    scroll.ScrollVertical = 0; await Frames(); Wheel(scroll); await Frames();
                    Check(scroll.ScrollVertical > 0 && Game.MapCamera.Zoom == zoom, "native wheel reveals facts without zooming sea " + locale);
                    int offset = scroll.ScrollVertical; Game.Refresh(); await Frames();
                    Check(scroll.ScrollVertical == offset, "same-object refresh keeps reading position " + locale);
                }
            }
            UiScale.Set(1, persist: false); Language.Set("en", persist: false); battle.SetPlayerColor(FleetColor.Red);
            Game.Refresh(); scroll.ScrollVertical = 0; await Frames(); await Capture("flagship");
            battle.SetGodEye(true); Game.Refresh();
            Game.Hud.ShowInformation(battle, battle.Villages[0].Position); await Frames();
            Check(scroll.ScrollVertical == 0 && seal.Symbol == ActionSymbol.City, "new town resets reading and has town seal");
            await Capture("town");
            Game.SelectCell(new(1, 1)); Game.Hud.ShowInformation(battle, new(1, 1)); await Frames();
            Check(!Game.Hud.InformationVisible, "empty terrain has no object card");
            GD.Print($"PASS: {_checks} v020.8 compact object counsel, faction seals, columns, native wheel and visibility checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
