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

/// <summary>Real pointer/keyboard coverage for anchored, readable command parchment.</summary>
public partial class Menu0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Menu0207b: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Opened()
    {
        await ToSignal(GetTree().CreateTimer(.30), SceneTreeTimer.SignalName.Timeout);
        await Frames(2);
    }
    private RadialPapyrus Paper => Nodes(Game.Hud).OfType<RadialPapyrus>().Single(p => p.Name == "ActionPapyrus");
    private SectorButton[] Commands => Nodes(Paper).OfType<SectorButton>().Where(c => c.IsVisibleInTree()).ToArray();
    private void KeyPress(Key key, bool control = false)
    {
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key,
                Pressed = down, CtrlPressed = control }, true);
    }
    private void Click(SectorButton command)
    {
        var point = command.GetGlobalTransformWithCanvas() * command.IconCenter;
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0 }, true);
    }
    private void Wheel(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.WheelDown, Pressed = down }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
        await Frames(1);
    }
    private BattleState Fixture(int credits = 100)
    {
        var town = new GridPosition(9, 9);
        var battle = new BattleState(new GameBoard(24, 24, p => p == town ? TerrainType.Land : TerrainType.Water),
            Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(6, 6)),
                (Side.Player, ShipClass.Garrison, new GridPosition(9, 5)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 9)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(21, 21)) },
            fishSpots: Array.Empty<GridPosition>(), villageSpots: new[] { town });
        var save = battle.CaptureSnapshot();
        save.Credits[0] = credits;
        save.Ships.Single(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership).Level = 5;
        save.Villages[0] = save.Villages[0] with { Owner = Side.Player, Level = 3 };
        return BattleState.LoadJson(BattleState.SerializeSnapshot(save));
    }
    private async Task Load(int credits = 100)
    {
        Game.LoadScenario(Fixture(credits));
        Game.Home.Hide();
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(6, 6));
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.ForceUpdateScroll();
        Game.SelectCell(new(6, 6)); Game.Refresh();
        await Opened();
    }
    private void CheckAnchor(string context)
    {
        var ship = Game.Battle.Find(Game.SelectedShipId!.Value)!;
        var expected = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(ship.Position));
        var actual = Paper.GetGlobalTransformWithCanvas() * Paper.RingCenter;
        Check(actual.DistanceTo(expected) < .15f, context + " parchment follows exact projected hull anchor");
        Check(Paper.Scale == Vector2.One, context + " icons and paper retain their physical size");
        Check(Math.Abs(Paper.BandWidth - 58) < .01f, context + " paper thickness survives zoom");
    }
    private void CheckKeys(string context)
    {
        var visible = Commands.OrderBy(c => c.IconCenter.X).ThenBy(c => c.IconCenter.Y).ToArray();
        for (int i = 0; i < visible.Length; i++)
            Check(visible[i].ShortcutNumber == (i < 9 ? i + 1 : 0), context + " stable key follows visible left-to-right slot");
        Check(Nodes(Paper).OfType<SectorButton>().Where(c => !c.IsVisibleInTree()).All(c => c.ShortcutNumber == 0),
            context + " closed commands retain no current menu key");
        Check(Commands.All(c => c._HasPoint(c.IconCenter)), context + " glyph centers remain native hit targets");
        Check(Commands.All(c => !c._HasPoint(Paper.RingCenter)), context + " hull itself remains selectable");
    }
    private async Task RevealInput()
    {
        await Load();
        string before = Game.Battle.SaveJson();
        Paper.Configure(Commands, unfold: true);
        Paper._Process(.18);
        Check(Paper.Reveal > .95f && Paper.Reveal < 1 && Paper.IsProcessing(),
            "fixture exercises the final unfinished reveal beyond the old 95% input threshold");
        Check(Commands.All(c => !c._HasPoint(c.IconCenter)), "unfinished reveal has no pointer command hit targets");
        KeyPress(Key.Key2);
        Check(Game.Battle.SaveJson() == before && !Game.Battle.Mothership(Side.Player)!.HasRadar,
            "unfinished reveal cannot execute a numeric purchase");
        await Opened();
        KeyPress(Key.Key2); await Game.CurrentOrder; await Opened();
        Check(Game.Battle.Mothership(Side.Player)!.HasRadar,
            "the same numeric command executes once its reveal actually finishes");
    }

    private async Task CommandInput()
    {
        await Load();
        Check(!Nodes(Game.Hud).OfType<SectorButton>().Any(c => c.Name == "ActionInformation" || c.Name == "ResourceInformation"),
            "no obsolete object/resource Info sectors remain");
        CheckKeys("flagship");
        Check(Commands.Single(c => c.Name == "ActionBuild").ShortcutNumber == 1, "flagship shipyard is key one");
        Check(Commands.Single(c => c.Name == "ActionRadar").ShortcutNumber == 2, "radar precedes mortar");
        Check(Commands.Single(c => c.Name == "ActionMortar").ShortcutNumber == 3, "mortar is key three");
        string before = Game.Battle.SaveJson();
        Check(Commands.Single(c => c.Name == "ActionMortar").Disabled, "existing radar prerequisite blocks mortar");
        KeyPress(Key.Key3); await Frames();
        Check(Game.Battle.SaveJson() == before, "reserved disabled mortar key does nothing");
        KeyPress(Key.Key1, control: true); await Frames();
        Check(Commands.Any(c => c.Name == "ActionBuild"), "modified key does not open shipyard");
        KeyPress(Key.Key1); await Opened();
        Check(Commands.All(c => c.Name.ToString().StartsWith("Build")), "real keyboard one opens only current shipyard commands");
        CheckKeys("shipyard");
        Check(Commands.Single(c => c.Name == "BuildLighthouse").ShortcutNumber == 1, "lighthouse is first construction key");
        Check(Commands.Single(c => c.Name == "BuildCannonTower").ShortcutNumber == 2, "tower is second construction key");
        Check(Commands.Single(c => c.Name == "BuildTogus").ShortcutNumber == 3, "Granado is third construction key");
        KeyPress(Key.Key2); await Frames();
        Check(Game.Mode == OrderMode.Build && Game.Hud.MessageText.Contains(Game.Battle.Rules.Get(ShipClass.CannonTower).Name),
            "submenu key two selects the tower, never its old parent radar");
        Check(Game.Battle.SaveJson() == before, "opening/navigating construction never spends currency or actions");
        await Load();
        Click(Commands.Single(c => c.Name == "ActionBuild")); await Opened();
        Check(Commands.All(c => c.Name.ToString().StartsWith("Build")), "native pointer also opens shipyard");
        Click(Commands.Single(c => c.Name == "BuildTogus")); await Frames();
        Check(Game.Mode == OrderMode.Build && Game.Hud.MessageText.Contains(Game.Battle.Rules.Get(ShipClass.Togus).Name),
            "outer-ring Granado glyph is clickable");
        await Load();
        before = Game.Battle.SaveJson();
        Game.Hud.SetMenuVisible(true); await Opened(); KeyPress(Key.Key2); await Frames();
        Check(Game.Battle.SaveJson() == before && !Game.Battle.Mothership(Side.Player)!.HasRadar,
            "game menu isolates numeric commands");
        Game.Hud.SetMenuVisible(false); await Opened();
        KeyPress(Key.Key2); await Game.CurrentOrder; await Opened();
        Check(Game.Battle.Mothership(Side.Player)!.HasRadar
            && Commands.Single(c => c.Name == "ActionRadar").Disabled
            && Commands.Single(c => c.Name == "ActionRadar").ShortcutNumber == 2
            && Commands.Single(c => c.Name == "ActionMortar").ShortcutNumber == 3,
            "installing radar retains reserved slot two and unlocks mortar in slot three");
        await Load(0); KeyPress(Key.Key1); await Opened();
        Check(Commands.All(c => c.Disabled && c.ShortcutNumber > 0), "scarce treasury retains grey slot numbers without allowing purchase");
        before = Game.Battle.SaveJson(); KeyPress(Key.Key1); await Frames();
        Check(Game.Mode == OrderMode.None && Game.Battle.SaveJson() == before, "disabled key cannot select/purchase a build");
    }
    private async Task ZoomAndPan()
    {
        await Load(); KeyPress(Key.Key1); await Opened();
        float nearSpan = Paper.ArcLength;
        foreach (float scale in new[] { .8f, 1.25f })
        {
            UiScale.Set(scale, persist: false);
            foreach (float zoom in new[] { 2.3f, 1.3f, .65f, .25f })
            {
                Game.MapCamera.Zoom = Vector2.One * zoom; Game.MapCamera.ForceUpdateScroll(); await Frames();
                CheckAnchor($"zoom {zoom} UI {scale}"); CheckKeys($"zoom {zoom} UI {scale}");
            }
            Check(Paper.ArcLength > nearSpan + .5f, "distant map wraps the same commands farther around the object");
            var origin = Game.MapCamera.Position;
            Game.MapCamera.Pan(new(540, 320)); await Frames();
            CheckAnchor("pan away from selected hull");
            Game.MapCamera.Position = origin; Game.MapCamera.ForceUpdateScroll(); await Frames();
            CheckAnchor("return to selected hull");
        }
        UiScale.Set(1, persist: false);
        Game.MapCamera.Zoom = Vector2.One * 1.4f; Game.MapCamera.ForceUpdateScroll(); await Frames();
        await Capture("shipyard-near");
        Game.MapCamera.Zoom = Vector2.One * .3f; Game.MapCamera.ForceUpdateScroll(); await Frames();
        await Capture("shipyard-far");
    }
    private async Task Counsel()
    {
        await Load();
        var card = Nodes(Game.Hud).OfType<PanelContainer>().Single(p => p.Name == "InformationScroll");
        var content = Nodes(card).OfType<ScrollContainer>().Single();
        Check(Game.Hud.InformationVisible && Game.Hud.InformationText.Contains("Health:"), "automatic counsel includes present health");
        Check(Nodes(card).OfType<ActionGlyph>().Count(g => g.Name == "InformationStatGlyph")
            == Nodes(card).OfType<Label>().Count(l => l.Name == "InformationFieldLabel"), "every current fact has a clear glyph");
        Check(Nodes(card).OfType<GridContainer>().All(g => g.Columns == 2), "facts retain distinct two-column groups");
        foreach (float scale in new[] { .8f, 1.25f })
            foreach (string language in new[] { "en", "uk", "nl" })
            {
                UiScale.Set(scale, false); Language.Set(language, false); Game.Refresh(); await Frames();
                Check(card.Size.X <= 352.1f && card.Size.Y <= 286.1f && card.Position.X == 14,
                    "readable bounded lower-left card " + language);
                var zoom = Game.MapCamera.Zoom;
                content.ScrollVertical = 0; Wheel(content); await Frames();
                Check(content.ScrollVertical > 0 && Game.MapCamera.Zoom == zoom, "native counsel wheel cannot zoom sea " + language);
                int at = content.ScrollVertical; Game.Refresh(); await Frames();
                Check(content.ScrollVertical == at, "same-object counsel retains reading position " + language);
            }
        UiScale.Set(1, false); Language.Set("en", false);
        Game.Refresh(); content.ScrollVertical = 0; await Frames(); await Capture("counsel");
        var rules = Game.Battle.Rules;
        var radarBattle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), rules, new[] {
            (Side.Player, ShipClass.Mothership, new GridPosition(4, 4)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(7, 4)) });
        var save = radarBattle.CaptureSnapshot();
        save.Ships[0].HasRadar = true;
        radarBattle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        var hidden = radarBattle.Mothership(Side.Enemy)!;
        Check(radarBattle.Vision.IsRadarContact(Side.Player, hidden.Position)
            && !radarBattle.Vision.IsVisible(Side.Player, hidden.Position), "fixture gives radar-only contact");
        Game.LoadScenario(radarBattle); Game.Hud.ShowInformation(radarBattle, hidden.Position); await Frames();
        Check(!Game.Hud.InformationText.Contains("Health") && !Game.Hud.InformationText.Contains("Mothership"),
            "new health facts and nation seal reveal no radar identity");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); UiScale.Set(1, false); Language.Set("en", false);
            await RevealInput(); await CommandInput(); await ZoomAndPan(); await Counsel();
            GD.Print($"PASS: {_checks} v020.7b native command keyboard/pointer, anchored zoom wrapping, current counsel glyphs and fog checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
