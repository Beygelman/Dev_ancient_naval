using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView : Node2D
{
    public GameBoard Board { get; set; } = null!;
    public IsometricProjection Projection { get; set; } = null!;
    public GridPosition? Selected { get; private set; }

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
        if (Selected is { } selected)
        {
            var vertices = Projection.Diamond(selected);
            DrawColoredPolygon(vertices, new Color(1, 0.85f, 0.35f, 0.25f));
            DrawPolyline(new[] { vertices[0], vertices[1], vertices[2], vertices[3], vertices[0] },
                new Color("ffe298"), 3, true);
        }
    }
}
