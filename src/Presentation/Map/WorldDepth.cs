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
    private readonly List<(GridPosition Cell, BoardTerrainLayer Canvas)> _depthObjects = new();
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
            BuildScenery();
            foreach (var item in _scenery)
            {
                if (item.Kind == 0)
                    continue;
                var canvas = new BoardTerrainLayer
                {
                    Position = item.Point,
                    DrawWorld = node => DrawSceneryObject(node, item, Vector2.Zero)
                };
                _depthGroup.AddChild(canvas);
                _depthObjects.Add((item.Cell, canvas));
            }

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
                            DrawVillage(node, current, Vector2.Zero);
                            DrawTownLife?.Invoke(node, current, Vector2.Zero);
                        }
                    }
                };
                _depthGroup.AddChild(canvas);
                _depthObjects.Add((town.Position, canvas));
                _townCanvases.Add((town.Position, canvas));
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

        AnimateTowns();
    }

    internal void AnimateTowns()
    {
        foreach (var(cell, canvas)in _townCanvases)
        {
            // An explored town keeps its last observed appearance while hidden.
            if (Battle.Vision.IsVisible(Side.Player, cell))
                canvas.QueueRedraw();
        }
    }
}
