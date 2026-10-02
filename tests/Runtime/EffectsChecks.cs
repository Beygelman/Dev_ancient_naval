using System;
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
/// <summary>Real engine checks for visual salvos, delayed impact, movement and
/// bounded environment effects. Core damage must be independent of pellet count.</summary>
public partial class EffectsChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
    private void Check(bool ok, string name)
    {
        if (!ok)
            throw new Exception(name);
        _checks++;
    }

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private void Reveal()
    {
        foreach (var tile in Game.Battle.Board.Tiles)
            Game.Battle.Vision.RevealCombat(Side.Player, tile.Position);
        Game.Battle.Vision.Recompute(Game.Battle.Ships, 1, Game.Battle.Villages);
        Game.Refresh();
    }

    private async Task Capture(string name)
    {
        string? arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + name + ".png")) == Error.Ok, "Capture " + name);
    }

    public override async void _Ready()
    {
        try
        {
            await Wait(.05f);
            await Salvos();
            await FleetGallery();
            await RotationAndSchools();
            await OutpostAndReef();
            await PresentedSinking();
            GD.Print($"PASS: {_checks} effects checks (salvo counts, unchanged damage, smooth movement, wildlife, veterans).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task Salvos()
    {
        var rules = Game.Battle.Rules;
        foreach (var(kind, count)in new[]
        {
            (ShipClass.Garrison, 1),
            (ShipClass.Invader, 3),
            (ShipClass.Kolonel, 3),
            (ShipClass.Mothership, 2),
            (ShipClass.Togus, 1)
        }

        )
        {
            var origin = new GridPosition(8, 8);
            var target = new GridPosition(kind == ShipClass.Togus ? 12 : kind is ShipClass.Garrison or ShipClass.Invader ? 9 : 10, 8);
            var setup = kind == ShipClass.Mothership ? new[]
            {
                (Side.Player, kind, origin),
                (Side.Enemy, ShipClass.Mothership, target)
            }

            : new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(0, 0)),
                (Side.Enemy, ShipClass.Mothership, target),
                (Side.Player, kind, origin)
            };
            Game.LoadScenario(new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, setup, Array.Empty<GridPosition>()));
            Reveal();
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(9, 8));
            Game.MapCamera.Zoom = Vector2.One * 1.6f;
            Game.MapCamera.ForceUpdateScroll();
            Game.FastChecks = false;
            await Wait(.35f);
            var ship = Game.Battle.At(origin)!;
            var defender = Game.Battle.At(target)!;
            double hp = defender.Health, damage = Game.Battle.Damage(ship, defender);
            Game.SelectCell(origin);
            Game.SelectCell(target);
            if (Game.Hud.SalvoChoiceVisible)
            {
                await Wait(.3f);
                SectorButton FindSingle(Node node)
                {
                    if (node is SectorButton { Name: var name } button && name == "SingleShot") return button;
                    foreach (var child in node.GetChildren())
                    {
                        var found = FindSingle(child);
                        if (found is not null) return found;
                    }
                    return null!;
                }
                var single = FindSingle(Game.Hud);
                var point = single.GlobalPosition + single.IconCenter;
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left, Position=point, GlobalPosition=point, Pressed=true }, true);
                GetViewport().PushInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left, Position=point, GlobalPosition=point, Pressed=false }, true);
            }
            float deckBefore = Game.Fleet.DeckAngle(ship.Id);
            var order = Game.CurrentOrder;
            Check(!Game.Fleet.TurningForShot && Game.Fleet.ActiveProjectileCount == 0, "The camera leads the shot before the battery aims");
            for (int frame = 0; frame < 180 && !Game.Fleet.TurningForShot && !order.IsCompleted; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Game.Fleet.TurningForShot && Game.Fleet.ActiveProjectileCount == 0, "The battery aims after the camera arrives and before any cannonball launches");
            Check(Game.Battle.Find(2)!.Health == hp, "Actual Core health remains unchanged until the last cannonball lands");
            await Capture("salvo-" + kind);
            await order;
            Check(Game.Battle.Find(2)!.Health == hp - damage, "Salvo damage is applied once after impact regardless of pellet count");
            Check(Game.Fleet.CompletedSalvos.Count > 0 && Game.Fleet.CompletedSalvos[0] == (count, count), $"{kind}: all {count} visual cannonballs launch and land");
            Check(Game.Fleet.ActiveProjectileCount == 0, "Every shell completes its flight");
            Check(kind == ShipClass.Togus ? Math.Abs(Game.Fleet.DeckAngle(ship.Id) - deckBefore) < .001 : Math.Abs(Game.Fleet.DeckAngle(ship.Id) - deckBefore) > .01, "Mortar aims its barrel; cannon ships turn broadside");
            await Capture("impact-" + kind);
        }
    }

    private async Task RotationAndSchools()
    {
        Game.CancelOrder();
        var mother = Game.Battle.Mothership(Side.Player)!;
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(mother.Position);
        Game.MapCamera.Zoom = Vector2.One * 3;
        Game.MapCamera.ForceUpdateScroll();
        for (int step = 0; step < 36; step++)
        {
            float yaw = step * Mathf.Tau / 36;
            var floor = DeckProjection.Point(8, -6, 0, yaw, 1.23f);
            var roof = DeckProjection.Point(8, -6, 12, yaw, 1.23f);
            Check(Math.Abs(roof.X - floor.X) < .001 && Math.Abs(roof.Y - floor.Y + 12 * 1.23f) < .001, "Deck rotation never changes upright building height");
            Game.Fleet.SetPreviewHeading(mother.Id, yaw);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (step % 6 == 0)
                await Capture("city-yaw-" + step);
        }

        Game.Fleet.SetPreviewHeading(mother.Id, 0);
        Check(Game.Ambience.AnimatedFishCount >= Game.Battle.KnownFish(Side.Player).Count() * 3, "Visible resource tiles contain swimming schools, three fish per ordinary site");
        Check(Game.Ambience.WildlifeCount <= 41, "Increased wildlife remains bounded");
    }

    private async Task OutpostAndReef()
    {
        var townCell = new GridPosition(6, 5);
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, p => p == townCell ? TerrainType.Land : TerrainType.Water), Game.Battle.Rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Enemy, ShipClass.Fishing, new GridPosition(8, 5)), (Side.Player, ShipClass.FishingDock, new GridPosition(5, 7)) }, new[] { new GridPosition(4, 7) }, villageSpots: new[] { townCell }));
        var town = Game.Battle.Villages.Single();
        for (int hit = 0; hit < 2; hit++)
        {
            Check(Game.Battle.AttackVillage(Side.Player, 1, town.Id).Success, "Outpost fixture lowers neutral defenses");
            Game.Battle.EndTurn(Side.Player);
            Game.Battle.EndTurn(Side.Enemy);
        }

        Check(Game.Battle.CaptureVillage(Side.Player, town.Id).Success && Game.Battle.FortifyVillage(Side.Player, town.Id).Success, "Captured village raises its outpost");
        for (int turn = 0; turn < 2; turn++)
        {
            Game.Battle.EndTurn(Side.Player);
            Game.Battle.EndTurn(Side.Enemy);
        }

        Check(town.Level == 2, "Outpost fixture reaches firing level");
        Reveal();
        Game.CancelOrder();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(6, 6));
        Game.MapCamera.Zoom = Vector2.One * 2;
        Game.MapCamera.ForceUpdateScroll();
        Check(Game.Ambience.AnimatedFishCount >= 17, "Dock reef hosts fourteen fish alongside a three-fish resource school");
        var prepared = Game.Battle.Prepare(b => b.EndTurn(Side.Player));
        var shot = prepared.Result;
        Check(Game.Battle.Find(3)!.Health == 5, "Outpost damage remains pending during gun flight");
        Check(shot.OutpostShots is { Count: 1 } && shot.OutpostShots[0].Damage == 2, "End turn commits one level-two outpost shot");
        var animation = Game.Fleet.Animate(shot, presentation: prepared);
        bool sawProjectile = false;
        for (int frame = 0; frame < 90 && !animation.IsCompleted; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Game.Fleet.ProjectilePosition is not null)
            {
                sawProjectile = true;
                break;
            }
        }

        Check(sawProjectile, "Automatic village fire plays its projectile");
        await Capture("outpost-and-reef");
        await animation;
        Check(Game.Fleet.ProjectilePosition is null && Game.Battle.Find(3)!.Health == 3, "Village animation leaves committed damage unchanged");
    }

    private async Task FleetGallery()
    {
        var town = new GridPosition(10, 10);
        var rules = Game.Battle.Rules;
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, p => p.X >= 10 && p.X <= 12 && p.Y >= 10 && p.Y <= 12 ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 7)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Kolonel, new GridPosition(8, 7)), (Side.Player, ShipClass.Invader, new GridPosition(11, 7)), (Side.Player, ShipClass.Togus, new GridPosition(14, 7)), (Side.Player, ShipClass.Garrison, new GridPosition(9, 10)), (Side.Player, ShipClass.Fishing, new GridPosition(15, 10)), (Side.Enemy, ShipClass.Fishing, new GridPosition(8, 6)), (Side.Enemy, ShipClass.Fishing, new GridPosition(9, 6)), (Side.Enemy, ShipClass.Fishing, new GridPosition(10, 6)) }, new[] { new GridPosition(8, 10), new GridPosition(14, 10) }, villageSpots: new[] { town }));
        // Earn the veteran hull through real combat rather than mutating visual state.
        for (int enemy = 8; enemy <= 10; enemy++)
        {
            Reveal();
            Check(Game.Battle.Attack(Side.Player, 3, enemy).Success, "Veterancy preparation shot");
            Check(Game.Battle.Attack(Side.Player, 3, enemy).Success, "Veterancy preparation sinking");
            Game.Battle.EndTurn(Side.Player);
            Game.Battle.EndTurn(Side.Enemy);
        }

        Check(Game.Battle.Find(3)!.IsVeteran, "Gallery Kolonel earns its quarterdeck and figurehead");
        var village = Game.Battle.Villages.Single();
        Game.Battle.AttackVillage(Side.Player, 6, village.Id);
        Game.Battle.EndTurn(Side.Player);
        Game.Battle.EndTurn(Side.Enemy);
        Game.Battle.AttackVillage(Side.Player, 6, village.Id);
        Game.Battle.EndTurn(Side.Player);
        Game.Battle.EndTurn(Side.Enemy);
        Check(Game.Battle.CaptureVillage(Side.Player, village.Id).Success, "Defeated town captured and retained");
        Reveal();
        Game.CancelOrder();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(10, 8));
        Game.MapCamera.Zoom = Vector2.One * 1.12f;
        Game.MapCamera.ForceUpdateScroll();
        Game.Ambience.SpawnGulls(new(8, 10), 3);
        Game.Ambience.SpawnDolphin(new(14, 10));
        await Wait(1.5f);
        Check(Game.Ambience.WildlifeCount >= 4, "Visible fish waters can host gulls and a dolphin");
        await Capture("fleet-and-wildlife");
        Game.SelectCell(new(5, 7));
        var start = Game.Fleet.Projection.GridToWorld(new(5, 7));
        var result = Game.Battle.Move(Side.Player, 1, new(6, 7));
        Check(result.Success, "Mothership movement fixture");
        var animation = Game.Fleet.Animate(result);
        await Wait(.10f);
        float early = Game.Fleet.AnimatedPosition.DistanceTo(start);
        await Wait(.18f);
        float middle = Game.Fleet.AnimatedPosition.DistanceTo(start);
        Check(middle - early > early * 1.5f, "Large ship accelerates smoothly after leaving rest");
        await Capture("mothership-underway");
        await animation;
        await Wait(.15f);
        Check(Game.Fleet.EffectCount > 0 && Game.Fleet.EffectCount <= 412, "Movement and impact effects remain bounded");
        Check(ShipVisualProfile.For(ShipClass.Mothership).TravelSeconds > ShipVisualProfile.For(ShipClass.Kolonel).TravelSeconds && ShipVisualProfile.For(ShipClass.Kolonel).TravelSeconds > ShipVisualProfile.For(ShipClass.Garrison).TravelSeconds, "Larger hulls travel with slower inertia");
        await Capture("fleet-at-rest");
    }
}
