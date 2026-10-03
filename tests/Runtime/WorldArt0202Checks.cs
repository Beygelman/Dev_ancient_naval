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

public partial class WorldArt0202Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string description)
    {
        if (!value) throw new InvalidOperationException("World art 0.20.2: " + description);
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
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var insetBeach = new[] { new Vector2(-2, -2), new(2, -2), new(2, 2), new(-2, 2) };
            var islandPatch = new[] { new Vector2(-10, -10), new(10, -10), new(10, 10), new(-10, 10) };
            var cutPieces = ConvexSoilClip.Subtract(islandPatch, insetBeach).ToArray();
            float Area(Vector2[] p)
            {
                float cross = 0;
                for (int i = 0; i < p.Length; i++) cross += p[i].Cross(p[(i + 1) % p.Length]);
                return MathF.Abs(cross) * .5f;
            }
            Check(MathF.Abs(cutPieces.Sum(Area) - 384) < .01f,
                "fully enclosed sand hole subtracts its area instead of painting both contour rings");
            Check(cutPieces.All(p => Geometry2D.TriangulatePolygon(p).Length >= 3 &&
                !Geometry2D.IsPointInPolygon(p.Aggregate(Vector2.Zero, (sum, v) => sum + v) / p.Length, insetBeach)),
                "hole subtraction leaves only independent drawable soil pieces");
            var board = new GameBoard(30, 25, p => p.X is >= 4 and <= 24 && p.Y is >= 5 and <= 18 &&
                !(p.X < 10 && p.Y < 8) && !(p.X > 19 && p.Y > 14) &&
                !(p.X is >= 18 and <= 24 && p.Y is >= 9 and <= 10) &&
                !(p.X is >= 13 and <= 15 && p.Y is >= 10 and <= 12) ? TerrainType.Land : TerrainType.Water, seed: 731);
            var spots = new[] { new GridPosition(4, 12), new(8, 8), new(13, 5), new(18, 8), new(24, 13), new(16, 18) };
            var battle = new BattleState(board, Game.Battle.Rules,
                new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 12)),
                    (Side.Enemy, ShipClass.Mothership, new GridPosition(27, 20)) },
                Array.Empty<GridPosition>(), villageSpots: spots);
            battle.SetPlayerColor(FleetColor.Red);
            battle.SetGodEye(true);
            var save = battle.CaptureSnapshot();
            save.Villages = save.Villages.Select((v, i) => v with { Owner = i == 5 ? Side.Pirates : Side.Player,
                Level = i == 5 ? 3 : i + 1, Health = (i == 5 ? 3 : i + 1) * 5,
                Fortified = i >= 2, Name = i == 5 ? "Blackwater Cove" : v.Name }).ToArray();
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            Game.LoadScenario(battle);
            Game.Home.Hide();
            Game.MapCamera.FitBoard();
            for (int i = 0; i < 8; i++) await Frame();
            await Capture("landscape");
            Check(!Game.BoardView.VisualLandContains(Game.BoardView.Projection.GridToWorld(new(14, 11))),
                "an enclosed inland lake remains water after shared-contour clipping");
            var features = TerrainFeatures.For(board);
            Check(features.MountainCells.Count > 0, "inland fixture contains a curved mountain ridge");
            Check(Game.BoardView.RenderedMountainCells.ToHashSet().SetEquals(features.MountainCells),
                "every sight-blocking ridge owns its large and connecting visual peaks");
            foreach (var footprint in Game.BoardView.MountainGroundFootprints)
                Check(footprint.All(p => board.GetTile(Game.BoardView.Projection.WorldToGrid(p)).Terrain == TerrainType.Land),
                    "every mountain skirt remains on land with a full coastal-cell beach buffer");
            Check(Game.BoardView.ConnectingMountainCount > 0, "adjacent summits have smaller connecting peaks");
            Check(Game.BoardView.SummitSizes.Any(size => size > 40), "large inland summits exceed the previous 25-38 pixel size");
            var land = board.Tiles.Where(t => t.Terrain == TerrainType.Land).ToArray();
            Check(land.Count(t => features.ForestDensity(t.Position) == 0) >= land.Length / 4,
                "whole plain tiles remain treeless");
            Check(Game.BoardView.TreeCount > 0 && Game.BoardView.TreeCount < land.Length * 5,
                "sparser forest groves still retain trees");
            var widths = Enumerable.Range(0, 300).Select(i => BoardView.BeachWidth(new Vector2(i * 10, i * 3))).ToArray();
            Check(widths.Max() - widths.Min() > 12, "beaches contrast narrow and broad bands");
            foreach (var town in battle.Villages)
            {
                Check(Game.BoardView.VillagePlacement(town).Scale > 0,
                    "even a concave coastal cell keeps a visible compact town");
                foreach (var point in Game.BoardView.VillageFootprintPoints(town))
                    Check(Game.BoardView.VillageSoilContains(town, point),
                        "whole town including tower bases fits the land at coastal town " + town.Id);
                var shapes = Game.BoardView.VillageGroundShapes(town).Concat(Game.BoardView.VillageFieldShapes(town)).ToArray();
                Check(shapes.Length > 0, "coastal village retains visible irregular ground and fields");
                foreach (var shape in shapes)
                {
                    Check(shape.All(p => p.IsFinite()) && Geometry2D.TriangulatePolygon(shape).Length >= 3,
                        "ground texture stays finite and triangulatable");
                    // Test each triangle's interior, rather than ambiguous shared shore edges.
                    var indices = Geometry2D.TriangulatePolygon(shape);
                    for (int i = 0; i < indices.Length; i += 3)
                        Check(Game.BoardView.VillageSoilContains(town,
                            (shape[indices[i]] + shape[indices[i + 1]] + shape[indices[i + 2]]) / 3),
                            "every rendered soil/field triangle stays away from beach and water at town " + town.Id + ": " +
                            Game.BoardView.VillageSoilDiagnostic(town, (shape[indices[i]] + shape[indices[i + 1]] + shape[indices[i + 2]]) / 3) +
                            "; triangle=" + string.Join(";", new[] { shape[indices[i]], shape[indices[i + 1]], shape[indices[i + 2]] }));
                }
            }
            for (int level = 2; level <= 5; level++)
                Check(BoardView.SanctuaryHeightScale(level) > BoardView.SanctuaryHeightScale(level - 1),
                    "village monument gets taller at every level");
            Check(FleetPalette.For(battle, Side.Pirates).V < .3f, "pirate bay uses a black nation palette");
            var anchor = new Vector2(13, -47);
            for (int i = 0; i < 30; i++)
            {
                var cloth = WorldAmbience.VillageFlagCloth(anchor, i * .17f, 3);
                Check(cloth[0] == anchor && cloth[9] == anchor + new Vector2(0, 10),
                    "flagcloth first column never leaves its pole as it waves");
            }
            foreach (var town in battle.Villages)
            {
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(town.Position);
                Game.MapCamera.Zoom = Vector2.One * 2.4f;
                Game.MapCamera.ForceUpdateScroll();
                for (int i = 0; i < 3; i++) await Frame();
                await Capture(town.Owner == Side.Pirates ? "pirate-bay" : "village-level-" + town.Level);
            }
            var focus = features.MountainCells.First();
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(focus);
            Game.MapCamera.Zoom = Vector2.One * 1.7f;
            Game.MapCamera.ForceUpdateScroll();
            await Frame();
            await Capture("mountains-and-plains");
            int atlas = Game.BoardView.SceneryAtlasBuildCount;
            for (int i = 0; i < 8; i++)
            {
                Game.MapCamera.Pan(new Vector2(9, 5));
                Game.BoardView.Select(board.Tiles[i].Position);
                await Frame();
            }
            Check(Game.BoardView.SceneryAtlasBuildCount == atlas && Game.BoardView.SceneryAtlasIdle,
                "pan and selection retain all mountain and tree atlases");
            GD.Print($"PASS: {_checks} v0.20.2 landscape, clipped coastal villages, beach width, shared mountains, meadows and anchored flags.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
