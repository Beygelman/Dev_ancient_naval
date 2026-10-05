using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>New advice uses saved rules, acknowledged v1 history and native nonmodal input.</summary>
public partial class Tutorial0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private TutorialHud _advice = null!;
    private TutorialAdvice _history = null!;
    private string _historySlot = "";
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Tutorial0207b: " + message);
        _checks++;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Opened()
    {
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        await Frames();
    }
    private static T Named<T>(Node root, string name) where T : Node =>
        Tutorial0207bCaptureChecks.Nodes(root).OfType<T>().Single(n => n.Name == name);
    private void Click(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 }, true);
    }
    private async Task Close()
    {
        Click(Named<Button>(_advice, "CloseTutorialAdvice"));
        await ToSignal(GetTree().CreateTimer(.8), SceneTreeTimer.SignalName.Timeout);
        await Frames();
        Check(!_advice.IsOpen, "real pointer acknowledges and folds advice without an extra modal");
    }
    private async Task Capture(string suffix)
    {
        string? file = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (file is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(file.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native advice capture " + suffix);
        await Frames(1);
    }
    private async Task Topic(string topic, bool newGame = false, bool commands = false)
    {
        var battle = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, topic);
        Game.LoadScenario(battle); Game.Home.Hide();
        _history.Begin(battle, newGame);
        string unchanged = battle.SaveJson();
        _history.Observe(battle, true, commands);
        await Opened();
        Check(_advice.IsOpen && _advice.Topic == topic, "public owned event opens " + topic);
        Check(battle.SaveJson() == unchanged, "opening " + topic + " changes no battle state or RNG");
        var picture = Named<TextureRect>(_advice, "TutorialAdviceScreenshot");
        Check(picture.Texture is not null && picture.Texture.GetSize() == new Vector2(512, 256),
            topic + " uses the reviewed native 512×256 photograph");
        await Capture(topic);
        _history.Observe(battle, false, commands);
        Check(!_advice.IsOpen && _advice.Topic == topic, "command/modal suspension preserves " + topic + " rather than dismissing it");
        _history.Observe(battle, true, commands);
        Check(_advice.IsOpen, "suspended advice returns safely");
        await Close();
        for (int i = 0; i < 10; i++) _history.Observe(battle, true, commands);
        Check(!_advice.IsOpen && _history.Shown.Contains(topic), "repeated refresh cannot repeat " + topic);
        var resumed = BattleState.LoadJson(battle.SaveJson());
        _history.Begin(resumed, false); _history.Observe(resumed, true, commands);
        Check(!_advice.IsOpen && _history.Shown.Contains(topic), "Continue retains acknowledged " + topic);
        Check(resumed.SaveJson() == unchanged, "Continue and advice retain full saved world/rules/RNG");
    }
    private void RuleAwareText()
    {
        var resources = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "resources");
        var mother = resources.Mothership(Side.Player)!;
        string text = TutorialAdvice.Content("resources", resources).Body;
        Check(text.Contains($"{mother.Resources}/{mother.ResourcesRequired}") && text.Contains($"{resources.CollectionCost(Side.Player)} Thors"),
            "resource advice reports the actual saved level threshold, partial row and collection price");
        Check(text.Contains($"{resources.DockPrice(Side.Player)} Thors") && text.Contains($"{resources.Rules.DockResourceReward} flagship resources"),
            "resource advice reports the actual dock price and level resource reward");
        Check(!text.Contains("Towns") && !text.Contains("Unlocks"), "resource advice explains flagship leveling rather than an unrelated upgrade catalogue");
        var complete = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "commands");
        Check(TutorialAdvice.Content("resources", complete).Body.Contains("no longer needed"), "level-five advice never suggests collecting another level");

        var village = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "village");
        text = TutorialAdvice.Content("village", village).Body;
        Check(text.Contains($"{village.VillageUpgradePrice(Side.Player, village.Villages.Single().Id)} Thors")
            && text.Contains("work for the turn"), "current village advice includes the saved actual price and work cost");
        var historicJson = JsonNode.Parse(village.SaveJson())!;
        historicJson["Rules"]!.AsObject().Remove("PaidVillageUpgrades");
        var historical = BattleState.LoadJson(historicJson.ToJsonString());
        Check(!historical.Rules.PaidVillageUpgrades, "missing optional paid-town field keeps historical automatic growth");
        text = TutorialAdvice.Content("village", historical).Body;
        Check(text.Contains("every two of their own turns") && !text.Contains("next level costs"),
            "historical village advice describes automatic growth instead of selling an unavailable order");

        var veterans = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "veterancy");
        var galleon = veterans.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Invader && !s.IsVeteran);
        text = TutorialAdvice.Content("veterancy", veterans).Body;
        Check(text.Contains("3 enemy vessels") && text.Contains("Destroyed structures do not count") && text.Contains("counterattack does"),
            "veterancy advice counts actual naval kills and counterfire");
        Check(text.Contains("full health") && text.Contains("25%")
            && text.Contains($"{galleon.Definition.MaxHealth} → {Ship.Whole(galleon.Definition.MaxHealth * 1.25)} maximum HP")
            && text.Contains($"{galleon.Definition.Damage} → {Ship.Whole(galleon.Definition.Damage * 1.25)} base damage"),
            "veterancy advice uses Core's whole-number strength and full restoration");
        Check(text.Contains($"gains {galleon.Definition.VeteranRangeBonus} tiles"), "only a hull with a saved range bonus advertises range growth");
        var noRangeSave = veterans.CaptureSnapshot();
        noRangeSave.Ships.Single(s => s.Id == galleon.Id).Kind = ShipClass.Garrison;
        noRangeSave.Ships.Single(s => s.Id == galleon.Id).Health = noRangeSave.Rules.Get(ShipClass.Garrison).MaxHealth;
        var noRange = BattleState.LoadJson(BattleState.SerializeSnapshot(noRangeSave));
        Check(TutorialAdvice.Content("veterancy", noRange).Body.Contains("range stays unchanged"), "Brig advice never borrows the Galleon's veteran range");

        foreach (var battle in new[] { resources, village, historical, veterans, noRange, complete })
            foreach (string topic in new[] { "resources", "kolonel", "village", "veterancy", "commands", "trade", "repair", "radar" })
                foreach (string locale in new[] { "uk", "nl" })
                {
                    var advice = TutorialAdvice.Content(topic, battle);
                    Check(LocalizedMessages.Translate(advice.Title, locale) != advice.Title,
                        topic + " title translated in " + locale);
                    string translated = LocalizedMessages.Translate(advice.Body, locale);
                    Check(translated != advice.Body && !translated.Contains("Gather for the flagship")
                        && !translated.Contains("Invest in the settlement") && !translated.Contains("A seasoned crew"),
                        topic + " rule-aware body and section headings translated in " + locale);
                }
    }
    private async Task HistoryAndEligibility()
    {
        var battle = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "commands");
        string voyage = $"{battle.Board.Seed}:{battle.Board.Width}:{battle.Board.Height}:{battle.Board.MapSize}:{battle.PlayerColor}";
        System.IO.File.WriteAllText(_historySlot + ".tutorials.json", JsonSerializer.Serialize(new {
            Version = 1, Voyage = voyage, Shown = new[] { "resources", "kolonel", "trade", "repair", "radar", "unknown-historical-topic" } }));
        _history.Begin(battle, false);
        Check(_history.ShownCount == 5 && !_history.Shown.Contains("unknown-historical-topic"), "v1 history retains all five old topics and safely ignores unknown IDs");
        UiHints.Set(false, false); _history.Observe(battle, true, true);
        Check(!_advice.IsOpen && _history.ShownCount == 5, "hints-off never consumes a newly added tutorial");
        UiHints.Set(true, false); _history.Observe(battle, true, true); await Opened();
        Check(_advice.Topic == "commands" && _history.ShownCount == 6, "new commands advice remains available after historical history load");
        await Close();
        _history.Begin(battle, false); _history.Observe(battle, true, true);
        Check(!_advice.IsOpen && _history.ShownCount == 6, "new appended topic survives the unchanged v1 sidecar contract");
        var json = JsonNode.Parse(System.IO.File.ReadAllText(_historySlot + ".tutorials.json"))!;
        Check(json["Version"]!.GetValue<int>() == 1 && json["Voyage"]!.GetValue<string>() == voyage,
            "writing added topics does not change sidecar format or voyage identity");

        var hidden = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "veterancy").CaptureSnapshot();
        foreach (var hull in hidden.Ships.Where(s => s.Kind == ShipClass.Invader)) hull.Owner = Side.Enemy;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(hidden));
        _history.Begin(battle, false); _history.Observe(battle, true);
        Check(!_advice.IsOpen, "enemy veteran evidence never triggers or reveals a player's advice");
        battle = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "village");
        var neutral = battle.CaptureSnapshot(); neutral.Villages[0] = neutral.Villages[0] with { Owner = null };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(neutral));
        _history.Begin(battle, false); _history.Observe(battle, true);
        Check(!_advice.IsOpen, "an unowned settlement does not pretend that a city has been acquired");
    }
    private async Task LocalizedLayoutAndMapInput()
    {
        var battle = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "resources");
        Game.LoadScenario(battle); Game.Home.Hide();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(7, 7));
        Game.MapCamera.Zoom = Vector2.One; Game.MapCamera.ForceUpdateScroll();
        _history.Begin(battle, true); _history.Observe(battle, true); await Opened();
        foreach (string locale in new[] { "en", "uk", "nl" }) foreach (float scale in new[] { .8f, 1.25f })
        {
            Language.Set(locale, false); UiScale.Set(scale, false); await Frames();
            var paper = Named<PanelContainer>(_advice, "TutorialAdvicePaper");
            var origin = paper.GetGlobalTransformWithCanvas().Origin;
            var size = paper.Size * UiScale.Value;
            var viewport = GetViewport().GetVisibleRect().Size;
            Check(origin.X >= 0 && origin.Y >= 0 && origin.X + size.X <= viewport.X && size.Y <= viewport.Y * .6f + 1,
                "sectioned translated advice fits at " + locale + " UI " + scale);
            var scroll = Named<ScrollContainer>(_advice, "TutorialAdviceScroll");
            var close = Named<Button>(_advice, "CloseTutorialAdvice");
            var closeOrigin = close.GetGlobalTransformWithCanvas().Origin;
            var closeEnd = close.GetGlobalTransformWithCanvas() * close.Size;
            Check(!scroll.IsAncestorOf(close) && close.IsVisibleInTree()
                && closeOrigin.Y >= origin.Y && closeEnd.Y <= origin.Y + size.Y + 1
                && closeEnd.X <= origin.X + size.X + 1,
                "acknowledgement is a visible fixed footer outside the clipped localized text");
            if (scale > 1)
                Check(scroll.GetVScrollBar().MaxValue > scroll.Size.Y,
                    "long localized advice can scroll while the footer stays available");
            var position = Game.MapCamera.Position; var zoom = Game.MapCamera.Zoom;
            var point = scroll.GetGlobalTransformWithCanvas() * (scroll.Size * .5f);
            GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            foreach (bool down in new[] { true, false })
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.WheelDown, Pressed = down }, true);
            await Frames();
            Check(Game.MapCamera.Position == position && Game.MapCamera.Zoom == zoom, "advice scrolling cannot pan or zoom the sea");
            scroll.ScrollVertical = int.MaxValue; await Frames();
            foreach (bool down in new[] { true, false })
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                    ButtonIndex = MouseButton.WheelDown, Pressed = down }, true);
            await Frames();
            Check(Game.MapCamera.Position == position && Game.MapCamera.Zoom == zoom,
                "wheel at the end of a tutorial still belongs to its paper");
            var footerPoint = close.GetGlobalTransformWithCanvas() * (close.Size * .5f);
            GetViewport().PushInput(new InputEventMouseMotion { Position = footerPoint, GlobalPosition = footerPoint }, true);
            foreach (bool down in new[] { true, false })
                GetViewport().PushInput(new InputEventMouseButton { Position = footerPoint, GlobalPosition = footerPoint,
                    ButtonIndex = MouseButton.WheelDown, Pressed = down }, true);
            await Frames();
            Check(Game.MapCamera.Position == position && Game.MapCamera.Zoom == zoom,
                "wheel over the fixed acknowledgement footer cannot zoom the sea");
        }
        Language.Set("en", false); UiScale.Set(1, false); await Frames();
        string unchanged = battle.SaveJson();
        var at = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(new(7, 7)));
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = at, GlobalPosition = at,
                ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0 }, true);
        await Frames();
        Check(Game.SelectedShipId == battle.Mothership(Side.Player)!.Id, "native map click outside the nonmodal paper still selects the flagship");
        Check(battle.SaveJson() == unchanged, "reading advice and selecting a hull apply no gameplay effects");
        await Close();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); UiHints.Set(true, false); Language.Set("en", false); UiScale.Set(1, false);
            Game.FastChecks = true;
            _advice = new TutorialHud(); Game.AddChild(_advice);
            _historySlot = Game.Saves.Path + "-tutorial0207b-check";
            _history = new TutorialAdvice(_advice, _historySlot);
            RuleAwareText();
            await Topic("resources", newGame: true);
            await Topic("village");
            await Topic("veterancy");
            await Topic("commands", commands: true);
            await HistoryAndEligibility();
            await LocalizedLayoutAndMapInput();
            _advice.QueueFree(); UiHints.Set(false, false);
            GD.Print($"PASS: {_checks} v020.7b tutorial saved rules, historical history, new public events, native input and EN/UK/NL checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
