using System;
using System.Collections.Generic;
using System.IO;
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

/// <summary>Native clicks and keyboard through guidance, confirmation, scaling and reward hints.</summary>
public partial class Hints0206Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("Hints0206: " + message); _checks++; }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var item in Nodes(child)) yield return item;
    }
    private Button Button(string name) => Nodes(Game.Hud).OfType<Button>().Single(b => b.Name == name);
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Click(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed }, true);
    }
    private void KeyPress(Key key)
    {
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed }, true);
    }
    private async Task Capture(string suffix)
    {
        string? file = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (file is null || DisplayServer.GetName() == "headless") return;
        await Frames(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(file.Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
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
        var save = battle.CaptureSnapshot(); save.Credits[0] = 100;
        save.Ships[0].Level = 3;
        save.Villages[0] = save.Villages[0] with { Owner = Side.Player };
        return BattleState.LoadJson(BattleState.SerializeSnapshot(save));
    }
    private void LoadFixture()
    {
        Game.LoadScenario(Fixture()); Game.Home.Hide(); Game.FastChecks = true;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new GridPosition(6, 6));
        Game.MapCamera.Zoom = Vector2.One * 1.1f; Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(5, 5)); Game.Refresh();
    }
    private async Task CheckSettings()
    {
        Game.Home.ShowHome(false); await Frames();
        Click(Nodes(Game.Home).OfType<Button>().Single(b => b.Name == "HomeSettings")); await Frames();
        var toggle = Nodes(Game.Home).OfType<Button>().Single(b => b.Name == "HintsToggle");
        Check(toggle.IsVisibleInTree(), "guidance preference is in title settings");
        bool old = UiHints.Enabled; Click(toggle); await Frames();
        Check(UiHints.Enabled != old, "native title toggle changes guidance");
        Click(toggle); await Frames(); Check(UiHints.Enabled == old, "toggle restores guidance");
        string? setting = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--ui-settings-file="))?[19..];
        if (setting is not null) Check(File.ReadAllText(Path.GetFullPath(setting) + ".hints").Trim() == (old ? "on" : "off"), "guidance persists separately from saved rules");
        Game.Home.Hide();
        Game.Hud.SetMenuVisible(true); await Frames(); Click(Button("GameSettings")); await Frames();
        var gameToggle = Button("HintsToggle");
        Check(gameToggle.IsVisibleInTree(), "guidance preference is in in-game settings");
        Click(gameToggle); await Frames(); Check(UiHints.Enabled != old, "both menus share one preference");
        Click(gameToggle); await Frames(); Game.Hud.SetMenuVisible(false);
    }
    private async Task CheckConfirmation()
    {
        UiHints.Set(true); LoadFixture(); await Frames();
        var expected = Game.Battle.ReadyActions(Side.Player);
        Check(Game.Hud.ReadyObjectCount == expected.Count && expected.Count >= 3, "amphora counts each genuinely ready object once");
        var jug = Nodes(Game.Hud).OfType<Control>().Single(n => n.Name == "ReadyActionsAmphora");
        Check(jug.IsVisibleInTree() && jug.Position.Y + jug.Size.Y < Button("EndTurn").Position.Y, "nation amphora sits above the end-turn scroll");
        string untouched = Game.Battle.SaveJson();
        int searches = Game.Hud.ReadyQueryRebuilds;
        for (int i = 0; i < 20; i++) Game.Refresh();
        Check(Game.Hud.ReadyQueryRebuilds == searches, "unchanged selection refreshes retain ready-action navigation results");
        Game.FastChecks = false;
        Click(Button("EndTurn")); await Frames(3);
        var endPaper = (EndTurnPaper)Button("EndTurn");
        Check(endPaper.StampProgress > 0 && endPaper.StampProgress < 1, "native click starts the four-finger red stamp before confirmation");
        Game.SelectCell(new(7, 5)); KeyPress(Key.R); await Frames(1);
        Check(Game.Battle.SaveJson() == untouched && Game.SelectedShipId == Game.Battle.Mothership(Side.Player)!.Id,
            "ink stamp locks map orders until its ceremony finishes");
        await Capture("end-turn-stamp");
        await Game.CurrentOrder; await Frames();
        Check(Game.Hud.TurnConfirmationVisible, "animated stamp proceeds to confirmation");
        Click(Button("CancelEndTurn")); await Frames();
        Game.FastChecks = true;
        Click(Button("EndTurn")); await Game.CurrentOrder; await Frames();
        Check(Game.Hud.TurnConfirmationVisible && Game.Battle.SaveJson() == untouched, "native end-turn click confirms before mutation");
        var grid = Nodes(Game.Hud).OfType<GridContainer>().Single(n => n.Name == "ReadyActionGrid");
        Check(grid.Columns == 5 && grid.GetChildCount() == expected.Count, "ready objects occupy five columns without duplicate resource sites");
        int? selected = Game.SelectedShipId;
        Game.SelectCell(new(7, 5)); KeyPress(Key.R); KeyPress(Key.Space);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = new(250, 200), GlobalPosition = new(250, 200),
                ButtonIndex = MouseButton.Right, Pressed = pressed }, true);
        var camera = Game.MapCamera.Position;
        GetViewport().PushInput(new InputEventKey { Keycode = Key.D, PhysicalKeycode = Key.D, Pressed = true }, true);
        await Frames(4);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.D, PhysicalKeycode = Key.D, Pressed = false }, true);
        Check(Game.MapCamera.Position == camera, "confirmation owns arrow/WASD camera keys");
        await Frames();
        Check(Game.Battle.SaveJson() == untouched && Game.SelectedShipId == selected, "modal blocks map selection, right-click deselection, repair and repeat Space commands");
        await Capture("ready-objects");
        foreach (var locale in new[] { "en", "uk", "nl" })
        foreach (float scale in new[] { .8f, 1.25f })
        {
            Language.Set(locale, persist: false); UiScale.Set(scale, persist: false); await Frames();
            var paper = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "EndTurnConfirmationPaper");
            var rect = new Rect2(paper.GetGlobalTransformWithCanvas().Origin, paper.Size * UiScale.Value);
            Check(rect.Size.Y <= GetViewport().GetVisibleRect().Size.Y * .6f + 1 && rect.Position.X >= 0
                && rect.End.X <= GetViewport().GetVisibleRect().Size.X, "localized scaled confirmation remains bounded");
            Check(grid.GetGlobalTransformWithCanvas().Origin.X >= rect.Position.X
                && grid.GetGlobalTransformWithCanvas().Origin.X + grid.Size.X * UiScale.Value <= rect.End.X + 1,
                "five-column object grid remains within parchment");
        }
        Language.Set("en", persist: false); UiScale.Set(1, persist: false); await Frames();
        var entry = grid.GetChildren().OfType<Button>().First(); Click(entry); await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && Game.Battle.SaveJson() == untouched, "selecting an object returns to map without spending actions");
        KeyPress(Key.Space); await Game.CurrentOrder; await Frames();
        Check(Game.Hud.TurnConfirmationVisible, "Space uses the same confirmation path");
        Click(Button("CancelEndTurn")); await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && Game.Battle.SaveJson() == untouched, "cancel never advances the turn");
        KeyPress(Key.Space); await Game.CurrentOrder; await Frames(); KeyPress(Key.Escape); await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && !Game.Hud.MenuVisible && Game.Battle.SaveJson() == untouched,
            "Escape closes confirmation without opening menu or advancing the turn");
        Click(Button("EndTurn")); await Game.CurrentOrder; await Frames();
        Click(Button("ConfirmEndTurn")); await Game.CurrentOrder; await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && Game.Battle.Round > 1, "explicit confirmation advances real player and rival turns");
        UiHints.Set(false); LoadFixture(); await Frames(); untouched = Game.Battle.SaveJson();
        Check(!jug.IsVisibleInTree(), "guidance off hides action counter");
        Click(Button("EndTurn")); await Game.CurrentOrder; await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && Game.Battle.SaveJson() != untouched, "guidance off ends the turn directly");
    }
    private async Task CheckHints()
    {
        var reward = new RewardPapyrusHud(); Game.AddChild(reward); reward.ShowHeavenly("hint-test", 2); await Frames();
        var hint = Nodes(reward).OfType<Label>().Single(n => n.Name == "RewardGuidance");
        UiHints.Set(true); await Frames(); Check(hint.IsVisibleInTree(), "reward hint is shown when enabled");
        UiHints.Set(false); await Frames(); Check(!hint.IsVisibleInTree(), "reward hint responds immediately to preference");
        var stories = Nodes(Game.Hud).OfType<Label>().Where(n => n.Name == "ActionGuidance").ToArray();
        Check(stories.Length >= 2 && stories.All(n => !n.Visible), "capture and relic hints share the setting");
        UiHints.Set(true); Check(stories.All(n => n.Visible), "capture and relic hints reappear without restarting scrolls");
        reward.Close(); reward.QueueFree();
        foreach (string text in new[] { "Hints: on ✓", "Hints: off", "End your turn?", "Keep exploring", "Accept the reward to continue.", "Click to capture the town.", "Click to collect relics." })
        foreach (string locale in new[] { "uk", "nl" })
            Check(LocalizedMessages.Translate(text, locale) != text, "guidance is translated into " + locale + ": " + text);
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); await CheckSettings(); await CheckConfirmation(); await CheckHints();
            UiHints.Set(false, persist: false);
            GD.Print($"PASS: {_checks} v020.6 native guidance, turn confirmation, setting, translation and modal checks."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
