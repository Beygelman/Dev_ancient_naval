using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Vision;
using Side = DevAncientNaval.Core.Units.Side;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DevAncientNaval.Presentation.Diagnostics;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView : Node2D
{
    public GameBoard Board { get; set; } = null !;
    public BattleState Battle { get; set; } = null !;
    public IsometricProjection Projection { get; set; } = null !;
    public GridPosition? Selected { get; private set; }
    public IReadOnlyCollection<GridPosition> Reachable { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> Targets { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> AttackArea { get; set; } = System.Array.Empty<GridPosition>();
    public int? SelectedShipId { get; set; }
    public IReadOnlyList<GridPosition> PreviewPath { get; set; } = System.Array.Empty<GridPosition>();
    public bool Building { get; set; }
    public IReadOnlyCollection<GridPosition> Collection { get; set; } = System.Array.Empty<GridPosition>();
    public IReadOnlyCollection<GridPosition> DockSites { get; set; } = System.Array.Empty<GridPosition>();

    private IReadOnlyCollection<GridPosition>? _cachedReachable;
    private IsometricProjection? _cachedProjection;
    private (Vector2[] Edge, Vector2 Inside)[] _movementBoundary = System.Array.Empty<(Vector2[], Vector2)>();
    private Vector2[][] _movementGlow = System.Array.Empty<Vector2[]>();
    private readonly Color[] _movementGlowColors = new Color[4];
    private TerrainRasterCache? _terrainCache;
    private BoardTerrainLayer? _observations;
    private readonly HashSet<GridPosition> _resourceCells = new();
    internal int ObservationDrawCount { get; private set; }

    private IsometricProjection? _terrainProjection;
    private BattleState? _terrainBattle;
    private IsometricProjection? _maskProjection;
    private byte[] _terrainMask = System.Array.Empty<byte>();
    private (GridPosition Origin, int Radius)? _radarKey;
    private Vector2[][] _radarBoundary = System.Array.Empty<Vector2[]>();
    internal int TerrainDrawCount { get; private set; }
    internal int TerrainTextureUpdateRequests { get; private set; }
    internal Vector2I TerrainTextureSize => _terrainCache?.Size ?? Vector2I.Zero;
    internal bool TerrainTextureIdle => _terrainCache?.Idle == true;
    internal int TerrainRegionCount => _terrainCache?.RegionCount ?? 0;
    internal int TerrainVisibilityUpdateCount => _terrainCache?.VisibilityChanges ?? 0;
    internal int TerrainKnownCellCount => _terrainCache?.KnownCellCount ?? 0;

    public override void _Ready()
    {
        _terrainCache = new TerrainRasterCache
        {
            Name = "LandscapeCache",
            ShowBehindParent = true
        };
        AddChild(_terrainCache);
        _observations = new BoardTerrainLayer
        {
            Name = "KnownObjects",
            ShowBehindParent = true,
            DrawWorld = DrawObservedWorld
        };
        AddChild(_observations);
        _terrainCache.Ensure(Projection, Board, DrawUnexploredWorld, DrawStaticWorld, DrawStaticShore, DrawIslandScenery);
        _terrainProjection = Projection;
        _terrainBattle = Battle;
        InvalidateWorld();
    }

    /// <summary>World/fog changes invalidate retained terrain draw commands.
    /// Hovering or selecting only redraws the small command overlay.</summary>
    public void InvalidateWorld(bool force = true)
    {
        bool newWorld = !ReferenceEquals(_maskProjection, Projection) || _terrainMask.Length != Board.Tiles.Count;
        if (newWorld)
        {
            _maskProjection = Projection;
            _terrainMask = new byte[Board.Tiles.Count];
        }

        var changed = new HashSet<GridPosition>();
        for (int i = 0; i < Board.Tiles.Count; i++)
        {
            var cell = Board.Tiles[i].Position;
            byte mask = Battle.Vision.IsVisible(Side.Player, cell) ? (byte)2 : Battle.Vision.IsExplored(Side.Player, cell) ? (byte)1 : (byte)0;
            if (_terrainMask[i] != mask)
                changed.Add(cell);
            _terrainMask[i] = mask;
        }

        _terrainCache?.Ensure(Projection, Board, DrawUnexploredWorld, DrawStaticWorld, DrawStaticShore, DrawIslandScenery);
        if (!ReferenceEquals(_terrainProjection, Projection) || !ReferenceEquals(_terrainBattle, Battle))
            _radarKey = null;
        _terrainProjection = Projection;
        _terrainBattle = Battle;
        if (_terrainCache?.Invalidate(changed, force || newWorld, cell => Battle.Vision.IsVisible(Side.Player, cell) ? (byte)2 : Battle.Vision.IsExplored(Side.Player, cell) ? (byte)1 : (byte)0) == true)
            TerrainTextureUpdateRequests++;
        RefreshOverlays();
    }

    public void Select(GridPosition? position)
    {
        Selected = position is { } cell && Board.Contains(cell) ? cell : null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        using var trace = PerformanceTrace.Measure("Board.Overlay.Draw");
        if (!ReferenceEquals(_terrainProjection, Projection) || !ReferenceEquals(_terrainBattle, Battle))
        {
            _terrainProjection = Projection;
            _terrainBattle = Battle;
            _radarKey = null;
            InvalidateWorld();
        }

        DrawProjectedCoverage();
        DrawTreasureRoute();
        if (Selected is { } selected && !_resourceCells.Contains(selected))
        {
            DrawPolyline(Projection.ClosedOutline(selected), new Color("ffe298"), 3, true);
        }
    }

    internal void RefreshOverlays()
    {
        _resourceCells.Clear();
        _resourceCells.UnionWith(Battle.KnownFish(Side.Player));
        _resourceCells.UnionWith(Battle.KnownShoals(Side.Player));
        _observations?.QueueRedraw();
        QueueRedraw();
    }

    private void DrawObservedWorld(Node2D canvas)
    {
        using var trace = PerformanceTrace.Measure("Board.Observation.Draw");
        ObservationDrawCount++;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var viewportSize = GetViewportRect().Size;
        var drawBounds = new Rect2(inverse * Vector2.Zero, Vector2.Zero).Expand(inverse * new Vector2(viewportSize.X, 0)).Expand(inverse * viewportSize).Expand(inverse * new Vector2(0, viewportSize.Y)).Grow(140);
        DrawTreasuries(canvas);
        DrawMovementContour(canvas, Building ? new Color(0.6f, 1, 0.8f, 0.8f) : new Color(0.64f, 0.82f, 1, 0.72f));
        foreach (var cell in Targets)
        {
            canvas.DrawPolyline(Projection.ClosedOutline(cell), new Color("ff9678"), 2, true);
        }

        if (SelectedShipId is { } id && Battle.FindObserved(Side.Player, id)is { Owner: Side.Player } ship)
        {
            if (ship.RadarRange > 0)
                DrawTileContour(canvas, ship.Position, ship.RadarRange, new Color(0.5f, 1, 0.65f, 0.38f));
        }

        foreach (var cell in Collection.Concat(DockSites).Distinct())
        {
            var center = Projection.GridToWorld(cell);
            if (!drawBounds.HasPoint(center))
                continue;
            var glow = DockSites.Contains(cell) ? new Color("96e5cb") : new Color("ffe49a");
            canvas.DrawSetTransform(center, 0, new Vector2(1, .48f));
            canvas.DrawArc(Vector2.Zero, 23, 0, Mathf.Tau, 24, new Color(glow, .27f), 3, true);
            canvas.DrawArc(Vector2.Zero, 23, 0, Mathf.Tau, 24, new Color(glow, .65f), 1, true);
            canvas.DrawSetTransform(Vector2.Zero);
        }

        foreach (var contact in Battle.Vision.Contacts(Side.Player))
        {
            var point = Projection.GridToWorld(contact);
            canvas.DrawArc(point, 13, 0, Mathf.Tau, 24, new Color("9bffac"), 2, true);
            canvas.DrawString(ThemeDB.FallbackFont, point + new Vector2(-5, 6), "?", fontSize: 19, modulate: new Color("ceffd1"));
        }
    }

    private void DrawUnexploredWorld(Node2D canvas)
    {
        foreach (var tile in Board.Tiles)
        {
            canvas.DrawColoredPolygon(Projection.Diamond(tile.Position), new Color("192a36"));
            canvas.DrawPolyline(Projection.ClosedOutline(tile.Position), new Color(.12f, .27f, .31f, .32f), .8f, true);
        }
    }

    private void DrawStaticShore(Node2D canvas, ISet<GridPosition> cells)
    {
        using var trace = PerformanceTrace.Measure("Board.Terrain.Shore.Draw");
        DrawCoastalWater(canvas, cells);
        DrawBeaches(canvas, cells);
    }

    private void DrawStaticWorld(Node2D canvas, ISet<GridPosition> cells)
    {
        using var trace = PerformanceTrace.Measure("Board.Terrain.Draw");
        TerrainDrawCount++;
        EnsureIslandGeometry();
        foreach (var tile in Board.Tiles)
        {
            if (!cells.Contains(tile.Position))
                continue;
            var vertices = Projection.Diamond(tile.Position);
            var terrain = tile.Terrain;
            var color = terrain switch
            {
                TerrainType.Land => new Color("77ab68"),
                TerrainType.Coast => new Color("30596b"),
                TerrainType.Water => new Color("30596b"),
                _ => new Color("192a36")};
            var location = Board.Center(tile.Position);
            float tone = (float)System.Math.Sin(location.X * .33 + location.Y * .27 + Board.Seed % 17) * .022f;
            color = tone > 0 ? color.Lightened(tone) : color.Darkened(-tone);
            if (terrain == TerrainType.Land && _landShapes.TryGetValue(tile.Position, out var landShapes))
            {
                // Water under a rounded coast fills the corner cut from the island union.
                canvas.DrawColoredPolygon(vertices, new Color("30596b"));
                foreach (var shape in landShapes)
                    canvas.DrawColoredPolygon(shape, color);
            }
            else
                canvas.DrawColoredPolygon(vertices, color);
            canvas.DrawPolyline(Projection.ClosedOutline(tile.Position), new Color(0.12f, 0.27f, 0.31f, 0.32f), .8f, true);
        }
    }

    public Vector2 VillageFlagPosition(GridPosition cell) => Projection.GridToWorld(cell) + new Vector2(19, -39);
    private void DrawVillage(Node2D canvas, Village village, Vector2? at = null)
    {
        var center = at ?? Projection.GridToWorld(village.Position);
        var accent = FleetPalette.For(Battle, village.Owner);
        var ground = new[]
        {
            new Vector2(-24, -5),
            new(0, -14),
            new(25, 0),
            new(1, 11)
        }.Select(p => p + center).ToArray();
        canvas.DrawColoredPolygon(ground, new Color("888e75"));
        canvas.DrawPolyline(ground.Append(ground[0]).ToArray(), new Color("dfd3a7"), 2, true);
        for (int i = 0; i < 3; i++)
        {
            var p = center + new Vector2((i - 1) * 14, i == 1 ? -10 : 0);
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-7, -3), p + new Vector2(4, -1), p + new Vector2(4, -13), p + new Vector2(-7, -15) }, new Color("e1cba1"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(4, -1), p + new Vector2(11, -5), p + new Vector2(11, -17), p + new Vector2(4, -13) }, new Color("ab9d7e"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-9, -15), p + new Vector2(1, -22), p + new Vector2(13, -17), p + new Vector2(4, -11) }, accent.Darkened(.3f));
            canvas.DrawLine(p + new Vector2(-2, -3), p + new Vector2(-2, -10), new Color("566264"), 3, true);
        }

        if (village.IsFortified)
        {
            canvas.DrawPolyline(new[] { center + new Vector2(-26, -5), center + new Vector2(-26, 3), center + new Vector2(0, 16), center + new Vector2(26, 4), center + new Vector2(26, -3) }, new Color("b9c2b9"), 5, true);
            for (int i = -2; i <= 2; i++)
                canvas.DrawRect(new Rect2(center + new Vector2(i * 10 - 3, 8 - System.Math.Abs(i) * 4), new Vector2(6, 6)), new Color("d4d7c7"));
        }

        canvas.DrawColoredPolygon(new[] { center + new Vector2(-28, -7), center + new Vector2(-17, -7), center + new Vector2(-20, -23), center + new Vector2(-25, -23) }, new Color("c6bb96"));
        var flag = center + new Vector2(19, -39);
        bool capture = Battle.CanCaptureVillage(Side.Player, village.Id);
        if (capture)
        {
            canvas.DrawCircle(flag, 16, new Color(1, .85f, .45f, .13f));
            canvas.DrawArc(flag, 15, 0, Mathf.Tau, 28, new Color("ffe29a"), 2, true);
        }

        if (capture || village.Owner is not null)
        {
            canvas.DrawLine(flag + new Vector2(-6, 15), flag + new Vector2(-6, -8), new Color("eadfc2"), 2, true);
        }

        if (village.Health >= 0)
        {
            var nameWidth = ThemeDB.FallbackFont.GetStringSize(village.Name, fontSize: 13).X;
            canvas.DrawString(ThemeDB.FallbackFont, center + new Vector2(-nameWidth * .5f, 20), village.Name, fontSize: 13, modulate: new Color("f0e6ce"));
            for (int i = 0; i < 5; i++)
                canvas.DrawRect(new Rect2(center + new Vector2(-18 + i * 8, 25), new Vector2(5, 4)), i < village.Level ? accent : new Color("475857"));
            canvas.DrawString(ThemeDB.FallbackFont, center + new Vector2(-8, -34), village.Health.ToString("0.##"), fontSize: 14, modulate: new Color("85e6a0"));
        }
    }

    private void DrawMovementContour(Node2D canvas, Color color)
    {
        if (!ReferenceEquals(_cachedReachable, Reachable) || !ReferenceEquals(_cachedProjection, Projection))
        {
            using var trace = PerformanceTrace.Measure("Board.Movement.Contours");
            _movementBoundary = Projection.BoundarySegments(Reachable).ToArray();
            var glows = new List<Vector2[]>();
            foreach (var(edge, inside)in _movementBoundary)
                for (int i = 0; i < edge.Length - 1; i++)
                {
                    var a = edge[i];
                    var b = edge[i + 1];
                    glows.Add(new[] { a, b, b.MoveToward(inside, System.Math.Min(7, b.DistanceTo(inside) * .3f)), a.MoveToward(inside, System.Math.Min(7, a.DistanceTo(inside) * .3f)) });
                }

            _movementGlow = glows.ToArray();
            _cachedReachable = Reachable;
            _cachedProjection = Projection;
        }

        var outer = new Color(color, .2f);
        var inner = new Color(color, 0);
        _movementGlowColors[0] = outer;
        _movementGlowColors[1] = outer;
        _movementGlowColors[2] = inner;
        _movementGlowColors[3] = inner;
        foreach (var glow in _movementGlow)
            canvas.DrawPolygon(glow, _movementGlowColors);
        foreach (var(edge, inside)in _movementBoundary)
        {
            canvas.DrawPolyline(edge, color, 1.7f, true);
        }
    }

    private void DrawTileContour(Node2D canvas, GridPosition origin, int radius, Color color)
    {
        if (_radarKey != (origin, radius))
        {
            _radarKey = (origin, radius);
            _radarBoundary = Projection.BoundaryEdges(Board.Tiles.Where(t => Board.InRadius(origin, t.Position, radius)).Select(t => t.Position)).ToArray();
        }

        foreach (var edge in _radarBoundary)
            canvas.DrawPolyline(edge, color, 2, true);
    }
}
