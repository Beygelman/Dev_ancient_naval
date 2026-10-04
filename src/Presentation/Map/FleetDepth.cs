using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Ship and scenery canvases share one native Y-sort root. Static scenery
/// retains shared texture regions; only moving hulls and effects redraw.</summary>
public partial class FleetView
{
    public BoardView? Landscape { get; set; }

    private CanvasItem? _ink;
    private CanvasItem Ink => _ink ?? this;

    private readonly Dictionary<int, HullCanvas> _hulls = new();
    private readonly HashSet<int> _shownHulls = new();
    private readonly List<int> _retiredHulls = new();
    private BoardTerrainLayer? _frontEffects;
    private sealed class HullCanvas
    {
        internal BoardTerrainLayer Canvas = null !;
        internal BoardTerrainLayer Badge = null !;
        internal readonly AmphoraHealthAnimation Health = new();
        internal ShipSnapshot Ship = null !;
        internal float Heading = float.NaN;
        internal float Barrel = float.NaN;
        internal float LastDraw = -1;
        internal bool Selected;
        internal Sprite2D? Glow;
        internal SceneryAtlasPage? Mask;
        internal string MaskKey = "";
        internal bool HealthAnimating;
    }

    public override void _Ready()
    {
        YSortEnabled = true;
        _frontEffects = new BoardTerrainLayer
        {
            Name = "ShellsAndFeedback",
            ZIndex = 8,
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
        _shownHulls.Clear();
        foreach (var entry in PrepareDrawOrder())
            ShowHull(entry.Ship, entry.Center);
        if (_movingVisible && _movingShip is { } moving)
            ShowHull(moving, _movingPosition);
        _retiredHulls.Clear();
        foreach (var pair in _hulls)
            if (!_shownHulls.Contains(pair.Key))
                _retiredHulls.Add(pair.Key);
        foreach (int id in _retiredHulls)
        {
            ReleaseTargetMask(_hulls[id].Mask);
            _hulls[id].Canvas.QueueFree();
            _hulls[id].Badge.QueueFree();
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
                    using var trace = DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Hull.Draw");
                    _ink = canvas;
                    try
                    {
                        DrawShip(hull.Ship, Vector2.Zero);
                    }
                    finally
                    {
                        _ink = null;
                    }
                }
            };
            AddChild(hull.Canvas);
            hull.Badge = new BoardTerrainLayer
            {
                Name = "HealthAmphorae" + ship.Id,
                ZIndex = 7,
                DrawWorld = canvas => AmphoraBadgeArt.Draw(canvas, Vector2.Zero, hull.Ship.Health, hull.Ship.MaxHealth,
                    FleetPalette.For(Battle, hull.Ship.Owner), hull.Ship.Class, hull.Health.Motion(_clock), hull.Ship.IsVeteran)
            };
            AddChild(hull.Badge);
            _hulls.Add(ship.Id, hull);
        }

        _shownHulls.Add(ship.Id);
        float heading = DeckAngle(ship.Id);
        float barrel = _barrelAngles.GetValueOrDefault(ship.Id);
        bool selected = ship.Id == SelectedId;
        bool redraw = hull.Ship != ship || hull.Heading != heading || hull.Barrel != barrel || hull.Selected != selected ||
            _sinking.ContainsKey(ship.Id) || ship.Class == Core.Units.ShipClass.Mothership && _clock - hull.LastDraw >= .10f;
        bool healthChanged = hull.Ship != ship;
        hull.Health.Observe(ship.Health, ship.MaxHealth, _clock);
        hull.Ship = ship;
        hull.Heading = heading;
        hull.Barrel = barrel;
        hull.Selected = selected;
        UpdateAttackHighlight(hull, AttackHighlight(ship, out bool lethal), lethal);
        var (bob, roll) = HullMotion(ship);
        hull.Canvas.Position = point + bob;
        hull.Canvas.Rotation = roll;
        hull.Canvas.Visible = !_sinking.ContainsKey(ship.Id);
        hull.Canvas.ZIndex = ship.Class == Core.Units.ShipClass.Balloon ? 1 : 0;
        hull.Canvas.Modulate = Colors.White;
        hull.Badge.Position = point + bob + HealthBadgeOffset(ship.Class);
        hull.Badge.Visible = !_sinking.ContainsKey(ship.Id);
        bool healthAnimating = hull.Health.Active(_clock);
        if (healthChanged || healthAnimating || hull.HealthAnimating)
            hull.Badge.QueueRedraw();
        hull.HealthAnimating = healthAnimating;
        if (redraw) { hull.LastDraw = _clock; hull.Canvas.QueueRedraw(); }
    }
}
