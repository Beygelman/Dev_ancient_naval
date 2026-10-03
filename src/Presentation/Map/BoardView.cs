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
    private BoardTerrainLayer? _terrainLayer;
    private SubViewport? _terrainViewport;
    private Sprite2D? _terrainSprite;
    private IsometricProjection? _terrainProjection;
    private BattleState? _terrainBattle;
    private IsometricProjection? _maskProjection;
    private byte[] _terrainMask = System.Array.Empty<byte>();
    private (GridPosition Origin, int Radius)? _radarKey;
    private Vector2[][] _radarBoundary = System.Array.Empty<Vector2[]>();
    internal int TerrainDrawCount { get; private set; }
    internal int TerrainTextureUpdateRequests { get; private set; }
    internal Vector2I TerrainTextureSize => _terrainViewport?.Size ?? Vector2I.Zero;
    internal bool TerrainTextureIdle => _terrainViewport?.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled;
    private const int MaximumTerrainTextureAxis = 4096;
    private const long MaximumTerrainTexturePixels = 8_388_608;

    public override void _Ready()
    {
        _terrainViewport = new SubViewport
        {
            Name = "TerrainCache",
            Disable3D = true,
            TransparentBg = true,
            World2D = new World2D(),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            RenderTargetClearMode = SubViewport.ClearMode.Always
        };
        AddChild(_terrainViewport);
        _terrainLayer = new BoardTerrainLayer { Name = "TerrainSource", DrawWorld = DrawStaticWorld };
        _terrainViewport.AddChild(_terrainLayer);
        _terrainSprite = new Sprite2D { Name = "TerrainImage", Centered = false, ShowBehindParent = true,
            Texture = _terrainViewport.GetTexture(), TextureFilter = TextureFilterEnum.Linear };
        AddChild(_terrainSprite);
        ConfigureTerrainTexture();
        RequestTerrainTextureUpdate();
    }

    private void ConfigureTerrainTexture()
    {
        if (_terrainViewport is null || _terrainLayer is null || _terrainSprite is null) return;
        var bounds = Projection.BoardBounds(Board).Grow(80);
        double scale = System.Math.Min(1, System.Math.Min(MaximumTerrainTextureAxis / (double)bounds.Size.X,
            MaximumTerrainTextureAxis / (double)bounds.Size.Y));
        scale = System.Math.Min(scale, System.Math.Sqrt(MaximumTerrainTexturePixels / ((double)bounds.Size.X * bounds.Size.Y)));
        int width = System.Math.Max(1, (int)System.Math.Floor(bounds.Size.X * scale));
        int height = System.Math.Max(1, (int)System.Math.Floor(bounds.Size.Y * scale));
        _terrainViewport.Size = new Vector2I(width, height);
        _terrainLayer.Scale = Vector2.One * (float)scale;
        _terrainLayer.Position = -bounds.Position * (float)scale;
        _terrainSprite.Position = bounds.Position;
        _terrainSprite.Scale = Vector2.One / (float)scale;
    }

    private void RequestTerrainTextureUpdate()
    {
        if (_terrainViewport is null || _terrainLayer is null) return;
        _terrainLayer.QueueRedraw();
        _terrainViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        TerrainTextureUpdateRequests++;
    }

    /// <summary>World/fog changes invalidate retained terrain draw commands.
    /// Hovering or selecting only redraws the small command overlay.</summary>
    public void InvalidateWorld(bool force = true)
    {
        bool changed = !ReferenceEquals(_maskProjection, Projection) || _terrainMask.Length != Board.Tiles.Count;
        if (changed)
        {
            _maskProjection = Projection;
            _terrainMask = new byte[Board.Tiles.Count];
        }
        for (int i = 0; i < Board.Tiles.Count; i++)
        {
            var cell = Board.Tiles[i].Position;
            byte mask = Battle.Vision.IsVisible(Side.Player, cell) ? (byte)2 : Battle.Vision.IsExplored(Side.Player, cell) ? (byte)1 : (byte)0;
            changed |= _terrainMask[i] != mask;
            _terrainMask[i] = mask;
        }
        if (!ReferenceEquals(_terrainProjection, Projection)) ConfigureTerrainTexture();
        if (force || changed) RequestTerrainTextureUpdate();
        QueueRedraw();
    }

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
    internal int TerrainSurfaceSourceCount => _terrainCache?.SurfaceSourceCount ?? 0;
    internal int TerrainSourceCopies(GridPosition cell) => _terrainCache?.SourceCopies(cell) ?? 0;
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
<<<<<<< Updated upstream
=======
        var inverse = GetGlobalTransformWithCanvas().AffineInverse();
        var viewportSize = GetViewportRect().Size;
        var drawBounds = new Rect2(inverse * Vector2.Zero, Vector2.Zero)
            .Expand(inverse * new Vector2(viewportSize.X, 0))
            .Expand(inverse * viewportSize).Expand(inverse * new Vector2(0, viewportSize.Y)).Grow(140);
