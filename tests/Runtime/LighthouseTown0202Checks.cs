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

public partial class LighthouseTown0202Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("Lighthouse/town 020.2: " + reason);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
    }
    private static (GridPosition, GridPosition) Canonical((GridPosition From, GridPosition To) edge) =>
        edge.From.Y < edge.To.Y || edge.From.Y == edge.To.Y && edge.From.X < edge.To.X ? edge : (edge.To, edge.From);

    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var wallCorners = BoardView.TownWallCorners;
            Check(wallCorners.Length == 4 && wallCorners.Distinct().Count() == 4,
                "closed city wall has four distinct corners reserved for four towers");
            Check(wallCorners[0].X == wallCorners[2].X && wallCorners[1].Y == wallCorners[3].Y &&
                Enumerable.Range(0, 4).Select(i => wallCorners[i].DistanceTo(wallCorners[(i + 1) % 4])).Distinct().Count() == 1,
                "city wall is an equal-sided isometric square rather than an open irregular fence");
            Check(BoardView.TownWallHeight(1) > 10 && BoardView.TownWallHeight(5) > BoardView.TownWallHeight(1),
                "tall city walls gain height along with the settlement");
            for (int seed = 0; seed < 32; seed++)
            {
                var homes = TownLayout.Build(seed);
                Check(homes.All(h => MathF.Abs(h.Position.X) < 25 && h.Position.Y < -9),
                    "all homes gather compactly behind the monument and foreground mills");
                Check(homes.Length == 13 && homes.Select(h => h.Style).Distinct().Count() == 5,
                    "five-level lattice has distinct house styles and thirteen reserved sites");
                Check(homes.Select(h => h.Position.Y).SequenceEqual(homes.Select(h => h.Position.Y).OrderBy(y => y)),
                    "houses are drawn from their back ground anchors toward the front");
                for (int i = 0; i < homes.Length; i++)
                    for (int j = i + 1; j < homes.Length; j++)
                        Check(!homes[i].GroundBounds.Intersects(homes[j].GroundBounds),
                            "different homes never share a ground footprint after their lattice warp");
                var sanctuary = new Rect2(new Vector2(-5, -5.5f), new Vector2(17, 9));
                Check(homes.All(h => !h.GroundBounds.Intersects(sanctuary)), "a dedicated open plaza stays reserved for the faction monument");
            }
            var spots = Enumerable.Range(0, 5).Select(i => new GridPosition(4 + i * 4, 8)).Append(new GridPosition(34, 8)).ToArray();
            var board = new GameBoard(40, 23, p => p.X is >= 2 and <= 35 && p.Y is >= 5 and <= 8 ? TerrainType.Land : TerrainType.Water, seed: 731);
            var battle = new BattleState(board, Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 12)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(35, 20)),
                    (Side.Player, ShipClass.Lighthouse, new GridPosition(12, 12)),
                    (Side.Player, ShipClass.Lighthouse, new GridPosition(20, 14)) },
                Array.Empty<GridPosition>(), villageSpots: spots);
            battle.SetPlayerColor(FleetColor.Blue);
            battle.SetGodEye(true);
            var save = battle.CaptureSnapshot();
            save.Villages = save.Villages.Select((v, i) => v with { Owner = i == 5 ? Side.Enemy : Side.Player, Level = i == 5 ? 3 : i + 1,
                Health = (i == 5 ? 3 : i + 1) * 5, Fortified = i >= 1, Port = i is >= 2 and <= 4 }).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(new(12, 10));
            Game.MapCamera.Zoom = Vector2.One * .7f;
            Game.MapCamera.ForceUpdateScroll();
            for (int i = 0; i < 8; i++) await Frame();
            Check(Game.BoardView.TownRasterCount == battle.Villages.Count * 2 && Game.BoardView.TownRasterIdle && Game.BoardView.TownRasterBounded,
                "static town artwork uses two bounded one-shot textures around its animated middle layer");
            var depth = Game.Fleet.GetNode<Node2D>("IslandDepth");
            foreach (var town in battle.Villages)
            {
                var placement = Game.BoardView.VillagePlacement(town);
                Check(placement.Scale > 0 && placement.Scale <= .82f,
                    "each city has a compact nonempty land fit shared by every visual layer");
                foreach (var point in Game.BoardView.VillageFootprintPoints(town))
                    Check(Game.BoardView.VillageSoilContains(town, point),
                        "complete town footprint including walls/tower bases stays off beach and sea at town " + town.Id +
                        ": " + Game.BoardView.VillageSoilDiagnostic(town, point));
                var canvas = depth.GetNode<Node2D>("Town" + town.Id);
                var children = canvas.GetChildren().Select(c => c.Name.ToString()).ToArray();
                Check(children.Length == 2 && children[0] == "TownLife" + town.Id && children[1] == "TownForeground" + town.Id,
                    "native canvas order paints back houses, animated mills, then front wall and wheat");
                var life = canvas.GetNode<Node2D>("TownLife" + town.Id);
                Check(life.Position == placement.Offset && life.Scale == Vector2.One * placement.Scale,
                    "moving flag and mill blades exactly share the retained town's inset placement");
                var homes = Game.BoardView.TownHomes(town).Take(3 + town.Level * 2).ToArray();
                Check(homes.Length == 3 + town.Level * 2, "each level adds actual distinct drawn buildings");
                foreach (var mill in BoardView.TownMills(town))
                {
                    var footprint = new Rect2(mill + new Vector2(-5, 0), new Vector2(11, 1));
                    Check(homes.All(h => !h.GroundBounds.Intersects(footprint)), "windmill bases remain separate from all houses");
                }
            }
            var network = battle.TradeRoutes(Side.Player);
            var drawn = Game.BoardView.RenderedTradeEdges.Select(Canonical).ToArray();
            Check(network.Routes.Sum(r => Math.Max(0, r.Count - 1)) > network.Edges.Count && drawn.Length > 0,
                "fixture contains genuinely overlapping logical port and lighthouse routes");
            Check(drawn.Distinct().Count() == drawn.Length && drawn.ToHashSet().SetEquals(network.Edges.Select(Canonical)),
                "all shortest-lane edges appear in the renderer once, including shared stretches");
            Check(Game.BoardView.TradeDashSegmentCount > 0, "merged white maritime lanes retain their dashed drawing");
            Check(Game.Fleet.LighthouseDrawCount >= 2, "lighthouses use their dedicated rock/tower model instead of ship hulls");
            var light = battle.Ships.First(s => s.Definition.Class == ShipClass.Lighthouse);
            var hull = Game.Fleet.GetNode<Node2D>("Hull" + light.Id);
            var initial = hull.Position;
            for (int i = 0; i < 20; i++) await Frame();
            Check(hull.Position == initial && hull.Rotation == 0, "fixed lighthouses never bob or roll on their rocks");
            Check(FleetView.LighthouseOffset.X < 0 && FleetView.LighthouseOffset.Y < 0,
                "lighthouse artwork occupies the upper-left part of its sea cell");
            Check(Game.Fleet.GetNode<Node2D>("HealthAmphorae" + light.Id).Position == initial + FleetView.HealthBadgeOffset(ShipClass.Lighthouse),
                "unrotated health badges follow the tower, above the sea-cell corner");
            await Capture("shared-trade-network");
            foreach (var town in battle.Villages)
            {
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(town.Position);
                Game.MapCamera.Zoom = Vector2.One * 2.4f;
                Game.MapCamera.ForceUpdateScroll();
                await Frame();
                await Capture(town.Owner == Side.Enemy ? "enemy-town" : "town-level-" + town.Level);
            }
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(light.Position) + FleetView.LighthouseOffset;
            Game.MapCamera.Zoom = Vector2.One * 3;
            Game.MapCamera.ForceUpdateScroll();
            await Frame();
            await Capture("lighthouse");
            int dashCount = Game.BoardView.TradeDashSegmentCount;
            int bakes = Game.BoardView.TownRasterBuildCount;
            Game.MapCamera.Pan(new Vector2(30, 15));
            await Frame();
            Check(Game.BoardView.TradeDashSegmentCount == dashCount, "panning reuses retained merged lane geometry");
            Check(Game.BoardView.TownRasterBuildCount == bakes && Game.BoardView.TownRasterIdle,
                "panning and environmental animation never repaint static town textures");
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(spots[0]);
            Game.MapCamera.ForceUpdateScroll();
            Check(battle.FortifyVillage(Side.Player, battle.Villages[0].Id).Success, "fixture can add a visible town wall");
            Game.Refresh();
            for (int i = 0; i < 5; i++) await Frame();
            Check(Game.BoardView.TownRasterBuildCount == bakes + 2 && Game.BoardView.TownRasterIdle,
                "fortification updates both halves of the closed square wall while every other town remains cached");
            var hiddenTown = battle.Villages.Last();
            battle.Vision.RevealCombat(Side.Player, hiddenTown.Position);
            battle.Vision.Recompute(battle.Ships, battle.TurnSerial, battle.Villages);
            battle.SetGodEye(false);
            Game.Refresh();
            for (int i = 0; i < 3; i++) await Frame();
            battle.Vision.ClearCombatFlashes();
            battle.Vision.Recompute(battle.Ships, battle.TurnSerial, battle.Villages);
            Game.Refresh();
            for (int i = 0; i < 3; i++) await Frame();
            Check(!battle.Vision.IsVisible(Side.Player, hiddenTown.Position) && battle.Vision.IsExplored(Side.Player, hiddenTown.Position),
                "rival settlement remains remembered behind the fog");
            int hiddenBakes = Game.BoardView.TownRasterBuildsFor(hiddenTown.Id);
            for (int turn = 0; turn < 2; turn++)
            {
                Check(battle.EndTurn(Side.Player).Success && battle.EndTurn(Side.Enemy).Success, "both sides advance the hidden town's upgrade clock");
                Game.Refresh();
                for (int i = 0; i < 3; i++) await Frame();
            }
            Check(hiddenTown.Level == 4 && Game.BoardView.TownRasterBuildsFor(hiddenTown.Id) == hiddenBakes,
                "an unseen rival level change cannot repaint its last observed town artwork");
            battle.Vision.RevealCombat(Side.Player, hiddenTown.Position);
            battle.Vision.Recompute(battle.Ships, battle.TurnSerial, battle.Villages);
            Game.Refresh();
            for (int i = 0; i < 5; i++) await Frame();
            Check(Game.BoardView.TownRasterBuildsFor(hiddenTown.Id) == hiddenBakes + 2 && Game.BoardView.TownRasterIdle,
                "rediscovery updates exactly the rival town's two changed layers once");
            GD.Print($"PASS: {_checks} lighthouse, spaced five-level town lattice, native layer ordering and deduplicated trade lane checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
