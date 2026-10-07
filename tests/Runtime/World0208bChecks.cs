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

/// <summary>Graphical retained-waterfall and shrine art checks. Disposable fixture
/// only; the saved simulation must remain byte-identical during animation/panning.</summary>
public partial class World0208bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool valid, string description)
    {
        if (!valid) throw new InvalidOperationException("World0208b: " + description);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private async Task Settle()
    {
        for (int frame = 0; frame < 220; frame++)
        {
            await Frame();
            if (frame > 3 && Game.BoardView.TerrainTextureIdle && Game.BoardView.TerrainDetailIdle && Game.BoardView.SceneryAtlasIdle) return;
        }
        throw new InvalidOperationException("World0208b: retained chart did not finish its visibility flight.");
    }
    private async Task Capture(string suffix)
    {
        var path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null) return;
        await Frame();
        int extension = path.LastIndexOf('.');
        path = extension < 0 ? path + "-" + suffix + ".png" : path[..extension] + "-" + suffix + path[extension..];
        Check(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "native photograph " + suffix);
    }
    public override async void _Ready()
    {
        try
        {
            Check(DisplayServer.GetName() != "headless", "requires a native graphical renderer");
            await Frame(); Game.FastChecks = true;
            var board = new GameBoard(18, 18, p => p.X is >= 7 and <= 10 && p.Y is >= 7 and <= 10
                ? TerrainType.Land : TerrainType.Water, seed: 2082);
            var battle = new BattleState(board, Game.Battle.Rules, new[] {
                (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)),
                (Side.Enemy, ShipClass.Mothership, new GridPosition(16, 16)) },
                Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>(), piratesEnabled: false);
            Game.LoadScenario(battle); Game.Home.Hide(); Game.Refresh(); Game.MapCamera.FitBoard();
            await Settle();
            int fullCount = Game.BoardView.Projection.BoundaryEdges(board.Tiles.Select(t => t.Position)).Count();
            int knownCount = Game.BoardView.EdgeFallsExposedEdgeCount;
            Check(knownCount > 0 && knownCount < fullCount, "fog excludes unknown chart lips from waterfall geometry");
            var falls = Game.BoardView.GetNode<WorldEdgeFalls>("WorldEdgeFalls");
            Check(!falls.IsProcessing() && falls.Mesh is not null && falls.ZIndex < 0,
                "one retained shader mesh falls behind the map with no per-frame CPU animation");
            int builds = Game.BoardView.EdgeFallsBuildCount;
            string original = battle.SaveJson();
            for (int i = 0; i < 12; i++)
            {
                Game.MapCamera.Pan(new Vector2(i % 2 == 0 ? 2 : -2, 0)); Game.Refresh(); await Frame();
            }
            Check(Game.BoardView.EdgeFallsBuildCount == builds && battle.SaveJson() == original,
                "pan/refresh leaves the falls mesh and all simulation state unchanged");
            battle.SetGodEye(true); Game.Refresh(); await Settle();
            Check(Game.BoardView.EdgeFallsExposedEdgeCount == fullCount,
                "God's eye draws the whole chart edge using the existing human visibility override");
            builds = Game.BoardView.EdgeFallsBuildCount;
            original = battle.SaveJson();
            await Capture("void-chart");
            for (int i = 0; i < 24; i++) await Frame();
            await Capture("void-chart-flow");
            Check(Game.BoardView.EdgeFallsBuildCount == builds && battle.SaveJson() == original,
                "flow changes shader time only, without mesh uploads, commands or RNG draws");
            battle.SetGodEye(false); Game.Refresh();
            Check(Game.BoardView.EdgeFallsExposedEdgeCount == knownCount,
                "fog downgrade replaces the known boundary synchronously without a hidden lip flash");
            var layer = new CanvasLayer { Layer = 90 };
            var gallery = new ShrineGallery { Name = "World0208bShrineGallery" };
            AddChild(layer); layer.AddChild(gallery);
            await Frame(); await Capture("silver-green-white-shrines");
            gallery.Clock = 2.1f; gallery.QueueRedraw(); await Frame(); await Capture("shrine-rituals");
            layer.QueueFree();
            GD.Print($"PASS: {_checks} native 020.8b world checks; explored lips={knownCount}, full lips={fullCount}; retained shader flow.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private partial class ShrineGallery : Node2D
    {
        internal float Clock;
        public override void _Draw()
        {
            var size = GetViewportRect().Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color("14242c"));
            var nations = new[] { FleetColor.Green, FleetColor.White, FleetColor.Purple };
            for (int row = 0; row < 2; row++)
                for (int column = 0; column < nations.Length; column++)
                {
                    var center = new Vector2(size.X * (column + .5f) / 3, size.Y * (row + .62f) / 2);
                    float yaw = row == 0 ? -.3f : .6f;
                    Vector2 Project(float x, float y, float z)
                    {
                        var floor = new Vector2(x, y).Rotated(yaw);
                        return center + new Vector2((floor.X - floor.Y) * 2.8f,
                            (floor.X + floor.Y) * 1.4f - z * 3.1f);
                    }
                    DrawCircle(center, 35, new Color(0, 0, 0, .22f));
                    FactionSanctuaryArt.Draw(this, Project, nations[column]);
                    if (row == 0) FactionSanctuaryArt.DrawEffects(this, Project, nations[column], Clock);
                    else
                    {
                        FactionSanctuaryArt.DrawTurnPulse(this, Project, nations[column], .25f, column);
                        SelfModulate = Colors.White;
                    }
                    DrawString(ThemeDB.FallbackFont, center + new Vector2(-50, 53),
                        nations[column] + (row == 0 ? " ritual" : " flare"), fontSize: 18, modulate: new Color("e6d6b2"));
                }
        }
    }
}
