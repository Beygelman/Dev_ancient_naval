using System;
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

public partial class Action0209Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        _checks++;
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private void Fixture()
    {
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water),
            Game.Battle.Rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)),
                (Side.Player, ShipClass.Invader, new GridPosition(8, 8)),
                (Side.Enemy, ShipClass.Fishing, new GridPosition(9, 8)),
                (Side.Player, ShipClass.Garrison, new GridPosition(6, 8))
            }, Array.Empty<GridPosition>());
        foreach (var tile in battle.Board.Tiles)
            battle.Vision.RevealCombat(Side.Player, tile.Position);
        battle.Vision.Recompute(battle.Ships, 1, battle.Villages);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        Game.LoadScenario(battle);
        Game.Refresh();
        Game.FastChecks = false;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(7, 8));
        Game.MapCamera.Zoom = Vector2.One;
        Game.MapCamera.ForceUpdateScroll();
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Fixture();
            var camera = Game.MapCamera.Position;
            Game.SelectCell(new(8, 8));
            Game.SelectCell(new(9, 8));
            var attack = Game.CurrentOrder;
            Check(Game.Battle.Find(4) is null && Game.Battle.PendingPresentation is null,
                "Player sinking commits immediately and leaves a stable save boundary");
            Check(!Game.Busy && !attack.IsCompleted, "Own effects play with commands unlocked");
            var saved = Game.Battle.SaveJson();
            for (int i = 0; i < 8; i++) await Frame();
            Check(Game.MapCamera.Position == camera, "Own attack never auto-focuses");
            Game.SelectCell(new(6, 8));
            Game.SelectCell(new(6, 9));
            var movement = Game.CurrentOrder;
            Check(Game.Battle.Find(5)!.Position == new GridPosition(6, 9),
                "Next movement executes while the previous attack was still animating");
            Check(attack.IsCompletedSuccessfully, "Next order settles the previous visual awaiter");
            Game.Fleet.FinishAnimation();
            await movement;
            Check(Game.Battle.Find(4) is null, "Skipping visuals never resurrects a sunk target");
            var continued = BattleState.LoadJson(saved);
            Check(continued.Find(4) is null && continued.Find(3)!.AttacksUsed == 1,
                "Continue retains one committed attack");
            Check(Game.Fleet.ActiveProjectileCount == 0 && Game.Fleet.SinkingCount == 0,
                "Settled effects leave no shells or wrecks");

            // Camera permission is explicit; own effects and replay share a renderer.
            int focuses = 0;
            var focus = Game.Fleet.FocusTarget;
            Game.Fleet.FocusTarget = _ => { focuses++; return Task.CompletedTask; };
            foreach (bool replay in new[] { false, true })
            {
                var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water),
                    Game.Battle.Rules, new[] {
                        (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                        (Side.Enemy, ShipClass.Mothership, new GridPosition(10, 8)),
                        (Side.Player, ShipClass.Invader, new GridPosition(8, 8))
                    }, Array.Empty<GridPosition>());
                battle.EndTurn(Side.Player);
                foreach (var tile in battle.Board.Tiles)
                    battle.Vision.RevealCombat(Side.Player, tile.Position);
                battle.Vision.Recompute(battle.Ships, 1, battle.Villages);
                Game.LoadScenario(battle);
                focuses = 0;
                var command = battle.Prepare(b => b.Attack(Side.Enemy, 2, 3));
                Check(command.Result.Success, "Opponent replay fixture");
                var animation = Game.Fleet.Animate(command.Result, presentation: command, opponentReplay: replay);
                Check(battle.Find(3)!.Health == 10 && battle.PendingPresentation is not null,
                    "Staged replay damage waits for its visual impact");
                Check(replay ? focuses > 0 : focuses == 0,
                    "Only explicitly marked opponent replay requests camera focus");
                Game.Fleet.FinishAnimation();
                await animation;
                Check(command.Complete && battle.Find(3)!.Health < 10,
                    "Settling replay commits exactly once");
            }
            Game.Fleet.FocusTarget = focus;

            Fixture();
            Game.SelectCell(new(8, 8));
            Game.SelectCell(new(9, 8));
            var old = Game.CurrentOrder;
            var previous = Game.Battle;
            Fixture();
            Check(old.IsCompletedSuccessfully && !ReferenceEquals(previous, Game.Battle),
                "World replacement settles the old order before changing references");
            Check(Game.Battle.Find(4) is not null && Game.Battle.Find(4)!.Health == 5,
                "Old visual continuations cannot damage the replacement world");
            await CityPanelChecks();
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "Capture");
            }
            GD.Print($"PASS: {_checks} action0209 camera, unlocked orders, save and replacement checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
    private T Find<T>(Node root, string name) where T : Node
    {
        if (root is T result && root.Name.ToString() == name) return result;
        foreach (var child in root.GetChildren())
        {
            var found = Find<T>(child, name);
            if (found is not null) return found;
        }
        return null!;
    }
    private void Click(Button button)
    {
        var point = button is SectorButton sector ? sector.GlobalPosition + sector.IconCenter
            : button.GetGlobalRect().GetCenter();
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left,
                Position = point, GlobalPosition = point, Pressed = pressed }, true);
    }
    private async Task CityPanelChecks()
    {
        foreach (var locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, persist: false);
            var cell = new GridPosition(8, 8);
            var battle = new BattleState(new GameBoard(20, 20, p => p == cell ? TerrainType.Land : TerrainType.Water),
                Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(17, 17)) },
                Array.Empty<GridPosition>(), villageSpots: new[] { cell });
            var saved = battle.CaptureSnapshot();
            saved.Villages[0] = saved.Villages[0] with { Owner = Side.Player, Level = 5, Health = 25 };
            saved.Credits[0] = 100;
            Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(saved)));
            Game.FastChecks = true;
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(cell);
            Game.MapCamera.Zoom = Vector2.One;
            Game.MapCamera.ForceUpdateScroll();
            Game.Refresh();
            Game.SelectCell(cell);
            await Frame(); await Frame();
            var open = Find<SectorButton>(Game.Hud, "ActionInfrastructure");
            Check(open.IsVisibleInTree() && !open.Disabled, "City infrastructure has a native command: " + locale);
            var ring = (RadialPapyrus)open.GetParent();
            for (int frame = 0; frame < 180 && (ring.Reveal < 1 || ring.IsProcessing()); frame++) await Frame();
            Check(ring.Reveal == 1 && !ring.IsProcessing(), "Command parchment fully unfolds before native input: " + locale);
            Click(open);
            await Frame(); await Frame();
            var panel = Game.GetNode<CityInfrastructureHud>("CityInfrastructureHud");
            Check(panel.IsOpen, "Native pointer opens infrastructure sheet: " + locale);
            var close = Find<Button>(panel, "InfrastructureClose");
            Check(close.IsVisibleInTree() && close.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().End.Y,
                "Modal footer remains reachable: " + locale);
            var camera = Game.MapCamera.Position;
            var point = close.GetGlobalRect().GetCenter();
            GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown,
                Position = point, GlobalPosition = point, Pressed = true }, true);
            Check(Game.MapCamera.Position == camera && Game.MapCamera.Zoom == Vector2.One,
                "Modal wheel cannot move or zoom the world: " + locale);
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..].Replace(".png", "-city-" + locale + ".png")) == Error.Ok,
                    "City panel capture " + locale);
            }
            var install = Find<Button>(panel, "Facility_0_TradeYard");
            int presses = 0;
            install.Pressed += () => presses++;
            Click(install);
            await Frame(); await Frame();
            await Game.CurrentOrder;
            Check(presses == 1, "Native facility button receives exactly one press: " + locale);
            Check(!panel.IsOpen && Game.Battle.Villages[0].Facility1 == CityFacility.TradeYard,
                $"Native facility install: {locale}, open={panel.IsOpen}, facility={Game.Battle.Villages[0].Facility1}, selected={Game.SelectedVillageId}, busy={Game.Busy}");
        }
        Language.Set("en", persist: false);
    }

}

