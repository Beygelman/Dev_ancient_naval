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

/// <summary>Native chart navigation, exact world UI anchors and finite human-turn ceremonies.
/// Eligibility is established by actual sailing and owner turns in disposable scenarios.</summary>
public partial class Navigation0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private static readonly GridPosition ClaimCell = new(5, 5), SecondClaimCell = new(6, 6),
        TreasuryCell = new(33, 31), OwnTownCell = new(5, 31), MotherCell = new(32, 5),
        HiddenTownCell = new(19, 19), HiddenTreasuryCell = new(20, 20), ChartCenter = new(19, 19);

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Navigation0207b: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (Node item in Nodes(child)) yield return item;
    }
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Delay(double seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
    private void Click(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed,
                ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0 }, true);
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null) return;
        await Frames(2);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        int extension = path.LastIndexOf('.');
        path = extension < 0 ? path + "-" + suffix + ".png" : path[..extension] + "-" + suffix + path[extension..];
        Check(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "native photograph " + suffix);
    }
    private Vector2 World(GridPosition cell) => Game.BoardView.ToGlobal(
        Game.Battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell) is { } town
            ? Game.BoardView.VillageWorldAnchor(town) : Game.BoardView.Projection.GridToWorld(cell));
    private Vector2 Screen(GridPosition cell) => GetViewport().GetCanvasTransform() * World(cell);
    private void LookAt(GridPosition cell, float zoom = 1.6f)
    {
        Game.MapCamera.CancelFlight();
        Game.MapCamera.Position = World(cell);
        Game.MapCamera.Zoom = Vector2.One * zoom;
        Game.MapCamera.ForceUpdateScroll();
        Game.Refresh();
    }
    private void Load(BattleState battle)
    {
        Game.Home.Hide(); Game.Hud.Show(); Game.BoardView.Show(); Game.Fleet.Show();
        Game.Hud.SetMenuVisible(false);
        Game.LoadScenario(battle);
        Game.FastChecks = true; Game.Hud.InstantPaperAnimations = true;
        LookAt(ChartCenter);
    }
    private BattleState EligibleFixture()
    {
        var spots = new[] { ClaimCell, SecondClaimCell, OwnTownCell, HiddenTownCell };
        var battle = new BattleState(new GameBoard(38, 38,
                p => spots.Contains(p) ? TerrainType.Land : TerrainType.Water, seed: 2710),
            Game.Battle.Rules, new[] {
                (Side.Player, ShipClass.Mothership, MotherCell),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(35, 35)),
                (Side.Player, ShipClass.Garrison, new GridPosition(4, 5)),
                (Side.Player, ShipClass.Garrison, new GridPosition(32, 31)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(31, 5)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(6, 31)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(18, 19)),
                (Side.Enemy, ShipClass.Garrison, new GridPosition(20, 21)),
                (Side.Player, ShipClass.Garrison, new GridPosition(7, 6)) },
            fishSpots: Array.Empty<GridPosition>(), villageSpots: spots, piratesEnabled: false);
        var save = battle.CaptureSnapshot();
        save.Villages = save.Villages.Select(v => v.Position == OwnTownCell
            ? v with { Owner = Side.Player, Level = 2, Health = 10 }
            : v with { Health = 0 }).ToArray();
        int ownTreasure = save.NextId++, foreignTreasure = save.NextId++;
        save.Treasuries = new[] { new Treasury(ownTreasure, TreasuryCell), new Treasury(foreignTreasure, HiddenTreasuryCell) };
        save.Outcomes = new[] { new SavedOutcome(ownTreasure, TreasuryReward.Currency),
            new SavedOutcome(foreignTreasure, TreasuryReward.Currency) };
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        Check(!battle.CanCaptureVillage(Side.Player, battle.VillageAt(ClaimCell)!.Id),
            "a defeated town starts without a fictitious completed holding turn");
        Check(battle.Move(Side.Player, 4, TreasuryCell).Success && !battle.CanLootTreasury(Side.Player, 4),
            "sailing onto a treasury initially requires waiting");
        Check(battle.EndTurn(Side.Player).Success && battle.Move(Side.Enemy, 8, HiddenTreasuryCell).Success
            && battle.EndTurn(Side.Enemy).Success, "both factions sail and complete actual turns");
        Check(battle.CanCaptureVillage(Side.Player, battle.VillageAt(ClaimCell)!.Id)
            && battle.CanCaptureVillage(Side.Player, battle.VillageAt(SecondClaimCell)!.Id)
            && battle.CanLootTreasury(Side.Player, 4), "only the real waiting human crews become ready");
        Check(battle.EndTurn(Side.Player).Success && battle.CanLootTreasury(Side.Enemy, 8)
            && battle.CanCaptureVillage(Side.Enemy, battle.VillageAt(HiddenTownCell)!.Id)
            && battle.EndTurn(Side.Enemy).Success, "hidden rival crews also establish legitimate ready actions");
        foreach (var award in battle.PendingAwards.ToArray())
            Check(battle.ClaimAward(Side.Player, award.Id).Success, "disposable fixture dismisses its real encounter award");
        Check(!battle.GodEye && !battle.Vision.IsVisible(Side.Player, HiddenTownCell)
            && !battle.Vision.IsVisible(Side.Player, HiddenTreasuryCell), "rival action coordinates remain outside optical sight");
        return battle;
    }
    private void CheckCircle(string context)
    {
        var hud = Game.NavigationHud;
        var physical = GetViewport().GetVisibleRect().Size;
        var center = physical * .5f;
        float radius = Math.Min(physical.X, physical.Y) * .3f;
        Check((hud.CircleCenter * UiScale.Value).DistanceTo(center) < .1f
            && Math.Abs(hud.CircleRadius * UiScale.Value - radius) < .1f,
            context + " guide circle uses thirty percent of the shorter physical viewport side");
        var arrows = Nodes(hud).OfType<OffscreenNavigationArrow>().Where(a => a.IsVisibleInTree()).ToArray();
        Check(arrows.Length == hud.VisibleMarkerCount, context + " native visible marker accounting is exact");
        foreach (var arrow in arrows)
        {
            var point = arrow.GetGlobalTransformWithCanvas() * (arrow.Size * .5f);
            var expectedDirection = (Screen(arrow.Target.Cell) - center).Normalized();
            Check(Math.Abs(point.DistanceTo(center) - radius) < .25f,
                context + " every arrow center stays on the requested physical circle");
            Check(arrow.Direction.Dot(expectedDirection) > .999f,
                context + " the illuminated arrow keeps pointing toward its actual world object");
            Check(!arrow.Disabled && arrow._HasPoint(arrow.Size * .5f), context + " visible arrow is a native click target");
        }
        for (int i = 0; i < arrows.Length; i++) for (int j = i + 1; j < arrows.Length; j++)
            Check((arrows[i].GetGlobalTransformWithCanvas() * (arrows[i].Size * .5f))
                .DistanceTo(arrows[j].GetGlobalTransformWithCanvas() * (arrows[j].Size * .5f)) >= 61 * UiScale.Value,
                context + " nearby action bearings fan into independent nonoverlapping native hit targets");
        var compass = hud.CompassButton;
        var compassCenter = compass.GetGlobalTransformWithCanvas() * (compass.Size * .5f);
        Check(compass.IsVisibleInTree() && !compass.Disabled && Math.Abs(compassCenter.X - center.X) < .1f
            && compassCenter.Y > physical.Y * .8f && compassCenter.Y < physical.Y,
            context + " the Mothership compass stays centered at the bottom in hints-off mode");
    }
    private void CheckStoryAnchor(ActionPapyrus paper, GridPosition cell, string context)
    {
        var expected = Screen(cell) - new Vector2(paper.Size.X * UiScale.Value / 2, 195);
        Check(paper.GetGlobalTransformWithCanvas().Origin.DistanceTo(expected) < .25f,
            context + " the story and its caption remain at their exact world anchor without viewport clamping");
        var label = Nodes(paper).OfType<Label>().Single(l => l.Name == "ActionGuidance");
        Check(Math.Abs(label.Position.X + label.Size.X / 2 - paper.Size.X / 2) < .1f,
            context + " its caption remains centered under its own paper");
    }
    private async Task NativeMarkersAndWorldStories()
    {
        UiHints.Set(false, false);
        Load(EligibleFixture());
        await Frames();
        var battle = Game.Battle;
        string unchanged = battle.SaveJson();
        int claim = battle.VillageAt(ClaimCell)!.Id, claim2 = battle.VillageAt(SecondClaimCell)!.Id;
        int hiddenClaim = battle.VillageAt(HiddenTownCell)!.Id;
        Check(Game.NavigationHud.VisibleMarkerCount == 3
            && Game.NavigationHud.Marker(NavigationMarkerKind.Capture, claim) is not null
            && Game.NavigationHud.Marker(NavigationMarkerKind.Capture, claim2) is not null
            && Game.NavigationHud.Marker(NavigationMarkerKind.Treasury, 4) is not null,
            "all own ready world actions acquire guides without requiring selection");
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.Capture, hiddenClaim) is null
            && Game.NavigationHud.Marker(NavigationMarkerKind.Treasury, 8) is null
            && !Nodes(Game.NavigationHud).OfType<OffscreenNavigationArrow>().Any(a => a.Target.Kind == NavigationMarkerKind.OwnAttack),
            "hidden rival eligibility and initial health never create information-bearing guides");
        foreach (float scale in new[] { .8f, 1.25f })
        {
            UiScale.Set(scale, false); LookAt(ChartCenter); await Frames();
            CheckCircle("UI scale " + scale);
            CheckStoryAnchor(Game.Hud.ClaimPapyrus, ClaimCell, "offscreen harbor " + scale);
            CheckStoryAnchor(Game.Hud.TreasuryPapyrus, TreasuryCell, "offscreen treasury " + scale);
            Check(!Game.Hud.ClaimPapyrus.IsVisibleInTree() && !Game.Hud.ClaimPapyrus.IsProcessing()
                && !Game.Hud.TreasuryPapyrus.IsVisibleInTree() && !Game.Hud.TreasuryPapyrus.IsProcessing(),
                "offscreen ready papers suspend their animation while their separate pointers remain available");
            var arrow = Game.NavigationHud.Marker(NavigationMarkerKind.Capture, claim)!;
            Click(arrow); await Game.CurrentOrder; await Frames();
            Check(Game.MapCamera.Position.DistanceTo(World(ClaimCell)) < .25f && Game.Hud.ClaimPapyrus.IsVisibleInTree()
                && !Game.NavigationHud.Marker(NavigationMarkerKind.Capture, claim)!.IsVisibleInTree(),
                "real harbor-arrow click completes a camera flight and reveals the original world paper");
            await Delay(.43); await Frames(1);
            Check(Game.Hud.ClaimPapyrus.Reveal == 1 && battle.VillageAt(ClaimCell)!.Owner is null,
                "navigation reveals the action but does not capture the harbor");
            CheckStoryAnchor(Game.Hud.ClaimPapyrus, ClaimCell, "focused harbor " + scale);
            LookAt(ChartCenter); await Frames();
            Check(Game.Hud.ClaimPapyrus.Reveal == 1 && !Game.Hud.ClaimPapyrus.IsProcessing(),
                "panning away keeps completed unfolding progress and suspends animation");
            Click(Game.NavigationHud.Marker(NavigationMarkerKind.Treasury, 4)!); await Game.CurrentOrder; await Frames();
            Check(Game.MapCamera.Position.DistanceTo(World(TreasuryCell)) < .25f
                && Game.Hud.TreasuryPapyrus.IsVisibleInTree() && battle.CanLootTreasury(Side.Player, 4),
                "real treasury-arrow click reaches its waiting crew without consuming the discovery");
            CheckStoryAnchor(Game.Hud.TreasuryPapyrus, TreasuryCell, "focused treasury " + scale);
            LookAt(ChartCenter); await Frames();
            Click(Game.NavigationHud.CompassButton); await Game.CurrentOrder; await Frames();
            Check(Game.MapCamera.Position.DistanceTo(World(MotherCell)) < .25f,
                "native bottom compass finds the real human Mothership");
        }
        UiScale.Set(1, false); LookAt(ChartCenter); await Frames();
        await Capture("navigation-ready-guides");
        var beforeCamera = Game.MapCamera.Position;
        Game.Hud.SetMenuVisible(true); Game.Refresh(); await Frames();
        Check(Game.NavigationHud.VisibleMarkerCount == 0 && !Game.NavigationHud.CompassButton.IsVisibleInTree()
            && !Game.NavigationHud.IsProcessing(), "an open session modal hides and stops all navigation controls");
        Click(Game.NavigationHud.CompassButton); await Game.CurrentOrder; await Frames();
        Check(Game.MapCamera.Position == beforeCamera && Game.Hud.MenuVisible,
            "hidden compass cannot fire a camera command through the modal");
        Game.Hud.SetMenuVisible(false); Game.Refresh(); await Frames();
        Check(Game.NavigationHud.VisibleMarkerCount == 3, "closing the modal restores the same eligible guides");
        Check(battle.SaveJson() == unchanged, "all UI scales, pointer clicks, flights, papers and modal transitions preserve Core/save/RNG state");
    }
    private async Task AnchoredSelectionAndSalvo()
    {
        var battle = Tutorial0207bCaptureChecks.Fixture(Game.Battle.Rules, "kolonel");
        Load(battle);
        var gun = battle.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Kolonel);
        var target = battle.Ships.Single(s => s.Owner == Side.Enemy && s.Definition.Class == ShipClass.Invader);
        string unchanged = battle.SaveJson();
        foreach (float scale in new[] { .8f, 1.25f })
        {
            UiScale.Set(scale, false); LookAt(gun.Position, 1.6f);
            Game.SelectCell(gun.Position); await Frames();
            var ring = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(p => p.Name == "ActionPapyrus");
            Check((ring.GetGlobalTransformWithCanvas() * ring.RingCenter).DistanceTo(Screen(gun.Position)) < .25f,
                "selected hull commands keep their exact on-screen world origin " + scale);
            LookAt(new(21, 21)); await Frames();
            Check((ring.GetGlobalTransformWithCanvas() * ring.RingCenter).DistanceTo(Screen(gun.Position)) < .25f
                && !GetViewport().GetVisibleRect().HasPoint(Screen(gun.Position)),
                "selected hull commands follow their offscreen ship rather than following the camera " + scale);
            LookAt(gun.Position); Game.SelectCell(target.Position); await Frames();
            Check(Game.Hud.SalvoChoiceVisible && Game.Fleet.ActiveProjectileCount == 0
                && battle.PendingPresentation is null, "real target selection opens the single/double chooser without firing");
            var salvo = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(p => p.Name == "TargetSalvoPapyrus");
            foreach (var cell in new[] { target.Position, new GridPosition(21, 21) })
            {
                LookAt(cell); await Frames();
                Check((salvo.GetGlobalTransformWithCanvas() * salvo.RingCenter).DistanceTo(Screen(target.Position)) < .25f
                    && Math.Abs(salvo.BandWidth - 58) < .01f,
                    "target salvo stays centered on its real target with fixed paper thickness " + scale);
            }
            Game.CancelOrder();
        }
        Check(battle.SaveJson() == unchanged, "world-anchored selection and double-salvo browsing spend no attacks or simulation draws");
        UiScale.Set(1, false);
    }
    private async Task CompactCompassClearance()
    {
        var window = GetWindow();
        var originalWindow = window.Size;
        var originalContentScale = window.ContentScaleSize;
        float originalScale = UiScale.Value;
        try
        {
            // The project's normal 1280px content minimum otherwise enlarges
            // the logical viewport instead of exercising this compact layout.
            window.ContentScaleSize = new(800, 520);
            window.Size = new(800, 520);
            await Frames();
            UiScale.Set(1.25f, false);
            Load(EligibleFixture());
            await Frames();
            Check(GetViewport().GetVisibleRect().Size.DistanceTo(new Vector2(800, 520)) < .25f,
                "compact compass coverage uses the actual 800 x 520 viewport");
            CheckCircle("compact 800 x 520 at 125%");
            var compass = Game.NavigationHud.CompassButton;
            var compassCenter = compass.GetGlobalTransformWithCanvas() * (compass.Size * .5f);
            var arrows = Nodes(Game.NavigationHud).OfType<OffscreenNavigationArrow>()
                .Where(a => a.IsVisibleInTree()).ToArray();
            Check(arrows.Length == 3 && arrows.Any(a => a.Direction.Dot(Vector2.Down) > .97f),
                "the compact fixture exercises a south-bearing action beside the compass");
            foreach (var arrow in arrows)
            {
                var arrowCenter = arrow.GetGlobalTransformWithCanvas() * (arrow.Size * .5f);
                Check(arrowCenter.DistanceTo(compassCenter) >= 62 * UiScale.Value - .25f,
                    "every compact guide reserves the compass's independent native hit area");
                Check(compass.ZIndex > arrow.ZIndex,
                    "the compass retains visual priority over direction guides");
            }
            await Capture("navigation-compact-compass");
            string unchanged = Game.Battle.SaveJson();
            Click(compass); await Game.CurrentOrder; await Frames();
            Check(Game.MapCamera.Position.DistanceTo(World(MotherCell)) < .25f,
                "the real compact compass click reaches the Mothership beside a south-facing guide");
            Check(Game.Battle.SaveJson() == unchanged,
                "compact guide separation and compass navigation leave Core state unchanged");
        }
        finally
        {
            window.ContentScaleSize = originalContentScale;
            window.Size = originalWindow;
            UiScale.Set(originalScale, false);
            await Frames();
        }
    }
    private async Task PortraitAndContinue()
    {
        var originalWindow = GetWindow().Size;
        GetWindow().Size = new(618, 1400); await Frames();
        UiScale.Set(1.25f, false);
        Load(EligibleFixture());
        Game.SelectCell(MotherCell); LookAt(ChartCenter); await Frames();
        CheckCircle("portrait 618 x 1400 at 125%");
        var counsel = Nodes(Game.Hud).OfType<PanelContainer>().Single(p => p.Name == "InformationScroll");
        var compass = Game.NavigationHud.CompassButton;
        var counselRect = new Rect2(counsel.GetGlobalTransformWithCanvas().Origin, counsel.Size * UiScale.Value);
        var compassRect = new Rect2(compass.GetGlobalTransformWithCanvas().Origin, compass.Size * UiScale.Value);
        Check(counsel.IsVisibleInTree() && !counselRect.Intersects(compassRect),
            "the selected-object counsel leaves the portrait bottom compass unobstructed");
        string unchanged = Game.Battle.SaveJson();
        Click(compass); await Game.CurrentOrder; await Frames();
        Check(Game.MapCamera.Position.DistanceTo(World(MotherCell)) < .25f && Game.Hud.InformationVisible,
            "real portrait compass click works alongside the selected object's current counsel");
        await Capture("navigation-portrait-compass");
        Check(OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--save-file=")),
            "Continue testing requires the launcher's explicit disposable save path");
        LookAt(ChartCenter); await Frames();
        var savedCamera = Game.MapCamera.Position;
        Game.Saves.Write(Game.Battle, savedCamera, Game.MapCamera.Zoom.X);
        Game.ShowHome(); await Frames();
        Check(Game.Home.IsOpen && !Game.NavigationHud.Visible && !Game.NavigationHud.IsProcessing(),
            "returning to the title hides and stops navigation together with the battle layers");
        var resume = Nodes(Game.Home).OfType<Button>().Single(b => b.Name == "HomeContinue");
        Check(resume.IsVisibleInTree() && !resume.Disabled, "the real Continue button recognizes the disposable voyage");
        Click(resume); await Game.CurrentOrder; await Frames();
        var restored = Nodes(Game.NavigationHud).OfType<OffscreenNavigationArrow>().ToArray();
        int expectedOffscreen = restored.Count(a => !GetViewport().GetVisibleRect().HasPoint(Screen(a.Target.Cell)));
        Check(!Game.Home.IsOpen && Game.NavigationHud.Visible && restored.Length == 3
            && Game.NavigationHud.VisibleMarkerCount == expectedOffscreen
            && Game.NavigationHud.CompassButton.IsVisibleInTree(),
            "native Continue restores all three saved actions and shows guides only for actually offscreen targets");
        Check(Game.MapCamera.Position.DistanceTo(savedCamera) < .25f && Game.Battle.SaveJson() == unchanged,
            "Continue preserves the saved camera, world geometry, waiting crews, policies and RNG");
        Game.Saves.Delete();
        GetWindow().Size = originalWindow; UiScale.Set(1, false); await Frames();
    }
    private async Task CommittedOwnAttackAlerts()
    {
        Load(EligibleFixture()); await Frames();
        var battle = Game.Battle;
        int ownTown = battle.VillageAt(OwnTownCell)!.Id;
        Check(battle.EndTurn(Side.Player).Success, "real opponent turn begins for the staged assault");
        Game.Refresh(); await Frames();
        double beforeMother = battle.Find(1)!.Health;
        var attack = battle.Prepare(b => b.Attack(Side.Enemy, 5, 1));
        Check(attack.Result.Success && battle.PendingPresentation == attack && battle.Find(1)!.Health == beforeMother,
            "prepared opponent shot leaves live flagship health untouched");
        Game.Refresh(); await Frames();
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1) is null,
            "future projectile result cannot create a pre-impact flagship alert");
        attack.Impact("attack");
        double motherAfterImpact = battle.Find(1)!.Health;
        Check(motherAfterImpact < beforeMother, "actual Core impact reduces flagship health");
        Game.Refresh(); await Frames();
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1) is null,
            "partially committed presentation does not expose a premature attack guide");
        attack.Impact("attack");
        Check(battle.Find(1)!.Health == motherAfterImpact, "duplicate impact never applies additional damage");
        attack.Finish(); Game.Refresh(); await Frames();
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1) is not null
            && !Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1)!.IsVisibleInTree(),
            "completed committed attack records an own-only alert while opponent-turn input remains hidden");
        double beforeTown = battle.VillageAt(OwnTownCell)!.Health;
        var townAttack = battle.Prepare(b => b.AttackVillage(Side.Enemy, 6, ownTown));
        Check(townAttack.Result.Success && battle.VillageAt(OwnTownCell)!.Health == beforeTown,
            "prepared town salvo also leaves live town health unchanged");
        Game.Refresh();
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, -ownTown) is null,
            "uncommitted town damage has no red pointer");
        townAttack.Impact("village"); Game.Refresh();
        Check(battle.VillageAt(OwnTownCell)!.Health < beforeTown
            && Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, -ownTown) is null,
            "town impact changes health but waits for presentation completion before creating a guide");
        townAttack.Finish(); Game.Refresh();
        Check(battle.EndTurn(Side.Enemy).Success, "the real owner turn resumes after both finished assaults");
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        LookAt(ChartCenter); await Frames();
        Check(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1) is { } motherArrow && motherArrow.IsVisibleInTree()
            && Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, -ownTown) is { } townArrow && townArrow.IsVisibleInTree(),
            "committed assaults show red guides for only the human flagship and harbor");
        Check(Nodes(Game.NavigationHud).OfType<OffscreenNavigationArrow>().Where(a => a.Target.Kind == NavigationMarkerKind.OwnAttack)
            .All(a => a.Target.Id == 1 || a.Target.Id == -ownTown), "no rival identity or health enters the own-attack guide list");
        CheckCircle("committed attack guides"); await Capture("navigation-committed-attacks");
        string unchanged = battle.SaveJson();
        Click(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1)!); await Game.CurrentOrder; await Frames();
        Check(Game.MapCamera.Position.DistanceTo(World(MotherCell)) < .25f
            && Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, 1) is null,
            "clicking the flagship attack pointer flies there and acknowledges that cosmetic alert once");
        LookAt(ChartCenter); await Frames();
        Click(Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, -ownTown)!); await Game.CurrentOrder; await Frames();
        Check(Game.MapCamera.Position.DistanceTo(World(OwnTownCell)) < .25f
            && Game.NavigationHud.Marker(NavigationMarkerKind.OwnAttack, -ownTown) is null,
            "town attack pointer resolves to the actual city art anchor and acknowledges its alert");
        Check(battle.SaveJson() == unchanged, "acknowledging attacks changes no health, reward, actions, save or RNG");
        Load(EligibleFixture()); await Frames();
        Check(!Nodes(Game.NavigationHud).OfType<OffscreenNavigationArrow>().Any(a => a.Target.Kind == NavigationMarkerKind.OwnAttack),
            "replacing the voyage clears old attack alerts and establishes fresh health baselines");
    }
    private async Task HumanTurnSanctuaryLifetime()
    {
        Load(EligibleFixture());
        LookAt(MotherCell); await Frames(2);
        var battle = Game.Battle;
        string unchanged = battle.SaveJson();
        int starts = Game.TurnSanctuaryPulseCount;
        var flares = Nodes(Game).OfType<SanctuaryTurnPulse>().Where(p => !p.IsQueuedForDeletion()).ToArray();
        Check(flares.Length == 2 && Game.BoardView.ActiveSanctuaryPulseCount == 1 && Game.Fleet.ActiveSanctuaryPulseCount == 1,
            "a real human-turn start brightens only its one living town shrine and one flagship shrine");
        Check(Game.BoardView.PulseOwnedSanctuaries(Side.Enemy) == 0 && Game.Fleet.PulseOwnedSanctuaries(Side.Enemy) == 0,
            "the cosmetic ceremony API cannot pulse hidden rival sanctuaries");
        Check(flares.Any(p => p.IsProcessing()) && flares.Any(p => !p.IsProcessing()),
            "on-screen shrine flares process while the distant own shrine is already dormant");
        foreach (var flare in flares) Check(flare.CanDraw(), "every created turn flare belongs to a visible living human shrine");
        Game.BoardView.Hide(); Game.Fleet.Hide(); await Frames(2);
        Check(Game.BoardView.ProcessingSanctuaryPulseCount == 0 && Game.Fleet.ProcessingSanctuaryPulseCount == 0,
            "hidden world layers stop all shrine flare processing immediately");
        Game.BoardView.Show(); Game.Fleet.Show();
        for (int i = 0; i < 5; i++) Game.Refresh();
        LookAt(ChartCenter); await Frames(2);
        Check(Game.TurnSanctuaryPulseCount == starts
            && Game.BoardView.ProcessingSanctuaryPulseCount == 0 && Game.Fleet.ProcessingSanctuaryPulseCount == 0,
            "refresh and camera pans neither restart the human-turn ceremony nor animate offscreen shrines");
        await Delay(SanctuaryTurnPulse.Duration + .15); await Frames(2);
        Check(Game.BoardView.ActiveSanctuaryPulseCount == 0 && Game.Fleet.ActiveSanctuaryPulseCount == 0
            && !Nodes(Game).OfType<SanctuaryTurnPulse>().Any(), "finite shrine flares expire and release hidden as well as visible nodes");
        Game.Refresh(); await Frames(2);
        Check(Game.TurnSanctuaryPulseCount == starts && battle.SaveJson() == unchanged,
            "expired ceremonies stay finished for this turn and never alter gameplay, save state or RNG");
        Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success,
            "the next real human turn advances through the command facade");
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        LookAt(OwnTownCell); await Frames(2);
        Check(Game.TurnSanctuaryPulseCount == starts + 1 && Game.BoardView.ActiveSanctuaryPulseCount == 1
            && Game.Fleet.ActiveSanctuaryPulseCount == 1, "the next actual human turn launches one new owned shrine ceremony");
        for (int i = 0; i < 3; i++) Game.Refresh();
        Check(Game.TurnSanctuaryPulseCount == starts + 1, "repeated refreshes cannot duplicate the new turn's flare");
        await Capture("navigation-owned-shrine-flare");
        await Delay(SanctuaryTurnPulse.Duration + .15); await Frames(2);
    }
    public override async void _Ready()
    {
        try
        {
            Check(DisplayServer.GetName() != "headless", "navigation interaction and layering checks require the native renderer");
            await Frames(); UiScale.Set(1, false); Language.Set("en", false); UiHints.Set(false, false);
            await NativeMarkersAndWorldStories();
            await AnchoredSelectionAndSalvo();
            await CompactCompassClearance();
            await PortraitAndContinue();
            await CommittedOwnAttackAlerts();
            await HumanTurnSanctuaryLifetime();
            GD.Print($"PASS: {_checks} v020.7b native navigation circle/clicks, world paper anchors, committed own-attack alerts and finite owned shrine ceremonies.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
