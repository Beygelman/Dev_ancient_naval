using System;
using System.Collections.Generic;
using System.Globalization;
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

/// <summary>Native modal bounds, translated layout, transformed hit testing and paid town commands.</summary>
public partial class Ui0204Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("v020.4 UI: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (var child in root.GetChildren())
            foreach (var node in Nodes(child)) yield return node;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Click(Control control, Vector2? local = null)
    {
        var point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left,
            Position = point, GlobalPosition = point, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left,
            Position = point, GlobalPosition = point, Pressed = false }, true);
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
    private BattleState Fixture(int motherLevel = 1, bool choice = false, int townLevel = 1, bool port = false,
        bool damagedTown = false)
    {
        var town = new GridPosition(8, 8);
        var battle = new BattleState(new GameBoard(24, 24, cell => cell == town ? TerrainType.Land : TerrainType.Water),
            Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(6, 8)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)) },
            Array.Empty<GridPosition>(), villageSpots: new[] { town });
        var saved = battle.CaptureSnapshot();
        saved.Credits[(int)Side.Player] = 100;
        saved.Ships[0].Level = motherLevel;
        saved.Ships[0].PendingUpgradeLevel = choice ? motherLevel : 0;
        saved.Villages[0] = saved.Villages[0] with { Owner = Side.Player, Level = townLevel,
            Health = townLevel * 5 - (damagedTown ? 2 : 0), Port = port };
        return BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
    }
    private void CheckPaper(PanelContainer paper, string context)
    {
        var size = UiScale.LogicalViewport(this);
        Check(paper.Size.Y <= size.Y * .6f + 2, context + " is limited to 60% of the screen height");
        Check(paper.Size.X <= Math.Min(PapyrusModal.Width, size.X - 24) + 2,
            context + $" has a bounded readable width without horizontal clipping: paper={paper.Size}, viewport={size}; "
            + string.Join("; ", Nodes(paper).OfType<Control>().Where(node => node.GetCombinedMinimumSize().X > 250)
                .Select(node => $"{node.GetPath()} min={node.GetCombinedMinimumSize()} size={node.Size}")));
        var scroll = Nodes(paper).OfType<ScrollContainer>().Single();
        Check(scroll.HorizontalScrollMode == ScrollContainer.ScrollMode.Disabled
            && scroll.VerticalScrollMode == ScrollContainer.ScrollMode.Auto,
            context + " keeps native vertical scrolling available for its full contents");
    }
    private async Task CheckModalFamilies()
    {
        var original = GetWindow().Size;
        foreach (var window in new[] { new Vector2I(1280, 720), new Vector2I(618, 1400), new Vector2I(800, 520) })
        {
            GetWindow().Size = window; await Frames();
            UiScale.Set(1.25f, persist: false);
            foreach (string locale in new[] { "en", "uk", "nl" })
            {
                DevAncientNaval.Presentation.UI.Language.Set(locale, persist: false);
                Game.LoadScenario(Fixture(3, choice: true)); await Frames();
                Check(Game.Hud.UpgradeVisible, "upgrade choices open in " + locale);
                Check(Nodes(Game.Hud).OfType<Label>().Single(node => node.Name == "UpgradeLevelReward").Text
                    == $"+{Game.Battle.Rules.LevelCurrencyRewards[1]} · Level reward"
                    && !Nodes(Game.Hud).OfType<Label>().Any(node => node.Text == "0 · Level reward"),
                    "the level reward is shown once with its actual amount rather than as option prices");
                var upgrade = Nodes(Game.Hud).OfType<PanelContainer>().Single(node => node.Name == "UpgradePapyrus");
                CheckPaper(upgrade, $"{window} {locale} upgrade parchment");
                foreach (var button in Nodes(upgrade).OfType<MysticUpgradeButton>().Where(node => node.Visible))
                    Check(button.Size.X <= upgrade.Size.X - 30 && button.Size.Y >= 48,
                        "translated mystical choices retain full-size clickable buttons");
                await Capture($"upgrade-{window.X}x{window.Y}-{locale}");
            }
            Game.LoadScenario(Fixture());
            Game.Hud.SetMenuVisible(true); await Frames();
            CheckPaper(Nodes(Game.Hud).OfType<ScrollContainer>().Single(node => node.Name == "GameMenuScroll")
                .GetParent<PanelContainer>(), "game settings parchment");
            Game.Hud.SetMenuVisible(false);
            var reward = Nodes(Game).OfType<RewardPapyrusHud>().Single();
            reward.ShowNation("layout-only", "Captain Harukaze", new Color("72549b"));
            reward._Process(.31); await Frames();
            CheckPaper(Nodes(reward).OfType<PanelContainer>().Single(node => node.Name == "RewardPapyrus"), "nation parchment");
            await Capture($"reward-{window.X}x{window.Y}");
            reward.Close();
            Game.Home.ShowColors(); await Frames();
            CheckPaper(Nodes(Game.Home).OfType<PanelContainer>().Single(node => node.Name == "VoyageSetupPaper"), "new voyage parchment");
            await Capture($"setup-{window.X}x{window.Y}");
            Game.Home.Hide();
            var outcome = Nodes(Game).OfType<VictoryScreen>().Single();
            outcome.ShowVictory(Game.Battle.Statistics, Game.Battle.Round); await Frames();
            CheckPaper(Nodes(outcome).OfType<PanelContainer>().Single(node => node.Name == "VictoryPaper"), "voyage outcome parchment");
            await Capture($"victory-{window.X}x{window.Y}");
            outcome.Close();
        }
        GetWindow().Size = original;
        UiScale.Set(1, persist: false);
        DevAncientNaval.Presentation.UI.Language.Set("en", persist: false);
        await Frames();
    }
    private async Task CheckScaleAndTown()
    {
        Check(Game.Battle.Rules.PaidVillageUpgrades, "new voyages use paid village progression");
        foreach (float scale in new[] { .8f, 1.25f })
        {
            var battle = Fixture(damagedTown: true); Game.LoadScenario(battle);
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(8, 8));
            Game.MapCamera.Zoom = Vector2.One; Game.MapCamera.ForceUpdateScroll();
            var camera = Game.MapCamera.Position;
            var worldPoint = Game.BoardView.Projection.GridToWorld(new(7, 8));
            UiScale.Set(scale, persist: false); Game.Refresh();
            Check(Game.MapCamera.Position == camera && Game.MapCamera.Zoom == Vector2.One
                && Game.BoardView.Projection.GridToWorld(new(7, 8)) == worldPoint,
                "interface scaling never changes world position or zoom");
            Check(Game.Hud.Scale == Vector2.One * scale && Game.Home.Scale == Vector2.One * scale,
                "title and in-game CanvasLayers share the same interface preference");
            Game.SelectCell(new(8, 8)); await Frames();
            var fan = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(node => node.Name == "ActionPapyrus");
            fan._Process(1); await Frames();
            var upgrade = Nodes(Game.Hud).OfType<SectorButton>().Single(node => node.Name == "ActionVillageUpgrade");
            Check(upgrade.IsVisibleInTree() && !upgrade.Disabled && upgrade.Cost == 5
                && Math.Abs(upgrade.CenterAngle - Mathf.Pi / 2) < .001,
                "a town upgrade has the central lower wedge and its current five-Thor price");
            var repair = Nodes(Game.Hud).OfType<SectorButton>().Single(node => node.Name == "ActionRepair");
            Check(!repair.Disabled, "a damaged idle town offers repair before spending construction");
            var centered = fan.GetGlobalTransformWithCanvas() * SectorButton.Center;
            var townScreen = GetViewport().GetCanvasTransform() * Game.BoardView.Projection.GridToWorld(new(8, 8));
            Check(centered.DistanceTo(townScreen) < 1, "a scaled action parchment remains attached to its town");
            int credits = battle.Credits(Side.Player);
            Click(upgrade, upgrade.IconCenter); await Game.CurrentOrder; await Frames();
            Check(battle.Villages.Single().Level == 2 && battle.Credits(Side.Player) == credits - 5,
                "a real scaled sector click calls Main and pays for the town upgrade once");
            Check(upgrade.Disabled && upgrade.Cost == 8 && Math.Abs(upgrade.CenterAngle - Mathf.Pi / 2) < .001,
                "the next price updates immediately and construction locks for this turn");
            Check(battle.Villages.Single().Health < battle.Villages.Single().MaxHealth && repair.Disabled,
                "paid construction blocks same-turn repair even while the town still needs healing");
            upgrade.EmitSignal(BaseButton.SignalName.Pressed); await Game.CurrentOrder;
            Check(battle.Villages.Single().Level == 2 && battle.Credits(Side.Player) == credits - 5,
                "an extra command cannot bypass town construction locking");
            await Capture($"town-scaled-{MathF.Round(scale * 100)}");
        }
        Game.LoadScenario(Fixture(townLevel: 3, port: true)); Game.SelectCell(new(8, 8)); await Frames();
        var higher = Nodes(Game.Hud).OfType<SectorButton>().Single(node => node.Name == "ActionVillageUpgrade");
        Nodes(Game.Hud).OfType<RadialPapyrus>().Single(node => node.Name == "ActionPapyrus")._Process(1);
        Check(higher.Cost == 12 && Math.Abs(higher.CenterAngle - Mathf.Pi / 2) < .001,
            "adding a port changes the action count without shifting the central upgrade");
        Game.Hud.ShowInformation(Game.Battle, new(8, 8)); await Frames();
        Check(Game.Hud.InformationText.Contains("paid upgrades") && !Game.Hud.InformationText.Contains("grows after two turns"),
            "town counsel explains paid progression rather than automatic growth");
        Game.Hud.CloseMenus();
        Game.Hud.SetMenuVisible(true); await Frames();
        var slider = Nodes(Game.Hud).OfType<HSlider>().Single(node => node.Name == "InterfaceScale");
        Click(slider, new Vector2(slider.Size.X * .78f, slider.Size.Y * .5f)); await Frames();
        Check(UiScale.Value >= UiScale.Minimum && UiScale.Value <= UiScale.Maximum
            && Nodes(Game.Hud).OfType<Label>().Single(node => node.Name == "InterfaceScaleCaption").Text
                == $"Interface scale · {MathF.Round(UiScale.Value * 100)}%",
            "a real settings slider updates the active interface percentage");
        string? config = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--ui-settings-file="))?[19..];
        if (config is not null)
            Check(System.IO.File.Exists(config) && Math.Abs(float.Parse(System.IO.File.ReadAllText(config), CultureInfo.InvariantCulture) - UiScale.Value) < .001,
                "scale persistence writes only the explicitly disposable test preference path");
        Game.Hud.SetMenuVisible(false);
        UiScale.Set(1, persist: config is not null);
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); Game.FastChecks = true;
            await CheckModalFamilies();
            await CheckScaleAndTown();
            GD.Print($"PASS: {_checks} v020.4 modal, translation, scaled input and paid town UI checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
