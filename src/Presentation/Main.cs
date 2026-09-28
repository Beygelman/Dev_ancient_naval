using System;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Presentation;

public partial class Main : Node2D
{
    public BoardView BoardView { get; private set; } = null!;
    public MapCamera MapCamera { get; private set; } = null!;
    public MapInput MapInput { get; private set; } = null!;
    public DebugHud Hud { get; private set; } = null!;

    public override void _Ready()
    {
        var board = PrototypeBoard.Create();
        var projection = new IsometricProjection();
        BoardView = new BoardView { Name = "Board", Board = board, Projection = projection };
        AddChild(BoardView);
        MapCamera = new MapCamera { Name = "MapCamera", MapBounds = projection.BoardBounds(board.Width, board.Height) };
        AddChild(MapCamera);
        MapCamera.MakeCurrent();
        MapCamera.FitBoard();
        MapInput = new MapInput { Name = "MapInput", Camera = MapCamera };
        MapInput.Tapped += SelectAtScreen;
        AddChild(MapInput);
        Hud = new DebugHud { Name = "DebugHud" };
        Hud.ResetRequested += MapCamera.FitBoard;
        Hud.ZoomRequested += factor => MapCamera.ZoomAt(GetViewportRect().Size / 2, factor);
        AddChild(Hud);
        GetViewport().SizeChanged += OnViewportResized;
        if (OS.HasFeature("debug") && Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--smoke-test"))
            AddChild(new Tests.Runtime.PrototypeChecks { Game = this });
    }

    public void SelectAtScreen(Vector2 screen)
    {
        var local = BoardView.ToLocal(MapCamera.ScreenToWorld(screen));
        var cell = BoardView.Projection.WorldToGrid(local);
        BoardView.Board.TryGetTile(cell, out var tile);
        BoardView.Select(tile?.Position);
        Hud.ShowTile(tile);
    }

    public override void _Process(double delta) => Hud.ShowZoom(MapCamera.Zoom.X);

    private void OnViewportResized()
    {
        MapInput.CancelGesture();
        MapCamera.FitBoard();
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= OnViewportResized;
}
