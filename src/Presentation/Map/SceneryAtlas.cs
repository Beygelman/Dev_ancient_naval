using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    // Bake the original primitives once, but retain a separate sprite at every
    // ground anchor. Sharing textures lets Godot batch a forest without moving
    // it outside the fleet's Y sort or merging its per-cell fog ownership.
    private const int SceneryPageSize = 2048;
    private const float SceneryBakeScale = 2;
    private Node? _sceneryAtlasRoot;
    private readonly List<SubViewport> _sceneryPages = new();
    private readonly List<SceneryLodSprite> _sceneryLodSprites = new();
    internal int FarSceneryPageCount { get; private set; }
    // Transparent viewport textures already contain premultiplied edge colors.
    // Multiplying alpha again would give canopies and peaks a dark fringe.
    private readonly CanvasItemMaterial _sceneryMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha
    };
    internal int SceneryAtlasPageCount => _sceneryPages.Count;
    internal int SceneryAtlasRegionCount { get; private set; }
    internal int SceneryAtlasBuildCount { get; private set; }
    internal bool SceneryAtlasIdle => _sceneryPages.TrueForAll(page =>
        page.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled);

    private sealed record SceneryStamp(Scenery Item, Vector2 Anchor);
    private sealed class SceneryLodSprite(Scenery item, Sprite2D sprite, Texture2D detail, Vector2 detailOffset)
    {
        internal readonly Scenery Item = item;
        internal readonly Sprite2D Sprite = sprite;
        internal readonly Texture2D Detail = detail;
        internal readonly Vector2 DetailOffset = detailOffset;
        internal Texture2D? Far;
        internal Vector2 FarOffset;
    }
    private sealed class SceneryShelf(int y, int height)
    {
        internal readonly int Y = y, Height = height;
        internal int X;
    }

    private void BuildSceneryAtlas()
    {
        if (_sceneryAtlasRoot is not null)
        {
            RemoveChild(_sceneryAtlasRoot);
            _sceneryAtlasRoot.QueueFree();
        }
        _sceneryPages.Clear();
        _sceneryLodSprites.Clear();
        FarSceneryPageCount = 0;
        _treasuryRuinSprites.Clear();
        SceneryAtlasRegionCount = 0;
        SceneryAtlasBuildCount++;
        _sceneryAtlasRoot = new Node { Name = "SceneryAtlases" };
        AddChild(_sceneryAtlasRoot);

        SubViewport? page = null;
        List<SceneryStamp>? stamps = null;
        int freeY = 0;
        var shelves = new List<SceneryShelf>();
        // Samples are already sorted by ground Y. Pack in that order so adjacent
        // depth-sorted sprites usually reference the same atlas page.
        foreach (var item in _scenery)
        {
            if (item.Kind == 0) continue;
            float s = item.Size;
            var min = item.Kind == 1
                ? new Vector2(-s - 4, -s * 2.85f - 4)
                : item.Kind == 4 ? new Vector2(-38, -55)
                : item.Kind == 3 ? new Vector2(-s * 1.55f - 4, -s * 2.5f - 4)
                : new Vector2(-s * 1.15f - 3, -s * item.HeightRatio - 3);
            var max = item.Kind == 1
                ? new Vector2(Math.Max(s * 1.3f + 4, 9 + s * .6f), 5 + s * .24f)
                : item.Kind == 4 ? new Vector2(38, 22)
                : item.Kind == 3 ? new Vector2(s * 1.55f + 4, s * .7f + 4)
                : new Vector2(s * 1.15f + 3, s * .60f + 3);
            var pixelMin = new Vector2I(Mathf.FloorToInt(min.X * SceneryBakeScale), Mathf.FloorToInt(min.Y * SceneryBakeScale));
            var pixelMax = new Vector2I(Mathf.CeilToInt(max.X * SceneryBakeScale), Mathf.CeilToInt(max.Y * SceneryBakeScale));
            var size = pixelMax - pixelMin;
            // A mountain must not set the height of an entire row of small
            // trees. Reuse the closest fitting shelf while keeping page order
            // aligned with ground Y for rendering.
            SceneryShelf? shelf = null;
            foreach (var candidate in shelves)
                if (candidate.Height >= size.Y && candidate.X + size.X <= SceneryPageSize &&
                    (shelf is null || candidate.Height < shelf.Height)) shelf = candidate;
            int shelfHeight = (size.Y + 15) / 16 * 16;
            if (page is null || shelf is null && freeY + shelfHeight > SceneryPageSize)
            {
                freeY = 0;
                shelves.Clear();
                shelf = null;
                var atlasPage = new SceneryAtlasPage
                {
                    Name = "Page" + _sceneryPages.Count,
                    Size = Vector2I.One * SceneryPageSize,
                    Disable3D = true,
                    TransparentBg = true,
                    World2D = new World2D(),
                    RenderTargetClearMode = SubViewport.ClearMode.Always,
                    RenderTargetUpdateMode = SubViewport.UpdateMode.Once
                };
                page = atlasPage;
                _sceneryAtlasRoot.AddChild(page);
                _sceneryPages.Add(page);
                var pageStamps = new List<SceneryStamp>();
                stamps = pageStamps;
                atlasPage.SetPainter(SceneryBakeScale, canvas =>
                {
                    foreach (var stamp in pageStamps)
                        DrawSceneryObject(canvas, stamp.Item, stamp.Anchor / SceneryBakeScale);
                });
            }
            if (shelf is null)
            {
                shelf = new SceneryShelf(freeY, shelfHeight);
                shelves.Add(shelf);
                freeY += shelfHeight;
            }
            int x = shelf.X, y = shelf.Y;
            var region = new Rect2I(new Vector2I(x, y), size);
            stamps!.Add(new SceneryStamp(item, new Vector2(x - pixelMin.X, y - pixelMin.Y)));
            var sprite = new Sprite2D
            {
                Name = item.Kind == 1 ? "Tree" : item.Kind == 3 ? "PassiveRuin" : item.Kind == 4 ? "TreasuryRuin" : "Mountain",
                Position = item.Point,
                Centered = false,
                Offset = pixelMin,
                Scale = Vector2.One / SceneryBakeScale,
                Material = _sceneryMaterial,
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
                Texture = new AtlasTexture { Atlas = page.GetTexture(), Region = new Rect2(region.Position, region.Size), FilterClip = true }
            };
            _depthGroup!.AddChild(sprite);
            _sceneryLodSprites.Add(new(item, sprite, sprite.Texture, pixelMin));
            if (item.Kind == 4) _treasuryRuinSprites[item.Cell] = sprite;
            _depthObjects.Add((item.Cell, sprite));
            SceneryAtlasRegionCount++;
            shelf.X += size.X;
        }
        BuildFarSceneryAtlas();
        SetSceneryLod(_farScenery);
    }

    private const float FarSceneryBakeScale = .5f;
    private const int FarSceneryPageSize = 1024;
    private void BuildFarSceneryAtlas()
    {
        // Far objects keep their individual ground-Y anchor and original owning
        // cell. A shared quarter-resolution page removes texture-page switches
        // without flattening a foreground tree behind the fleet or leaking fog.
        SceneryAtlasPage? page = null;
        List<SceneryStamp>? stamps = null;
        int x = 0, y = 0, row = 0;
        foreach (var entry in _sceneryLodSprites)
        {
            var original = (AtlasTexture)entry.Detail;
            var minimum = (entry.DetailOffset / SceneryBakeScale) * FarSceneryBakeScale;
            var pixelMin = new Vector2I(Mathf.FloorToInt(minimum.X) - 2, Mathf.FloorToInt(minimum.Y) - 2);
            var size = new Vector2I(Mathf.CeilToInt(original.Region.Size.X * FarSceneryBakeScale / SceneryBakeScale) + 4,
                Mathf.CeilToInt(original.Region.Size.Y * FarSceneryBakeScale / SceneryBakeScale) + 4);
            if (x + size.X > FarSceneryPageSize) { y += row; x = 0; row = 0; }
            if (page is null || y + size.Y > FarSceneryPageSize)
            {
                x = y = row = 0;
                page = new SceneryAtlasPage
                {
                    Name = "FarScenery" + FarSceneryPageCount++, Size = Vector2I.One * FarSceneryPageSize,
                    Disable3D = true, TransparentBg = true, World2D = new World2D(),
                    RenderTargetClearMode = SubViewport.ClearMode.Always,
                    RenderTargetUpdateMode = SubViewport.UpdateMode.Once
                };
                _sceneryAtlasRoot!.AddChild(page); _sceneryPages.Add(page);
                stamps = new();
                var pageStamps = stamps;
                page.SetPainter(FarSceneryBakeScale, canvas =>
                {
                    foreach (var stamp in pageStamps)
                        DrawSceneryObject(canvas, stamp.Item, stamp.Anchor / FarSceneryBakeScale);
                });
            }
            stamps!.Add(new(entry.Item, new Vector2(x - pixelMin.X, y - pixelMin.Y)));
            entry.Far = new AtlasTexture { Atlas = page.GetTexture(), Region = new Rect2(new Vector2(x, y), size), FilterClip = true };
            entry.FarOffset = pixelMin;
            x += size.X; row = Math.Max(row, size.Y);
        }
    }

    private void SetSceneryLod(bool far)
    {
        foreach (var entry in _sceneryLodSprites)
        {
            entry.Sprite.Texture = far ? entry.Far : entry.Detail;
            entry.Sprite.Offset = far ? entry.FarOffset : entry.DetailOffset;
            entry.Sprite.Scale = Vector2.One / (far ? FarSceneryBakeScale : SceneryBakeScale);
        }
    }
}

/// <summary>Explicitly stop a one-shot viewport after its source has painted.
/// Do not retain thousands of primitive commands after the texture is baked.</summary>
internal partial class SceneryAtlasPage : SubViewport
{
    private BoardTerrainLayer? _painter;
    private bool _painted;
    private bool _connected;

    internal void SetPainter(float scale, Action<Node2D> draw)
    {
        _painter = new BoardTerrainLayer
        {
            Scale = Vector2.One * scale,
            DrawWorld = canvas => { draw(canvas); _painted = true; }
        };
        AddChild(_painter);
    }

    public override void _Ready()
    {
        RenderingServer.Singleton.FramePostDraw += FinishBake;
        _connected = true;
    }
    public override void _ExitTree() => DisconnectBake();

    private void DisconnectBake()
    {
        if (!_connected) return;
        RenderingServer.Singleton.FramePostDraw -= FinishBake;
        _connected = false;
    }

    private void FinishBake()
    {
        if (!_painted) return;
        RenderTargetUpdateMode = UpdateMode.Disabled;
        DisconnectBake();
        _painter?.QueueFree();
        _painter = null;
    }
}
