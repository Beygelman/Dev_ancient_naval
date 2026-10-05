using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Independent bounded rasters avoid repainting the entire sea for local fog changes.</summary>
internal partial class TerrainRasterCache : Node2D
{
    private const int MaximumAxis = 4096, RegionAxis = 512, Border = 2;
    private const long MaximumPixels = 8_388_608;
    private readonly Dictionary<GridPosition, Cell> _cells = new();
    private readonly List<Region> _regions = new();
    private readonly Queue<Region> _updates = new();
    private IsometricProjection? _projection;
    private int _layerCount;
    internal Vector2I Size { get; private set; }
    internal int RegionCount => _cells.Count;
    internal int VisibilityChanges { get; private set; }
    internal int KnownCellCount => _cells.Values.Count(cell => cell.Mask != 0);
    internal bool Idle => _regions.All(r => !r.Pending && !r.Rendering && r.Raster.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled);
    internal int SuppressedRegionCount => _regions.Count(r => r.Suppressed);
    internal bool SuppressedImagesHidden => _regions.All(r => !r.Suppressed || !r.Sprite.Visible);
    internal bool VisibilityCurrent => _regions.All(r => r.Sprite.Visible && !r.Pending &&
        r.PaintedGeneration == r.RequestedGeneration &&
        r.Sources.All(p => p.Source.Canvas.Visible == (p.Cell.Mask != 0) &&
            p.Source.Canvas.Modulate == (p.Cell.Mask == 1 ? new Color(.43f, .43f, .43f) : Colors.White)));
    internal int LastUpdatedRegions { get; private set; }
    internal int SurfaceSourceCount => _cells.Values.Sum(c => c.Sources.Count) / _layerCount;
    internal int SourceCopies(GridPosition cell) => _cells[cell].Sources.Count / _layerCount;
    private sealed class Region
    {
        internal SubViewport Raster = null!;
        internal Sprite2D Sprite = null!;
        internal BoardTerrainLayer Background = null!;
        internal bool Pending;
        internal bool Queued;
        internal bool Rendering;
        internal bool Suppressed;
        internal long RequestedGeneration, PreparedGeneration, PaintedGeneration;
        internal readonly List<(Cell Cell, Source Source)> Sources = new();
    }
    private sealed class Source
    {
        internal BoardTerrainLayer Canvas = null!;
        internal Region Region = null!;
        internal long Requested, Prepared, Drawn;
    }
    private sealed class Cell
    {
        internal readonly List<Source> Sources = new();
        internal byte Mask;
    }
    public override void _Ready() => RenderingServer.Singleton.FramePostDraw += CompleteRasterization;
    public override void _ExitTree() => RenderingServer.Singleton.FramePostDraw -= CompleteRasterization;

