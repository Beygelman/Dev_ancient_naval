using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Ship and scenery canvases share one native Y-sort root. Static scenery
/// retains its draw commands; only moving hulls and effects redraw.</summary>
public partial class FleetView
{
    public BoardView? Landscape { get; set; }

    private CanvasItem? _ink;
    private CanvasItem Ink => _ink ?? this;

    private readonly Dictionary<int, HullCanvas> _hulls = new();
    private readonly List<int> _retiredHulls = new();
    private BoardTerrainLayer? _frontEffects;
    private sealed class HullCanvas
    {
        internal BoardTerrainLayer Canvas = null !;
        internal ShipSnapshot Ship = null !;
    }

    public override void _Ready()
    {
        YSortEnabled = true;
        _frontEffects = new BoardTerrainLayer
        {
            Name = "ShellsAndFeedback",
            ZIndex = 2,
            DrawWorld = canvas =>
            {
                _ink = canvas;
                try
                {
                    DrawFrontEffects();
                }
                finally
                {
                    _ink = null;
                }
            }
        };
        AddChild(_frontEffects);
    }

    private void RefreshHullCanvases()
    {
        Landscape?.RefreshDepthObjects(this);
        foreach (var hull in _hulls.Values)
            hull.Canvas.Visible = false;
        foreach (var entry in PrepareDrawOrder())
            ShowHull(entry.Ship, entry.Center);
        if (_movingVisible && _movingShip is { } moving)
            ShowHull(moving, _movingPosition);
        _retiredHulls.Clear();
        foreach (var pair in _hulls)
            if (!pair.Value.Canvas.Visible)
                _retiredHulls.Add(pair.Key);
        foreach (int id in _retiredHulls)
        {
            _hulls[id].Canvas.QueueFree();
            _hulls.Remove(id);
        }

        _frontEffects?.QueueRedraw();
    }

    private void ShowHull(ShipSnapshot ship, Vector2 point)
    {
        if (!_hulls.TryGetValue(ship.Id, out var hull))
        {
            hull = new HullCanvas();
            hull.Canvas = new BoardTerrainLayer
            {
                Name = "Hull" + ship.Id,
                DrawWorld = canvas =>
                {
                    _ink = canvas;
                    try
                    {
                        DrawShip(hull.Ship, Vector2.Zero);
                        if (_sinking.ContainsKey(hull.Ship.Id))
                            DrawWreck(hull.Ship, Vector2.Zero);
                    }
                    finally
                    {
                        _ink = null;
                    }
                }
            };
            AddChild(hull.Canvas);
            _hulls.Add(ship.Id, hull);
        }

        hull.Ship = ship;
        hull.Canvas.Position = point;
        hull.Canvas.Visible = true;
        hull.Canvas.ZIndex = ship.Class == Core.Units.ShipClass.Balloon ? 1 : 0;
        float sink = _sinking.GetValueOrDefault(ship.Id);
        hull.Canvas.Modulate = new Color(1, 1, 1, 1 - sink * sink);
        hull.Canvas.QueueRedraw();
    }
}
