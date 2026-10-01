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
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Exercise actual retained canvases at a full circle of headings and real health changes.</summary>
public partial class FleetArtChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool value, string name)
    {
        if (!value)
            throw new InvalidOperationException(name);
        _checks++;
    }

    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }

    private async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok, "Capture " + suffix);
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            HealthTransitions();
            await GalleryAndHeadings();
            await RealImpactAndRepair();
            await RadarHealthPrivacy();
            GD.Print($"PASS: {_checks} fleet-art checks (upright 360-degree hulls, paired clay seals, damage/healing, fog).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private void HealthTransitions()
    {
        foreach (var (health, stage) in new[] { (100d, 0), (75.01, 0), (75d, 1), (50.01, 1), (50d, 2), (25.01, 2), (25d, 3), (1d, 3), (0d, 3) })
            Check(AmphoraBadgeArt.Stage(health, 100) == stage, "Exact clay threshold " + health);
        var animation = new AmphoraHealthAnimation();
        animation.Observe(100, 100, 0);
        Check(!animation.Active(0), "A loaded healthy ship does not pretend to take damage");
        animation.Observe(50, 100, 1);
        var damage = animation.Motion(1.03f);
        Check(!damage.Healing && damage.PreviousStage == 0 && damage.Stage == 2 && damage.Shake.Length() > .1f, "Damage checks the pottery with a sharp impulse");
        animation.Observe(100, 100, 2);
        Check(animation.Motion(2.1f).Healing && animation.Motion(2.1f).Shake == Vector2.Zero && animation.Motion(2.1f).Restore < 1, "Repair restores fragments gently");
        Check(animation.Motion(2.11f).Flash > .1f && animation.Motion(2.54f).Flash > .1f, "Healing pottery glints twice");
        Check(!animation.Active(3) && animation.Motion(3).Stage == 0, "Restoration settles into the final intact seal");
        Check(WorldAmbience.CloudThickness > 0 && WorldAmbience.CloudAltitude > 100, "Connected cloud solids retain height above their shadows");
    }

    private async Task GalleryAndHeadings()
    {
        var setup = new List<(Side, ShipClass, GridPosition)>
        {
            (Side.Player, ShipClass.Mothership, new(5, 8)),
            (Side.Enemy, ShipClass.Mothership, new(27, 20))
        };
        var kinds = Enum.GetValues<ShipClass>().Where(k => k != ShipClass.Mothership).ToArray();
        for (int i = 0; i < kinds.Length; i++)
            setup.Add((Side.Player, kinds[i], new(9 + i % 4 * 3, 8 + i / 4 * 4)));
        var battle = new BattleState(new GameBoard(31, 24, _ => TerrainType.Water, seed: 731), Game.Battle.Rules, setup, Array.Empty<GridPosition>());
        Game.LoadScenario(battle);
        Game.CancelOrder();
        await Frame();
        await Frame();
        Check(Game.Fleet.DisplayedHealthStage(2) is null, "A hidden enemy never receives an HP seal");
        battle.SetGodEye(true);
        Game.Refresh();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(12, 12));
        Game.MapCamera.Zoom = Vector2.One * .9f;
        Game.MapCamera.ForceUpdateScroll();
        await Frame();
        await Frame();
        Check(Game.Fleet.HealthBadgeCount == setup.Count, "Every visible class, including docks and balloons, owns a paired seal");
        await Capture("fleet-gallery");
        for (int step = 0; step < 24; step++)
        {
            float yaw = step * Mathf.Tau / 24;
            foreach (var ship in battle.Ships)
            {
                var profile = ShipVisualProfile.For(ship.Definition.Class);
                var floor = DeckProjection.Point(7, -6, 0, yaw, profile.Size, profile.DeckWidth);
                var roof = DeckProjection.Point(7, -6, 12, yaw, profile.Size, profile.DeckWidth);
                Check(MathF.Abs(floor.X - roof.X) < .001f && MathF.Abs(roof.Y - floor.Y + 12 * profile.Size) < .001f, "Upright class at heading " + step);
                var a = DeckProjection.Point(0, 0, 0, yaw, profile.Size, profile.DeckWidth);
                var b = DeckProjection.Point(10, 0, 0, yaw, profile.Size, profile.DeckWidth);
                var c = DeckProjection.Point(0, 10, 0, yaw, profile.Size, profile.DeckWidth);
                Check(MathF.Abs((b - a).Cross(c - a)) > 15, "Deck floor never folds at any yaw");
                Game.Fleet.SetPreviewHeading(ship.Id, yaw);
            }
            await Frame();
            if (step % 6 == 0)
                await Capture("heading-" + step);
        }

        foreach (var ship in battle.Ships)
            Game.Fleet.SetPreviewHeading(ship.Id, 0);
        // Persisted health thresholds produce the same four shapes on every reload.
        var snapshot = battle.CaptureSnapshot();
        var ratios = new[] { 1d, .75, .5, .25 };
        for (int i = 0; i < snapshot.Ships.Length; i++)
            snapshot.Ships[i].Health = battle.Find(snapshot.Ships[i].Id)!.MaxHealth * ratios[i % 4];
        Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot)));
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(12, 12));
        Game.MapCamera.Zoom = Vector2.One * .9f;
        Game.MapCamera.ForceUpdateScroll();
        await Frame();
        await Frame();
        for (int i = 0; i < snapshot.Ships.Length; i++)
            Check(Game.Fleet.DisplayedHealthStage(snapshot.Ships[i].Id) == i % 4, "Loaded fractional health chooses stage " + i % 4);
        await Capture("four-clay-stages");
    }

    private async Task RealImpactAndRepair()
    {
        var battle = new BattleState(new GameBoard(22, 22, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)), (Side.Enemy, ShipClass.Mothership, new GridPosition(19, 19)),
                (Side.Player, ShipClass.Kolonel, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(9, 8)) }, Array.Empty<GridPosition>());
        battle.SetGodEye(true);
        battle.EndTurn(Side.Player);
        Game.LoadScenario(battle);
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(8, 8));
        Game.MapCamera.Zoom = Vector2.One * 2.2f;
        Game.MapCamera.ForceUpdateScroll();
        await Frame();
        await Frame();
        var prepared = battle.Prepare(b => b.Attack(Side.Enemy, 4, 3));
        Check(prepared.Result.Success, "Real health impact fixture attacks legally");
        var animation = Game.Fleet.Animate(prepared.Result, presentation: prepared);
        bool sawDamage = false;
        for (int frame = 0; frame < 150 && !animation.IsCompleted; frame++)
        {
            await Frame();
            if (Game.Fleet.DisplayedHealthMotion(3) is { } motion && motion.PreviousStage < motion.Stage && !motion.Healing)
            {
                sawDamage = true;
                await Capture("clay-impact");
                break;
            }
        }
        await animation;
        Check(sawDamage && Game.Fleet.DisplayedHealthStage(3) == 1, "Actual cannon impact cracks both retained seals only after landing");

        battle.EndTurn(Side.Enemy);
        Game.Refresh();
        await Frame();
        await Wait(.9f);
        var damaged = battle.CaptureSnapshot();
        damaged.Ships.Single(s => s.Id == 3).Health = 7;
        Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(damaged)));
        Game.FastChecks = false;
        Game.SelectCell(new(8, 8));
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(8, 8));
        Game.MapCamera.Zoom = Vector2.One * 2.2f;
        Game.MapCamera.ForceUpdateScroll();
        await Frame();
        await Frame();
        Check(Game.Fleet.DisplayedHealthStage(3) == 2, "Repair begins with broken pottery");
        var repair = Game.RepairSelected();
        await Wait(.1f);
        await Frame();
        var healing = Game.Fleet.DisplayedHealthMotion(3);
        Check(healing is { Healing: true } && healing.Value.PreviousStage > healing.Value.Stage && healing.Value.Shake == Vector2.Zero, "Actual repair restores both seals without a damage shake");
        await Capture("clay-restoration");
        await repair;
        await Wait(.9f);
        await Frame();
        Check(Game.Fleet.DisplayedHealthStage(3) == AmphoraBadgeArt.Stage(Game.Battle.Find(3)!.Health, Game.Battle.Find(3)!.MaxHealth), "Repair settles to the authoritative health stage");
        var hpPoint = FleetView.HealthAnchor(Game.BoardView.Projection.GridToWorld(new(8, 8)), ShipClass.Kolonel);
        Check(hpPoint.X > Game.BoardView.Projection.GridToWorld(new(8, 8)).X && hpPoint.Y < Game.BoardView.Projection.GridToWorld(new(8, 8)).Y, "Damage and repair rise from the upper-right pottery digits");
        await Capture("restored-kolonel");
    }

    private async Task RadarHealthPrivacy()
    {
        var battle = new BattleState(new GameBoard(24, 24, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(21, 21)),
                (Side.Player, ShipClass.Togus, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Fishing, new GridPosition(12, 8)) }, Array.Empty<GridPosition>());
        Game.LoadScenario(battle);
        await Frame();
        await Frame();
        Check(!battle.Vision.IsVisible(Side.Player, new(12, 8)) && battle.Vision.IsRadarContact(Side.Player, new(12, 8)), "Privacy fixture sees only an anonymous radar contact");
        var prepared = battle.Prepare(b => b.Attack(Side.Player, 3, 4));
        Check(prepared.Result.Success && prepared.Result.Shots is { Count: > 0 } && !prepared.Result.Shots[0].TargetVisibleToPlayer && prepared.Result.Shots[0].TargetSunk,
            "A legal anonymous mortar kill stays visually hidden");
        var animation = Game.Fleet.Animate(prepared.Result, presentation: prepared);
        while (!animation.IsCompleted)
        {
            await Frame();
            Check(!Game.Fleet.AnimatedShipIds.Contains(4) && Game.Fleet.DisplayedHealthStage(4) is null, "Even a destroyed radar contact never leaks its hull or health seal");
        }
        await animation;
    }
}
