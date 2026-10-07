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
    private WorldEdgeFalls? _edgeFalls;
    internal int EdgeFallsBuildCount => _edgeFalls?.BuildCount ?? 0;
    internal int EdgeFallsExposedEdgeCount => _edgeFalls?.ExposedEdgeCount ?? 0;
    private TerrainDetailCache? _terrainDetail;
    private Transform2D _detailViewTransform;
    private Vector2 _detailViewSize;
    private bool _detailViewInitialized;
    private bool _farScenery;
    internal bool FarSceneryActive => _farScenery;
    internal int TerrainDetailChunkCount => _terrainDetail?.ChunkCount ?? 0;
    internal int TerrainDetailBuildCount => _terrainDetail?.BuildCount ?? 0;
    internal bool TerrainDetailIdle => _terrainDetail?.Idle == true;
    internal float TerrainDetailSampleScale => _terrainDetail?.SampleScale ?? 0;
    internal long TerrainDetailPixels => _terrainDetail?.AllocatedPixels ?? 0;
    internal bool TerrainDetailVisibilityCurrent => _terrainDetail?.VisibilityCurrent == true;
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
    internal int TerrainSuppressedRegionCount => _terrainCache?.SuppressedRegionCount ?? 0;
    internal bool TerrainSuppressedImagesHidden => _terrainCache?.SuppressedImagesHidden == true;
    internal bool TerrainCoarseVisibilityCurrent => _terrainCache?.VisibilityCurrent == true;
    internal int TerrainRegionCount => _terrainCache?.RegionCount ?? 0;
    internal int TerrainSurfaceSourceCount => _terrainCache?.SurfaceSourceCount ?? 0;
    internal int TerrainSourceCopies(GridPosition cell) => _terrainCache?.SourceCopies(cell) ?? 0;
    internal int TerrainVisibilityUpdateCount => _terrainCache?.VisibilityChanges ?? 0;
    internal int TerrainKnownCellCount => _terrainCache?.KnownCellCount ?? 0;

    public override void _Ready()
    {
        _edgeFalls = new WorldEdgeFalls { Name = "WorldEdgeFalls", ShowBehindParent = true, ZIndex = -2 };
        AddChild(_edgeFalls);
        _terrainCache = new TerrainRasterCache
        {
            Name = "LandscapeCache",
            ShowBehindParent = true
        };
        AddChild(_terrainCache);
        _terrainDetail = new TerrainDetailCache { Name = "LandscapeDetail", ShowBehindParent = true };
        AddChild(_terrainDetail);
        _observations = new BoardTerrainLayer
        {
            Name = "KnownObjects",
            ShowBehindParent = true,
            DrawWorld = DrawObservedWorld
        };
        AddChild(_observations);
        _terrainCache.Ensure(Projection, Board, DrawUnexploredWorld, DrawStaticWorld, DrawStaticShore, DrawIslandScenery);
        _terrainDetail.Ensure(Projection, Board, DrawUnexploredWorld, TerrainVisibility, DrawStaticWorld, DrawStaticShore, DrawIslandScenery);
        _terrainProjection = Projection;
        _terrainBattle = Battle;
        InvalidateWorld();
    }

    private byte TerrainVisibility(GridPosition cell) => Battle.Vision.IsVisible(Side.Player, cell)
        ? (byte)2 : Battle.Vision.IsExplored(Side.Player, cell) ? (byte)1 : (byte)0;

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        var transform = GetGlobalTransformWithCanvas();
        var size = GetViewportRect().Size;
        if (_detailViewInitialized && transform == _detailViewTransform && size == _detailViewSize) return;
        _detailViewInitialized = true; _detailViewTransform = transform; _detailViewSize = size;
        var inverse = transform.AffineInverse();
        var bounds = new Rect2(inverse * Vector2.Zero, Vector2.Zero).Expand(inverse * new Vector2(size.X, 0))
            .Expand(inverse * size).Expand(inverse * new Vector2(0, size.Y));
        float zoom = transform.X.Length();
        _terrainDetail?.View(bounds, size, zoom);
        // One distant tier, with a small hysteresis gap: scroll-wheel jitter
        // cannot alternate models or rebuild textures at a single threshold.
        bool far = _farScenery ? zoom < .52f : zoom < .44f;
        if (far == _farScenery) return;
        SetDistantDetail(far);
    }

    internal void SetDistantDetail(bool far)
    {
        _farScenery = far;
        SetSceneryLod(far);
        foreach (var (_, canvas) in _townCanvases) canvas.Visible = !far;
        // An explored, hidden town keeps its last painted interface. Redrawing
        // it merely for LOD would consult today's hidden owner/HP/level.
        foreach (var (cell, canvas) in _townInterfaces)
            if (Battle.Vision.IsVisible(Side.Player, cell)) canvas.QueueRedraw();
        if (_depthGroup?.GetParent() is FleetView fleet) fleet.InvalidateLod();
    }

    /// <summary>World/fog changes invalidate retained terrain draw commands.
    /// Hovering or selecting only redraws the small command overlay.</summary>
    public void InvalidateWorld(bool force = true)
    {
        // A repair or shipyard action may change town UI without changing sight.
        _depthVision = -1;
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
        _terrainDetail?.Ensure(Projection, Board, DrawUnexploredWorld, TerrainVisibility, DrawStaticWorld, DrawStaticShore, DrawIslandScenery);
        if (!ReferenceEquals(_terrainProjection, Projection) || !ReferenceEquals(_terrainBattle, Battle))
            _radarKey = null;
        _terrainProjection = Projection;
        _terrainBattle = Battle;
        if (_terrainCache?.Invalidate(changed, force || newWorld, cell => Battle.Vision.IsVisible(Side.Player, cell) ? (byte)2 : Battle.Vision.IsExplored(Side.Player, cell) ? (byte)1 : (byte)0) == true)
            TerrainTextureUpdateRequests++;
        _terrainDetail?.Invalidate(changed, force || newWorld);
        if (newWorld || changed.Count > 0) _edgeFalls?.Rebuild(this);
        if (newWorld) _detailViewInitialized = false;
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
        foreach (var (_, canvas) in _townArt) canvas.QueueRedraw();
        QueueRedraw();
    }

    private void DrawObservedWorld(Node2D canvas)
    {
        using var trace = PerformanceTrace.Measure("Board.Observation.Draw");
        ObservationDrawCount++;
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var viewportSize = GetViewportRect().Size;
        var drawBounds = new Rect2(inverse * Vector2.Zero, Vector2.Zero).Expand(inverse * new Vector2(viewportSize.X, 0)).Expand(inverse * viewportSize).Expand(inverse * new Vector2(0, viewportSize.Y)).Grow(140);
        DrawTradeRoutes(canvas, drawBounds);
        DrawTreasuries(canvas);
        DrawMovementContour(canvas, Building ? new Color(0.6f, 1, 0.8f, 0.8f) : new Color(0.64f, 0.82f, 1, 0.72f));
        // Attack contours belong to observed object silhouettes, never hidden identities.

        if (SelectedShipId is { } id && Battle.FindObserved(Side.Player, id)is { Owner: Side.Player } ship)
        {
            if (ship.RadarRange > 0)
                DrawTileContour(canvas, ship.Position, ship.RadarRange, new Color(0.5f, 1, 0.65f, 0.38f));
        }

        DrawResourceContours(canvas);
        foreach (var contact in Battle.Vision.Contacts(Side.Player))
        {
            var point = Projection.GridToWorld(contact);
            canvas.DrawArc(point, 13, 0, Mathf.Tau, 24, new Color("9bffac"), 2, true);
            canvas.DrawString(ThemeDB.FallbackFont, point + new Vector2(-5, 6), "?", fontSize: 19, modulate: new Color("ceffd1"));
        }
    }

    private void DrawUnexploredWorld(Node2D canvas, ISet<GridPosition> cells)
    {
        foreach (var tile in Board.Tiles)
        {
            if (!cells.Contains(tile.Position)) continue;
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
        var features = TerrainFeatures.For(Board);
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
            color = color.Darkened(terrain == TerrainType.Land ? features.InlandDepth(tile.Position) * .07f :
                System.Math.Min(8, System.Math.Max(0, features.DistanceFromCoast(tile.Position) - 1)) * .012f);
            // Draw water beneath the shared smooth island shape. Filling an
            // entire land tile with sand left pointed tile corners in the sea.
            var seaColor = terrain == TerrainType.Land ? new Color("30596b") : color;
            canvas.DrawColoredPolygon(vertices, seaColor);
            if (_landShapes.TryGetValue(tile.Position, out var landShapes))
            {
                var landColor = new Color("77ab68");
                landColor = tone > 0 ? landColor.Lightened(tone) : landColor.Darkened(-tone);
                landColor = landColor.Darkened(terrain == TerrainType.Land ? features.InlandDepth(tile.Position) * .07f : 0);
                foreach (var shape in landShapes)
                    canvas.DrawColoredPolygon(shape, landColor);
            }
            canvas.DrawPolyline(Projection.ClosedOutline(tile.Position), new Color(0.12f, 0.27f, 0.31f, 0.32f), .8f, true);
        }
    }

    internal static Vector2 VillageFlagOffset => new(13, -47);
    public Vector2 VillageFlagPosition(GridPosition cell) => Projection.GridToWorld(cell) + VillageFlagOffset;
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
