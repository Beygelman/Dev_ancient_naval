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

/// <summary>Real native input, ownership-only cycling, hint confirmation and dormant ritual checks.</summary>
public partial class Guidance0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("Guidance0207b: " + message); _checks++; }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (Node child in node.GetChildren()) foreach (Node item in Nodes(child)) yield return item;
    }
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Delay(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
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
    }
    private BattleState Fixture()
    {
        var town = new GridPosition(9, 7);
        var battle = new BattleState(new GameBoard(20, 20, p => p == town ? TerrainType.Land : TerrainType.Water),
            Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)),
                (Side.Player, ShipClass.Garrison, new GridPosition(7, 5)),
                (Side.Player, ShipClass.Fishing, new GridPosition(5, 7)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(8, 5)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)) },
            new[] { new GridPosition(4, 7) }, villageSpots: new[] { town });
        var save = battle.CaptureSnapshot();
        save.Credits[0] = 100;
        save.Ships[0].Level = 4;
        save.Villages[0] = save.Villages[0] with { Owner = Side.Player, Level = 3 };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        // The nearby enemy exists solely to give the Brig a legal Attack glyph.
        // A current voyage awards first optical contact; finish that disposable
        // fixture setup before testing native counter input behind no reward modal.
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        return battle;
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
    private async Task CycleWithoutHints()
    {
        UiHints.Set(false, false);
        Load();
        await Frames();
        var counter = Nodes(Game.Hud).OfType<ReadyActionJug>().Single();
        var ready = Game.Battle.ReadyActions(Side.Player);
        Check(ready.Count >= 3 && counter.Count == ready.Count && counter.IsVisibleInTree(),
            "hints-off retains the command-derived ready count and visible nation monument");
        Check(!Nodes(Game.Hud).Any(n => n.Name == "ReadyActionsSidePaper"), "no separate ready-object side paper exists");
        Check(counter._HasPoint(counter.PrintedCountCenter), "slanted printed count is inside its native click footprint");
        string unchanged = Game.Battle.SaveJson();
        int rebuilds = Game.Hud.ReadyQueryRebuilds;
        for (int i = 0; i <= ready.Count; i++)
        {
            var item = ready[i % ready.Count];
            Game.FastChecks = i != 0;
            Click(counter, counter.PrintedCountCenter);
            await Game.CurrentOrder;
            await Frames();
            Check(item.ShipClass is null ? Game.SelectedVillageId == item.Id : Game.SelectedShipId == item.Id,
                $"counter native click {i} selects owned ready {(item.ShipClass is null ? "town" : "ship")} {item.Id} and wraps; selected ship={Game.SelectedShipId}, town={Game.SelectedVillageId}, disabled={counter.Disabled}");
            Check(!Game.Hud.ReadyActionsMenuVisible && !Game.Hud.TurnConfirmationVisible,
                "cycling never opens a list or a confirmation");
            if (i == 0)
                Check(Game.MapCamera.Position.DistanceTo(Game.BoardView.Projection.GridToWorld(item.Position)) < 1,
                    "first real camera flight reaches the selected ready hull");
        }
        Check(Game.Battle.SaveJson() == unchanged, "camera cycling consumes neither actions nor simulation RNG");
        Check(Game.Hud.ReadyQueryRebuilds == rebuilds, "camera and selection cycling retain the ready-navigation cache");
        await Capture("nation-counter");
    }
    private async Task ConfirmationActions()
    {
        UiHints.Set(true, false);
        Load();
        await Frames();
        string unchanged = Game.Battle.SaveJson();
        Click(Nodes(Game.Hud).OfType<Button>().Single(b => b.Name == "EndTurn"));
        await Game.CurrentOrder;
        await Frames();
        Check(Game.Hud.TurnConfirmationVisible, "hints-on end-turn opens the only ready-object list");
        var grid = Nodes(Game.Hud).OfType<GridContainer>().Single(n => n.Name == "ReadyActionGrid");
        var ready = Game.Battle.ReadyActions(Side.Player);
        var cards = grid.GetChildren().OfType<Button>().ToArray();
        Check(cards.Length == ready.Count && grid.Columns is >= 1 and <= 3,
            "readable adaptive confirmation grid counts each owned object once");
        for (int i = 0; i < ready.Count; i++)
        {
            var glyphs = Nodes(cards[i]).OfType<ReadyActionGlyph>().ToArray();
            Check(glyphs.Length == ReadyActionHints.Count(ready[i].Actions),
                "each object shows exactly its current ready action glyphs");
            Check(glyphs.Any(g => g.Symbol == ActionSymbol.Move) == ready[i].Actions.HasFlag(ReadyActionKind.Move),
                "winding chart glyph appears only on legal remaining movement");
            Check(glyphs.Any(g => g.Symbol == ActionSymbol.Attack) == ready[i].Actions.HasFlag(ReadyActionKind.Attack),
                "attack glyph follows Core readiness rather than hidden enemy forecasts");
        }
        await Capture("confirmation-actions");
        foreach (string language in new[] { "en", "uk", "nl" })
        foreach (float scale in new[] { .8f, 1.25f })
        {
            Language.Set(language, false);
            UiScale.Set(scale, false);
            await Frames();
            var paper = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "EndTurnConfirmationPaper");
            float left = grid.GetGlobalTransformWithCanvas().Origin.X;
            float paperLeft = paper.GetGlobalTransformWithCanvas().Origin.X;
            Check(left >= paperLeft && left + grid.Size.X * scale <= paperLeft + paper.Size.X * scale + 1,
                "localized action glyph cards remain inside the scaled scroll");
        }
        Language.Set("en", false);
        UiScale.Set(1, false);
        await Frames();
        Click(cards[1]);
        await Game.CurrentOrder;
        await Frames();
        Check(!Game.Hud.TurnConfirmationVisible && Game.SelectedShipId == ready[1].Id,
            "confirmation object button closes its modal and returns to that object");
        Check(Game.Battle.SaveJson() == unchanged, "confirmation browsing changes no Core state");
    }
    private async Task RitualLifetime()
    {
        var counter = Nodes(Game.Hud).OfType<ReadyActionJug>().Single();
        counter.SetHumanTurn(true, true);
        await Frames();
        Check(counter.HumanTurnActive && counter.ActivityProgress == 1 && counter.IsProcessing(),
            "human turn displays the living faction ritual");
        counter.SetHumanTurn(false, false);
        await Delay(.16);
        Check(counter.ActivityProgress is > 0 and < 1, "end turn visibly fades the nation symbol into dormancy");
        await Delay(.5);
        Check(counter.ActivityProgress == 0 && !counter.IsProcessing(), "dormant opponent turn stops the cosmetic processor");
        counter.SetHumanTurn(true, false);
        await Delay(.16);
        Check(counter.ActivityProgress is > 0 and < 1, "next human turn restores the ritual with a bounded transition");
        counter.Hide();
        float phase = counter.EffectPhase;
        await Delay(.12);
        Check(!counter.IsProcessing() && counter.EffectPhase == phase, "hidden symbol freezes every decorative animation");
        counter.Show();
        await Delay(.5);
        Check(counter.IsProcessing() && counter.ActivityProgress == 1, "visible human-turn symbol safely resumes its ritual");
    }
    private async Task NationGallery()
    {
        var layer = new CanvasLayer { Layer = 90 };
        AddChild(layer);
        var overlay = new Control { Name = "NationCounterGallery", MouseFilter = Control.MouseFilterEnum.Stop };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color("18343f"), MouseFilter = Control.MouseFilterEnum.Stop };
        overlay.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        float column = GetViewport().GetVisibleRect().Size.X / 6;
        var factions = new[] { FleetColor.Blue, FleetColor.Purple, FleetColor.Yellow, FleetColor.White, FleetColor.Green, FleetColor.Red };
        for (int i = 0; i < factions.Length; i++)
        {
            var symbol = new ReadyActionJug { Position = new(column * (i + .5f) - 66, 140), Size = new(132, 148) };
            overlay.AddChild(symbol);
            symbol.Update(12 + i, FleetPalette.Color(factions[i]), factions[i]);
            symbol.SetHumanTurn(true, true);
        }
        await Capture("six-nation-monuments");
        layer.QueueFree();
        await Frames();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            UiScale.Set(1, false);
            await CycleWithoutHints();
            await ConfirmationActions();
            await RitualLifetime();
            await NationGallery();
            GD.Print($"PASS: {_checks} v020.7b native nation-counter cycling, confirmation glyph, localization and ritual lifetime checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
