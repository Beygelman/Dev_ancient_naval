using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Vision;
using Side = DevAncientNaval.Core.Units.Side;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView : Node2D
{
    public GameBoard Board { get; set; } = null!;
    public BattleState Battle { get; set; } = null!;
    public IsometricProjection Projection { get; set; } = null!;
    public GridPosition? Selected { get; private set; }
    public IReadOnlyCollection<GridPosition> Reachable { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> Targets { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> AttackArea { get; set; } = System.Array.Empty<GridPosition>();
    public int? SelectedShipId { get; set; }
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
            var terrain = Battle.Vision.KnownTerrain(Side.Player, tile.Position);
            var color = terrain switch
            {
                TerrainType.Land => new Color(alternate ? "89986a" : "819363"),
                TerrainType.Coast => new Color(alternate ? "4b9c9c" : "499696"),
                TerrainType.Water => new Color(alternate ? "206779" : "226b7c"),
                _ => new Color(alternate ? "101e30" : "112033")
            };
            if (terrain is not null && !Battle.Vision.IsVisible(Side.Player, tile.Position)) color = color.Darkened(0.57f);
            DrawColoredPolygon(vertices, color);
            DrawPolyline(new[] { vertices[0], vertices[1], vertices[2], vertices[3], vertices[0] },
                new Color("123f50"), 1, true);
        }
        foreach (var cell in Reachable)
            DrawColoredPolygon(Projection.Diamond(cell), Building ? new Color(0.5f, 1, 0.7f, 0.4f) : new Color(0.35f, 0.85f, 1, 0.22f));
        foreach (var cell in AttackArea)
            DrawColoredPolygon(Projection.Diamond(cell), new Color(1, 0.4f, 0.25f, 0.23f));
        foreach (var cell in Targets)
        {
            var v = Projection.Diamond(cell);
            DrawColoredPolygon(v, new Color(1, 0.3f, 0.2f, 0.32f));
            DrawPolyline(new[] { v[0], v[1], v[2], v[3], v[0] }, new Color("ff9678"), 2, true);
        }
        if (SelectedShipId is { } id && Battle.FindObserved(Side.Player, id) is { Owner: Side.Player } ship)
        {
            DrawRange(ship.Position, ship.Definition.VisualRange, new Color(0.6f, 0.95f, 1, 0.7f));
            if (ship.Definition.RadarRange > 0) DrawRange(ship.Position, ship.Definition.RadarRange, new Color(0.5f, 1, 0.65f, 0.6f));
            if (AttackArea.Count > 0) DrawRange(ship.Position, ship.Definition.AttackRange, new Color(1, 0.55f, 0.35f, 0.8f));
        }
        foreach (var contact in Battle.Vision.Contacts(Side.Player))
        {
            var point = Projection.GridToWorld(contact);
            DrawArc(point, 13, 0, Mathf.Tau, 24, new Color("9bffac"), 2, true);
            DrawString(ThemeDB.FallbackFont, point + new Vector2(-5, 6), "?", fontSize: 19, modulate: new Color("ceffd1"));
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

    private void DrawRange(GridPosition origin, int radius, Color color)
    {
        var points = new Vector2[97];
        var center = Projection.GridToWorld(origin);
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.Tau / (points.Length - 1);
            float x = Mathf.Cos(angle) * radius, y = Mathf.Sin(angle) * radius;
            points[i] = center + new Vector2((x - y) * Projection.TileWidth / 2, (x + y) * Projection.TileHeight / 2);
        }
        DrawPolyline(points, color, 1.5f, true);
    }
}
