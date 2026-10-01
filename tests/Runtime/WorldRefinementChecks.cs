using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
/// <summary>Whole-island contour geometry and terrain cache invalidation regressions.</summary>
public partial class WorldRefinementChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
    }

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string? selectedMap = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--world-opponents="));
            foreach (int opponents in selectedMap is null ? new[]
            {
                1,
                2,
                3,
                4
            }

            : new[]
            {
                int.Parse(selectedMap[18..])
            }

            )
            {
                GD.Print($"WORLD begin {opponents}");
                var board = ArchipelagoGenerator.Create(731, opponents);
                Game.LoadScenario(SkirmishSetup.Create(board, Game.Battle.Rules, opponents));
                Game.Home.Hide();
                if (opponents == 1 && DisplayServer.GetName() != "headless")
                {
                    await DrawFrame();
                    await DrawFrame();
                    Check(Game.BoardView.TerrainKnownCellCount == Game.Battle.Vision.ExploredCount(Side.Player), "Unexplored terrain sources remain hidden, including their shores and canopies.");
                    int beforeDiscovery = Game.BoardView.TerrainDrawCount;
                    var unknown = board.Tiles.First(tile => !Game.Battle.Vision.IsExplored(Side.Player, tile.Position));
                    Game.Battle.Vision.RevealCombat(Side.Player, unknown.Position);
                    Game.Battle.Vision.Recompute(Game.Battle.Ships, 1, Game.Battle.Villages);
                    Game.Refresh();
                    await DrawFrame();
                    await DrawFrame();
                    Check(Game.BoardView.TerrainDrawCount == beforeDiscovery + 1, "Discovering one unknown tile builds exactly that tile's terrain geometry.");
                }

                foreach (var tile in board.Tiles)
                    Game.Battle.Vision.RevealCombat(Side.Player, tile.Position);
                Game.Battle.Vision.Recompute(Game.Battle.Ships, 1, Game.Battle.Villages);
                Game.Refresh();
                Game.BoardView.InvalidateWorld();
                Game.MapCamera.FitBoard();
                GD.Print($"WORLD geometry {opponents}");
                foreach (var contour in Game.BoardView.IslandContours)
                {
                    Check(contour.Length >= 12 && contour.All(point => point.IsFinite()), "Island union contour has finite smooth geometry.");
                    Check(Geometry2D.TriangulatePolygon(contour).Length >= 3, "Connected island shoreline triangulates without crossing itself.");
                }

                var beachPolygons = Game.BoardView.BeachPolygons.ToArray();
                Check(Game.BoardView.BeachSegmentCount > 0 && beachPolygons.Length >= Game.BoardView.BeachSegmentCount * .95, "Rounded islands retain a continuous useful beach band.");
                foreach (var strip in beachPolygons)
                    Check(strip.Length == 4 && Geometry2D.TriangulatePolygon(strip).Length == 6, "Every retained beach quad triangulates without a folded inset.");
                foreach (var triangle in Game.BoardView.CoastalWaterTriangles)
                    Check(triangle.Length == 3 && triangle.All(point => point.IsFinite()) && Math.Abs((triangle[1] - triangle[0]).Cross(triangle[2] - triangle[0])) > .001f, "Continuous coastal water bands contain finite nondegenerate triangles.");
                Check(board.Mesh!.Faces.Values.Count(face => face.Count == 4) >= board.Tiles.Count * .85, "Less-deformed worlds keep a clear quadrilateral majority.");
                Check(Enumerable.Range(0, opponents + 1).Select(index => board.FleetAnchor(index, opponents + 1)).Distinct().Count() == opponents + 1, "All factions have distinct maritime starting anchors.");
                if (DisplayServer.GetName() != "headless")
                {
                    GD.Print($"WORLD raster {opponents}");
                    await DrawFrame();
                    await DrawFrame(); // The SubViewport's Once update settles on the following frame.
                    // Window/viewport resize notifications may arrive after the first
                    // raster. Measure hover only after the camera overlay has settled.
                    for (int warmup = 0; warmup < 5; warmup++)
                        await DrawFrame();
                    int terrainBefore = Game.BoardView.TerrainDrawCount;
                    int observationsBefore = Game.BoardView.ObservationDrawCount;
                    int textureBefore = Game.BoardView.TerrainTextureUpdateRequests;
                    var textureSize = Game.BoardView.TerrainTextureSize;
                    Check(textureSize.X is> 0 and <= 4096 && textureSize.Y is> 0 and <= 4096 && (long)textureSize.X * textureSize.Y <= 8_388_608, "Terrain raster dimensions obey the axis and memory budget.");
                    Check(Game.BoardView.TerrainTextureIdle, "Cached terrain viewport stops rendering after one update.");
                    GD.Print($"WORLD hover {opponents}");
                    Check(Game.BoardView.TreeCount > board.Tiles.Count(tile => tile.Terrain == TerrainType.Land) * 2, "Island coastlines contain dense groves.");
                    Check(Game.BoardView.MountainCount > 0, "Seeded islands contain larger mountain ridges.");
                    Check(Game.Fleet.YSortEnabled && Game.Fleet.GetNode<Node2D>("IslandDepth").YSortEnabled, "Ships and land objects share native screen-depth sorting.");
                    Check(Game.BoardView.DepthObjectCount == Game.BoardView.TreeCount + Game.BoardView.MountainCount + Game.Battle.Villages.Count, "Every tall landscape object has its own ground anchor, rather than a tile-wide draw order.");
                    foreach (var tile in board.Tiles.Take(10))
                    {
                        Game.BoardView.Select(tile.Position);
                        Game.BoardView.PreviewPath = new[]
                        {
                            board.CentralCell,
                            tile.Position
                        };
                        Game.BoardView.QueueRedraw();
                        await DrawFrame();
                    }

                    Check(Game.BoardView.TerrainDrawCount == terrainBefore, "Route/selection redraws must reuse terrain draw commands.");
                    Check(Game.BoardView.ObservationDrawCount == observationsBefore,
                        $"Hovering reuses known resources and range contours ({observationsBefore} -> {Game.BoardView.ObservationDrawCount}).");
                    Check(Game.BoardView.TerrainTextureUpdateRequests == textureBefore && Game.BoardView.TerrainTextureIdle, "Hovering/selection cannot trigger terrain GPU rerasterization.");
                    Game.BoardView.InvalidateWorld();
                    await DrawFrame();
                    await DrawFrame();
                    Check(Game.BoardView.TerrainDrawCount == terrainBefore + Game.BoardView.TerrainRegionCount, "Explicit world changes redraw each bounded terrain region once.");
                    Check(Game.BoardView.TerrainTextureUpdateRequests == textureBefore + 1 && Game.BoardView.TerrainTextureIdle, "Explicit world changes request one bounded GPU raster update.");
                    if (opponents == 1)
                    {
                        Game.Battle.Vision.ClearCombatFlashes();
                        Game.Battle.Vision.Recompute(Game.Battle.Ships, 2, Game.Battle.Villages);
                        Game.Refresh();
                        await DrawFrame();
                        await DrawFrame();
                        int beforeReveal = Game.BoardView.TerrainDrawCount;
                        int visibilityBefore = Game.BoardView.TerrainVisibilityUpdateCount;
                        var revealed = board.Tiles.First(tile => !Game.Battle.Vision.IsVisible(Side.Player, tile.Position));
                        Game.Battle.Vision.RevealCombat(Side.Player, revealed.Position);
                        Game.Battle.Vision.Recompute(Game.Battle.Ships, 3, Game.Battle.Villages);
                        Game.Refresh();
                        await DrawFrame();
                        await DrawFrame();
                        int affected = Game.BoardView.TerrainDrawCount - beforeReveal;
                        Check(affected == 0 && Game.BoardView.TerrainVisibilityUpdateCount == visibilityBefore + 1, "Restoring visibility tints exactly one retained cell without rebuilding geometry.");
                        Check(Game.BoardView.TerrainTextureIdle, "Incremental fog raster settles without continuous updates.");
                    }
                }

                GD.Print($"WORLD {opponents} enemies: {board.Tiles.Count} cells; {Game.BoardView.IslandContours.Count} smooth island contours; trees={Game.BoardView.TreeCount}; peaks={Game.BoardView.MountainCount}; terrainTexture={Game.BoardView.TerrainTextureSize}.");
            }

            var capture = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                Game.BoardView.PreviewPath = Array.Empty<DevAncientNaval.Core.Grid.GridPosition>();
                Game.BoardView.Select(null);
                Game.BoardView.QueueRedraw();
                await DrawFrame();
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "World screenshot saved.");
                var land = Game.Battle.Board.Tiles.Where(tile => tile.Terrain == TerrainType.Land).OrderByDescending(tile => Game.Battle.Board.GetSurrounding(tile.Position).Count(p => Game.Battle.Board.GetTile(p).Terrain == TerrainType.Land)).First();
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(land.Position);
                Game.MapCamera.Zoom = Vector2.One * 2;
                Game.MapCamera.ForceUpdateScroll();
                await DrawFrame();
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..].Replace(".png", "-landscape.png")) == Error.Ok, "Close-up depth screenshot saved.");
                var ship = Game.Battle.OwnShips(Side.Player).First(s => s.Definition.Class == DevAncientNaval.Core.Units.ShipClass.Garrison);
                Game.SelectCell(ship.Position);
                var routes = Game.Battle.PreviewMovement(ship.Id);
                var destination = routes.Costs.OrderByDescending(pair => pair.Value).First().Key;
                Game.BoardView.PreviewPath = routes.PathTo(destination);
                Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(ship.Position).Lerp(Game.BoardView.Projection.GridToWorld(destination), .5f);
                Game.MapCamera.Zoom = Vector2.One * 1.25f;
                Game.MapCamera.ForceUpdateScroll();
                Game.BoardView.QueueRedraw();
                await DrawFrame();
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..].Replace(".png", "-route.png")) == Error.Ok, "Treasure route and destination coverage screenshot saved.");
            }

            GD.Print($"PASS: {_checks} world refinement checks ({DisplayServer.GetName()}).");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task DrawFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // A hidden diagnostic window can omit presentation when its cached
        // contents are unchanged. Force a real raster before inspecting it.
        RenderingServer.ForceDraw();
    }
}
