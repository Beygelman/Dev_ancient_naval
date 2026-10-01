using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Retained per-cell geometry in one bounded raster. Fog changes tint
/// existing commands; only newly discovered cells build geometry.</summary>
internal partial class TerrainRasterCache : Node2D
{
    private const int MaximumAxis = 4096;
    private const long MaximumPixels = 8_388_608;
    private readonly Dictionary<GridPosition, Cell> _cells = new();
    private SubViewport? _raster;
    private Sprite2D? _image;
    private IsometricProjection? _projection;
    private bool _pending;
    internal Vector2I Size { get; private set; }
    internal int RegionCount => _cells.Count;
    internal int VisibilityChanges { get; private set; }
    internal int KnownCellCount => _cells.Values.Count(cell => cell.Mask != 0);
    internal bool Idle => !_pending && _raster?.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled;

    private sealed class Source
    {
        internal BoardTerrainLayer Canvas = null !;
        internal long Requested, Drawn;
    }

    private sealed class Cell
    {
        internal Source[] Sources = Array.Empty<Source>();
        internal byte Mask;
    }

    public override void _Ready() => RenderingServer.Singleton.FramePostDraw += CompleteRasterization;
    public override void _ExitTree() => RenderingServer.Singleton.FramePostDraw -= CompleteRasterization;
    internal void Ensure(IsometricProjection projection, GameBoard board, Action<Node2D> background, params Action<Node2D, ISet<GridPosition>>[] layers)
    {
        if (ReferenceEquals(_projection, projection))
            return;
        _projection = projection;
        if (_raster is null)
        {
            _raster = new SubViewport
            {
                Name = "LandscapeRaster",
                Disable3D = true,
                TransparentBg = true,
                World2D = new World2D(),
                RenderTargetClearMode = SubViewport.ClearMode.Always,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled
            };
            AddChild(_raster);
            _image = new Sprite2D
            {
                Name = "LandscapeImage",
                Centered = false,
                Texture = _raster.GetTexture(),
                TextureFilter = TextureFilterEnum.Linear
            };
            AddChild(_image);
        }

        foreach (var child in _raster.GetChildren())
        {
            _raster.RemoveChild(child);
            child.QueueFree();
        }

        _cells.Clear();
        var bounds = projection.BoardBounds(board).Grow(80);
        double scale = Math.Min(1, Math.Min(MaximumAxis / (double)bounds.Size.X, MaximumAxis / (double)bounds.Size.Y));
        scale = Math.Min(scale, Math.Sqrt(MaximumPixels / ((double)bounds.Size.X * bounds.Size.Y)));
        Size = new(Math.Max(1, (int)Math.Floor(bounds.Size.X * scale)), Math.Max(1, (int)Math.Floor(bounds.Size.Y * scale)));
        _raster.Size = Size;
        _image!.Position = bounds.Position;
        _image.Scale = Vector2.One / (float)scale;
        var origin = new Node2D
        {
            Position = -bounds.Position * (float)scale,
            Scale = Vector2.One * (float)scale
        };
        _raster.AddChild(origin);
        origin.AddChild(new BoardTerrainLayer { Name = "UnexploredSea", DrawWorld = background });
        foreach (var tile in board.Tiles)
            _cells.Add(tile.Position, new Cell { Sources = new Source[layers.Length] });
        // Preserve global draw order: all surfaces, then shores, then trees/peaks.
        for (int layer = 0; layer < layers.Length; layer++)
        {
            var group = new Node2D
            {
                Name = "Layer" + layer
            };
            origin.AddChild(group);
            foreach (var(position, cell)in _cells)
            {
                var source = new Source();
                var singleCell = new HashSet<GridPosition>
                {
                    position
                };
                var draw = layers[layer];
                source.Canvas = new BoardTerrainLayer
                {
                    Visible = false,
                    DrawWorld = canvas =>
                    {
                        draw(canvas, singleCell);
                        source.Drawn = source.Requested;
                    }
                };
                cell.Sources[layer] = source;
                group.AddChild(source.Canvas);
            }
        }
    }

    internal bool Invalidate(ISet<GridPosition> changed, bool force, Func<GridPosition, byte> visibility)
    {
        bool requested = false;
        foreach (var(position, cell)in _cells)
        {
            if (!force && !changed.Contains(position))
                continue;
            byte mask = visibility(position);
            bool firstDiscovery = cell.Mask == 0 && mask != 0;
            if (cell.Mask != mask)
                VisibilityChanges++;
            cell.Mask = mask;
            foreach (var source in cell.Sources)
            {
                source.Canvas.Visible = mask != 0;
                source.Canvas.Modulate = mask == 1 ? new Color(.43f, .43f, .43f) : Colors.White;
                if (mask == 0 || (!firstDiscovery && !force))
                    continue;
                source.Requested++;
                source.Canvas.QueueRedraw();
            }

            requested = true;
        }

        if (requested && _raster is not null)
        {
            _pending = true;
            _raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        }

        return requested;
    }

    private void CompleteRasterization()
    {
        if (!_pending || _raster is null || _cells.Values.Any(cell => cell.Mask != 0 && cell.Sources.Any(source => source.Drawn != source.Requested)))
            return;
        _raster.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        _pending = false;
    }
}
