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

public partial class WorldVisual0205Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string description)
    {
        if (!value) throw new InvalidOperationException("World visual v020.5: " + description);
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
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "capture " + suffix);
    }
    private static float Area(Vector2[] polygon)
    {
        if (polygon.Length < 3) return 0;
        var zero = polygon[0];
        float area = 0;
        for (int i = 0; i < polygon.Length; i++)
            area += (polygon[i] - zero).Cross(polygon[(i + 1) % polygon.Length] - zero);
        return MathF.Abs(area) * .5f;
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var board = new GameBoard(38, 28, p => p.X is >= 6 and <= 30 && p.Y is >= 5 and <= 20 &&
                !(p.X < 10 && p.Y > 16) && !(p.X > 26 && p.Y < 8) ? TerrainType.Land : TerrainType.Water, seed: 2047);
            var townCells = new[] { new GridPosition(6, 10), new(14, 5), new(23, 20), new(30, 13) };
            var battle = new BattleState(board, Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(36, 25)) },
                Array.Empty<GridPosition>(), villageSpots: townCells);
            var save = battle.CaptureSnapshot();
            var levels = new[] { 1, 2, 3, 5 };
            save.Villages = save.Villages.Select((town, i) => town with
            {
                Owner = Side.Player, Level = levels[i], Health = levels[i] * 5,
                Fortified = i > 0, Port = i >= 2
            }).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            battle.SetGodEye(true);
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.Refresh();
            Game.MapCamera.FitBoard();
            string untouched = battle.SaveJson();
            for (int i = 0; i < 14; i++) await Frame();
            var view = Game.BoardView;
            var rivers = view.CosmeticRiverDiagnostics.ToArray();
            Check(rivers.Length >= 3, "a large island supports several separate streams despite towns and mountain masks");
            Check(rivers.All(r => r.MinimumWidth is > 0 and < .2f && r.Cells.Length >= 2),
                "streams retain the thin visual scale while spanning actual adjacent landscape cells");
            Check(rivers.Any(r => r.Centerline.Zip(r.Centerline.Skip(1)).Zip(r.Centerline.Skip(2))
                .Any(pair => MathF.Abs((pair.First.Second - pair.First.First).Normalized().Cross(
                    (pair.Second - pair.First.Second).Normalized())) > .035f)),
                "river centerlines contain rounded changing tangents rather than straight tile rows");
            bool clean = true;
            var obstacles = view.CosmeticRiverExclusionShapes.Select(shape =>
                (Shape: shape, Bounds: shape.Aggregate(new Rect2(shape[0], Vector2.Zero), (r, p) => r.Expand(p)))).ToArray();
            foreach (var (_, shape) in view.CosmeticRiverWaterShapes)
            {
                var bounds = shape.Aggregate(new Rect2(shape[0], Vector2.Zero), (r, p) => r.Expand(p));
                clean &= Geometry2D.TriangulatePolygon(shape).Length >= 3;
                foreach (var obstacle in obstacles.Where(o => o.Bounds.Intersects(bounds)))
                    clean &= Geometry2D.IntersectPolygons(shape, obstacle.Shape).Sum(Area) < .03f;
            }
            Check(clean, "no drawn river water extends underneath actual mountain skirts or future town footprints");
            bool crossings = false;
            for (int a = 0; a < rivers.Length; a++)
                for (int b = a + 1; b < rivers.Length; b++)
                    for (int i = 1; i < rivers[a].Centerline.Length; i++)
                        for (int j = 1; j < rivers[b].Centerline.Length; j++)
                        {
                            bool junction = rivers[b].JoinedNetwork == rivers[a].Network && j >= rivers[b].Centerline.Length - 14 ||
                                rivers[a].JoinedNetwork == rivers[b].Network && i >= rivers[a].Centerline.Length - 14;
                            if (!junction && BoardView.RiverSegmentsDistance(rivers[a].Centerline[i - 1], rivers[a].Centerline[i],
                                rivers[b].Centerline[j - 1], rivers[b].Centerline[j]) < .001f) crossings = true;
                        }
            Check(!crossings, "separate river networks never cross away from a designated terminal confluence");
            foreach (var river in rivers.Where(r => r.JoinedNetwork is not null))
            {
                var trunk = rivers.Single(r => r.Network == river.JoinedNetwork);
                Check(trunk.Centerline.Zip(trunk.Centerline.Skip(1)).Any(segment =>
                    BoardView.RiverSegmentsDistance(river.Centerline[^1], river.Centerline[^1], segment.First, segment.Second) < .01f),
                    "each tributary ends exactly on its main channel");
            }
            var deltas = view.CosmeticRiverDeltas.ToArray();
            Check(deltas.Length > 0, "at least one accepted river makes a complete visible estuary");
            Check(deltas.All(d => view.Board.GetTile(d.OceanCell).Terrain != TerrainType.Land &&
                d.OceanColor.IsEqualApprox(view.RiverOceanColor(d.OceanCell))),
                "mouth gradients finish in ocean water at its actual tile tint");
            Check(deltas.All(d => Geometry2D.TriangulatePolygon(d.Channel).Length >= 3 &&
                Geometry2D.IsPointInPolygon(d.OceanEnd.Lerp(d.Channel[0], .002f), d.Channel)),
                "each rounded flared estuary has a valid continuous outlet polygon");
            var stamps = view.CosmeticRiverDeltaStamps.ToArray();
            var riverWater = view.CosmeticRiverWaterShapes.Select(p => p.Shape).ToArray();
            Check(deltas.All(delta => riverWater.Any(shape => Geometry2D.IsPointInPolygon(delta.InlandAnchor, shape))),
                "every estuary joins actual painted inland river water across the entire beach width");
            Check(deltas.All(delta => delta.Centerline.Take(delta.Centerline.Length / 2).All(point =>
                stamps.Any(stamp => stamp.Colors.Any(color => color.A > .5f) &&
                    Geometry2D.IsPointInPolygon(point, stamp.Shape)))),
                "the opaque inland half of each estuary is drawn continuously rather than leaving a sand gap");
            Check(stamps.Any(stamp => view.Board.GetTile(stamp.Cell).Terrain != TerrainType.Land &&
                stamp.Colors.All(color => color.A < .01f)),
                "the river's last ocean section fades completely and preserves the existing shallow-water glow");
            Check(stamps.All(stamp => stamp.Shape.Length == stamp.Colors.Length &&
                Geometry2D.TriangulatePolygon(stamp.Shape).Length >= 3 &&
                obstacles.All(o => Geometry2D.IntersectPolygons(stamp.Shape, o.Shape).Sum(Area) < .03f)),
                "retained gradient stamps are valid and cannot cover a town or mountain footprint");
            foreach (var delta in deltas)
            {
                Game.MapCamera.Position = delta.OceanEnd;
                Game.MapCamera.Zoom = Vector2.One * 4;
                Game.MapCamera.ForceUpdateScroll();
                for (int i = 0; i < 3; i++) await Frame();
                await Capture("estuary-" + Array.IndexOf(deltas, delta));
            }
            Check(rivers.Any(r => Enumerable.Range(0, Math.Max(0, r.Centerline.Length - 8)).Any(i =>
            {
                float length = Enumerable.Range(i + 1, 7).Sum(j => r.Centerline[j].DistanceTo(r.Centerline[j - 1]));
                return length > r.Centerline[i].DistanceTo(r.Centerline[i + 7]) * 1.045f;
            })), "streams include pronounced smooth meanders inside an individual cell span");
            await Capture("rivers-around-towns-and-mountains");
            foreach (var town in battle.Villages)
            {
                var fit = view.VillagePlacement(town);
                Check(fit.Scale >= .50f && view.VillageFootprintPoints(town).All(p => view.VillageSoilContains(town, p)),
                    "medium-sized towns and future fortifications remain within unsanded contiguous soil");
                var townArt = view.VillageSceneryEnvelope(town);
                float anchorY = view.Projection.GridToWorld(town.Position).Y;
                Check(view.MountainSilhouettes.Where(peak => peak.Anchor.Y >= anchorY).All(peak =>
                    Geometry2D.IntersectPolygons(peak.Shape, townArt).Sum(Area) < .03f),
                    "foreground peak silhouettes cannot hide any town roofs, paving or mills");
                var plaza = BoardView.TownPlaza(town.Id);
                Check(plaza.All(p => view.VillageSoilContains(town, fit.Point(p))) &&
                    Geometry2D.IsPointInPolygon(new Vector2(3.4f, -1.7f), plaza),
                    "deformed round paving stays on land and encloses the church base");
                var homes = view.TownHomes(town);
                var founders = BoardView.TownVisibleHomes(homes, 1);
                Check(founders.Count(h => h.Position.X < -23) == 2 && founders.Count(h => h.Position.X > 23) == 2,
                    "four of the five founding houses visibly flank the taller sanctuary");
                Check(homes.Length >= 17 && BoardView.TownHouseCount(1, homes.Length) == 5 &&
                    BoardView.TownHouseCount(5, homes.Length) == 17,
                    "villages gain three distinct house sites each level within the same compact footprint");
                Check(homes.All(h => BoardView.TownHouseHeight(h, 5) - BoardView.TownHouseHeight(h, 1) >= 14),
                    "upper-level homes gain floors without shrinking low-level houses");
                Check(homes.All(h => BoardView.TownHouseHeight(h, 1) >= 12 &&
                    BoardView.TownHouseHeight(h, 5) / BoardView.TownHouseHeight(h, 1) <= 1.9f),
                    "low-level buildings are substantial and upper levels gain the requested taller houses without changing the ground footprint");
                if (town.HasPort)
                {
                    var road = view.PortRoad(town);
                    Check(road[0].DistanceTo(fit.Point(new Vector2(3.4f, -1.7f))) < .001f &&
                        (road[^1] + view.Projection.GridToWorld(town.Position)).DistanceTo(view.PortAnchor(town)) < .001f,
                        "rear port road connects the relocated town plaza directly to the actual pier anchor");
                }
                Game.MapCamera.Position = view.Projection.GridToWorld(town.Position) + new Vector2(0, -15);
                Game.MapCamera.Zoom = Vector2.One * 3.3f;
                Game.MapCamera.ForceUpdateScroll();
                for (int i = 0; i < 3; i++) await Frame();
                await Capture("town-level-" + town.Level + (town.HasPort ? "-rear-port-road" : "-round-plaza"));
            }
            Check(battle.SaveJson() == untouched, "visual world generation and native screenshots preserve all simulation state");
            int builds = view.CosmeticRiverBuildCount, terrain = view.TerrainDrawCount, towns = view.TownRasterBuildCount;
            int shoreSplits = view.RiverShoreSplitBuildCount;
            for (int i = 0; i < 4; i++) view.VisibleShoreSegments().ToArray();
            Check(view.RiverShoreSplitBuildCount == shoreSplits,
                "observation refresh reuses every retained estuary shore split without polygon intersection work");
            for (int i = 0; i < 5; i++)
            {
                Game.MapCamera.Pan(new Vector2(8, 4));
                view.Select(new GridPosition(10 + i, 11));
                await Frame();
            }
            Check(view.CosmeticRiverBuildCount == builds && view.TerrainDrawCount == terrain &&
                view.TownRasterBuildCount == towns && view.TownRasterIdle && view.CosmeticRiverDeltas.Count == deltas.Length,
                "pan and selection preserve river plans, terrain rasters and bounded town art");
            var oldProjection = view.Projection;
            var nextBoard = new GameBoard(20, 16, p => p.X is >= 5 and <= 14 && p.Y is >= 4 and <= 11 ?
                TerrainType.Land : TerrainType.Water, seed: 2059);
            var nextBattle = new BattleState(nextBoard, battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(2, 2)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 14)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            nextBattle.SetGodEye(true);
            Game.LoadScenario(nextBattle);
            Game.Home.Hide();
            Game.Refresh();
            for (int i = 0; i < 6; i++) await Frame();
            Check(!ReferenceEquals(view.Projection, oldProjection) && view.CosmeticRiverBuildCount == builds + 1 &&
                view.RiverShoreSplitBuildCount > shoreSplits,
                "a new world invalidates the shore-plan cache and builds its own complete edge geometry");
            int newSplits = view.RiverShoreSplitBuildCount;
            for (int i = 0; i < 4; i++) view.VisibleShoreSegments().ToArray();
            Check(view.RiverShoreSplitBuildCount == newSplits,
                "the replacement world's observation refresh is also free of repeated shore intersections");
            GD.Print($"PASS: {_checks} v020.5 curved separate/confluent rivers, mountain/town exclusions, substantial low-level buildings, round plazas and rear port roads.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
