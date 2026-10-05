using System;
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

public partial class Animation0207Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string text)
    {
        if (!good) throw new InvalidOperationException("Animation0207: " + text);
        _checks++;
    }
    private async Task Frame(int count = 3)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frame();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "capture " + suffix);
    }
    private void Load(BattleState battle, GridPosition at, float zoom = 2)
    {
        battle.SetGodEye(true);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        Game.LoadScenario(battle);
        Game.Home.Hide();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(at);
        Game.MapCamera.Zoom = Vector2.One * zoom;
        Game.MapCamera.ForceUpdateScroll();
        Game.Refresh();
    }

    private async Task TradeAndIdleGuns()
    {
        var board = new GameBoard(28, 20, p => p.Y == 5 && p.X is >= 3 and <= 21
            ? TerrainType.Land : TerrainType.Water, seed: 2067);
        var battle = new BattleState(board, Game.Battle.Rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(4, 10)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(25, 18)),
            (Side.Player, ShipClass.Lighthouse, new GridPosition(12, 8)),
            (Side.Player, ShipClass.CannonTower, new GridPosition(9, 8)),
            (Side.Player, ShipClass.AncientGun, new GridPosition(15, 8))
        }, Array.Empty<GridPosition>(), villageSpots: new[] { new GridPosition(6, 5), new(11, 5), new(16, 5) });
        var save = battle.CaptureSnapshot();
        save.Credits[0] = 100;
        save.Villages = save.Villages.Select(town => town with
        { Owner = Side.Player, Level = 3, Health = 15, Port = true, PortCell = new(town.Position.X + 1, 6) }).ToArray();
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        Load(battle, new(11, 7), 2.1f);
        await Frame(8);
        var traffic = Game.Ambience.TradeTraffic;
        traffic.SetProcess(false);
        traffic.Advance(20);
        Check(traffic.BoatCount == 3 && traffic.Skins.Distinct().Count() == 3, "three active distinct merchant skins");
        var at = traffic.Positions.ToArray();
        traffic.Advance(1);
        Check(at.Zip(traffic.Positions, (a, b) => a.DistanceTo(b)).All(d => d > 0), "merchant vessels visibly advance");
        int boats = traffic.BoatCount, departures = traffic.Departures;
        // A real staged restoration invalidates the Core network object, without changing its lanes.
        var command = battle.Prepare(b => b.UpgradeVillage(Side.Player, b.Villages[0].Id));
        Check(command.Result.Success, "staged town command fixture");
        command.Finish();
        Game.Refresh();
        await Frame();
        traffic.SetProcess(false);
        Check(traffic.BoatCount == boats && traffic.Departures == departures,
            "ordinary command cache restoration preserves ongoing merchants");
        traffic.Advance(1);
        Check(traffic.BoatCount == boats, "preserved traffic continues along unchanged lanes");
        Check(battle.PortBerth(battle.Villages[0]) == new GridPosition(7, 6), "port uses saved diagonal sea berth");
        var port = Game.BoardView.TradePortAnchor(battle.Villages[0]);
        var coast = Game.BoardView.Projection.GridToWorld(battle.Villages[0].Position);
        var water = Game.BoardView.Projection.GridToWorld(battle.PortBerth(battle.Villages[0]));
        Check(port.DistanceTo(water) < coast.DistanceTo(water), "diagonal dock joins actual coast toward sea berth");
        for (int id = 1; id <= 30; id++)
            Check(FleetView.IdleBatteryInterval(board.Seed, id, id) is >= 5 and < 10,
                "cosmetic battery turn interval remains five through ten seconds");
        Check(MathF.Abs(FleetView.LighthouseBeamAngle(20, 3) - FleetView.LighthouseBeamAngle(0, 3) - 3.2f) < .001f,
            "light cone rotates slowly at constant speed");
        string untouched = battle.SaveJson();
        float gun = Game.Fleet.PassiveBatteryHeading(4), mortar = Game.Fleet.PassiveBatteryHeading(5);
        await ToSignal(GetTree().CreateTimer(12.4), SceneTreeTimer.SignalName.Timeout);
        Check(MathF.Abs(Game.Fleet.PassiveBatteryHeading(4) - gun) > .04f,
            "native cannon battery chooses and slowly turns to another heading");
        Check(MathF.Abs(Game.Fleet.PassiveBatteryHeading(5) - mortar) > .04f,
            "native mortar battery turns independently");
        Check(battle.SaveJson() == untouched, "ritual light, idle guns and traffic do not alter gameplay or RNG");
        await Capture("ports-lighthouse-guns");
        Game.Ambience.Hide();
        await Frame();
        Check(!traffic.IsProcessing(), "hidden map pauses merchant processing");
        Game.Ambience.Show();
    }

    private async Task Balloon()
    {
        var target = new GridPosition(7, 6);
        var battle = new BattleState(new GameBoard(22, 20, _ => TerrainType.Water), Game.Battle.Rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)),
            (Side.Player, ShipClass.Kolonel, new GridPosition(5, 6)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(19, 17)),
            (Side.Enemy, ShipClass.Balloon, target),
            (Side.Enemy, ShipClass.Garrison, new GridPosition(7, 7)),
            (Side.Player, ShipClass.Fishing, new GridPosition(6, 7))
        }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var save = battle.CaptureSnapshot();
        save.Ships.Single(s => s.Kind == ShipClass.Balloon).Health = 1;
        save.Ships.Single(s => s.Kind == ShipClass.Garrison).Health = 2;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
        Load(battle, target, 2.7f);
        await Frame(8);
        var order = battle.Prepare(b => b.Attack(Side.Player, 2, 4));
        Check(order.Result.Success && order.Result.BalloonCrashes.Count == 1, "anti-air command owns one staged crash");
        double initial = battle.Find(6)!.Health;
        var animation = Game.Fleet.Animate(order.Result, presentation: order);
        for (int frames = 0; Game.Fleet.SinkingCount == 0 && !animation.IsCompleted && frames < 240; frames++)
            await Frame(1);
        Check(Game.Fleet.WreckPartCount(WreckPartKind.Cloth) == 8
            && Game.Fleet.WreckPartCount(WreckPartKind.Strut) == 4
            && Game.Fleet.WreckPartCount(WreckPartKind.Basket) == 1,
            "curved envelope gores, rigging and solid wicker basket fall as separate parts");
        Check(Game.Fleet.WrecksOpaque && Game.Fleet.BalloonCrashImpactCount == 0 && battle.Find(6)!.Health == initial,
            "solid falling wreck precedes area damage");
        await Capture("balloon-tearing");
        for (int frames = 0; Game.Fleet.BalloonCrashImpactCount == 0 && !animation.IsCompleted && frames < 240; frames++)
            await Frame(1);
        Check(Game.Fleet.BalloonCrashImpactCount == 1 && battle.Find(6)!.Health == initial - 2,
            "crash commits exact two area damage at sea impact");
        Check(battle.Find(5) is null, "two-HP nearby hull is destroyed by the same impact");
        await Capture("balloon-impact");
        await animation;
        Check(order.Complete && Game.Fleet.WreckPieceCount == 0 && Game.Fleet.SinkingCount == 0,
            "balloon and victim debris are released after staged order");
        Check(Game.Fleet.BalloonCrashImpactCount == 1 && battle.Find(6)!.Health == initial - 2,
            "final presentation cannot apply crash damage twice");
        await Capture("balloon-aftermath");
    }

    private async Task Shrines()
    {
        // Native canvas capture shows every nation ritual and pirate roof smoke at one common scale.
        var painter = new BoardTerrainLayer
        {
            Position = new(35, 80),
            ZIndex = 100,
            DrawWorld = canvas =>
        {
            canvas.DrawRect(new Rect2(0, 0, 1170, 330), new("315767"));
            int index = 0;
            foreach (var color in Enum.GetValues<FleetColor>())
            {
                var origin = new Vector2(95 + index++ * 157, 245);
                Vector2 P(float x, float y, float z) => origin + new Vector2((x - y) * 2.4f, (x + y) * 1.15f - z * 2.4f);
                FactionSanctuaryArt.Draw(canvas, P, color);
                FactionSanctuaryArt.DrawEffects(canvas, P, color, 2.3f, index);
                canvas.DrawString(ThemeDB.FallbackFont, origin + new Vector2(-25, 45), color.ToString(), fontSize: 17,
                    modulate: new("f0e5bd"));
            }
            Vector2 Pirate(float x, float y, float z) => new Vector2(1040, 245)
                + new Vector2((x - y) * 2.4f, (x + y) * 1.15f - z * 2.4f);
            FactionSanctuaryArt.DrawPirate(canvas, Pirate);
            FactionSanctuaryArt.DrawPirateEffects(canvas, Pirate, 2.3f, 99);
        }
        };
        var layer = new CanvasLayer { Layer = 100 };
        AddChild(layer);
        layer.AddChild(painter);
        await Capture("rituals-and-pirate-smoke");
        layer.QueueFree();
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            await TradeAndIdleGuns();
            await Balloon();
            await Shrines();
            GD.Print($"PASS: {_checks} v020.7 retained merchants, diagonal ports, ritual effects, idle batteries and staged solid balloon crash.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
