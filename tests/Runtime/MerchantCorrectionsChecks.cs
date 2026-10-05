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

/// <summary>Native cosmetic traffic across several limited Core links and live construction.</summary>
public partial class MerchantCorrectionsChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string message)
    {
        if (!good) throw new InvalidOperationException("Merchant corrections: " + message);
        _checks++;
    }
    private async Task Frames(int count = 3)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "capture " + suffix);
    }
    private BattleState Fixture()
    {
        var board = new GameBoard(34, 20, p => p.Y == 5 && p.X is >= 3 and <= 29
            ? TerrainType.Land : TerrainType.Water, seed: 20715);
        var battle = new BattleState(board, Game.Battle.Rules, new[]
        {
            (Side.Player, ShipClass.Mothership, new GridPosition(17, 7)),
            (Side.Enemy, ShipClass.Mothership, new GridPosition(31, 18)),
            (Side.Player, ShipClass.Lighthouse, new GridPosition(11, 6)),
            (Side.Player, ShipClass.Lighthouse, new GridPosition(16, 6)),
            (Side.Player, ShipClass.Lighthouse, new GridPosition(21, 6))
        }, Array.Empty<GridPosition>(), villageSpots: new[]
        { new GridPosition(6, 5), new(26, 5), new(16, 5) });
        var snapshot = battle.CaptureSnapshot();
        snapshot.Credits[0] = 500;
        snapshot.Villages = snapshot.Villages.Select(town => town with
        {
            Owner = Side.Player, Level = 3, Health = 15,
            Port = town.Position.X != 16,
            PortCell = town.Position.X != 16 ? new GridPosition(town.Position.X, 6) : null
        }).ToArray();
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        battle.SetGodEye(true);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        return battle;
    }
    private (string[] Ids, Vector2[] At, float[] Sailed, Dictionary<int, float> Due) Record(TradeTraffic traffic)
        => (traffic.BoatIds.ToArray(), traffic.Positions.ToArray(), traffic.SailedDistances.ToArray(),
            traffic.DepartureTimes.ToDictionary(item => item.Key, item => item.Value));
    private void Preserved(TradeTraffic traffic,
        (string[] Ids, Vector2[] At, float[] Sailed, Dictionary<int, float> Due) before, string reason)
    {
        Check(traffic.BoatIds.SequenceEqual(before.Ids), reason + " retains canvas identities");
        Check(traffic.Positions.SequenceEqual(before.At), reason + " cannot teleport or rewind the hull");
        Check(traffic.SailedDistances.SequenceEqual(before.Sailed), reason + " retains sailed distance");
        Check(before.Due.All(item => traffic.DepartureTimes.TryGetValue(item.Key, out float due) && due == item.Value),
            reason + " retains every existing port departure time");
    }
    private void CheckMotion(TradeTraffic traffic, BattleState battle, int steps, float delta)
    {
        string unchanged = battle.SaveJson();
        for (int step = 0; step < steps; step++)
        {
            var at = traffic.BoatIds.Zip(traffic.Positions).ToDictionary(item => item.First, item => item.Second);
            traffic.Advance(delta);
            for (int boat = 0; boat < traffic.BoatCount; boat++)
            {
                string id = traffic.BoatIds[boat];
                var position = traffic.Positions[boat];
                if (at.TryGetValue(id, out var previous))
                    Check(previous.DistanceTo(position) <= TradeTraffic.Speed * delta + .02f,
                        "motion follows its lane at the preserved twelve-pixel speed");
                var cell = Game.BoardView.Projection.WorldToGrid(position);
                Check(battle.Board.Contains(cell) && battle.Board.GetTile(cell).Terrain != TerrainType.Land
                    && !battle.IsForbidden(cell), "tiny hull stays in navigable water");
                foreach (var beacon in battle.Ships.Where(ship => ship.Definition.Class == ShipClass.Lighthouse))
                {
                    var anchor = Game.BoardView.Projection.GridToWorld(beacon.Position) + FleetView.LighthouseOffset;
                    Check(((position - anchor) / new Vector2(28, 19)).LengthSquared() >= .998f,
                        "merchants skirt the lighthouse rock instead of stopping there");
                }
            }
        }
        Check(battle.SaveJson() == unchanged, "departures, sailing and arrivals leave all saved gameplay and RNG untouched");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            Game.FastChecks = true;
            var battle = Fixture();
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new GridPosition(16, 6));
            Game.MapCamera.Zoom = Vector2.One * 1.1f;
            Game.MapCamera.ForceUpdateScroll();
            Game.Refresh();
            await Frames(8);
            var traffic = Game.Ambience.TradeTraffic;
            traffic.SetProcess(false);
            var ports = battle.Villages.Where(town => town.HasPort).ToArray();
            var network = battle.TradeRoutes(Side.Player);
            Check(battle.Rules.Ports.MaximumRouteLength == 6, "fixture uses the released six-cell individual link limit");
            Check(!network.Routes.Any(route => route.First() == battle.PortBerth(ports[0])
                && route.Last() == battle.PortBerth(ports[1]) || route.Last() == battle.PortBerth(ports[0])
                && route.First() == battle.PortBerth(ports[1])), "cities twenty cells apart have no direct Core link");
            Check(network.Routes.All(route => route.Count - 1 <= 6), "Core links retain their original maximum length");
            string unchanged = battle.SaveJson();
            Check(traffic.DepartureTimes.Values.All(due => due is >= 12 and <= 20), "first departures use the original 12–20 second interval");
            traffic.Advance(11);
            Check(traffic.Departures == 0, "no port sends a boat early");
            traffic.Advance(9);
            Check(traffic.Departures == 2 && traffic.BoatCount == 2, "both distant cities dispatch through the beacon chain");
            Check(traffic.Routes.All(route => route.Length > 6
                && new[] { new GridPosition(11, 6), new(16, 6), new(21, 6) }.All(route.Contains)),
                "a single voyage traverses all three intermediate lighthouses");
            Check(traffic.Voyages.All(voyage => ports.Any(port => port.Id == voyage.Source)
                && ports.Any(port => port.Id == voyage.Destination) && voyage.Source != voyage.Destination),
                "only port cities can be destinations");
            Check(battle.SaveJson() == unchanged, "initial dispatch leaves simulation random state and save bytes unchanged");
            await Capture("three-beacon-voyage");
            traffic.SetProcess(false);

            var before = Record(traffic);
            var upgrade = battle.Prepare(state => state.UpgradeVillage(Side.Player, ports[0].Id));
            Check(upgrade.Result.Success, "real staged city upgrade succeeds");
            upgrade.Finish();
            traffic.Refresh();
            traffic.SetProcess(false);
            Preserved(traffic, before, "identical Core cache replacement");

            before = Record(traffic);
            var city = battle.Villages.Single(town => !town.HasPort);
            var portOrder = battle.Prepare(state => state.BuildPort(Side.Player, city.Id));
            Check(portOrder.Result.Success, "real new port command succeeds");
            portOrder.Finish();
            traffic.Refresh();
            traffic.SetProcess(false);
            Preserved(traffic, before, "opening an intermediate city port");
            Check(traffic.DepartureTimes.Count == 3, "new port gets its own schedule without resetting the old pair");
            float newDelay = traffic.DepartureTimes[city.Id] - traffic.ElapsedSeconds;
            Check(newDelay is >= 12 and <= 20, "new port uses twelve through twenty seconds from construction");

            before = Record(traffic);
            var mother = battle.Mothership(Side.Player)!;
            var beaconOrder = battle.Prepare(state => state.BuildLighthouse(Side.Player, mother.Id, new GridPosition(17, 6)));
            Check(beaconOrder.Result.Success, "real new lighthouse command succeeds");
            beaconOrder.Finish();
            traffic.Refresh();
            traffic.SetProcess(false);
            Preserved(traffic, before, "building another beacon in the existing route");
            Check(traffic.RouteRepairs > 0, "changed lighthouse footprint repairs the remaining voyage rather than discarding it");
            CheckMotion(traffic, battle, 500, .25f);
            Check(traffic.Arrivals >= 2, "the two original long voyages arrive at their cities after all beacon bypasses");
            Check(traffic.LastArrivalCity is { } arrived && battle.Villages.Any(town => town.HasPort && town.Id == arrived),
                "arrival is recorded at a city instead of at an intermediate beacon");
            Check(traffic.Departures > 3 && traffic.Skins.Distinct().Count() == 3,
                "construction cannot stop recurring departures or the three hull skins");
            Check(traffic.BoatCount <= TradeTraffic.MaximumBoats, "traffic population remains bounded");
            await Capture("persistent-trade-after-construction");
            traffic.SetProcess(false);
            battle.SetGodEye(false);
            traffic.Refresh();
            traffic.SetProcess(false);
            unchanged = battle.SaveJson();
            traffic.Advance(.25f);
            var activeNames = traffic.BoatIds.ToHashSet();
            Check(traffic.GetChildren().OfType<MerchantCanvas>().Where(canvas => activeNames.Contains(canvas.Name.ToString()))
                .All(canvas => canvas.Visible == battle.Vision.IsVisible(Side.Player,
                    Game.BoardView.Projection.WorldToGrid(canvas.Position))),
                "merchant hulls follow optical fog without radar or God's eye leaks");
            Check(battle.SaveJson() == unchanged, "optical visibility decisions do not explore terrain or change simulation RNG");
            Game.Ambience.Hide();
            await Frames();
            Check(!traffic.IsProcessing(), "hidden world traffic does not process");
            var hidden = Record(traffic);
            await ToSignal(GetTree().CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
            Preserved(traffic, hidden, "hidden world");
            Game.Ambience.Show();
            var resumed = Record(traffic);
            await Frames(10);
            traffic.SetProcess(false);
            Check(traffic.BoatIds.Zip(traffic.Positions).Any(item =>
            {
                int prior = Array.IndexOf(resumed.Ids, item.First);
                return prior >= 0 && resumed.At[prior].DistanceTo(item.Second) > .01f;
            }), "showing the world resumes actual native frame-driven sailing");
            Check(battle.SaveJson() == unchanged, "native frame-driven traffic retains save and simulation RNG bytes");
            GD.Print($"PASS: {_checks} native merchant chain, arrival, construction, schedule and RNG checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
