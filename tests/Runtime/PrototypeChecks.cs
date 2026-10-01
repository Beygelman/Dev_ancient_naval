using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Camera;
using Godot;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Opt-in checks in the actual engine: -- --smoke-test.
/// No test framework or third-party packages are needed.</summary>
public partial class PrototypeChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RunChecks();
            var args = OS.GetCmdlineUserArgs();
            var capture = args.FirstOrDefault(arg => arg.StartsWith("--capture="));
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                Game.MapCamera.FitBoard();
                Game.BoardView.Select(new GridPosition(6, 6));
                Game.Hud.ShowTile(Game.Battle, new(6, 6));
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture[10..]) == Error.Ok, "Capture saved");
            }
            GD.Print($"PASS: {_checks} runtime checks (projection, selection, mouse, touch, camera, HUD).");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"FAIL: {name}");
        _checks++;
    }

    private void RunChecks()
    {
        // Input scenarios use known cells and camera positions. Generated-map
        // geometry is independently checked below against the actual topology.
        var fixture = new DevAncientNaval.Core.World.GameBoard(20,20,_=>DevAncientNaval.Core.World.TerrainType.Water);
        Game.LoadScenario(DevAncientNaval.Core.World.SkirmishSetup.Create(fixture,Game.Battle.Rules));
        var projection = Game.BoardView.Projection;
        var camera = Game.MapCamera;
        var input = Game.MapInput;
        var viewport = GetViewport();
        Vector2 Screen(GridPosition cell) => viewport.GetCanvasTransform() * projection.GridToWorld(cell);
        void Dispatch(InputEvent e) => input._UnhandledInput(e);
        void Mouse(Vector2 position, bool pressed) => Dispatch(new InputEventMouseButton
            { Position = position, ButtonIndex = MouseButton.Left, Pressed = pressed });
        void Touch(int index, Vector2 position, bool pressed, bool canceled = false) => Dispatch(new InputEventScreenTouch
            { Index = index, Position = position, Pressed = pressed, Canceled = canceled });
        void Drag(int index, Vector2 position) => Dispatch(new InputEventScreenDrag { Index = index, Position = position });
        void Tap(GridPosition cell)
        {
            var screen = Screen(cell);
            Mouse(screen, true);
            Mouse(screen, false);
        }


        foreach (var tile in Game.BoardView.Board.Tiles)
        {
            var center = projection.GridToWorld(tile.Position);
            var quad=projection.Diamond(tile.Position);
            Check(projection.CornerCount(tile.Position)>=3,"Actual polygon has at least three corners");
            Check(Geometry2D.TriangulatePolygon(quad).Length==(quad.Length-2)*3,"Curved tile is a valid simple polygon");
            Check(projection.WorldToGrid(center) == tile.Position, "Center round trip");
            foreach (var corner in projection.Diamond(tile.Position))
                Check(projection.WorldToGrid(center.Lerp(corner, 0.98f)) == tile.Position, $"Inside polygon {tile.Position} seed {Game.Battle.Board.Seed}");
            Game.CancelOrder();
            Game.SelectAtScreen(Screen(tile.Position));
            Check(Game.BoardView.Selected == tile.Position, "Selection through canvas transform");
        }
        Check(projection.WorldToGrid(projection.GridToWorld(new(-1,0))) == new GridPosition(-1, 0), "Negative warped cell");
        Game.SelectAtScreen(viewport.GetCanvasTransform() * projection.GridToWorld(new(-1, 0)));
        Check(Game.BoardView.Selected is null, "Off-board selection clears");

        // Fixed input coordinates exercise gestures on a rectangular test arena;
        // the generated pentagon and every actual mesh cell were checked above.
        Game.LoadScenario(new DevAncientNaval.Core.Battle.BattleState(new DevAncientNaval.Core.World.GameBoard(20,20,_=>DevAncientNaval.Core.World.TerrainType.Water),Game.Battle.Rules,
            new[] { (DevAncientNaval.Core.Units.Side.Player,DevAncientNaval.Core.Units.ShipClass.Mothership,new GridPosition(0,0)),
                (DevAncientNaval.Core.Units.Side.Enemy,DevAncientNaval.Core.Units.ShipClass.Mothership,new GridPosition(19,19)) }));
        projection=Game.BoardView.Projection;

        camera.Pan(new Vector2(140, -70));
        var anchor = new Vector2(750, 420);
        var worldAnchor = camera.ScreenToWorld(anchor);
        camera.ZoomAt(anchor, 1.5f);
        Check(worldAnchor.DistanceTo(camera.ScreenToWorld(anchor)) < 0.01f, "Zoom preserves cursor anchor");
        Tap(new(6, 6));
        Check(Game.BoardView.Selected == new GridPosition(6, 6), "Mouse selection after pan/zoom");
        Check(!Game.Hud.SelectionText.Contains("X:") && Game.Hud.SelectionText.Length > 0, "Terrain inspection has no coordinates");

        var start = Screen(new(6, 6));
        var beforePan = camera.Position;
        Mouse(start, true);
        Dispatch(new InputEventMouseMotion { Position = start + new Vector2(80, 30) });
        Mouse(start + new Vector2(80, 30), false);
        Check(camera.Position.DistanceTo(beforePan) > 1, "Mouse drag pans");
        Check(Game.BoardView.Selected == new GridPosition(6, 6), "Drag does not select");

        start = Screen(new(8, 8));
        Touch(3, start, true);
        Touch(3, start + new Vector2(2, 1), false);
        Check(Game.BoardView.Selected == new GridPosition(8, 8), "Touch tap with jitter");
        beforePan = camera.Position;
        Touch(3, start, true);
        Drag(3, start + new Vector2(50, 20));
        Touch(3, start + new Vector2(50, 20), false);
        Check(camera.Position.DistanceTo(beforePan) > 1, "One finger pans");
        Check(Game.BoardView.Selected == new GridPosition(8, 8), "Touch drag does not select");

        float beforeZoom = camera.Zoom.X;
        Touch(2, new(500, 360), true);
        Touch(7, new(700, 360), true);
        Drag(2, new(460, 360));
        Drag(7, new(740, 360));
        Check(camera.Zoom.X > beforeZoom, "Pinch out zooms in");
        var afterPinch = camera.Zoom.X;
        Drag(2, new(500, 360));
        Drag(7, new(700, 360));
        Check(camera.Zoom.X < afterPinch, "Pinch in zooms out");
        Touch(2, new(500, 360), false);
        beforePan = camera.Position;
        Drag(7, new(720, 365));
        Check(camera.Position.DistanceTo(beforePan) > 1, "Pinch continues as single-finger pan");
        Touch(7, new(720, 365), false);
        Check(Game.BoardView.Selected == new GridPosition(8, 8), "Pinch release does not select");

        Touch(0, Screen(new(2, 2)), true);
        Touch(0, Screen(new(2, 2)), false, true);
        Check(Game.BoardView.Selected == new GridPosition(8, 8), "Canceled touch does not select");
        Mouse(Screen(new(3, 3)), true);
        input._Notification((int)NotificationApplicationFocusOut);
        Mouse(Screen(new(3, 3)), false);
        Check(Game.BoardView.Selected == new GridPosition(8, 8), "Focus loss cancels gesture");

        // Exercise real viewport routing, including the HUD's input boundary.
        camera.FitBoard();
        var realTap = Screen(new(10, 10));
        viewport.PushInput(new InputEventMouseButton { Position = realTap, GlobalPosition = realTap,
            ButtonIndex = MouseButton.Left, Pressed = true }, true);
        viewport.PushInput(new InputEventMouseButton { Position = realTap, GlobalPosition = realTap,
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
        Check(Game.BoardView.Selected == new GridPosition(10, 10), "Viewport mouse dispatch");
        var selected = Game.BoardView.Selected;
        viewport.PushInput(new InputEventMouseButton { Position = new(640, 40), GlobalPosition = new(640, 40),
            ButtonIndex = MouseButton.Left, Pressed = true }, true);
        viewport.PushInput(new InputEventMouseButton { Position = new(640, 40), GlobalPosition = new(640, 40),
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
        Check(Game.BoardView.Selected == selected, "HUD blocks map click-through");

        realTap = Screen(new(9, 9));
        viewport.PushInput(new InputEventScreenTouch { Index = 0, Position = realTap, Pressed = true }, true);
        viewport.PushInput(new InputEventScreenTouch { Index = 0, Position = realTap, Pressed = false }, true);
        Check(Game.BoardView.Selected == new GridPosition(9, 9), "Viewport touch dispatch");
        selected = Game.BoardView.Selected;
        viewport.PushInput(new InputEventScreenTouch { Index = 0, Position = new(640, 40), Pressed = true }, true);
        viewport.PushInput(new InputEventScreenTouch { Index = 0, Position = new(640, 40), Pressed = false }, true);
        Check(Game.BoardView.Selected == selected, "HUD blocks touch-through");
        beforeZoom = camera.Zoom.X;
        viewport.PushInput(new InputEventMouseButton { Position = realTap, ButtonIndex = MouseButton.WheelUp,
            Pressed = true }, true);
        Check(camera.Zoom.X > beforeZoom, "Viewport wheel zoom");

        // Releasing a map drag over the HUD must end the gesture.
        viewport.PushInput(new InputEventMouseButton { Position = realTap, ButtonIndex = MouseButton.Left,
            Pressed = true }, true);
        viewport.PushInput(new InputEventMouseMotion { Position = new(640, 40), ButtonMask = MouseButtonMask.Left }, true);
        viewport.PushInput(new InputEventMouseButton { Position = new(640, 40), ButtonIndex = MouseButton.Left,
            Pressed = false }, true);
        beforePan = camera.Position;
        viewport.PushInput(new InputEventMouseMotion { Position = new(700, 300) }, true);
        Check(camera.Position.IsEqualApprox(beforePan), "Release over HUD ends drag");
        camera.FitBoard();
        camera.Pan(new(55, -30));
        camera.ZoomAt(new(640, 360), 1.3f);

        foreach (var tile in Game.BoardView.Board.Tiles)
        {
            Game.CancelOrder();
            Game.SelectAtScreen(Screen(tile.Position));
            Check(Game.BoardView.Selected == tile.Position, "All cells after camera transform");
        }

        int triangles=0,pentagons=0,hexagons=0;
        for(int seed=0;seed<16;seed++)
        {
            var mesh=new DevAncientNaval.Presentation.Map.IsometricProjection(seed:seed);
            triangles+=mesh.TriangleCount; pentagons+=mesh.PentagonCount; hexagons+=mesh.HexagonCount;
            int quadrilaterals=0;
            for(int y=0;y<20;y++) for(int x=0;x<20;x++)
            {
                var p=new GridPosition(x,y); var outline=mesh.Diamond(p);
                if(mesh.CornerCount(p)==4) quadrilaterals++;
                Check(mesh.SmallestCornerAngle(p)>=DevAncientNaval.Presentation.Map.IsometricProjection.MinimumCornerAngle-.01f,"Projected corners remain wide enough to select");
                Check(Geometry2D.TriangulatePolygon(outline).Length==(outline.Length-2)*3,"Seeded curved topology has no self intersection");
                Check(mesh.WorldToGrid(mesh.GridToWorld(p))==p,"Seeded mesh picking matches centers");
            }
            Check(quadrilaterals>320,"Organic grids retain at least 80 percent quadrilateral cells");
        }
        Check(triangles>0&&pentagons>0&&hexagons>0,"Organic mesh family retains occasional non-quadrilateral cells");
        for(int seed=0;seed<8;seed++)
        {
            var board=DevAncientNaval.Core.World.ArchipelagoGenerator.Create(seed);
            var mesh=new DevAncientNaval.Presentation.Map.IsometricProjection(board);
            var edges=new System.Collections.Generic.Dictionary<(Vector2,Vector2),int>();
            foreach(var tile in board.Tiles)
            {
                var p=tile.Position; var outline=mesh.Diamond(p); var center=mesh.GridToWorld(p);
                Check(Geometry2D.TriangulatePolygon(outline).Length==(outline.Length-2)*3,"Pentagon map polygons triangulate");
                foreach(var v in outline) Check(mesh.WorldToGrid(center.Lerp(v,.98f))==p,$"Pentagon edge picking: seed {seed}, cell {p}, point {center.Lerp(v,.98f)}, picked {mesh.WorldToGrid(center.Lerp(v,.98f))}");
                foreach(var edge in mesh.CellEdges(p))
                {
                    var a=edge[0]; var b=edge[^1]; var key=a.X<b.X||a.X==b.X&&a.Y<b.Y?(a,b):(b,a);
                    edges[key]=edges.GetValueOrDefault(key)+1;
                }
            }
            Check(edges.Values.All(n=>n is 1 or 2),"Mesh seams have one or two incident cells");
            Check(mesh.BoundaryEdges(board.Tiles.Select(t=>t.Position)).Count()==edges.Values.Count(n=>n==1),"Contours exclude all interior seams");
        }
        camera.ZoomAt(anchor, 10000);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, MapCamera.MaxZoom), "Maximum zoom");
        camera.ZoomAt(anchor, 0.00001f);
        Check(Mathf.IsEqualApprox(camera.Zoom.X, MapCamera.MinZoom), "Minimum zoom");
        camera.FitBoard();
    }
}
