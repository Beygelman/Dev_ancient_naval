using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView : Node2D
{
    public GameBoard Board { get; set; } = null!;
    public IsometricProjection Projection { get; set; } = null!;
    public GridPosition? Selected { get; private set; }
    public IReadOnlyCollection<GridPosition> Reachable { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> Targets { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyList<GridPosition> PreviewPath { get; set; } = System.Array.Empty<GridPosition>();
    public bool Building { get; set; }

    public void Select(GridPosition? position)
    {
        Selected = position is { } cell && Board.Contains(cell) ? cell : null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var tile in Board.Tiles)
        {
            var vertices = Projection.Diamond(tile.Position);
            bool alternate = (tile.Position.X + tile.Position.Y) % 2 == 0;
            var color = tile.Terrain == TerrainType.Land
                ? new Color(alternate ? "89986a" : "819363")
                : new Color(alternate ? "206779" : "226b7c");
            DrawColoredPolygon(vertices, color);
            DrawPolyline(new[] { vertices[0], vertices[1], vertices[2], vertices[3], vertices[0] },
                new Color("123f50"), 1, true);
        }
        foreach (var cell in Reachable)
            DrawColoredPolygon(Projection.Diamond(cell), Building ? new Color(0.5f, 1, 0.7f, 0.4f) : new Color(0.35f, 0.85f, 1, 0.22f));
        foreach (var cell in Targets)
        {
            var v = Projection.Diamond(cell);
            DrawColoredPolygon(v, new Color(1, 0.3f, 0.2f, 0.32f));
            DrawPolyline(new[] { v[0], v[1], v[2], v[3], v[0] }, new Color("ff9678"), 2, true);
        }
        if (PreviewPath.Count > 1)
        {
            var line = new Vector2[PreviewPath.Count];
            for (int i = 0; i < PreviewPath.Count; i++) line[i] = Projection.GridToWorld(PreviewPath[i]);
            DrawPolyline(line, new Color("f8dea1"), 3, true);
            foreach (var point in line) DrawCircle(point, 3, new Color("f8dea1"));
        }
        if (Selected is { } selected)
        {
            var vertices = Projection.Diamond(selected);
            DrawColoredPolygon(vertices, new Color(1, 0.85f, 0.35f, 0.25f));
            DrawPolyline(new[] { vertices[0], vertices[1], vertices[2], vertices[3], vertices[0] },
                new Color("ffe298"), 3, true);
        }
    }
}
