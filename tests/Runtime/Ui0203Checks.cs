using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class Ui0203Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("v020.3 UI: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (var child in root.GetChildren())
            foreach (var node in Nodes(child)) yield return node;
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task Frames(int count = 3) { for (int i = 0; i < count; i++) await Frame(); }
    private Vector2 Screen(GridPosition cell) => GetViewport().GetCanvasTransform()
        * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(cell));
    private void Click(Vector2 point)
    {
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left,
            Position = point, GlobalPosition = point, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left,
            Position = point, GlobalPosition = point, Pressed = false }, true);
    }
    private async Task Hover(GridPosition cell)
    {
        var point = Screen(cell);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        await Frames();
    }
    private void Focus(GridPosition cell)
    {
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(cell);
        Game.MapCamera.ForceUpdateScroll();
    }
    private RewardPapyrusHud BoundReward => Nodes(Game).OfType<RewardPapyrusHud>().Single();
    private static void ClaimFixtureAwards(BattleState battle)
    {
        foreach (var award in battle.PendingAwards.ToArray())
            if (!battle.ClaimAward(Side.Player, award.Id).Success)
                throw new InvalidOperationException("Could not settle a fixture's initial encounter.");
    }
    private static InputEventKey KeyEvent(Key key, bool pressed = true, bool echo = false) =>
        new() { PhysicalKeycode = key, Keycode = key, Pressed = pressed, Echo = echo };

    private void CheckKeyboard()
    {
        bool enabled = true;
        int turns = 0, repairs = 0;
        var camera = new MapCamera { MapBounds = new Rect2(-2000, -2000, 4000, 4000), Zoom = Vector2.One };
        AddChild(camera);
        var input = new MapInput { Camera = camera, KeyboardEnabled = () => enabled };
        AddChild(input);
        input.SetProcess(false);
        input.SetProcessInput(false);
        input.SetProcessUnhandledInput(false);
        input.EndTurnRequested += () => turns++;
        input.RepairRequested += () => repairs++;
        input._Input(KeyEvent(Key.Space));
        input._Input(KeyEvent(Key.Space, echo: true));
        input._Input(KeyEvent(Key.R));
        input._Input(KeyEvent(Key.R, echo: true));
        Check(turns == 1 && repairs == 1, "Space and R trigger once; key echo cannot repeat commands");
        input._Input(KeyEvent(Key.W));
        input._Input(KeyEvent(Key.D));
        input._Process(.02);
        Check(camera.Position.X > 0 && camera.Position.Y < 0,
            "physical WASD pans diagonally in the expected direction");
        Check(Math.Abs(camera.Position.Length() - MapCamera.KeyboardPanPixelsPerSecond * .02) < .05,
            "diagonal keyboard speed matches cardinal keyboard speed");
        var before = camera.Position;
        enabled = false;
        input._Input(KeyEvent(Key.Space)); input._Input(KeyEvent(Key.R)); input._Process(.02);
        Check(camera.Position == before && turns == 1 && repairs == 1,
            "a modal/busy/finished gate blocks camera and command shortcuts");
        enabled = true; input._Process(.02);
        Check(camera.Position == before, "closing a modal cannot resume stale held keys");
        input._Input(KeyEvent(Key.Left)); input._Process(.02);
        Check(camera.Position.X < before.X, "arrow keys pan the map");
        input._Notification((int)NotificationApplicationFocusOut);
        before = camera.Position; input._Process(.02);
        Check(camera.Position == before, "focus loss clears keyboard movement");
        input.Free(); camera.Free();
    }

    private async Task CheckObjectCounsel()
    {
        Game.Battle.SetGodEye(true);
        Game.Refresh();
        foreach (var terrain in new[] { TerrainType.Water, TerrainType.Land })
        {
            var cell = Game.Battle.Board.Tiles.First(tile => tile.Terrain == terrain
                && Game.Battle.At(tile.Position) is null && !Game.Battle.Villages.Any(v => v.Position == tile.Position)
                && Game.Battle.TreasuryAt(tile.Position) is null && !Game.Battle.FishSpots.Contains(tile.Position)
                && !Game.Battle.Shoals.Contains(tile.Position)).Position;
            Game.SelectCell(cell);
            Game.Hud.ShowInformation(Game.Battle, cell);
            await Frame();
            Check(!Game.Hud.InformationVisible && Game.Hud.InformationText.Length == 0,
                "terrain alone never opens a counsel parchment: " + terrain);
            Check(!Nodes(Game.Hud).OfType<RadialPapyrus>().Single(n => n.Name == "ActionPapyrus").Visible,
                "terrain alone has no command ring");
        }
        var mother = Game.Battle.Mothership(Side.Player)!;
        Game.SelectCell(mother.Position);
        Game.Hud.ShowInformation(Game.Battle, mother.Position);
        await Frame();
        Check(Game.Hud.InformationVisible && Game.Hud.InformationText.Contains("Progress cells"),
            "object counsel explains its progress cells");
        Check(Nodes(Game.Hud).OfType<Label>().Single(n => n.Name == "FleetUsage").Text
            == $"{Game.Battle.FleetUsed(Side.Player)}/{Game.Battle.FleetCapacity(Side.Player)}",
            "top fleet count matches Core's dynamic used/capacity contract");
        Check(!Nodes(Game.Hud).OfType<SectorButton>().Single(n => n.Name == "ActionScuttle").Visible,
            "a flagship cannot be scuttled through the UI");
        var fisher = Game.Battle.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Fishing);
        Game.SelectCell(fisher.Position);
        await Frame();
        Check(Nodes(Game.Hud).OfType<SectorButton>().Single(n => n.Name == "ActionScuttle").Visible,
            "a selected own fishing ship exposes its separate scuttle action");
        if (Game.Battle.Rules.FishingLighthouses && Game.Battle.Rules.LighthousesEnabled)
        {
            var yard = Nodes(Game.Hud).OfType<SectorButton>().Single(n => n.Name == "ActionBuild");
            Check(yard.Visible, "a fishing ship exposes its Lighthouse construction parchment");
            yard.EmitSignal(BaseButton.SignalName.Pressed);
            var builds = Nodes(Game.Hud).OfType<SectorButton>().Where(n => n.Visible
                && n.Name.ToString().StartsWith("Build")).ToArray();
            var expected = Game.Battle.Rules.FishingCannonTowers
                ? new[] { "BuildCannonTower", "BuildLighthouse" } : new[] { "BuildLighthouse" };
            Check(builds.Select(b => b.Name.ToString()).OrderBy(n => n).SequenceEqual(expected.OrderBy(n => n)),
                "support construction choices match this voyage's tower and lighthouse policy");
        }
        Game.Hud.CloseMenus();
    }

    private async Task Capture(string suffix)
    {
        string? argument = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="));
        if (argument is null || DisplayServer.GetName() == "headless") return;
        await Frame(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(argument[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
    }

    private async Task CheckRewards()
    {
        var hud = new RewardPapyrusHud();
        AddChild(hud);
        int claims = 0;
        string? id = null;
        hud.ClaimRequested += receipt => { claims++; id = receipt; };
        string original = Game.Battle.SaveJson();
        hud.ShowNation("test-nation", "Captain Harukaze", new Color("72549b"));
        var button = Nodes(hud).OfType<Button>().Single(n => n.Name == "ClaimReward");
        Check(hud.IsOpen && button.Disabled && claims == 0, "showing a nation neither claims nor changes its reward");
        button.EmitSignal(BaseButton.SignalName.Pressed);
        Check(claims == 0, "a rolled receipt cannot be activated prematurely");
        hud._Process(.31);
        await Frame();
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            DevAncientNaval.Presentation.UI.Language.Set(locale, persist: false);
            await Frame();
            Check(button.Tr(button.Text).ToString() == DevAncientNaval.Presentation.UI.Language.Translate(button.Text),
                "reward claim caption renders in " + locale);
            await Capture("nation-" + locale);
        }
        var point = button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = point, GlobalPosition = point, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = point, GlobalPosition = point, Pressed = false }, true);
        Check(claims == 1 && id == "test-nation", "a real mouse claim emits the original pending receipt identity");
        button.EmitSignal(BaseButton.SignalName.Pressed);
        hud.ShowNation("test-nation", "Captain Harukaze", new Color("72549b"));
        button.EmitSignal(BaseButton.SignalName.Pressed);
        Check(claims == 1 && button.Disabled, "repeated clicks and refresh cannot submit a receipt twice");
        hud.Close();
        Check(!hud.IsOpen && hud.PendingId is null, "closing a committed receipt releases its modal state");
        hud.ShowHeavenly("test-heaven", 8);
        hud._Process(.31); await Frame();
        Check(button.Text == "Claim 8 Thors", "heavenly offering shows its own exact reward amount");
        await Capture("heavenly");
        Check(Game.Battle.SaveJson() == original, "reward presentation never grants currency or mutates Core");
        hud.Close(); hud.Free();
        DevAncientNaval.Presentation.UI.Language.Set("en", persist: false);
    }

    private async Task CheckBoundRewards()
    {
        Check(Game.Battle.Rules.DeferredRewards, "the current voyage defers reward currency until claim");
        var battle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)),
                (Side.Player, ShipClass.Garrison, new GridPosition(4, 3)),
                (Side.Enemy, ShipClass.Kolonel, new GridPosition(8, 3)) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        Game.LoadScenario(battle); Game.FastChecks = true; Focus(new(4, 3));
        Check(!battle.HasMet(Side.Enemy) && battle.PendingAwards.Count == 0,
            "the encounter fixture begins with an unseen nation and no receipt");
        int credits = battle.Credits(Side.Player);
        Game.SelectCell(new(4, 3)); Game.SelectCell(new(6, 3)); await Game.CurrentOrder; await Frame();
        Check(battle.HasMet(Side.Enemy) && battle.Credits(Side.Player) == credits,
            "actual sailing discovers a nation without automatically paying its award");
        var receipt = battle.PendingAwards.Single();
        Check(receipt.Kind == AwardKind.Nation && receipt.Amount == 5
            && BoundReward.IsOpen && BoundReward.PendingId == receipt.Id,
            "Main binds the fresh five-Thor Core receipt to the actual modal parchment");
        BoundReward._Process(.31); await Frame();
        string before = battle.SaveJson(); int? selection = Game.SelectedShipId;
        foreach (Key key in new[] { Key.Space, Key.R })
        {
            GetViewport().PushInput(KeyEvent(key), true);
            GetViewport().PushInput(KeyEvent(key, pressed: false), true);
        }
        Click(Screen(new(2, 2)));
        Game.SelectCell(new(2, 2));
        await Frames();
        Check(battle.SaveJson() == before && Game.SelectedShipId == selection && !Game.Busy,
            "the real reward modal blocks Space, R, mouse selection and selection dispatch");
        await Capture("bound-nation-pending");

        bool persistence = OS.GetCmdlineUserArgs().Any(arg => arg.StartsWith("--save-file="));
        if (persistence)
        {
            Game.ShowHome();
            Check(Game.Saves.Read().Battle.PendingAwards.Single().Id == receipt.Id,
                "leaving the voyage saves the still-unclaimed receipt");
            await Game.ContinueSession(); await Frame();
            battle = Game.Battle;
            Check(battle.Credits(Side.Player) == credits && BoundReward.IsOpen
                && BoundReward.PendingId == receipt.Id,
                "actual Continue restores the same receipt without granting currency");
        }
        BoundReward._Process(.31); await Frame();
        var claim = Nodes(BoundReward).OfType<Button>().Single(n => n.Name == "ClaimReward");
        Click(claim.GetGlobalRect().GetCenter()); await Game.CurrentOrder; await Frame();
        Check(!BoundReward.IsOpen && battle.PendingAwards.Count == 0
            && battle.Credits(Side.Player) == credits + 5,
            "a real Main claim grants five Thors once and closes its modal");
        claim.EmitSignal(BaseButton.SignalName.Pressed); Game.Refresh(); await Game.CurrentOrder;
        Check(battle.Credits(Side.Player) == credits + 5 && !BoundReward.IsOpen,
            "stale receipt clicks and refresh cannot pay a second reward");
        if (persistence)
        {
            Game.ShowHome(); await Game.ContinueSession(); await Frame();
            Check(Game.Battle.PendingAwards.Count == 0 && !BoundReward.IsOpen
                && Game.Battle.Credits(Side.Player) == credits + 5,
                "saved claimed voyage resumes without recreating its paid reward");
            Game.Saves.Delete();
        }
        await Capture("bound-nation-claimed");
    }

    private async Task CheckMovementPreview()
    {
        var battle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(8, 8)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)),
                (Side.Enemy, ShipClass.Kolonel, new GridPosition(10, 8)) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        ClaimFixtureAwards(battle);
        var mobility = battle.CaptureSnapshot();
        mobility.Ships.Single(ship => ship.Id == 1).MobilityUpgrade = true;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(mobility));
        Game.LoadScenario(battle); Focus(new(8, 8));
        var mother = battle.Find(1)!;
        Game.SelectCell(mother.Position); await Frames();
        await Hover(new(8, 9));
        Check(Game.BoardView.PreviewPath.Count > 1, "native water hover presents a legal pre-action sailing route");
        var original = mother.Position;
        await Hover(new(10, 8));
        Check(mother.Position == original && !Game.Busy && Game.BoardView.PreviewPath.Count == 0,
            "hovering an enemy previews combat without sailing into its occupied tile");
        Game.SelectCell(new(9, 8)); await Game.CurrentOrder;
        Check(mother.Position == new GridPosition(9, 8) && mother.HasMoved && mother.CanMove,
            $"the flagship moved while retaining a sailing allowance before firing: pos={mother.Position}, moved={mother.HasMoved}, canMove={mother.CanMove}, remaining={mother.MovementRemainingUnits}, selected={Game.SelectedShipId}, busy={Game.Busy}, mode={Game.Mode}");
        await Hover(new(8, 7)); // Free water inside the ring's empty center, outside enemy threat.
        Check(Game.BoardView.PreviewPath.Count > 1, "route cache presents the flagship's remaining legal movement");
        Game.SelectCell(new(10, 8)); await Game.CurrentOrder; await Frame();
        Check(mother.MovementLocked && !mother.CanMove && mother.AttacksUsed > 0,
            "moving then firing locks the flagship's remaining movement");
        await Hover(new(8, 7));
        Check(Game.BoardView.PreviewPath.Count == 0 && Game.BoardView.Reachable.Count == 0,
            "native hover cannot resurrect cached sailing routes after the movement lock");
        await Capture("locked-movement-preview");
    }

    private async Task CheckTargetMasks()
    {
        foreach (bool lethal in new[] { false, true })
        {
            var battle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)),
                    (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)),
                    (Side.Enemy, ShipClass.Kolonel, new GridPosition(10, 8)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            ClaimFixtureAwards(battle);
            if (lethal)
            {
                var snapshot = battle.CaptureSnapshot();
                snapshot.Ships.Single(ship => ship.Id == 4).Health = 7;
                battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
            }
            Game.LoadScenario(battle); Focus(new(8, 8)); Game.SelectCell(new(8, 8)); await Frames(8);
            Check(battle.FindObserved(Side.Player, 4) is not null && battle.CanAttack(3, 4),
                "the optical target is eligible for its real attack contour");
            var glow = Nodes(Game.Fleet).OfType<Sprite2D>().Single(node => node.Name == "AttackContour4");
            var shader = glow.Material as ShaderMaterial;
            Check(glow.IsVisibleInTree() && shader is not null
                && shader.GetShaderParameter("mask_only").AsBool()
                && shader.GetShaderParameter("lethal").AsBool() == lethal,
                "native target contour uses the silhouette-only shader with correct lethal state");
            var mask = Nodes(Game.Fleet).OfType<SubViewport>().Single(node => node.Name == "AttackMask4");
            Check(mask.Size == new Vector2I(340, 380) && mask.TransparentBg && glow.Texture == mask.GetTexture(),
                "attack contour samples a bounded transparent hull mask rather than repainting the ship");
            if (DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = mask.GetTexture().GetImage();
                bool hull = false, empty = false;
                for (int y = 0; y < image.GetHeight(); y += 13)
                    for (int x = 0; x < image.GetWidth(); x += 13)
                    {
                        float alpha = image.GetPixel(x, y).A;
                        hull |= alpha > .1f; empty |= alpha < .01f;
                    }
                Check(hull && empty, "the native mask contains both drawn hull ink and transparent surroundings");
            }
            await Capture(lethal ? "target-lethal-mask" : "target-nonlethal-mask");
        }
        var hidden = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(22, 22)),
                (Side.Player, ShipClass.Togus, new GridPosition(5, 5)),
                (Side.Enemy, ShipClass.Kolonel, new GridPosition(10, 5)) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        Game.LoadScenario(hidden); Focus(new(7, 5)); Game.SelectCell(new(5, 5)); await Frames(8);
        Check(hidden.FindObserved(Side.Player, 4) is null && hidden.CanAttack(3, 4)
            && hidden.Vision.IsRadarContact(Side.Player, new(10, 5)),
            "the mortar fixture has an attackable anonymous radar contact");
        Check(!Nodes(Game.Fleet).OfType<Sprite2D>().Any(node => node.Name == "AttackContour4" && node.IsVisibleInTree())
            && !Nodes(Game.Fleet).Any(node => node.Name == "AttackMask4"),
            "a radar-only target has neither a visible hull glow nor a silhouette mask leaking its shape");
        await Capture("target-hidden-radar");
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            CheckKeyboard();
            Check(SectorButton.Outer - SectorButton.Inner == 36, "command parchment uses the thinner 36-pixel band");
            Check(PapyrusGrain.Texture.GetWidth() == 64 && PapyrusGrain.Texture.GetHeight() == 64,
                "all parchment wear shares one bounded 64-pixel texture");
            await CheckObjectCounsel();
            await CheckRewards();
            await CheckBoundRewards();
            await CheckMovementPreview();
            await CheckTargetMasks();
            GD.Print($"PASS: {_checks} v020.3 UI, keyboard, object counsel and pending-reward checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
