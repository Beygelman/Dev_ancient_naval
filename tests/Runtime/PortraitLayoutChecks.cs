using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Actual narrow native viewport, translated content and rotation input regressions.
/// Synthetic desktop input/layout evidence; not an iPhone device test.</summary>
public partial class PortraitLayoutChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private static readonly GridPosition Mother = new(5, 5);
    private void Check(bool valid, string message)
    { if (!valid) throw new InvalidOperationException("PortraitLayout: " + message); _checks++; }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node descendant in Nodes(child)) yield return descendant;
    }
    private static T Named<T>(Node root, string name) where T : Node =>
        Nodes(root).OfType<T>().Single(node => node.Name == name);
    private async Task Frames(int count = 6)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Opened()
    { await ToSignal(GetTree().CreateTimer(.6), SceneTreeTimer.SignalName.Timeout); await Frames(); }
    private static Vector2 Center(Control control) => control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
    private static Rect2 Bounds(Control control)
    {
        var transform = control.GetGlobalTransformWithCanvas();
        return new Rect2(transform * Vector2.Zero, transform * control.Size - transform * Vector2.Zero).Abs();
    }
    private void Inside(Control control, string context)
    {
        var safe = UiScale.ViewportSafeArea(this);
        var bounds = Bounds(control);
        bool fits = control.IsVisibleInTree() && bounds.Position.X >= safe.Position.X - 1
            && bounds.Position.Y >= safe.Position.Y - 1 && bounds.End.X <= safe.End.X + 1
            && bounds.End.Y <= safe.End.Y + 1;
        if (!fits)
        {
            foreach (Control child in Nodes(control).OfType<Control>())
                GD.Print($"LAYOUT {child.GetPath()} size={child.Size} min={child.GetCombinedMinimumSize()}"
                    + (child is ScrollContainer scroll ? $" vertical={scroll.VerticalScrollMode}" : ""));
            // Save the last completed native frame before the assertion exits.
            // This is especially useful for container minima that resize between frames.
            var path = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
            if (path is not null && DisplayServer.GetName() != "headless")
            {
                int extension = path.LastIndexOf('.');
                var suffix = "-failure-" + string.Concat(context.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
                path = extension < 0 ? path + suffix + ".png" : path[..extension] + suffix + path[extension..];
                using var picture = GetViewport().GetTexture().GetImage();
                GD.Print("Failure capture: " + path + " (" + picture.SavePng(path) + ")");
            }
        }
        Check(fits, context + " stays inside safe viewport; bounds=" + bounds + ", safe=" + safe);
    }
    private void Click(Control control, Vector2? local = null)
    {
        var point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 }, true);
    }
    private void Touch(Vector2 point, bool pressed) => GetViewport().PushInput(
        new InputEventScreenTouch { Index = 0, Position = point, Pressed = pressed }, true);
    private void Drag(Vector2 point) => GetViewport().PushInput(
        new InputEventScreenDrag { Index = 0, Position = point }, true);
    private void Wheel(Control control)
    {
        var point = Center(control);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.WheelDown, Pressed = pressed }, true);
    }
    private async Task Capture(string suffix)
    {
        var path = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var picture = GetViewport().GetTexture().GetImage();
        int extension = path.LastIndexOf('.');
        path = extension < 0 ? path + "-" + suffix + ".png" : path[..extension] + "-" + suffix + path[extension..];
        Check(picture.SavePng(path) == Error.Ok, "native capture " + suffix);
    }
    private async Task Resize(Vector2I size, Vector2I baseline)
    {
        var window = GetWindow();
        window.ContentScaleSize = baseline;
        window.Size = size;
        await Frames(10);
    }
    private void SafeAreaConversion()
    {
        var landscape = new Vector2I(1280, 720);
        Check(MobileViewport.ChooseScaleSize(new(1170, 2532), landscape) == new Vector2I(540, 960),
            "physical iPhone portrait chooses a readable narrow content baseline");
        Check(MobileViewport.ChooseScaleSize(new(2532, 1170), landscape) == landscape,
            "physical iPhone landscape restores the original landscape content baseline");
        Check(MobileViewport.ChooseScaleSize(new(0, 0), landscape) == landscape,
            "an unavailable native size retains a usable landscape baseline");
        var visible = new Rect2(0, 0, 540, 1168);
        var stretch = new Transform2D(0, new Vector2(2, 2), 0, new Vector2(12, 18));
        var safe = UiScale.ConvertSafeArea(new Rect2(12, 136, 1080, 2150), stretch, visible);
        Check(safe.Position.IsEqualApprox(new Vector2(0, 59)) && safe.Size.IsEqualApprox(new Vector2(540, 1075)),
            "portrait notch and home-indicator pixels convert through the translated screen transform");
        Check(UiScale.ConvertSafeArea(new Rect2(12, 18, 1080, 2336), stretch, visible) == visible,
            "an inset-free portrait returns the exact viewport");
    }
    private async Task HomeAndSetup(string context)
    {
        Game.Home.ShowHome(false); await Frames();
        var homeRoot = Named<Control>(Game.Home, "HomeRoot");
        var visible = GetViewport().GetVisibleRect().Size;
        Check(visible.X < visible.Y && visible.X <= 540.1f,
            "native portrait is actually narrow, without a stretched landscape canvas; viewport=" + visible);
        Check((homeRoot.Size * UiScale.Value).IsEqualApprox(visible), "scaled home hit regions cover actual portrait viewport");
        Inside(Named<Control>(Game.Home, "AncientNavalTitle"), context + " title");
        Inside(Named<Button>(Game.Home, "HomeNewGame"), context + " new-voyage button");
        Inside(Named<Button>(Game.Home, "HomeSettings"), context + " settings button");
        Inside(Named<Label>(Game.Home, "HomeVersionSignature"), context + " signature");
        await Capture(context + "-home");
        Click(Named<Button>(Game.Home, "HomeSettings")); await Opened();
        var settings = Named<RollingModalPaper>(Game.Home, "HomeSettingsPaper");
        Inside(settings, context + " home settings");
        foreach (var button in Nodes(settings).OfType<Button>().Where(button => button.IsVisibleInTree()))
            Check(Bounds(button).Position.X >= Bounds(settings).Position.X - 1 && Bounds(button).End.X <= Bounds(settings).End.X + 1,
                context + " settings button stays horizontally inside its paper: " + button.Name);
        var back = Named<Button>(Game.Home, "CloseHomeSettings");
        var settingsScroll = Named<ScrollContainer>(Game.Home, "HomeSettingsScroll");
        settingsScroll.EnsureControlVisible(back); await Frames(); Click(back); await Opened();
        Check(!settings.IsVisibleInTree() && Named<Button>(Game.Home, "HomeNewGame").IsVisibleInTree(),
            context + " native settings Back returns to the menu");
        Click(Named<Button>(Game.Home, "HomeNewGame")); await Opened();
        var paper = Named<RollingVoyagePaper>(Game.Home, "VoyageSetupPaper");
        Inside(paper, context + " setup paper");
        var scroll = Named<ScrollContainer>(Game.Home, "VoyageSetupScroll");
        var scrollBounds = Bounds(scroll);
        foreach (var button in Nodes(paper).OfType<Button>().Where(button => button.IsVisibleInTree()))
            Check(Bounds(button).Position.X >= scrollBounds.Position.X - 1 && Bounds(button).End.X <= scrollBounds.End.X + 1,
                context + " all setup choices remain reachable horizontally: " + button.Name);
        var purple = Named<Button>(Game.Home, "FleetColorPurple");
        scroll.EnsureControlVisible(purple); await Frames(); Click(purple); await Frames();
        Check(Game.Home.SelectedColor == FleetColor.Purple, context + " native narrow nation choice responds");
        await Capture(context + "-setup");
        var cancel = Named<Button>(Game.Home, "CancelColor");
        scroll.EnsureControlVisible(cancel); await Frames(); Click(cancel); await Opened();
        Check(!paper.IsVisibleInTree(), context + " native setup Back remains accessible after scrolling");
        Game.Home.Hide();
    }
    private void Load()
    {
        var fixture = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, Mother),
                (Side.Player, ShipClass.Garrison, new GridPosition(7, 5)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)) }, piratesEnabled: false);
        Game.LoadScenario(fixture); Game.Home.Hide(); Game.Hud.Show(); Game.BoardView.Show(); Game.Fleet.Show();
        Game.FastChecks = true; Game.Hud.InstantPaperAnimations = true;
        Game.MapCamera.CancelFlight();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(Mother) + new Vector2(45, 25);
        Game.MapCamera.Zoom = Vector2.One * 1.15f; Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(Mother); Game.Refresh();
    }
    private Vector2 SeaPoint()
    {
        var visible = GetViewport().GetVisibleRect().Size;
        for (int row = 2; row < 8; row++) for (int column = 1; column < 9; column++)
        {
            var point = new Vector2(visible.X * column / 10, visible.Y * row / 10);
            if (!Game.MapInput.TouchOverInterface(point)) return point;
        }
        throw new InvalidOperationException("PortraitLayout: no exposed sea remains for real gesture input");
    }
    private void CheckActionGlyphs(RadialPapyrus paper, string context)
    {
        var commands = Nodes(paper).OfType<SectorButton>().Where(button => button.IsVisibleInTree() && !button.Disabled).ToArray();
        Check(commands.Length > 0, context + " exposes actual available commands");
        var counselControl = Named<PanelContainer>(Game.Hud, "InformationScroll");
        var counsel = Bounds(counselControl);
        var compass = Bounds(Game.NavigationHud.CompassButton);
        var relic = Bounds(Named<ReadyActionJug>(Game.Hud, "EndTurn"));
        foreach (var command in commands)
        {
            var point = command.GetGlobalTransformWithCanvas() * command.IconCenter;
            var glyph = new Rect2(point - Vector2.One * 16 * UiScale.Value, Vector2.One * 32 * UiScale.Value);
            bool reachable = GetViewport().GetVisibleRect().Encloses(glyph)
                && (!counselControl.IsVisibleInTree() || !glyph.Intersects(counsel))
                && !glyph.Intersects(compass) && !glyph.Intersects(relic) && command._HasPoint(command.IconCenter);
            if (!reachable)
            {
                GD.Print($"ACTION {command.Name} icon={command.IconCenter} unfolded={command.UnfoldedIconCenter}"
                    + $" reveal={paper.Reveal} transform={command.GetGlobalTransformWithCanvas()}");
                foreach (Control child in Nodes(counselControl).OfType<Control>())
                    GD.Print($"LAYOUT {child.GetPath()} size={child.Size} min={child.GetCombinedMinimumSize()}"
                        + (child is ScrollContainer scroll ? $" vertical={scroll.VerticalScrollMode}" : ""));
            }
            Check(reachable,
                context + " available command glyph remains visible and reachable: " + command.Name
                + "; glyph=" + glyph + ", counsel=" + counsel);
        }
    }
    private async Task ActionReachability(RadialPapyrus paper, string context)
    {
        CheckActionGlyphs(paper, context + " main actions");
        string unchanged = Game.Battle.SaveJson();
        var shipyard = Named<SectorButton>(paper, "ActionBuild");
        Check(shipyard.IsVisibleInTree() && !shipyard.Disabled, context + " ordinary selected Mothership exposes its shipyard");
        Click(shipyard, shipyard.IconCenter); await Opened();
        Check(Named<SectorButton>(paper, "BuildLighthouse").IsVisibleInTree() && !shipyard.IsVisibleInTree(),
            context + " real native shipyard glyph opens the construction submenu");
        CheckActionGlyphs(paper, context + " shipyard actions");
        Check(Game.Battle.SaveJson() == unchanged, context + " opening and inspecting the shipyard spends no actions, currency or RNG");
        Game.Hud.CloseMenus(); Game.Refresh(); await Opened();
        Check(shipyard.IsVisibleInTree() && Game.Hud.InformationVisible, context + " returning to the main action page restores counsel");
        CheckActionGlyphs(paper, context + " restored actions");
    }
    private async Task BattleAndRotation(string context, Vector2I portraitBaseline)
    {
        Load(); await Frames();
        var paper = Named<RadialPapyrus>(Game.Hud, "ActionPapyrus");
        for (int frame = 0; frame < 180 && paper.Reveal < 1; frame++) await Frames(1);
        Check(paper.Reveal == 1, context + " command paper finishes its real reveal before hit testing");
        var counsel = Named<PanelContainer>(Game.Hud, "InformationScroll");
        var compass = Game.NavigationHud.CompassButton;
        var relic = Named<ReadyActionJug>(Game.Hud, "EndTurn");
        Inside(counsel, context + " selected-object counsel");
        Inside(compass, context + " Mothership compass");
        Inside(Named<Button>(Game.Hud, "Menu"), context + " game menu trigger");
        var metrics = Named<PanelContainer>(Game.Hud, "VoyageMetrics");
        Inside(metrics, context + " turn, currency and fleet ledger");
        Check(!Bounds(metrics).Intersects(Bounds(Named<Button>(Game.Hud, "Menu"))),
            context + " menu does not cover the turn, currency or fleet ledger");
        Check(!Bounds(counsel).Intersects(Bounds(compass)) && !Bounds(counsel).Intersects(Bounds(relic))
            && !Bounds(compass).Intersects(Bounds(relic)), context + " counsel, compass and end-turn targets remain separate");
        var counter = relic.GetGlobalTransformWithCanvas() * relic.PrintedCountCenter;
        Check(GetViewport().GetVisibleRect().HasPoint(counter), context + " end-turn numeral remains visible above its protruding pedestal");
        var anchor = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(Mother));
        var dock = paper.GetGlobalTransformWithCanvas() * paper.RingCenter;
        var safe = UiScale.ViewportSafeArea(this);
        Check(Math.Abs(dock.X - safe.GetCenter().X) < .25f && dock.Y > safe.GetCenter().Y
            && paper.Scale.X <= .9f, context + " requested portrait commands form a compact centered bottom dock");
        Check(Math.Abs(Bounds(counsel).GetCenter().X - safe.GetCenter().X) < 1,
            context + " portrait counsel is centered above the bottom menu");
        Check(Bounds(compass).End.Y < safe.GetCenter().Y && Bounds(relic).End.Y < safe.GetCenter().Y
            && !Bounds(relic).Intersects(Bounds(Named<Button>(Game.Hud, "Menu")))
            && !Bounds(relic).Intersects(Bounds(metrics)), context + " find-Mothership and end-turn controls occupy separate top targets");
        await Capture(context + "-battle-actions");
        Check(!Bounds(counsel).HasPoint(anchor),
            context + " selected hull ground anchor remains visible outside counsel; anchor=" + anchor + ", counsel=" + Bounds(counsel));
        await ActionReachability(paper, context);
        string unchanged = Game.Battle.SaveJson();
        var battle = Game.Battle; int? selected = Game.SelectedShipId;
        var camera = Game.MapCamera.Position; var zoom = Game.MapCamera.Zoom;
        Wheel(counsel); await Frames();
        Check(Game.MapCamera.Zoom == zoom, context + " counsel wheel never zooms through the narrow UI");
        await Capture(context + "-battle");
        Touch(SeaPoint(), true);
        await Resize(new(844, 390), new(1280, 720));
        var landscapeAnchor = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(Mother));
        Check((paper.GetGlobalTransformWithCanvas() * paper.RingCenter).DistanceTo(landscapeAnchor) < .25f
            && paper.Scale == Vector2.One, context + " landscape rotation restores the original world-anchored command paper");
        Drag(new(450, 230)); Touch(new(450, 230), false); await Frames();
        Check(ReferenceEquals(battle, Game.Battle) && Game.Battle.SaveJson() == unchanged && Game.SelectedShipId == selected,
            context + " portrait-to-landscape rotation and orphan touch preserve battle, RNG and selection");
        Check(Game.MapCamera.Position.DistanceTo(camera) < .01f && Game.MapCamera.Zoom == zoom,
            context + " portrait-to-landscape rotation preserves camera pan and zoom");
        await Resize(new(390, 844), portraitBaseline);
        Check(Game.MapCamera.Position.DistanceTo(camera) < .01f && Game.MapCamera.Zoom == zoom && Game.Battle.SaveJson() == unchanged,
            context + " reverse rotation preserves the exact voyage and camera");
        var sea = SeaPoint();
        Touch(sea, true); Drag(sea + new Vector2(35, 0)); Touch(sea + new Vector2(35, 0), false); await Frames();
        Check(Game.MapCamera.Position.DistanceTo(camera) > 1 && Game.SelectedShipId == selected && Game.Battle.SaveJson() == unchanged,
            context + " a fresh one-finger pan still works after both rotations without activating a command");
        Click(Named<Button>(Game.Hud, "Menu")); await Frames();
        Check(Game.Hud.MenuVisible, context + " native game-menu target remains usable");
        Inside(Named<RollingModalPaper>(Game.Hud, "GameMenuPaper"), context + " game menu paper");
        Click(Named<Button>(Game.Hud, "GameSettings")); await Frames();
        Inside(Named<RollingModalPaper>(Game.Hud, "GameMenuPaper"), context + " game settings paper");
        var returnSettings = Named<Button>(Game.Hud, "CloseGameSettings");
        Named<ScrollContainer>(Game.Hud, "GameMenuScroll").EnsureControlVisible(returnSettings); await Frames(); Click(returnSettings); await Frames();
        var close = Named<Button>(Game.Hud, "CloseMenu");
        Named<ScrollContainer>(Game.Hud, "GameMenuScroll").EnsureControlVisible(close); await Frames(); Click(close); await Frames();
        Check(!Game.Hud.MenuVisible && Game.Battle.SaveJson() == unchanged, context + " native settings and menu return without changing gameplay");
    }
    public override async void _Ready()
    {
        try
        {
            Check(OS.GetCmdlineUserArgs().Any(arg => arg.StartsWith("--save-file=")), "requires an explicit disposable save path");
            await Frames(); UiHints.Set(false, false); Game.FastChecks = true; SafeAreaConversion();
            foreach (float scale in new[] { .8f, 1.25f }) foreach (string language in new[] { "en", "uk", "nl" })
            {
                await Resize(new(390, 844), new(540, 960));
                UiScale.Set(scale, false); Language.Set(language, false); await Frames();
                var context = "portrait-" + language + "-" + MathF.Round(scale * 100);
                await HomeAndSetup(context); await BattleAndRotation(context, new(540, 960));
            }
            await Resize(new(390, 844), new(390, 844));
            UiScale.Set(1.25f, false); Language.Set("uk", false); await Frames();
            await HomeAndSetup("portrait-narrow-uk-125");
            await BattleAndRotation("portrait-narrow-uk-125", new(390, 844));
            GD.Print($"PASS: {_checks} native portrait EN/UK/NL, scale, rotation, safe-area and input checks (not device/iOS-tested).");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
