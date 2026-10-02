using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    private Node2D? _depthGroup;
    private IsometricProjection? _depthProjection;
    private long _depthVision = -1;
    private readonly List<(GridPosition Cell, Node2D Canvas)> _depthObjects = new();
    private readonly List<(GridPosition Cell, BoardTerrainLayer Canvas)> _townArt = new();
    private readonly List<(GridPosition Cell, BoardTerrainLayer Canvas)> _townInterfaces = new();
    private readonly List<(GridPosition Cell, BoardTerrainLayer Canvas)> _townCanvases = new();
    internal Action<Node2D, Village, Vector2>? DrawTownLife { get; set; }
    internal int DepthObjectCount => _depthObjects.Count;

    internal void RefreshDepthObjects(Node2D parent)
    {
        if (!ReferenceEquals(_depthProjection, Projection))
        {
            _depthProjection = Projection;
            _depthVision = -1;
            if (_depthGroup is not null)
            {
                _depthGroup.GetParent().RemoveChild(_depthGroup);
                _depthGroup.QueueFree();
            }

            _depthGroup = new Node2D
            {
                Name = "IslandDepth",
                YSortEnabled = true
            };
            parent.AddChild(_depthGroup);
            _depthObjects.Clear();
            _townCanvases.Clear();
            _townArt.Clear();
            _townHomes.Clear();
            _townHealth.Clear();
            _townInterfaces.Clear();
            BuildScenery();
            BuildSceneryAtlas();

            foreach (var town in Battle.Villages)
            {
                int id = town.Id;
                var canvas = new BoardTerrainLayer
                {
                    Name = "Town" + id,
                    Position = Projection.GridToWorld(town.Position),
                    DrawWorld = node =>
                    {
                        // Draw the current observed town; impacts can update its owner or level.
                        var current = Battle.VillageAt(town.Position);
                        if (current is not null)
                        {
                            DrawCachedTown(node, current, false);

                        }
                    }
                };
                _depthGroup.AddChild(canvas);
                _depthObjects.Add((town.Position, canvas));
                _townArt.Add((town.Position, canvas));
                var life = new BoardTerrainLayer
                {
                    Name = "TownLife" + id,
                    Position = VillagePlacement(town).Offset,
                    Scale = Vector2.One * Math.Max(.001f, VillagePlacement(town).Scale),
                    DrawWorld = node => { if (Battle.VillageAt(town.Position) is { } current) DrawTownLife?.Invoke(node, current, Vector2.Zero); }
                };
                canvas.AddChild(life);
                _townCanvases.Add((town.Position, life));
                var foreground = new BoardTerrainLayer
                {
                    Name = "TownForeground" + id,
                    DrawWorld = node => { if (Battle.VillageAt(town.Position) is { } current) DrawCachedTown(node, current, true); }
                };
                // Animated mill blades sit between the houses and the retained
                // front wall/wheat, instead of painting over every town layer.
                canvas.AddChild(foreground);
                _townArt.Add((town.Position, foreground));
                var townUi = new BoardTerrainLayer
                {
                    Name = "TownInterface" + id,
                    ZIndex = 6,
                    Position = canvas.Position,
                    DrawWorld = node => { if (Battle.VillageAt(town.Position) is { } current) DrawVillageInterface(node, current); }
                };
                _depthGroup.AddChild(townUi);
                _depthObjects.Add((town.Position, townUi));
                _townInterfaces.Add((town.Position, townUi));
            }
        }

        if (_depthVision == Battle.Vision.Revision)
            return;
        _depthVision = Battle.Vision.Revision;
        foreach (var(cell, canvas)in _depthObjects)
        {
            bool visible = Battle.Vision.IsVisible(Side.Player, cell);
            canvas.Visible = visible || Battle.Vision.IsExplored(Side.Player, cell);
            canvas.Modulate = visible ? Colors.White : new Color(.43f, .43f, .43f);
        }

        foreach (var (cell, canvas) in _townArt)
            if (Battle.Vision.IsVisible(Side.Player, cell)) canvas.QueueRedraw();
        foreach (var (cell, canvas) in _townInterfaces)
            if (Battle.Vision.IsVisible(Side.Player, cell)) canvas.QueueRedraw();
        AnimateTowns();
    }

    internal void AnimateTowns()
    {
        float time = TownAnimationTime;
        foreach (var (cell, canvas) in _townInterfaces)
            if (Battle.Vision.IsVisible(Side.Player, cell) && Battle.VillageAt(cell) is { } town
                && _townHealth.TryGetValue(town.Id, out var health) && health.Active(time))
                canvas.QueueRedraw();
        foreach (var(cell, canvas)in _townCanvases)
        {
            // An explored town keeps its last observed appearance while hidden.
            if (Battle.Vision.IsVisible(Side.Player, cell))
                canvas.QueueRedraw();
        }
    }
}
