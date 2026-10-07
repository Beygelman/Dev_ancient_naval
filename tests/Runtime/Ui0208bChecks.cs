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

/// <summary>Native input checks for compact counsel, advice, relic controls and translated ship seals.</summary>
public partial class Ui0208bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private static readonly string[] InkStates = { "font_color", "font_hover_color", "font_pressed_color",
        "font_focus_color", "font_disabled_color", "font_hover_pressed_color" };
    private void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Ui0208b: " + message); _checks++; }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node item in Nodes(child)) yield return item;
    }
    private static T Named<T>(Node root, string name) where T : Node =>
        Nodes(root).OfType<T>().Single(node => node.Name == name);
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Opened()
    { await ToSignal(GetTree().CreateTimer(.55), SceneTreeTimer.SignalName.Timeout); await Frames(); }
    private void Click(Control control, Vector2? local = null)
    {
        Vector2 point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0 }, true);
    }
    private void Wheel(Control control, MouseButton button, Vector2? local = null)
    {
        Vector2 point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = button, Pressed = down }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
    }
    private BattleState Fixture(bool doubleShot = false)
    {
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)),
                (Side.Player, ShipClass.Garrison, new GridPosition(7, 5)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)) });
        var save = battle.CaptureSnapshot();
        save.Credits[0] = 100;
        var mother = save.Ships.Single(ship => ship.Owner == Side.Player && ship.Kind == ShipClass.Mothership);
        mother.Level = 3;
        mother.SecondAttackUpgrade = doubleShot;
        mother.Health = Game.Battle.Rules.Get(ShipClass.Mothership).MaxHealth
            + 2 * Game.Battle.Rules.Get(ShipClass.Mothership).HealthPerLevel;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        return battle;
    }
    private async Task Load()
    {
        UiHints.Set(false, false);
        Game.LoadScenario(Fixture()); Game.Home.Hide(); Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(5, 5));
        Game.MapCamera.Zoom = Vector2.One; Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(5, 5)); Game.Refresh();
        await Frames();
    }
    private async Task Advice()
    {
        var startup = Game.Tutorial;
        if (startup.Topic is null)
            Check(!Named<PanelContainer>(startup, "TutorialAdvicePaper").IsVisibleInTree(), "startup has no empty advice paper");
        else
            Check(!string.IsNullOrWhiteSpace(Named<Label>(startup, "TutorialAdviceTitle").Text)
                && !string.IsNullOrWhiteSpace(Named<Label>(startup, "TutorialAdviceDescription").Text),
                "any startup advice has a real title and body");
        await Load();
        var advice = new TutorialHud(); AddChild(advice);
        UiHints.Set(true, false); Game.TutorialHistory.Suspend();
        advice.Present("empty", "", "", TutorialAdvice.ScreenshotPath("kolonel"));
        advice.SetAllowed(true);
        Check(!advice.IsOpen && advice.Topic is null && !Named<PanelContainer>(advice, "TutorialAdvicePaper").IsVisibleInTree(),
            "an empty presentation stays hidden even after SetAllowed");
        var battle = Fixture(doubleShot: true);
        Check(battle.OwnShips(Side.Player).All(ship => ship.Definition.Class != ShipClass.Kolonel),
            "double-salvo fixture has only a Mothership and Brig");
        Check(battle.BuildBlockReason(Side.Player, battle.Mothership(Side.Player)!.Id, ShipClass.Kolonel) is not null,
            "Mothership double shot is available before Kolonel construction");
        string unchanged = battle.SaveJson();
        var history = new TutorialAdvice(advice, null);
        history.Begin(battle, false); history.Observe(battle, true);
        await Opened();
        Check(advice.IsOpen && advice.Topic == "kolonel", "Mothership upgrade triggers double-salvo advice without a Kolonel");
        Check(battle.SaveJson() == unchanged, "advice consumes no Core state or RNG");
        Check(Named<PanelContainer>(advice, "TutorialAdvicePaper").Size.X <= 330.1f,
            "advice is no wider than 330 logical pixels");
        Check(Named<Label>(advice, "TutorialAdviceTitle").GetThemeFontSize("font_size") == 15
            && Named<Label>(advice, "TutorialAdviceDescription").GetThemeFontSize("font_size") == 11,
            "compact advice keeps the requested smaller readable font sizes");
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, false);
            string source = TutorialAdvice.Content("kolonel", battle).Body;
            string translated = Language.Translate(source);
            Check(source.Contains("Mothership or Kolonel") && (locale == "en" || translated != source),
                "Mothership double-shot advice is translated " + locale);
            string command = TutorialAdvice.Content("commands", battle).Body;
            Check(locale == "en" || Language.Translate(command) != command, "new relic instruction is translated " + locale);
        }
        Language.Set("en", false);
        await Capture("compact-mothership-advice");
        advice.Clear(); advice.QueueFree(); UiHints.Set(false, false); await Frames();
    }
    private async Task CounselInput()
    {
        await Load();
        var paper = Named<PanelContainer>(Game.Hud, "InformationScroll");
        var scroll = Named<ScrollContainer>(paper, "InformationContentScroll");
        string[] facts = Nodes(paper).OfType<Label>().Where(label => label.Name == "InformationFieldLabel")
            .Select(label => label.Text).ToArray();
        Check(facts.Take(3).SequenceEqual(new[] { "Health", "Movement", "Resources" }),
            "primary health, movement and progression facts precede auxiliary sections");
        Check(Array.IndexOf(facts, "Sight") > Array.IndexOf(facts, "Resources"), "lookout follows primary progression facts");
        foreach (float scale in new[] { .8f, 1.25f })
        {
            UiScale.Set(scale, false); await Frames();
            var zoom = Game.MapCamera.Zoom; var position = Game.MapCamera.Position;
            scroll.ScrollVertical = 0; await Frames();
            Wheel(scroll, MouseButton.WheelUp); await Frames();
            Check(Game.MapCamera.Zoom == zoom && Game.MapCamera.Position == position, "top-end wheel belongs to counsel at UI " + scale);
            scroll.ScrollVertical = int.MaxValue; await Frames();
            Wheel(scroll, MouseButton.WheelDown); await Frames();
            Check(Game.MapCamera.Zoom == zoom && Game.MapCamera.Position == position, "bottom-end wheel belongs to counsel at UI " + scale);
            Wheel(paper, MouseButton.WheelDown, new(20, 20)); await Frames();
            Check(Game.MapCamera.Zoom == zoom && Game.MapCamera.Position == position, "blank ornament margin owns wheel at UI " + scale);
            Wheel(Named<Label>(paper, "SelectedObjectName"), MouseButton.WheelDown); await Frames();
            Check(Game.MapCamera.Zoom == zoom && Game.MapCamera.Position == position, "counsel header owns wheel at UI " + scale);
        }
        UiScale.Set(1, false); await Frames(); await Capture("counsel-primary-facts");
    }
    private async Task Relic()
    {
        await Load();
        var relic = Nodes(Game.Hud).OfType<ReadyActionJug>().Single();
        Check(relic.Size == new Vector2(196, 230), "nation relic has the larger 196 by 230 footprint");
        Vector2 bottom = relic.GetGlobalTransformWithCanvas() * relic.Size;
        Vector2 count = relic.GetGlobalTransformWithCanvas() * relic.PrintedCountCenter;
        Check(bottom.Y > GetViewport().GetVisibleRect().Size.Y && count.Y < GetViewport().GetVisibleRect().Size.Y,
            "relic pedestal protrudes below the viewport while its number remains visible");
        var ready = Game.Battle.ReadyActions(Side.Player);
        string unchanged = Game.Battle.SaveJson();
        Click(relic, relic.PrintedCountCenter); await Game.CurrentOrder; await Frames();
        Check(Game.SelectedShipId == ready[0].Id && !Game.Hud.TurnConfirmationVisible,
            "native numeral click cycles to the first useful object without ending the turn");
        Check(Game.Battle.SaveJson() == unchanged, "numeral camera cycling changes no actions or RNG");
        UiHints.Set(true, false); Game.TutorialHistory.Suspend(); await Frames();
        Click(relic, new(relic.Size.X * .5f, 70)); await Game.CurrentOrder; await Frames();
        Check(Game.Hud.TurnConfirmationVisible, "native upper relic click opens hints-on end-turn confirmation");
        Check(Game.Battle.SaveJson() == unchanged, "confirmation does not commit the turn before acceptance");
        await Capture("relic-confirmation");
        Click(Named<Button>(Game.Hud, "CancelEndTurn")); await Opened();
        Check(!Game.Hud.TurnConfirmationVisible, "native cancel keeps the current turn");
        UiHints.Set(false, false);
        Game.FastChecks = false;
        Game.Hud.InstantPaperAnimations = false;
        Task ending = Game.EndPlayerTurn();
        Click(Named<Button>(Game.Hud, "Menu"));
        Check(Game.Hud.MenuVisible, "native menu click interrupts the relic fade before the turn commits");
        await ending;
        Check(Game.Battle.ActiveSide == Side.Player && Game.Battle.SaveJson() == unchanged,
            "interrupted end-turn animation preserves the original human turn and Core state");
        Click(Named<Button>(Game.Hud, "CloseMenu"));
        for (int frame = 0; frame < 180 && Game.Hud.MenuVisible; frame++) await Frames(1);
        Game.Refresh();
        Check(!Game.Hud.MenuVisible, "interrupting menu finishes its stamp and fold transition");
        Check(relic.HumanTurnActive, "closing the interrupting menu restores the active nation relic");
        await ToSignal(GetTree().CreateTimer(.6), SceneTreeTimer.SignalName.Timeout);
        Check(relic.ActivityProgress == 1, "cancelled end-turn relic restores its light within the finite transition");
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
    }
    private async Task NationsAndGlyphs()
    {
        Game.Home.ShowHome(false); Game.Home.ShowColors(); await Opened();
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, false); await Frames();
            foreach (FleetColor nation in Enum.GetValues<FleetColor>())
            {
                var swatch = Named<Button>(Game.Home, "FleetColor" + nation);
                Check(swatch.GetNode<Label>("NationCaption").Text == NationIdentity.Name(nation),
                    "nation picker names " + nation + " in " + locale);
                Check(locale == "en" || NationIdentity.Name(nation) != NationIdentity.SourceName(nation),
                    "nation has a real localized name " + nation + " " + locale);
                swatch.GrabFocus();
                foreach (string state in InkStates)
                    Check(swatch.GetThemeColor(state).V < .65f, "native focused swatch uses dark " + state + " " + nation);
            }
            await Capture("nation-picker-" + locale);
        }
        Language.Set("en", false); Game.Home.Hide();
        var layer = new CanvasLayer { Layer = 90 }; AddChild(layer);
        var background = new ColorRect { Color = PapyrusStyle.Paper, MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(background); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var kinds = new[] { ShipClass.Mothership, ShipClass.Fishing, ShipClass.Garrison, ShipClass.Invader,
            ShipClass.Kolonel, ShipClass.Togus, ShipClass.Balloon };
        for (int i = 0; i < kinds.Length; i++)
        {
            var glyph = new ActionGlyph { Symbol = NavalGlyphArt.Symbol(kinds[i]), InkScale = 1.8f,
                Position = new(30 + i * 145, 130), Size = new(110, 110) };
            background.AddChild(glyph);
            var label = new Label { Text = Game.Battle.Rules.Get(kinds[i]).Name,
                Position = new(25 + i * 145, 250), Size = new(125, 70),
                AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeColorOverride("font_color", PapyrusStyle.Ink); background.AddChild(label);
            Check(NavalGlyphArt.IsNaval(glyph.Symbol), "class uses its shared vector seal " + kinds[i]);
        }
        await Capture("ship-outline-gallery"); layer.QueueFree(); await Frames();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); Game.FastChecks = true; UiScale.Set(1, false); Language.Set("en", false);
            await Advice(); await CounselInput(); await Relic(); await NationsAndGlyphs();
            GD.Print($"PASS: {_checks} v020.8b native advice, counsel wheel, relic and nation/ship-icon checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
