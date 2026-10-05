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

/// <summary>Native input checks for occupied symmetric arcs, the rear-layer amphora and outcome paint.</summary>
public partial class Action0208Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Action0208: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node node in root.GetChildren())
            foreach (Node child in Nodes(node)) yield return child;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Delay(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private void Click(Control control, Vector2? local = null)
    {
        var point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down,
                ButtonMask = down ? MouseButtonMask.Left : (MouseButtonMask)0 }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
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
    private SectorButton[] CheckFilledArc(RadialPapyrus paper, string context)
    {
        paper._Process(1);
        var commands = Nodes(paper).OfType<SectorButton>().Where(b => b.IsVisibleInTree()).ToArray();
        Check(commands.Length == paper.ActionCount && commands.Length > 0,
            context + " paper count includes only displayed real commands");
        Check(!Nodes(paper).Any(n => n.Name == "TownArcGap"), context + " has no synthetic empty command");
        Check(commands.All(b => b._HasPoint(b.IconCenter)), context + " actual glyph centers belong to their native input sectors");
        Check(commands.All(b => !b._HasPoint(SectorButton.Center)), context + " object center remains selectable");
        foreach (var command in commands)
        {
            float mirror = Mathf.Pi - command.CenterAngle;
            Check(commands.Any(other => Math.Abs(Mathf.Wrap(other.CenterAngle - mirror, -Mathf.Pi, Mathf.Pi)) < .001f),
                context + " each real command has a symmetric companion or central position");
        }
        return commands;
    }
    private async Task RealCommandArcs()
    {
        Load();
        await Frames();
        var fan = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(n => n.Name == "ActionPapyrus");
        CheckFilledArc(fan, "flagship");
        Check(!Nodes(Game.Hud).OfType<SectorButton>().Any(n => n.Name == "ActionInformation" || n.Name == "ResourceInformation"),
            "information no longer occupies an object or resource command");
        Game.SelectCell(new(8, 7));
        Game.Refresh();
        await Frames();
        var townCommands = CheckFilledArc(fan, "town");
        var upgrade = townCommands.Single(n => n.Name == "ActionVillageUpgrade");
        Check(Math.Abs(upgrade.CenterAngle - Mathf.Pi / 2) < .001f, "town upgrade retains its lower central location");
        int credits = Game.Battle.Credits(Side.Player);
        Click(upgrade, upgrade.IconCenter);
        await Game.CurrentOrder;
        await Frames();
        Check(Game.Battle.Villages.Single().Level == 2 && Game.Battle.Credits(Side.Player) == credits - 5,
            "real central-upgrade mouse input pays once and advances the town");
        Game.SelectCell(new(5, 5));
        Game.Refresh();
        await Frames();
        CheckFilledArc(fan, "town to flagship");
        var yard = Nodes(fan).OfType<SectorButton>().Single(n => n.Name == "ActionBuild");
        Click(yard, yard.IconCenter);
        await Frames();
        var builds = CheckFilledArc(fan, "flagship shipyard");
        Check(builds.All(n => n.Name.ToString().StartsWith("Build")), "production ring contains only its actual construction choices");
        if (builds.Length >= 3 && builds.Length % 2 == 1)
            Check(!builds.Any(n => Math.Abs(n.CenterAngle - Mathf.Pi / 2) < .001f),
                "odd shipyard count pairs its lower choices without a vacant wedge");
        await Capture("filled-shipyard");
    }
    private async Task AmphoraAndSidebar()
    {
        UiHints.Set(true, false);
        Load();
        await Frames();
        var jug = Nodes(Game.Hud).OfType<ReadyActionJug>().Single();
        var end = Nodes(Game.Hud).OfType<EndTurnPaper>().Single();
        Check(jug.Size.X >= 110 && jug.Size.Y >= 120, "amphora has the requested larger clay silhouette");
        Check(jug.GetParent() == end.GetParent() && jug.GetIndex() < end.GetIndex(),
            "amphora paints behind the end-turn parchment");
        Check(jug.Position.Y + jug.Size.Y > end.Position.Y && jug.Position.Y + jug.PrintedCountCenter.Y < end.Position.Y - 10,
            "lower clay overlaps the paper while its printed count stays exposed");
        Check(jug.Count == Game.Battle.ReadyActions(Side.Player).Count, "printed count retains the exact useful-object query");
        string before = Game.Battle.SaveJson();
        Click(jug, jug.PrintedCountCenter);
        await Frames();
        Check(Game.Hud.ReadyActionsMenuVisible, "exposed amphora input opens its real object list");
        var paper = Nodes(Game.Hud).OfType<RollingModalPaper>().Single(n => n.Name == "ReadyActionsSidePaper");
        var viewport = UiScale.LogicalViewport(this);
        Check(paper.Size.X <= 112 && Math.Abs(paper.Position.X + paper.Size.X - (viewport.X - 18)) < 1,
            "ready list is narrow and aligned against the right edge");
        Check(Math.Abs(paper.Position.Y + paper.Size.Y / 2 - viewport.Y / 2) < 1,
            "ready list is vertically centered on the right side of the screen");
        Check(!paper.CeremonialDecorations, "utility list stays free of ceremonial handles and beads");
        var entries = Nodes(paper).OfType<BrushPaperButton>().ToArray();
        Check(entries.Length == jug.Count && entries.All(b => b.Text.Length == 0 && b.TooltipText.Length > 0),
            "remaining actions are native invisible icon buttons with concrete hover clauses");
        await Capture("amphora-sidebar");
        var brig = Game.Battle.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Garrison);
        Click(entries.Single(n => n.Name == "SideReadyShip" + brig.Id));
        await Game.CurrentOrder;
        await Frames();
        Check(!Game.Hud.ReadyActionsMenuVisible && Game.SelectedShipId == brig.Id,
            "real side-icon mouse input returns to the chosen ship");
        Check(Game.Battle.SaveJson() == before, "list and amphora navigation consume no Core actions");
        var caption = Nodes(end).OfType<Label>().Single(n => n.Name == "EndTurnCaption");
        var folding = end.FoldAsync(false);
        await Delay(.12f);
        Check(end.UnrollProgress is > 0 and < 1 && caption.Scale == Vector2.One && caption.Modulate == Colors.White,
            "end-turn text remains full-sized and opaque while clipping into the right roll");
        await folding;
        end.SetHumanTurn(false, true);
        end.SetHumanTurn(true, true);
    }
    private async Task ResultPaint()
    {
        UiHints.Set(false, false);
        var result = Game.Victory!;
        result.InstantAnimations = false;
        result.ShowOutcome(Game.Battle.Statistics, Game.Battle.Round, true, nationColor: FleetPalette.Color(FleetColor.Red));
        await Delay(.16f);
        var paper = Nodes(result).OfType<RollingModalPaper>().Single(n => n.Name == "VictoryPaper");
        Check(paper.Scale == Vector2.One && paper.RollProgress is > 0 and < 1,
            "outcome reveals through its rolls without squashing the full-scale content");
        Check(paper.CeremonialDecorations && paper.Nation == FleetColor.Red, "outcome inherits the actual ceremonial faction design");
        float partialRadius = paper.RollRadius;
        await Delay(.48f);
        Check(partialRadius > paper.RollRadius, "wound paper has a thicker roll than opened paper");
        var home = Nodes(result).OfType<BrushPaperButton>().Single(n => n.Name == "VictoryHome");
        foreach (string state in new[] { "font_color", "font_focus_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color" })
            Check(home.GetThemeColor(state) == PapyrusStyle.Ink, "home inscription keeps the same dark ink in " + state);
        var inspect = Nodes(result).OfType<BrushPaperButton>().Single(n => n.Name == "InspectMap");
        var stroke = Nodes(inspect).OfType<BrushInscriptionBackdrop>().Single();
        Check(stroke.ShowBehindParent && Math.Abs(stroke.StrokeCenterY - inspect.Size.Y / 2) < .01f,
            "nation brush stroke is centered behind its text rather than under the button");
        var beads = Nodes(paper).OfType<NationBeads>().Single();
        var position = beads.PendantPosition;
        await Delay(.3f);
        Check(beads.IsProcessing() && beads.PendantPosition != position,
            "ceremonial pendant swings with three-dimensional perspective");
        Check(result.ActiveSparkCount is > 0 and <= 240, "larger brighter fireworks preserve the bounded particle population");
        await Capture("bright-victory");
        Click(inspect);
        await Delay(.16f);
        Check(result.IsOpen && paper.Scale == Vector2.One && paper.RollProgress is > 0 and < 1,
            "real result input clips the content inward at unchanged scale");
        await Delay(.4f);
        Check(!result.IsOpen && result.ActiveSparkCount == 0 && !beads.IsProcessing(),
            "closed victory stops every hidden spark and dangling ornament");
        result.InstantAnimations = true;
        result.ShowOutcome(Game.Battle.Statistics, Game.Battle.Round, false, nationColor: FleetPalette.Color(FleetColor.White));
        await Delay(.55f);
        Check(result.ActiveFlameCount == 24 && result.ActiveSparkCount == 0,
            "defeat shows a bounded living fire rather than victory sparks");
        Check(paper.Nation == FleetColor.White, "defeat ceremony switches to the correct pearl-and-rune ornament");
        await Capture("burning-defeat");
        Click(Nodes(result).OfType<BrushPaperButton>().Single(n => n.Name == "InspectMap"));
        await Frames();
        Check(!result.IsOpen && result.ActiveFlameCount == 0 && !beads.IsProcessing(),
            "map inspection stops hidden defeat flames and pendants");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            UiScale.Set(1, persist: false);
            await RealCommandArcs();
            await AmphoraAndSidebar();
            await ResultPaint();
            GD.Print($"PASS: {_checks} v020.8 native occupied arcs, amphora, side-list, full-scale rolls, dark inscriptions and outcome effects checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
