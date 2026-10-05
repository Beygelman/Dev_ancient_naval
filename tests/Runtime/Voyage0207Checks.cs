using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class Voyage0207Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string why)
    {
        if (!value) throw new InvalidOperationException("Voyage0207: " + why);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node n in Nodes(child)) yield return n;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Click(Vector2 point)
    {
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = point,
                GlobalPosition = point,
                ButtonIndex = MouseButton.Left,
                Pressed = pressed,
                ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0
            }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
        await Frames(1);
    }
    private void Judgements()
    {
        var simple = new VoyageEvidence(10, 1, 0, 1, 1, 0, 10, 0, 1, 2);
        Check(VoyageJudgement.SwiftLimit(simple) == 10, "small one-rival swift limit");
        Check(VoyageJudgement.SwiftLimit(simple with { Rivals = 2, Area = 2 }) == 20, "medium two-rival swift limit");
        Check(VoyageJudgement.SwiftLimit(simple with { Rivals = 4, Area = 3 }) == 31, "ocean four-rival swift limit");
        Check(VoyageJudgement.Evaluate(simple) == VoyageDistinction.Pathfinder, "modest swift victory");
        Check(VoyageJudgement.Evaluate(simple with { Kills = 40, RivalBestKills = 20 }) == VoyageDistinction.Admiral, "twice strongest rival direct losses");
        Check(VoyageJudgement.Evaluate(simple with { Kills = 39, RivalBestKills = 20 }) != VoyageDistinction.Admiral, "combat threshold not rounded prematurely");
        Check(VoyageJudgement.Evaluate(simple with { Turn = 30, OwnedTowns = 7, StructuresBuilt = 6, SeaStrongholdsBuilt = 6 }) == VoyageDistinction.Conqueror, "six sea strongholds and seventy percent towns");
        Check(VoyageJudgement.Evaluate(simple with { Turn = 30, OwnedTowns = 7, StructuresBuilt = 6 }) != VoyageDistinction.Conqueror,
            "fishing docks alone cannot qualify as six towers or beacons");
        Check(VoyageJudgement.Evaluate(simple with { Turn = 30, Income = 20 }) == VoyageDistinction.Strategist, "long sustainable expedition");
        Check(VoyageJudgement.EconomyTarget(simple with { ShipsBuilt = 24, StructuresBuilt = 8, OwnedTowns = 6 }) > VoyageJudgement.EconomyTarget(simple), "economy expectation accounts for expedition investment");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            Game.FastChecks = true;
            UiHints.Set(false, persist: false);
            var initial = TutorialCapture0206Checks.Fixture(Game.Battle.Rules, "resources");
            var save = initial.CaptureSnapshot();
            var pos = initial.Mothership(Side.Player)!.Position;
            save.Fish = save.Fish.Append(pos).ToArray();
            var battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            string untouched = battle.SaveJson();
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(pos);
            Game.MapCamera.Zoom = Vector2.One;
            Game.MapCamera.ForceUpdateScroll();
            Game.Refresh();
            await Frames();
            var screen = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(pos));
            Click(screen); await Frames();
            Check(Game.SelectedShipId == battle.Mothership(Side.Player)!.Id, "first physical click selects hull over resource");
            var portrait = Nodes(Game.Hud).OfType<ActionGlyph>().Single(n => n.Name == "SelectedObjectGlyph");
            Check(portrait.IsVisibleInTree() && portrait.Size.X >= 60, "selected object's large matching glyph");
            Click(screen); await Frames();
            Check(Game.SelectedShipId is null && Game.BoardView.Selected == pos, "second physical click selects underlying resource");
            Click(screen); await Frames();
            Check(Game.SelectedShipId == battle.Mothership(Side.Player)!.Id, "third click cycles back to hull");
            Check(Game.Battle.SaveJson() == untouched, "cycling is purely presentation");
            Judgements();
            foreach (string lang in new[] { "en", "uk", "nl" })
            {
                Language.Set(lang, persist: false);
                string story = VoyageJudgement.Build(battle, true);
                Check(story.Length > 120 && (lang == "en" || !story.StartsWith("With modest")), "localized personal outcome " + lang);
                Game.Home.ShowColors(); await Frames();
                var setup = Nodes(Game.Home).OfType<Control>().Single(n => n.Name == "VoyageSetupPaper");
                var viewport = UiScale.LogicalViewport(Game.Home);
                float right = viewport.X - setup.Position.X - setup.Size.X;
                Check(Math.Abs(setup.Position.Y - right) < 1 && Math.Abs(viewport.Y - setup.Position.Y - setup.Size.Y - right) < 1,
                    "setup equal top bottom right margins " + lang);
                Game.Home.Hide();
            }
            UiHints.Set(true, persist: false);
            Game.BeginTutorialVoyage(true); await Frames();
            string advice = Nodes(Game.Tutorial).OfType<Label>().Single(n => n.Name == "TutorialAdviceDescription").Text;
            Game.SelectCell(new GridPosition(9, 7)); await Frames();
            Check(Nodes(Game.Tutorial).OfType<Label>().Single(n => n.Name == "TutorialAdviceDescription").Text == advice,
                "advice remains fixed across object selection");
            Game.Tutorial.Clear();
            Game.VoyageWelcome.InstantAnimations = false;
            var welcome = Game.VoyageWelcome.Welcome(FleetColor.Yellow);
            await Frames(32); await Capture("rose-welcome");
            var accept = Nodes(Game.VoyageWelcome).OfType<Button>().Single(n => n.Name == "AcceptVoyage");
            float zoom = Game.MapCamera.Zoom.X;
            foreach (bool pressed in new[] { true, false })
                GetViewport().PushInput(new InputEventMouseButton
                {
                    Position = new(12, 12),
                    GlobalPosition = new(12, 12),
                    ButtonIndex = MouseButton.WheelUp,
                    Pressed = pressed
                }, true);
            await Frames();
            Check(Game.MapCamera.Zoom.X == zoom, "welcome wheel cannot zoom background");
            Click(accept.GetGlobalTransformWithCanvas() * (accept.Size * .5f));
            await Frames(52);
            Check(welcome.IsCompleted && !Game.VoyageWelcome.IsOpen, "real hand acknowledgement folds welcome then releases game");
            GD.Print($"PASS: {_checks} v020.7 stacked selection, welcome and personal voyage checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
