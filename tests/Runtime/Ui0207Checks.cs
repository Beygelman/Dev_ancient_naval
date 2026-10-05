using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Real native inputs through the parchment HUD and its map-input boundaries.</summary>
public partial class Ui0207Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Ui0207: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node node in root.GetChildren()) foreach (Node child in Nodes(node)) yield return child;
    }
    private Button Button(string name) => Nodes(Game.Hud).OfType<Button>().Single(b => b.Name == name);
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Delay(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private static Vector2 Center(Control control) => control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
    private void Click(Control control)
    {
        Vector2 point = Center(control);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = point,
                GlobalPosition = point,
                ButtonIndex = MouseButton.Left,
                Pressed = down,
                ButtonMask = down ? MouseButtonMask.Left : (MouseButtonMask)0
            }, true);
    }
    private void Wheel(Control control)
    {
        Vector2 point = Center(control);
        GetViewport().PushInput(new InputEventMouseButton
        {
            Position = point,
            GlobalPosition = point,
            ButtonIndex = MouseButton.WheelDown,
            Pressed = true
        }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            Position = point,
            GlobalPosition = point,
            ButtonIndex = MouseButton.WheelDown,
            Pressed = false
        }, true);
    }
    private async Task Capture(string suffix)
    {
        string? file = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (file is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(file.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
        // Resume on a scene frame before injecting native input after rendering.
        await Frames(1);
    }
    private BattleState Fixture()
    {
        var town = new GridPosition(8, 7);
        var battle = new BattleState(new GameBoard(20, 20, p => p == town ? TerrainType.Land : TerrainType.Water),
            Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)),
                (Side.Player, ShipClass.Garrison, new GridPosition(7, 5)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 7)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)) },
            new[] { new GridPosition(4, 7) }, villageSpots: new[] { town });
        var save = battle.CaptureSnapshot();
        save.Credits[0] = 100;
        save.Ships[0].Level = 4;
        save.Villages[0] = save.Villages[0] with { Owner = Side.Player };
        return BattleState.LoadJson(BattleState.SerializeSnapshot(save));
    }
    private void Load()
    {
        Game.LoadScenario(Fixture());
        Game.Home.Hide();
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new GridPosition(6, 6));
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(5, 5));
        Game.Refresh();
    }
    private async Task ReadyList()
    {
        UiHints.Set(true, false);
        Load();
        await Frames();
        var ready = Game.Battle.ReadyActions(Side.Player);
        Check(ready.Count >= 3 && Game.Hud.ReadyObjectCount == ready.Count, "jug counts the exact ready object query");
        string unchanged = Game.Battle.SaveJson();
        Click(Button("ReadyActionsAmphora"));
        await Frames();
        Check(Game.Hud.ReadyActionsMenuVisible, "actual amphora mouse click opens the side parchment");
        var entries = Nodes(Game.Hud).OfType<Button>().Where(b => b.Name.ToString().StartsWith("SideReady")).ToArray();
        Check(entries.Length == ready.Count, "side menu includes each ready object once");
        foreach (var item in ready)
        {
            var entry = entries.Single(b => b.Name == "SideReady" + (item.ShipClass is null ? "Town" : "Ship") + item.Id);
            Check(entry.TooltipText == DebugHud.ReadyDescription(item), "hover text follows command-derived actions for " + item.Id);
            Check(entry.Text.Length == 0 && entry.GetThemeStylebox("normal") is StyleBoxEmpty, "ready icon has an invisible native plate");
        }
        var paper = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "ReadyActionsSidePaper");
        Check(paper.Position.X > UiScale.LogicalViewport(this).X * .6f, "ready parchment opens at the side");
        Check(paper.Size.Y <= UiScale.LogicalViewport(this).Y * .6f + 1, "ready parchment height remains bounded");
        var zoom = Game.MapCamera.Zoom;
        Wheel(paper);
        await Frames();
        Check(Game.MapCamera.Zoom == zoom, "scrolling the ready list never zooms the sea behind it");
        await Capture("ready-list");
        int id = ready.First(item => item.ShipClass == ShipClass.Garrison).Id;
        var chosen = entries.Single(b => b.Name == "SideReadyShip" + id);
        Click(chosen);
        await Game.CurrentOrder;
        await Frames();
        Check(!Game.Hud.ReadyActionsMenuVisible && Game.SelectedShipId == id,
            "ready icon returns to and selects its real object");
        Check(Game.Battle.SaveJson() == unchanged, "ready browsing and selection spend no battle actions");
    }
    private async Task MenuAndConfirmation()
    {
        Load();
        Game.FastChecks = false;
        Game.Hud.InstantPaperAnimations = false;
        Game.Hud.SetMenuVisible(true);
        await Frames(3);
        var paper = (RollingModalPaper)Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "GameMenuPaper");
        Check(paper.IsAnimating && paper.RollProgress is > 0 and < 1 && paper.Scale == Vector2.One,
            "menu reveals normal-scale content between moving rolled lips");
        await Delay(.5);
        await Frames();
        var scroll = Nodes(Game.Hud).OfType<ScrollContainer>().Single(n => n.Name == "GameMenuScroll");
        Check(scroll.VerticalScrollMode == ScrollContainer.ScrollMode.ShowNever, "menu keeps native scroll without a visible scrollbar");
        Check(!Nodes(paper).OfType<Label>().Any(label => label.Text == "Menu"), "paper has no redundant menu heading");
        var creative = Button("Creative");
        var eye = Button("GodEye");
        Check(creative.GetParent() == eye.GetParent() && creative.Position.X < eye.Position.X,
            "creative and divine eye icons share one horizontal row");
        Check(creative.Text == "" && eye.Text == "" && creative.TooltipText.Contains("Free ships") && eye.TooltipText.Contains("without fog"),
            "special modes have icon art and descriptive hover text");
        Check(Button("CloseMenu").Text == "Return to the voyage" && ((BrushPaperButton)Button("CloseMenu")).Underline,
            "return inscription has a nation brush underline");
        Vector2 zoom = Game.MapCamera.Zoom;
        Wheel(paper);
        await Frames();
        Check(Game.MapCamera.Zoom == zoom, "in-game menu wheel stays out of the map zoom");
        await Capture("game-menu");
        Click(Button("GameSettings"));
        await Frames();
        Check(Nodes(Game.Hud).OfType<HSlider>().Single().IsVisibleInTree(), "scale slider remains available inside Settings");
        Click(Button("CloseGameSettings"));
        await Frames();
        Check(!Nodes(Game.Hud).OfType<HSlider>().Single().IsVisibleInTree(), "main menu hides the Settings slider");
        Click(Button("CloseMenu"));
        await Frames(3);
        Check(Game.Hud.MenuVisible && ((BrushPaperButton)Button("CloseMenu")).StampProgress is > 0 and < 1,
            "return acknowledgement paints a handprint before folding");
        await Delay(.8);
        await Frames();
        Check(!Game.Hud.MenuVisible, "menu finishes closing after hand and roll animation");
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        Game.Hud.ShowTurnConfirmation();
        await Frames();
        Check(Game.Hud.TurnConfirmationVisible, "hints-on opens turn confirmation");
        Check(Button("ConfirmEndTurn") is BrushPaperButton && Button("CancelEndTurn") is BrushPaperButton,
            "confirmation uses invisible brush buttons");
        zoom = Game.MapCamera.Zoom;
        var turnPaper = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "EndTurnConfirmationPaper");
        Wheel(turnPaper);
        await Frames();
        Check(Game.MapCamera.Zoom == zoom, "confirmation wheel never zooms the map");
        await Capture("confirmation");
        Click(Button("CancelEndTurn"));
        await Frames();
        UiHints.Set(false, false);
        Game.Hud.ShowTurnConfirmation();
        Check(!Game.Hud.TurnConfirmationVisible, "hints-off bypasses confirmation");
        int before = Game.Battle.Round;
        Click(Button("EndTurn"));
        await Game.CurrentOrder;
        await Frames();
        Check(Game.Battle.Round > before && !Game.Hud.TurnConfirmationVisible,
            "hints-off actual EndTurn click advances the voyage immediately");
        Check(Button("EndTurn").IsVisibleInTree(), "turn parchment returns when the human turn resumes");
    }
    private async Task Advice()
    {
        Load();
        UiHints.Set(true, false);
        Game.TutorialHistory.Begin(Game.Battle, true);
        Game.TutorialHistory.Observe(Game.Battle, true);
        await Frames();
        var hud = Game.Tutorial;
        await Delay(.5);
        await Frames();
        Check(hud.IsOpen && hud.Topic == "resources", "resource event opens advice");
        var text = Nodes(hud).OfType<Label>().Single(n => n.Name == "TutorialAdviceDescription");
        string initial = text.Text;
        Game.SelectCell(new(7, 5));
        Game.Refresh();
        await Frames();
        Check(hud.IsOpen && hud.Topic == "resources" && text.Text == initial, "selection changes never replace the active advice");
        var ack = (BrushPaperButton)Nodes(hud).OfType<Button>().Single(n => n.Name == "CloseTutorialAdvice");
        Check(ack.Text == "Taken to heart" && ack.Position.Y >= text.Position.Y + text.Size.Y,
            "advice acknowledgement is at the bottom instead of a cross");
        await Capture("advice");
        Click(ack);
        await Frames(3);
        Check(hud.IsOpen && ack.StampProgress is > 0 and < 1, "advice handprint is visible before dismissal");
        hud.Clear();
        hud.Present("resources", "Fresh voyage", "New advice survives an interrupted older acknowledgement.",
            Nodes(hud).OfType<TextureRect>().Single(n => n.Name == "TutorialAdviceScreenshot").Texture!.ResourcePath);
        await Delay(.5);
        await Frames();
        Check(hud.IsOpen && text.Text.StartsWith("New advice"),
            "a replaced advice ceremony cannot clear a later voyage's same topic");
        Click(ack);
        await Delay(.8);
        await Frames();
        Check(!hud.IsOpen, "replacement advice can be acknowledged without a stranded stamp task");
        foreach (string locale in new[] { "uk", "nl" })
            foreach (string clause in new[] { "Taken to heart", "Return to the voyage", "Sail to an available tile", "Build a gun tower or lighthouse" })
                Check(LocalizedMessages.Translate(clause, locale) != clause, "new HUD clause is localized in " + locale);
    }
    private async Task Outcome()
    {
        UiHints.Set(false, false);
        var result = Game.Victory!;
        result.InstantAnimations = true;
        result.ShowOutcome(Game.Battle.Statistics, Game.Battle.Round, true,
            "You preserved your people.\nThe ship of new hope sails on.", new Color("d85d69"));
        await Frames();
        var paper = Nodes(result).OfType<PanelContainer>().Single(n => n.Name == "VictoryPaper");
        Check(paper is RollingModalPaper, "result uses the same rolled parchment family");
        Check(Nodes(result).OfType<ScrollContainer>().Single().VerticalScrollMode == ScrollContainer.ScrollMode.ShowNever,
            "result scrollbar stays invisible");
        Check(Nodes(result).OfType<Button>().All(button => button is BrushPaperButton), "result actions use invisible brush plaques");
        var zoom = Game.MapCamera.Zoom;
        Wheel(paper);
        await Frames();
        Check(Game.MapCamera.Zoom == zoom, "result wheel stays inside its parchment");
        Nodes(result).OfType<ScrollContainer>().Single().ScrollVertical = 0;
        await Frames();
        await Capture("outcome");
        Click(Nodes(result).OfType<Button>().Single(n => n.Name == "InspectMap"));
        await Frames();
        Check(!result.IsOpen, "actual result action folds the parchment and exposes the map");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            await ReadyList();
            await MenuAndConfirmation();
            await Advice();
            await Outcome();
            GD.Print($"PASS: {_checks} v020.7 native parchment, ready objects, static advice, acknowledgement and modal wheel checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}

