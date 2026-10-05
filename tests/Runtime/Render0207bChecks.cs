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

/// <summary>Native retained-detail/LOD checks. Draw-call comparisons use the same
/// scene and camera; they are not a quiet-host frame-pacing certification.</summary>
public partial class Render0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("020.7b renderer: " + message);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private async Task SettleDetail()
    {
        for (int frame = 0; frame < 220; frame++)
        {
            await Frame();
            if (frame > 3 && Game.BoardView.TerrainDetailIdle && Game.BoardView.TerrainTextureIdle && Game.BoardView.SceneryAtlasIdle) return;
        }
        throw new InvalidOperationException("Bounded detail raster did not finish its latest visibility flight.");
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null) return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
    }
    private async Task<double> DrawCalls()
    {
        for (int i = 0; i < 8; i++) await Frame();
        double sum = 0;
        for (int i = 0; i < 24; i++)
        {
            await Frame();
            sum += Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        }
        return sum / 24;
    }
    public override async void _Ready()
    {
        try
        {
            Check(DisplayServer.GetName() != "headless", "requires the native graphical renderer");
            await Frame(); Game.FastChecks = true;
            var board = ArchipelagoGenerator.Create(731, 4, WorldKind.Pangaea, MapSize.Ocean);
            var battle = SkirmishSetup.Create(board, Game.Battle.Rules, 4);
            battle.SetGodEye(true);
            Game.LoadScenario(battle); Game.Home.Hide(); Game.Refresh();
            var land = board.Tiles.First(tile => tile.Terrain == TerrainType.Land && board.GetNeighbors(tile.Position)
                .Any(p => board.GetTile(p).Terrain != TerrainType.Land));
            Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(land.Position);
            Game.MapCamera.Zoom = Vector2.One * 2.5f; Game.MapCamera.ForceUpdateScroll();
            await Frame(); await Frame();
            // Invalidate several times while the single-flight detail queue is
            // still baking. Tint-only revisions need a GPU repaint too.
            battle.SetGodEye(false); Game.Refresh();
            Check(Game.BoardView.TerrainSuppressedRegionCount > 0 && Game.BoardView.TerrainSuppressedImagesHidden,
                "queued God's-eye downgrades hide stale coarse images immediately before a GPU flight");
            battle.SetGodEye(true); Game.Refresh();
            battle.SetGodEye(false); Game.Refresh();
            Check(Game.BoardView.TerrainSuppressedRegionCount > 0 && Game.BoardView.TerrainSuppressedImagesHidden,
                "rapid in-flight visibility revisions cannot re-expose a suppressed coarse image");
            await SettleDetail();
            Check(Game.BoardView.TerrainDetailVisibilityCurrent, "an in-flight fog change cannot publish an obsolete optical texture");
            Check(Game.BoardView.TerrainCoarseVisibilityCurrent && Game.BoardView.TerrainSuppressedRegionCount == 0,
                "coarse queued regions finish the latest fog generation and restore only fresh images");
            Check(Game.BoardView.TerrainDetailChunkCount is > 0 and <= TerrainDetailCache.MaximumChunks &&
                Game.BoardView.TerrainDetailPixels <= (long)TerrainDetailCache.MaximumChunks *
                (TerrainDetailCache.ChunkPixels + TerrainDetailCache.Padding * 2) * (TerrainDetailCache.ChunkPixels + TerrainDetailCache.Padding * 2),
                "detail texture allocation is bounded independently of world area");
            battle.SetGodEye(true); Game.Refresh(); await SettleDetail();
            Check(Game.BoardView.TerrainDetailSampleScale >= 2.5f, "maximum zoom never stretches a coarse whole-world texel on an ordinary viewport");
            Check(!Game.BoardView.FarSceneryActive, "close views use the original full models");
            Check(WorldLabelArt.SampleScale / Game.MapCamera.Zoom.X >= 1.2f &&
                Math.Abs(WorldLabelArt.LogicalSize("Ashen Harbor").X -
                    ThemeDB.FallbackFont.GetStringSize("Ashen Harbor", fontSize: WorldLabelArt.FontSize).X) < 2,
                "town labels retain high-resolution glyphs without changing their logical width or world anchor");
            string untouched = battle.SaveJson();
            int builds = Game.BoardView.TerrainDetailBuildCount;
            int interfaces = Game.BoardView.TownInterfaceDrawCount;
            Game.MapCamera.Pan(new Vector2(2, -1)); await Frame(); await Frame();
            Check(Game.BoardView.TerrainDetailBuildCount == builds, "a small pan inside cached chunks does not repaint the sea");
            Check(Game.BoardView.TownInterfaceDrawCount == interfaces,
                "town label quality does not introduce camera-driven interface repainting");
            await Capture("close-coast");
            Game.MapCamera.FitBoard(); await Frame(); await Frame();
            Check(Game.BoardView.FarSceneryActive, "full-chart views select the one distant tier");
            Check(Game.BoardView.FarSceneryPageCount is > 0 and <= 4, "distant scenery uses a small shared atlas set");
            int atlases = Game.BoardView.SceneryAtlasBuildCount;
            var anchors = Game.Fleet.GetNode<Node2D>("IslandDepth").GetChildren().OfType<Sprite2D>().Select(n => n.Position).ToArray();
            Game.BoardView.SetDistantDetail(false);
            double full = await DrawCalls();
            await Capture("full-models-chart");
            Game.BoardView.SetDistantDetail(true);
            double far = await DrawCalls();
            await Capture("distant-models-chart");
            Check(far < full * .85, $"native GPU submissions fall materially at identical zoom (full={full:0.0}, distant={far:0.0})");
            Check(anchors.SequenceEqual(Game.Fleet.GetNode<Node2D>("IslandDepth").GetChildren().OfType<Sprite2D>().Select(n => n.Position)) &&
                Game.BoardView.SceneryAtlasBuildCount == atlases && Game.BoardView.SceneryAtlasIdle,
                "LOD changes preserve exact ground-Y anchors and never rebake scenery");
            Check(battle.SaveJson() == untouched, "quality and LOD never mutate saved geometry, rules, commands or simulation RNG");
            Check(WorldAmbience.CloudSilhouettes.Count == 3 && WorldAmbience.CloudSilhouettes.All(shape =>
                shape.Length > 30 && Geometry2D.TriangulatePolygon(shape).Length > 0), "three rounded connected volumetric cloud footprints triangulate");
            var hiddenBoard = new GameBoard(40, 40, _ => TerrainType.Water);
            var hidden = new BattleState(hiddenBoard, battle.Rules, new[]
            {
                (Side.Player, ShipClass.Mothership, new GridPosition(0, 0)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(39, 39))
            }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
            Game.LoadScenario(hidden); Game.Refresh(); Game.MapCamera.FitBoard(); await Frame(); await Frame();
            Check(Game.BoardView.FarSceneryActive && Game.Fleet.PrepareDrawOrder().Count == 1 && Game.Fleet.HealthBadgeCount == 1,
                "unknown enemy identity/health never enters distant artwork or badges");
            GD.Print($"PASS: {_checks} native 020.7b renderer checks; chart cells={board.Tiles.Count}; same-camera fullDrawCalls={full:0.0}, farDrawCalls={far:0.0}, reduction={(1-far/full)*100:0.0}%.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
