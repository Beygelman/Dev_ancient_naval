using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Retained close-view terrain. The whole-chart raster is a coarse fallback,
/// not the source of pixels at maximum zoom. Only a bounded set of visible chunks is
/// sampled again; panning within a chunk never repaints it.</summary>
internal partial class TerrainDetailCache : Node2D
{
    internal const int ChunkPixels = 512, MaximumChunks = 64, Padding = 4;
    private readonly Dictionary<Vector2I, Chunk> _chunks = new();
    private readonly Queue<Chunk> _pending = new();
    private readonly CanvasItemMaterial _material = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha };
    private readonly Dictionary<GridPosition, Rect2> _boxes = new();
    private IsometricProjection? _projection;
    private GameBoard? _board;
    private Action<Node2D, ISet<GridPosition>>? _background;
    private Action<Node2D, ISet<GridPosition>>[] _layers = Array.Empty<Action<Node2D, ISet<GridPosition>>>();
    private Func<GridPosition, byte>? _visibility;
    private Rect2 _bounds;
    private float _scale;
    private long _age;
    private bool _active;
    internal int BuildCount { get; private set; }
    internal int ChunkCount => _chunks.Count;
    internal float SampleScale => _scale;
    internal bool Idle => _pending.Count == 0 && _chunks.Values.All(c => !c.Rendering && !c.Dirty);
    internal long AllocatedPixels => _chunks.Values.Sum(c => (long)c.Viewport.Size.X * c.Viewport.Size.Y);
    internal bool VisibilityCurrent => _chunks.Values.All(c => !c.Ready ||
        !c.Dirty && !c.Rendering && c.PaintedGeneration == c.RequestedGeneration &&
        c.Sources.All(s => s.Mask == _visibility!(s.Cell) && SourceCurrent(s)));

    private sealed class Source
    {
        internal GridPosition Cell;
        internal BoardTerrainLayer Canvas = null!;
        internal byte Mask;
        internal long Requested, Prepared, Painted;
    }
    private sealed class Chunk
    {
        internal SubViewport Viewport = null!;
        internal Sprite2D Sprite = null!;
        internal readonly List<Source> Sources = new();
        internal BoardTerrainLayer Background = null!;
        internal long RequestedGeneration = 1, PreparedGeneration, PaintedGeneration;
        internal long Age;
        internal bool Queued, Dirty, Rendering, Ready;
    }

    public override void _Ready() => RenderingServer.Singleton.FramePostDraw += FinishFrame;
    public override void _ExitTree() => RenderingServer.Singleton.FramePostDraw -= FinishFrame;

    internal void Ensure(IsometricProjection projection, GameBoard board,
        Action<Node2D, ISet<GridPosition>> background, Func<GridPosition, byte> visibility,
        params Action<Node2D, ISet<GridPosition>>[] layers)
    {
        if (ReferenceEquals(_projection, projection)) return;
        ClearChunks();
        _projection = projection; _board = board; _background = background; _visibility = visibility; _layers = layers;
        _bounds = projection.BoardBounds(board).Grow(80);
        _boxes.Clear();
        foreach (var tile in board.Tiles)
        {
            var polygon = projection.Diamond(tile.Position);
            var box = new Rect2(polygon[0], Vector2.Zero);
            foreach (var p in polygon) box = box.Expand(p);
            _boxes[tile.Position] = box;
        }
    }

    internal void View(Rect2 worldView, Vector2 viewportPixels, float zoom)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Detail.View");
        _active = zoom >= .65f;
        Visible = _active;
        if (!_active || _projection is null || _board is null) return;
        // At most 64 padded 512-pixel textures, independent of world size. Large
        // monitors get a lower supersample ratio, never an unbounded allocation.
        float ratio = Math.Min(2, MathF.Sqrt(10_000_000f / Math.Max(1, viewportPixels.X * viewportPixels.Y)));
        float scale = Math.Clamp(MathF.Floor(zoom * ratio * 2) / 2, .5f, 3f);
        if (_scale != scale) { ClearChunks(); _scale = scale; }
        float step = ChunkPixels / _scale;
        var view = worldView.Grow(Math.Min(step * .6f, 160));
        var needed = new List<Vector2I>();
        int left = Math.Max(0, Mathf.FloorToInt((view.Position.X - _bounds.Position.X) / step));
        int top = Math.Max(0, Mathf.FloorToInt((view.Position.Y - _bounds.Position.Y) / step));
        int right = Math.Min(Mathf.CeilToInt(_bounds.Size.X / step) - 1, Mathf.FloorToInt((view.End.X - _bounds.Position.X) / step));
        int bottom = Math.Min(Mathf.CeilToInt(_bounds.Size.Y / step) - 1, Mathf.FloorToInt((view.End.Y - _bounds.Position.Y) / step));
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++) needed.Add(new(x, y));
        // The nearest chunks win if an unusual viewport exceeds the budget;
        // uncovered chart still uses the existing coarse raster.
        var center = worldView.GetCenter();
        needed.Sort((a, b) => ChunkCenter(a, step).DistanceSquaredTo(center).CompareTo(ChunkCenter(b, step).DistanceSquaredTo(center)));
        if (needed.Count > MaximumChunks) needed.RemoveRange(MaximumChunks, needed.Count - MaximumChunks);
        var wanted = needed.ToHashSet();
        foreach (var key in needed)
        {
            if (!_chunks.TryGetValue(key, out var chunk))
            {
                if (_chunks.Count == MaximumChunks)
                {
                    var old = _chunks.Where(pair => !wanted.Contains(pair.Key)).MinBy(pair => pair.Value.Age);
                    if (old.Value is null) break;
                    RemoveChunk(old.Key, old.Value);
                }
                chunk = BuildChunk(key, step);
                if (chunk is null) continue;
                _chunks.Add(key, chunk);
            }
            chunk.Age = ++_age;
            if (chunk.Dirty) Queue(chunk);
        }
        StartNext();
    }

    private Vector2 ChunkCenter(Vector2I key, float step) => _bounds.Position + ((Vector2)key + Vector2.One * .5f) * step;

    private Chunk? BuildChunk(Vector2I key, float step)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Detail.BuildChunk");
        var world = new Rect2(_bounds.Position + (Vector2)key * step, Vector2.One * step);
        var covered = _boxes.Where(pair => pair.Value.Grow(80).Intersects(world.Grow(Padding / _scale))).Select(pair => pair.Key).ToArray();
        if (covered.Length == 0) return null;
        var chunk = new Chunk { Dirty = true };
        // A plain SubViewport retains its sources for local visibility updates;
        // SceneryAtlasPage's one-shot painter is intentionally not used here.
        chunk.Viewport = new SubViewport
        {
            Name = $"Detail{key.X}_{key.Y}", Size = Vector2I.One * (ChunkPixels + Padding * 2),
            Disable3D = true, TransparentBg = true, World2D = new World2D(),
            RenderTargetClearMode = SubViewport.ClearMode.Always,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled
        };
        AddChild(chunk.Viewport);
        chunk.Sprite = new Sprite2D
        {
            Name = $"DetailImage{key.X}_{key.Y}", Centered = false, Position = world.Position,
            Scale = Vector2.One / _scale, Visible = false, Material = _material,
            TextureFilter = TextureFilterEnum.Linear,
            Texture = new AtlasTexture { Atlas = chunk.Viewport.GetTexture(), Region = new Rect2(Vector2.One * Padding, Vector2.One * ChunkPixels), FilterClip = true }
        };
        AddChild(chunk.Sprite);
        var origin = new Node2D { Position = -world.Position * _scale + Vector2.One * Padding, Scale = Vector2.One * _scale };
        chunk.Viewport.AddChild(origin);
        var backgroundCells = _boxes.Where(pair => pair.Value.Intersects(world.Grow(Padding / _scale))).Select(pair => pair.Key).ToHashSet();
        chunk.Background = new BoardTerrainLayer { DrawWorld = node =>
        {
            _background!(node, backgroundCells);
            chunk.PaintedGeneration = chunk.PreparedGeneration;
        } };
        origin.AddChild(chunk.Background);
        foreach (var layer in _layers)
            foreach (var cell in covered)
            {
                var source = new Source { Cell = cell, Mask = _visibility!(cell), Requested = 1 };
                var single = new HashSet<GridPosition> { cell };
                source.Canvas = new BoardTerrainLayer
                {
                    Visible = false,
                    DrawWorld = node => { layer(node, single); source.Painted = source.Prepared; }
                };
                origin.AddChild(source.Canvas); chunk.Sources.Add(source);
            }
        BuildCount++;
        Queue(chunk);
        return chunk;
    }

    internal void Invalidate(ISet<GridPosition> changed, bool force)
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Detail.Invalidate");
        foreach (var chunk in _chunks.Values)
        {
            bool dirty = false;
            foreach (var source in chunk.Sources)
            {
                if (!force && !changed.Contains(source.Cell)) continue;
                byte mask = _visibility!(source.Cell);
                bool discover = source.Mask == 0 && mask != 0;
                // Record desired state now; native retained commands are updated
                // only for the chunk whose GPU flight is actually starting.
                source.Mask = mask;
                if (mask != 0 && (discover || force)) source.Requested++;
                dirty = true;
            }
            if (!dirty) continue;
            // A previous optical texture may not persist while fog updates are
            // waiting. Reveal only the coarse, already masked fallback meanwhile.
            chunk.Sprite.Visible = false;
            chunk.Dirty = true;
            chunk.RequestedGeneration++;
            Queue(chunk);
        }
        StartNext();
    }

    private void Queue(Chunk chunk)
    {
        if (chunk.Queued || chunk.Rendering) return;
        chunk.Queued = true; _pending.Enqueue(chunk);
    }
    private static Color SourceTint(byte mask) => mask == 1 ? new Color(.43f, .43f, .43f) : Colors.White;
    private static bool SourceCurrent(Source source) => source.Canvas.Visible == (source.Mask != 0)
        && source.Canvas.Modulate == SourceTint(source.Mask)
        && (source.Mask == 0 || source.Painted == source.Requested);

    private static void PrepareChunk(Chunk chunk)
    {
        foreach (var source in chunk.Sources)
        {
            bool visible = source.Mask != 0;
            if (source.Canvas.Visible != visible) source.Canvas.Visible = visible;
            var tint = SourceTint(source.Mask);
            if (source.Canvas.Modulate != tint) source.Canvas.Modulate = tint;
            source.Prepared = source.Requested;
            if (visible && source.Painted != source.Prepared) source.Canvas.QueueRedraw();
        }
        // Stamp the prepared flight, never a later desired generation that can
        // arrive before its queued retained commands have reached the renderer.
        chunk.PreparedGeneration = chunk.RequestedGeneration;
        if (chunk.PaintedGeneration != chunk.PreparedGeneration) chunk.Background.QueueRedraw();
    }
    private void StartNext()
    {
        if (!_active || !IsVisibleInTree() || _chunks.Values.Any(c => c.Rendering)) return;
        while (_pending.TryDequeue(out var chunk))
        {
            if (!GodotObject.IsInstanceValid(chunk.Viewport) || chunk.Viewport.IsQueuedForDeletion()) continue;
            chunk.Queued = false; chunk.Rendering = true;
            PrepareChunk(chunk);
            chunk.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            return;
        }
    }
    private void FinishFrame()
    {
        using var trace = Diagnostics.PerformanceTrace.Measure("Terrain.Detail.FinishFrame");
        foreach (var chunk in _chunks.Values)
        {
            if (!chunk.Rendering) continue;
            if (chunk.PaintedGeneration != chunk.RequestedGeneration || chunk.Sources.Any(s => !SourceCurrent(s)))
            {
                // UpdateMode.Once may already have consumed the preceding
                // flight. Rearm explicitly when fog changed in that flight;
                // neither a stale optical texture nor a stranded queue is valid.
                PrepareChunk(chunk);
                chunk.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                continue;
            }
            chunk.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            chunk.Rendering = false; chunk.Dirty = false; chunk.Ready = true;
            chunk.Sprite.Visible = true;
        }
        StartNext();
    }
    private void RemoveChunk(Vector2I key, Chunk chunk)
    {
        chunk.Sprite.Visible = false;
        chunk.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        chunk.Sprite.QueueFree(); chunk.Viewport.QueueFree(); _chunks.Remove(key);
    }
    private void ClearChunks()
    {
        foreach (var pair in _chunks.ToArray()) RemoveChunk(pair.Key, pair.Value);
        _pending.Clear();
    }
}
