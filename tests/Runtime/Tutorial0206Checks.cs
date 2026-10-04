using System;
using System.Collections.Generic;
using System.IO;
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

public partial class Tutorial0206Checks : Node
{
    public Main Game { get;
    set; } = null!;
    private int _checks;
    private TutorialHud _advice = null!;
    private TutorialAdvice _history = null!;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Tutorial0206: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (Node child in node.GetChildren()) foreach (Node item in Nodes(child)) yield return item;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Click(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed }, true);
    }
    private async Task Capture(string suffix)
    {
        string? file = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (file is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(file.Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
    }
    private async Task Topic(string topic, bool first = false)
    {
        var battle = TutorialCapture0206Checks.Fixture(Game.Battle.Rules, topic);
        Game.LoadScenario(battle);
        Game.Home.Hide();
        _history.Begin(battle, newGame: first);
        string untouched = battle.SaveJson();
        _history.Observe(battle, allowed: true);
        await Frames();
        Check(_advice.IsOpen && _advice.Topic == topic, "own public event opens " + topic + " once");
        Check(battle.SaveJson() == untouched, "tutorial changes no money, random state or battle rules");
        var texture = Nodes(_advice).OfType<TextureRect>().Single(n => n.Name == "TutorialAdviceScreenshot");
        Check(texture.Texture is not null && texture.Texture.GetSize() == new Vector2(512, 256), "native close-up asset is present for " + topic);
        await Capture(topic);
        Click(Nodes(_advice).OfType<Button>().Single(n => n.Name == "CloseTutorialAdvice"));
        await Frames();
        Check(!_advice.IsOpen, "real close button dismisses advice");
        for (int i = 0; i < 12; i++) _history.Observe(battle, true);
        Check(!_advice.IsOpen && _history.Shown.Contains(topic), "repeat refreshes never reopen seen " + topic);
        var resumed = BattleState.LoadJson(battle.SaveJson());
        _history.Begin(resumed, newGame: false);
        _history.Observe(resumed, true);
        Check(!_advice.IsOpen && _history.Shown.Contains(topic), "Continue retains shown-topic history");
    }
    private async Task CheckExternalCaptions()
    {
        var reward = new RewardPapyrusHud();
        Game.AddChild(reward);
        reward.ShowHeavenly("caption-test", 2);
        await Frames(24);
        var paper = Nodes(reward).OfType<PanelContainer>().Single(n => n.Name == "RewardPapyrus");
        var caption = Nodes(reward).OfType<Label>().Single(n => n.Name == "RewardGuidance");
        Check(caption.GetParent() == paper.GetParent() && caption.Position.Y >= paper.Position.Y + paper.Size.Y,
            "reward guidance sits below paper on the sea, outside the parchment body");
        UiHints.Set(false, persist: false);
        Check(!caption.Visible, "external reward caption disappears with hints off");
        UiHints.Set(true, persist: false);
        reward.Close();
        reward.QueueFree();
        Game.LoadScenario(TutorialCapture0206Checks.Fixture(Game.Battle.Rules, "resources"));
        Game.Home.Hide();
        Game.FastChecks = true;
        Game.Hud.ShowTurnConfirmation();
        await Frames();
        var turnPaper = Nodes(Game.Hud).OfType<PanelContainer>().Single(n => n.Name == "EndTurnConfirmationPaper");
        var turnCaption = Nodes(Game.Hud).OfType<Label>().Single(n => n.Name == "TurnGuidance");
        Check(turnCaption.GetParent() == turnPaper.GetParent() && turnCaption.Position.Y >= turnPaper.Position.Y + turnPaper.Size.Y,
            "end-turn guidance also sits below its parchment");
        Game.Hud.CloseTurnConfirmation();
        UiHints.Set(false, persist: false);
        Game.Hud.ShowTurnConfirmation();
        Check(!Game.Hud.TurnConfirmationVisible, "hints off never creates end-turn confirmation");
        UiHints.Set(true, persist: false);
    }
    private async Task CheckPreferenceAndBounds()
    {
        var battle = TutorialCapture0206Checks.Fixture(Game.Battle.Rules, "resources");
        _history.Begin(battle, true);
        UiHints.Set(false, persist: false);
        _history.Observe(battle, true);
        Check(!_advice.IsOpen && _history.ShownCount == 0, "disabled tutorials neither appear nor consume a future event");
        UiHints.Set(true, persist: false);
        _history.Observe(battle, true);
        await Frames();
        Check(_advice.Topic == "resources", "enabling hints restores the pending new-voyage advice");
        _history.Observe(battle, false);
        Check(!_advice.IsOpen, "other modals temporarily suspend advice without resetting history");
        _history.Observe(battle, true);
        Check(_advice.IsOpen && _history.ShownCount == 1, "resume does not queue duplicates");
        foreach (string locale in new[] { "en", "uk", "nl" }) foreach (float scale in new[] { .8f, 1.25f })
        {
            Language.Set(locale, false);
            UiScale.Set(scale, false);
            await Frames();
            var paper = Nodes(_advice).OfType<PanelContainer>().Single(n => n.Name == "TutorialAdvicePaper");
            var origin = paper.GetGlobalTransformWithCanvas().Origin;
            var size = paper.Size * UiScale.Value;
            Check(origin.X >= 0 && origin.Y >= 0 && origin.X + size.X <= GetViewport().GetVisibleRect().Size.X
                && size.Y <= GetViewport().GetVisibleRect().Size.Y * .6f + 1, "localized advice stays in upper-left and below sixty percent height");
            var title = Nodes(_advice).OfType<Label>().Single(n => n.Name == "TutorialAdviceTitle");
            var body = Nodes(_advice).OfType<Label>().Single(n => n.Name == "TutorialAdviceDescription");
            if (locale != "en") Check(LocalizedMessages.Translate(title.Text, locale) != title.Text
                && LocalizedMessages.Translate(body.Text, locale) != body.Text, "both title and dynamic costs translate in " + locale);
        }
        Language.Set("en", false);
        UiScale.Set(1, false);
        _advice.Clear();
    }
    private async Task CheckWiring()
    {
        Game.LoadScenario(TutorialCapture0206Checks.Fixture(Game.Battle.Rules, "resources"));
        Game.Home.Hide();
        Game.BeginTutorialVoyage(true);
        Game.Refresh();
        await Frames();
        Check(Game.Tutorial.IsOpen && Game.Tutorial.Topic == "resources", "actual voyage service presents its starting resource tutorial");
        Game.SelectCell(new(7, 7));
        Check(Game.SelectedShipId == Game.Battle.Mothership(Side.Player)!.Id,
            "advice does not block map object selection");
        Click(Nodes(Game.Tutorial).OfType<Button>().Single(n => n.Name == "CloseTutorialAdvice"));
        await Frames();
        Check(!Game.Tutorial.IsOpen, "actual tutorial also supports real close input");
        Game.Refresh();
        Check(!Game.Tutorial.IsOpen, "actual refresh does not spam the dismissed voyage tip");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            UiHints.Set(true, persist: false);
            Game.FastChecks = true;
            _advice = new TutorialHud();
            Game.AddChild(_advice);
            string save = Game.Saves.Path + "-tutorial-check";
            _history = new TutorialAdvice(_advice, save);
            await Topic("resources", true);
            await Topic("kolonel");
            await Topic("trade");
            await Topic("repair");
            await Topic("radar");
            await CheckPreferenceAndBounds();
            await CheckExternalCaptions();
            await CheckWiring();
            _advice.QueueFree();
            UiHints.Set(false, persist: false);
            GD.Print($"PASS: {_checks} tutorial events, native close-ups, preference, history, external captions and input checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
