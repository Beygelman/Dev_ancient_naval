using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Vision;
using Side = DevAncientNaval.Core.Units.Side;
using System.Collections.Generic;
using System.Linq;
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
    public IReadOnlyCollection<GridPosition> Collection { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> DockSites { get; set; } = System.Array.Empty<GridPosition>();

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
            bool alternate = (tile.Position.X * 13 + tile.Position.Y * 7) % 5 < 2;
            var terrain = Battle.Vision.KnownTerrain(Side.Player, tile.Position);
            var color = terrain switch
            {
                TerrainType.Land => new Color(alternate ? "b2ac83" : "a5a581"),
                TerrainType.Coast => new Color(alternate ? "568d8d" : "528888"),
                TerrainType.Water => new Color(alternate ? "305769" : "2d5365"),
                _ => new Color(alternate ? "172431" : "182633")
            };
            if (terrain is not null && !Battle.Vision.IsVisible(Side.Player, tile.Position)) color = color.Darkened(0.57f);
            DrawColoredPolygon(vertices, color);
            DrawPolyline(vertices.Append(vertices[0]).ToArray(),
                new Color(0.09f,0.20f,0.25f,0.6f), 1, true);
        }
        DrawContour(Reachable, Building ? new Color(0.6f,1,0.8f,0.8f) : new Color(0.64f,0.82f,1,0.72f));
        foreach (var cell in Targets)
        {
            var v = Projection.Diamond(cell);
            DrawPolyline(v.Append(v[0]).ToArray(), new Color("ff9678"), 2, true);
        }
        if (SelectedShipId is { } id && Battle.FindObserved(Side.Player, id) is { Owner: Side.Player } ship)
        {
            if (ship.RadarRange > 0) DrawTileContour(ship.Position, ship.RadarRange, new Color(0.5f, 1, 0.65f, 0.38f));
        }
        foreach (var cell in Battle.KnownFish(Side.Player))
        {
            var p = Projection.GridToWorld(cell);
            DrawColoredPolygon(new[] { p+new Vector2(-9,0),p+new Vector2(-3,-5),p+new Vector2(6,-4),p+new Vector2(10,0),p+new Vector2(6,4),p+new Vector2(-3,5) }, new Color("d6d39a"));
            DrawColoredPolygon(new[] { p+new Vector2(-8,0),p+new Vector2(-15,-5),p+new Vector2(-15,5) }, new Color("d6d39a"));
        }
        foreach (var cell in Battle.KnownShoals(Side.Player))
        {
            var p=Projection.GridToWorld(cell);
            for(int i=0;i<3;i++)
            {
                var q=p+new Vector2((i-1)*13,i==1?-6:3);
                DrawColoredPolygon(new[] {q+new Vector2(-7,0),q+new Vector2(0,-4),q+new Vector2(8,0),q+new Vector2(0,4)},new Color("f4df89"));
                DrawLine(q+new Vector2(-7,0),q+new Vector2(-11,-4),new Color("f4df89"),2,true);
            }
        }
        foreach (var cell in Collection) DrawContour(new[] { cell },new Color("f0d98e"));
        foreach (var cell in DockSites) DrawContour(new[] { cell },new Color("96e5cb"));
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
            DrawPolyline(vertices.Append(vertices[0]).ToArray(),
                new Color("ffe298"), 3, true);
        }
    }

    private void DrawTileContour(GridPosition origin, int radius, Color color)
    {
        DrawContour(Board.Tiles.Where(t => BattleVision.InRadius(origin,t.Position,radius)).Select(t => t.Position),color);
    }
    private void DrawContour(IEnumerable<GridPosition> cells,Color color)
    { foreach(var edge in Projection.BoundaryEdges(cells)) DrawPolyline(edge,color,2,true); }
}