    internal void Ensure(IsometricProjection projection, GameBoard board, Action<Node2D, ISet<GridPosition>> background,
        params Action<Node2D, ISet<GridPosition>>[] layers)
    {
        if (ReferenceEquals(_projection, projection)) return;
        _projection = projection;
        _layerCount = layers.Length;
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        _cells.Clear(); _regions.Clear(); _updates.Clear();
        var bounds = projection.BoardBounds(board).Grow(80);
        double scale = Math.Min(1, Math.Min(MaximumAxis / (double)bounds.Size.X, MaximumAxis / (double)bounds.Size.Y));
        scale = Math.Min(scale, Math.Sqrt(MaximumPixels / ((double)bounds.Size.X * bounds.Size.Y)));
        Size = new(Math.Max(1, (int)(bounds.Size.X * scale)), Math.Max(1, (int)(bounds.Size.Y * scale)));
        var boxes = board.Tiles.ToDictionary(t => t.Position, t =>
        {
            var polygon = projection.Diamond(t.Position);
            var box = new Rect2(polygon[0], Vector2.Zero);
            foreach (var point in polygon) box = box.Expand(point);
            return box;
        });
        foreach (var tile in board.Tiles) _cells.Add(tile.Position, new Cell());
        for (int y = 0; y < Size.Y; y += RegionAxis)
        for (int x = 0; x < Size.X; x += RegionAxis)
        {
            var pixels = new Vector2I(Math.Min(RegionAxis, Size.X - x), Math.Min(RegionAxis, Size.Y - y));
            var world = new Rect2(bounds.Position + new Vector2(x, y) / (float)scale, (Vector2)pixels / (float)scale);
            var covered = boxes.Where(p => p.Value.Grow(80).Intersects(world.Grow(Border / (float)scale))).Select(p => p.Key).ToHashSet();
            // An empty corner has no source to invalidate. Do not present its
            // uninitialized viewport texture as an opaque rectangle.
            if (covered.Count == 0) continue;
            var region = new Region();
            region.Raster = new SubViewport { Name = $"TerrainRegion{x}_{y}", Size = pixels + Vector2I.One * Border * 2,
                Disable3D = true, TransparentBg = true, World2D = new World2D(),
                RenderTargetClearMode = SubViewport.ClearMode.Always, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled };
            AddChild(region.Raster); _regions.Add(region);
            region.Sprite = new Sprite2D { Name = $"TerrainImage{x}_{y}", Centered = false, Position = world.Position,
                Visible = false,
                Scale = Vector2.One / (float)scale, TextureFilter = TextureFilterEnum.Linear,
                Texture = new AtlasTexture { Atlas = region.Raster.GetTexture(), Region = new Rect2(Vector2.One * Border, pixels), FilterClip = true } };
            AddChild(region.Sprite);
            var origin = new Node2D { Position = -world.Position * (float)scale + Vector2.One * Border, Scale = Vector2.One * (float)scale };
            region.Raster.AddChild(origin);
            // Include geometry spilling across region edges, but present disjoint texture interiors.
            var backgroundCells = boxes.Where(p => p.Value.Intersects(world.Grow(Border / (float)scale))).Select(p => p.Key).ToHashSet();
            region.Background = new BoardTerrainLayer { Name = "UnexploredSea", DrawWorld = n =>
            {
                background(n, backgroundCells);
                region.PaintedGeneration = region.PreparedGeneration;
            } };
            origin.AddChild(region.Background);
            foreach (var draw in layers)
            {
                var group = new Node2D(); origin.AddChild(group);
                foreach (var position in covered)
                {
                    var cell = _cells[position];
                    var source = new Source { Region = region };
                    var single = new HashSet<GridPosition> { position };
                    source.Canvas = new BoardTerrainLayer { Visible = false,
                        DrawWorld = n => { draw(n, single); source.Drawn = source.Prepared; } };
                    group.AddChild(source.Canvas); cell.Sources.Add(source); region.Sources.Add((cell, source));
                }
            }
        }
    }
    internal bool Invalidate(ISet<GridPosition> changed, bool force, Func<GridPosition, byte> visibility)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Coarse.Invalidate");
        if (force)
        {
            _updates.Clear();
            foreach (var region in _regions) region.Queued = false;
        }
        var updated = new HashSet<Region>();
        var downgraded = new HashSet<Region>();
        foreach (var (position, cell) in _cells)
        {
            if (!force && !changed.Contains(position)) continue;
            byte mask = visibility(position);
            bool discover = cell.Mask == 0 && mask != 0;
            bool downgrade = mask < cell.Mask;
            if (cell.Mask != mask) VisibilityChanges++;
            cell.Mask = mask;
            foreach (var source in cell.Sources)
            {
                if (mask != 0 && (discover || force)) source.Requested++;
                updated.Add(source.Region);
                if (downgrade) downgraded.Add(source.Region);
            }
        }
        foreach (var region in updated)
        {
            region.RequestedGeneration++;
            region.Pending = true;
            // A previously optical raster cannot remain on screen while a
            // downgrade waits behind other regions. Upgrades keep their safe
            // older image; only freshly painted current generations restore a
            // suppressed image. This also covers a God's-eye reveal being closed.
            if (downgraded.Contains(region))
            {
                region.Suppressed = true;
                region.Sprite.Visible = false;
            }
            if (force)
            {
                PrepareRegion(region);
                region.Rendering = true;
                region.Raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            }
            else if (!region.Queued && !region.Rendering)
            {
                region.Queued = true;
                _updates.Enqueue(region);
            }
        }
        if (!force && !_regions.Any(r => r.Rendering)) StartNextRegion();
        LastUpdatedRegions = updated.Count;
        return updated.Count > 0;
    }
    private void CompleteRasterization()
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Coarse.FinishFrame");
        foreach (var region in _regions)
        {
            if (!region.Pending || !region.Rendering || region.Queued) continue;
            if (region.PaintedGeneration != region.RequestedGeneration ||
                region.Sources.Any(p => p.Source.Canvas.Visible != (p.Cell.Mask != 0)
                    || p.Source.Canvas.Modulate != (p.Cell.Mask == 1 ? new Color(.43f, .43f, .43f) : Colors.White)
                    || p.Cell.Mask != 0 && p.Source.Drawn != p.Source.Requested))
            {
                // Once may already have consumed the preceding GPU flight.
                // A tint-only revision also needs a new flight even though its
                // retained source geometry did not redraw.
                PrepareRegion(region);
                region.Raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                continue;
            }
            region.Raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            region.Rendering = false;
            region.Pending = false;
            region.Suppressed = false;
            region.Sprite.Visible = true;
        }
        if (!_regions.Any(r => r.Rendering)) StartNextRegion();
    }

    private static void PrepareRegion(Region region)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Coarse.PrepareRegion");
        // Logical masks change immediately; native retained geometry is touched
        // only for this scheduled flight. A queued downgrade has already hidden
        // its old image, so spreading these commands cannot expose stale sight.
        foreach (var (cell, source) in region.Sources)
        {
            bool visible = cell.Mask != 0;
            var tint = cell.Mask == 1 ? new Color(.43f, .43f, .43f) : Colors.White;
            if (source.Canvas.Visible != visible) source.Canvas.Visible = visible;
            if (source.Canvas.Modulate != tint) source.Canvas.Modulate = tint;
            if (visible && source.Drawn != source.Requested)
            {
                source.Prepared = source.Requested;
                source.Canvas.QueueRedraw();
            }
        }
        region.PreparedGeneration = region.RequestedGeneration;
        region.Background.QueueRedraw();
    }

    private void StartNextRegion()
    {
        while (_updates.TryDequeue(out var region))
        {
            region.Queued = false;
            if (!region.Pending || region.Rendering) continue;
            PrepareRegion(region);
            region.Rendering = true;
            region.Raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            return;
        }
    }
}
