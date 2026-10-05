using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Full-scale parchment content, revealed between moving cylindrical rolls.</summary>
internal partial class RollingModalPaper : PanelContainer
{
    private Tween? _motion;
    private TaskCompletionSource? _completion;
    private Control? _window;
    private PanelContainer? _content;
    private PaperSurface? _surface;
    private RolledEdges? _edges;
    private NationBeads? _beads;
    private float _reveal = 1;
    private float _rollTexturePhase;
    private bool _hasOpened, _decorations, _refreshingGeometry;
    private FleetColor _nation;

    internal bool IsAnimating { get; private set; }
    internal float RollProgress => _reveal;
    internal Rect2 RevealedContentRect => ScrollRollArt.RevealRect(Size, _reveal);
    internal float RollRadius => ScrollRollArt.Radius(Size, _reveal);
    internal Control ContentViewport => _window!;
    internal PanelContainer ContentSurface => _content!;
    internal float RollTexturePhase
    {
        get => _rollTexturePhase;
        set
        {
            _rollTexturePhase = value;
            _edges?.QueueRedraw();
        }
    }

    internal bool CeremonialDecorations
    {
        get => _decorations;
        set
        {
            _decorations = value;
            RefreshDecoration();
        }
    }

    internal FleetColor Nation
    {
        get => _nation;
        set
        {
            _nation = value;
            RefreshDecoration();
        }
    }

    internal void SetNation(Color color)
    {
        float best = float.MaxValue;
        foreach (FleetColor candidate in Enum.GetValues<FleetColor>())
        {
            Color tint = FleetPalette.Color(candidate);
            float distance = MathF.Pow(color.R - tint.R, 2) + MathF.Pow(color.G - tint.G, 2)
                + MathF.Pow(color.B - tint.B, 2);
            if (distance >= best) continue;
            best = distance;
            Nation = candidate;
        }
    }

    public override void _Ready()
    {
        EnsureContent();
        Resized += RefreshGeometry;
        VisibilityChanged += RefreshDecoration;
        RefreshGeometry();
    }

    /// <summary>Containers stay at their final size; the native viewport alone moves and clips.</summary>
    internal void AttachContent(Control body)
    {
        EnsureContent();
        _content!.AddChild(body);
        RefreshGeometry();
    }

    private void EnsureContent()
    {
        if (_window is not null) return;
        // A Node2D prevents the outer PanelContainer from resizing its reveal window.
        // The style keeps the ordinary modal margins while its full rectangle is invisible.
        var style = PapyrusStyle.Panel(.98f);
        style.BgColor = Colors.Transparent;
        style.BorderColor = Colors.Transparent;
        style.ShadowSize = 0;
        AddThemeStyleboxOverride("panel", style);
        var layer = new Node2D { Name = "PaperRevealLayer" };
        AddChild(layer);
        _window = new Control
        {
            Name = "PapyrusRevealViewport",
            ClipContents = true,
            MouseFilter = MouseFilterEnum.Pass
        };
        layer.AddChild(_window);
        _surface = new PaperSurface { Name = "PapyrusFullSizeTexture", MouseFilter = MouseFilterEnum.Ignore };
        _window.AddChild(_surface);
        _content = new PanelContainer { Name = "PapyrusFullSizeContent", MouseFilter = MouseFilterEnum.Pass };
        _content.AddThemeStyleboxOverride("panel", new StyleBoxEmpty
        {
            ContentMarginLeft = style.ContentMarginLeft,
            ContentMarginRight = style.ContentMarginRight,
            ContentMarginTop = style.ContentMarginTop,
            ContentMarginBottom = style.ContentMarginBottom
        });
        _window.AddChild(_content);
        _edges = new RolledEdges { Name = "PapyrusRolledEdges", Paper = this };
        layer.AddChild(_edges);
        _beads = new NationBeads { Name = "PapyrusNationBeads" };
        layer.AddChild(_beads);
        // Native containers grow immediately to a new minimum, but do not shrink
        // themselves again when it drops. Late footer/text layout can change that
        // minimum without changing the outer sheet's already bounded dimensions.
        _content.MinimumSizeChanged += RefreshGeometry;
        RefreshDecoration();
    }

    internal void SynchronizeContentBounds() => RefreshGeometry();

    private void RefreshGeometry()
    {
        if (_window is null || _refreshingGeometry) return;
        _refreshingGeometry = true;
        // Never animate Scale. Even the tiny remaining strip contains normal-size letters.
        Scale = Vector2.One;
        var reveal = RevealedContentRect;
        _window.Position = reveal.Position;
        _window.Size = reveal.Size;
        _surface!.Position = -reveal.Position;
        _surface.Size = Size;
        _surface.QueueRedraw();
        _content!.Position = -reveal.Position;
        _content.Size = Size;
        _edges!.QueueRedraw();
        _beads!.Position = new(Size.X + RollRadius + 10, ScrollRollArt.UpperRollY(Size, _reveal) - RollRadius * .15f);
        _beads.ArtScale = Math.Clamp(RollRadius / 7, .85f, 1.65f);
        _refreshingGeometry = false;
    }

    private void RefreshDecoration()
    {
        if (_edges is null || _beads is null) return;
        _edges.QueueRedraw();
        _beads.Nation = Nation;
        _beads.Visible = CeremonialDecorations;
        _beads.SetSwinging(CeremonialDecorations && IsVisibleInTree());
    }

    private void SetReveal(float progress)
    {
        _reveal = Math.Clamp(progress, 0, 1);
        RefreshGeometry();
    }

    internal Task OpenAsync(bool instant = false)
    {
        Show();
        if (!_hasOpened || _reveal >= .999f) SetReveal(instant ? 1 : 0);
        _hasOpened = true;
        Modulate = Colors.White;
        return Animate(1, instant);
    }

    internal Task FoldAsync(bool instant = false) => Animate(0, instant);

    private Task Animate(float target, bool instant)
    {
        _motion?.Kill();
        _completion?.TrySetResult();
        _completion = null;
        if (instant)
        {
            SetReveal(target);
            IsAnimating = false;
            return Task.CompletedTask;
        }
        IsAnimating = true;
        _completion = new TaskCompletionSource();
        var completion = _completion;
        _motion = CreateTween();
        _motion.TweenMethod(Callable.From<float>(SetReveal), _reveal, target, .42)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _motion.TweenCallback(Callable.From(() =>
        {
            IsAnimating = false;
            if (ReferenceEquals(_completion, completion)) _completion = null;
            completion.TrySetResult();
        }));
        return completion.Task;
    }

    public override void _ExitTree()
    {
        _motion?.Kill();
        _completion?.TrySetCanceled();
    }

    private partial class PaperSurface : Control
    {
        public override void _Draw() => PapyrusGrain.Draw(this, new Rect2(Vector2.Zero, Size));
    }

    private partial class RolledEdges : Node2D
    {
        internal RollingModalPaper Paper { get; init; } = null!;
        public override void _Draw() => ScrollRollArt.Draw(this, Paper.Size, Paper.RollProgress,
            Paper.CeremonialDecorations, Paper.Nation, Paper.RollTexturePhase);
    }
}