>>>>>>> Stashed changes
        if (!ReferenceEquals(_terrainProjection, Projection) || !ReferenceEquals(_terrainBattle, Battle))
        {
            _terrainProjection = Projection;
            _terrainBattle = Battle;
            _radarKey = null;
<<<<<<< Updated upstream
            InvalidateWorld();
        }

        DrawProjectedCoverage();
        DrawTreasureRoute();
        if (Selected is { } selected && !_resourceCells.Contains(selected))
        {
            DrawPolyline(Projection.ClosedOutline(selected), new Color("ffe298"), 3, true);
=======
            ConfigureTerrainTexture();
            RequestTerrainTextureUpdate();
        }
        DrawTreasuries();
        DrawMovementContour(Building ? new Color(0.6f, 1, 0.8f, 0.8f) : new Color(0.64f, 0.82f, 1, 0.72f));
        foreach (var cell in Targets)
        {
            DrawPolyline(Projection.ClosedOutline(cell), new Color("ff9678"), 2, true);
        }
        if (SelectedShipId is { } id && Battle.FindObserved(Side.Player, id) is { Owner: Side.Player } ship)
        {
            if (ship.RadarRange > 0) DrawTileContour(ship.Position, ship.RadarRange, new Color(0.5f, 1, 0.65f, 0.38f));
        }
        var fish = Battle.KnownFish(Side.Player).ToHashSet();
        var shoals = Battle.KnownShoals(Side.Player).ToHashSet();
        var collectible = Collection.ToHashSet();
        var dockable = DockSites.ToHashSet();
        foreach (var cell in fish)
        {
            var p = Projection.GridToWorld(cell);
            if (!drawBounds.HasPoint(p)) continue;
            DrawFish(p, 1, new Color("d6d39a"), collectible.Contains(cell) ? new Color("ffe49a") : null);
        }
        foreach (var cell in shoals)
        {
            var p = Projection.GridToWorld(cell);
            if (!drawBounds.HasPoint(p)) continue;
            for (int i = 0; i < 3; i++)
            {
                var q = p + new Vector2((i - 1) * 13, i == 1 ? -6 : 3);
                DrawFish(q, .7f, new Color("f4df89"), dockable.Contains(cell) ? new Color("96e5cb") : null);
            }
        }
        foreach (var village in Battle.ObservedVillages(Side.Player))
            if (drawBounds.HasPoint(Projection.GridToWorld(village.Position))) DrawVillage(village);
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
        if (Selected is { } selected && !fish.Contains(selected) && !shoals.Contains(selected))
        {
            DrawPolyline(Projection.ClosedOutline(selected), new Color("ffe298"), 3, true);
        }
    }

    private void DrawStaticWorld(Node2D canvas)
    {
        using var trace = PerformanceTrace.Measure("Board.Terrain.Draw");
        TerrainDrawCount++;
        EnsureIslandGeometry();
        foreach (var tile in Board.Tiles)
        {
            var vertices = Projection.Diamond(tile.Position);
            var terrain = Battle.Vision.KnownTerrain(Side.Player, tile.Position);
            var color = terrain switch
            {
                TerrainType.Land => new Color("77ab68"),
                TerrainType.Coast => new Color("30596b"),
                TerrainType.Water => new Color("30596b"),
                _ => new Color("192a36")
            };
            var location = Board.Center(tile.Position);
            float tone = (float)System.Math.Sin(location.X * .33 + location.Y * .27 + Board.Seed % 17) * .022f;
            color = tone > 0 ? color.Lightened(tone) : color.Darkened(-tone);
            if (terrain is not null && !Battle.Vision.IsVisible(Side.Player, tile.Position)) color = color.Darkened(0.57f);
            if (terrain == TerrainType.Land && _landShapes.TryGetValue(tile.Position, out var landShapes))
            {
                // Water under a rounded coast fills the corner cut from the island union.
                canvas.DrawColoredPolygon(vertices, new Color("30596b").Darkened(Battle.Vision.IsVisible(Side.Player, tile.Position) ? 0 : .57f));
                foreach (var shape in landShapes) canvas.DrawColoredPolygon(shape, color);
            }
            else canvas.DrawColoredPolygon(vertices, color);
            canvas.DrawPolyline(Projection.ClosedOutline(tile.Position),
                new Color(0.12f, 0.27f, 0.31f, 0.32f), .8f, true);
        }
        DrawCoastalWater(canvas);
        DrawBeaches(canvas);
        DrawIslandScenery(canvas);
    }

    private void DrawFish(Vector2 center, float size, Color color, Color? highlight)
    {
        var shape = new[] { new Vector2(-8, 0), new(-15, -5), new(-15, 5), new(-8, 0), new(-3, -5), new(6, -4), new(10, 0), new(6, 4), new(-3, 5) };
        var points = shape.Select(p => center + p * size).ToArray();
        var outline = points.Append(points[0]).ToArray();
        if (highlight is { } glow) DrawPolyline(outline, new Color(glow, .22f), 7, true);
        // Separate the tail from the body: the shared neck is a point, not a self-intersecting polygon.
        DrawColoredPolygon(points.Take(3).ToArray(), color);
        DrawColoredPolygon(points.Skip(3).ToArray(), highlight ?? color);
        if (highlight is { } accent) DrawPolyline(outline, accent, 1.7f, true);
        DrawCircle(center + new Vector2(5, -1) * size, 1.2f * size, new Color("355468"));
    }

    public Vector2 VillageFlagPosition(GridPosition cell) => Projection.GridToWorld(cell) + new Vector2(19, -39);

    private void DrawVillage(Village village)
    {
        var center = Projection.GridToWorld(village.Position);
        var accent = FleetPalette.For(Battle, village.Owner);
        var ground = new[] { new Vector2(-24, -5), new(0, -14), new(25, 0), new(1, 11) }.Select(p => p + center).ToArray();
        DrawColoredPolygon(ground, new Color("888e75"));
        DrawPolyline(ground.Append(ground[0]).ToArray(), new Color("dfd3a7"), 2, true);
        for (int i = 0; i < 3; i++)
        {
            var p = center + new Vector2((i - 1) * 14, i == 1 ? -10 : 0);
            DrawColoredPolygon(new[] { p + new Vector2(-7, -3), p + new Vector2(4, -1), p + new Vector2(4, -13), p + new Vector2(-7, -15) }, new Color("e1cba1"));
            DrawColoredPolygon(new[] { p + new Vector2(4, -1), p + new Vector2(11, -5), p + new Vector2(11, -17), p + new Vector2(4, -13) }, new Color("ab9d7e"));
            DrawColoredPolygon(new[] { p + new Vector2(-9, -15), p + new Vector2(1, -22), p + new Vector2(13, -17), p + new Vector2(4, -11) }, accent.Darkened(.3f));
            DrawLine(p + new Vector2(-2, -3), p + new Vector2(-2, -10), new Color("566264"), 3, true);
        }
        if (village.IsFortified)
        {
            DrawPolyline(new[] { center + new Vector2(-26, -5), center + new Vector2(-26, 3), center + new Vector2(0, 16), center + new Vector2(26, 4), center + new Vector2(26, -3) }, new Color("b9c2b9"), 5, true);
            for (int i = -2; i <= 2; i++) DrawRect(new Rect2(center + new Vector2(i * 10 - 3, 8 - System.Math.Abs(i) * 4), new Vector2(6, 6)), new Color("d4d7c7"));
        }
        DrawColoredPolygon(new[] { center + new Vector2(-28, -7), center + new Vector2(-17, -7), center + new Vector2(-20, -23), center + new Vector2(-25, -23) }, new Color("c6bb96"));
        var flag = VillageFlagPosition(village.Position);
        bool capture = Battle.CanCaptureVillage(Side.Player, village.Id);
        if (capture)
        {
            DrawCircle(flag, 16, new Color(1, .85f, .45f, .13f));
            DrawArc(flag, 15, 0, Mathf.Tau, 28, new Color("ffe29a"), 2, true);
        }
        if (capture || village.Owner is not null)
        {
            DrawLine(flag + new Vector2(-6, 15), flag + new Vector2(-6, -8), new Color("eadfc2"), 2, true);
        }
        if (village.Health >= 0)
        {
            var health = center + new Vector2(-19, 18);
            DrawRect(new Rect2(health, new Vector2(38, 4)), new Color("273b43"));
            DrawRect(new Rect2(health, new Vector2(38 * (float)(village.Health / village.MaxHealth), 4)), new Color("85e6a0"));
            for (int i = 0; i < 5; i++) DrawRect(new Rect2(center + new Vector2(-18 + i * 8, 25), new Vector2(5, 4)), i < village.Level ? accent : new Color("475857"));
            DrawString(ThemeDB.FallbackFont, center + new Vector2(-8, -34), village.Health.ToString("0.##"), fontSize: 14, modulate: new Color("85e6a0"));
        }
    }

    private void DrawMovementContour(Color color)
    {
        if (!ReferenceEquals(_cachedReachable, Reachable) || !ReferenceEquals(_cachedProjection, Projection))
        {
            using var trace = PerformanceTrace.Measure("Board.Movement.Contours");
            _movementBoundary = Projection.BoundarySegments(Reachable).ToArray();
            var glows = new List<Vector2[]>();
            foreach (var (edge, inside) in _movementBoundary)
            for (int i = 0; i < edge.Length - 1; i++)
            {
                var a = edge[i]; var b = edge[i + 1];
                glows.Add(new[] { a, b, b.MoveToward(inside, System.Math.Min(7, b.DistanceTo(inside) * .3f)),
                    a.MoveToward(inside, System.Math.Min(7, a.DistanceTo(inside) * .3f)) });
            }
            _movementGlow = glows.ToArray();
            _cachedReachable = Reachable; _cachedProjection = Projection;
        }
        var outer = new Color(color, .2f); var inner = new Color(color, 0);
        _movementGlowColors[0] = outer;
        _movementGlowColors[1] = outer;
        _movementGlowColors[2] = inner;
        _movementGlowColors[3] = inner;
        foreach (var glow in _movementGlow) DrawPolygon(glow, _movementGlowColors);
        foreach (var (edge, inside) in _movementBoundary)
        {
            DrawPolyline(edge, color, 1.7f, true);
>>>>>>> Stashed changes
        }
    }

    internal void RefreshOverlays()
    {
<<<<<<< Updated upstream
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
=======
>>>>>>> Stashed changes
        if (_radarKey != (origin, radius))
        {
            _radarKey = (origin, radius);
            _radarBoundary = Projection.BoundaryEdges(Board.Tiles.Where(t => Board.InRadius(origin, t.Position, radius)).Select(t => t.Position)).ToArray();
        }
<<<<<<< Updated upstream

        foreach (var edge in _radarBoundary)
            canvas.DrawPolyline(edge, color, 2, true);
    }
=======
        foreach (var edge in _radarBoundary) DrawPolyline(edge, color, 2, true);
    }
}

public partial class BoardTerrainLayer : Node2D
{
    internal System.Action<Node2D>? DrawWorld { get; set; }
    public override void _Draw() => DrawWorld?.Invoke(this);
>>>>>>> Stashed changes
}
