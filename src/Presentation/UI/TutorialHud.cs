using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Nonmodal upper-left advice; its controls intercept only their own paper.</summary>
internal partial class TutorialHud : CanvasLayer
{
    private Control _root = null!;
    private RollingModalPaper _paper = null!;
    private VBoxContainer _body = null!;
    private ScrollContainer _scroll = null!;
    private Label _heading = null!, _description = null!;
    private TextureRect _picture = null!;
    private BrushPaperButton _close = null!;
    private bool _layingOut, _dismissing;
    private int _presentation;
    internal string? Topic { get; private set; }
    internal bool IsOpen => Visible && Topic is not null;
    internal event Action? Dismissed;
    public override void _Ready()
    {
        Layer = 32;
        Name = "TutorialAdvice";
        _root = new Control { Name = "TutorialAdviceRoot", Theme = PapyrusStyle.ChartTheme(), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);
        _paper = new RollingModalPaper { Name = "TutorialAdvicePaper", MouseFilter = Control.MouseFilterEnum.Stop,
            MouseForcePassScrollEvents = false, Visible = false };
        _root.AddChild(_paper);
        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 8);
        var top = new HBoxContainer();
        _body.AddChild(top);
        _heading = Text("", 15);
        _heading.Name = "TutorialAdviceTitle";
        _heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(_heading);
        _picture = new TextureRect { Name = "TutorialAdviceScreenshot", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new(0, 112) };
        _body.AddChild(_picture);
        _description = Text("", 11);
        _description.Name = "TutorialAdviceDescription";
        _body.AddChild(_description);
        _close = new BrushPaperButton { Name = "CloseTutorialAdvice", Text = "Taken to heart",
            TooltipText = "Acknowledge this advice", CustomMinimumSize = new(0, 40), Underline = true };
        // Advice can grow after translation. Keep acknowledgement outside the
        // clipped reading area so a long tip never hides the only close action.
        _scroll = PapyrusModal.WrapWithFooter(_paper, _body, _close, "TutorialAdviceScroll");
        // A nonmodal sheet must consume wheel input even when it has reached an
        // edge or all its text fits. The rolling paper's Node2D reveal layer is
        // outside the ordinary Control bubbling chain, so guard its content too.
        _scroll.MouseForcePassScrollEvents = false;
        _paper.ContentSurface.MouseFilter = Control.MouseFilterEnum.Stop;
        _paper.ContentSurface.MouseForcePassScrollEvents = false;
        _close.Pressed += async () =>
        {
            if (_dismissing) return;
            _dismissing = true;
            int presentation = _presentation;
            await _close.StampAsync();
            if (presentation != _presentation) return;
            await _paper.FoldAsync();
            if (presentation != _presentation) return;
            Clear();
            _dismissing = false;
            Dismissed?.Invoke();
        };
        _body.MinimumSizeChanged += Layout;
        Language.Changed += Layout;
        TreeExiting += () => Language.Changed -= Layout;
        UiScale.Bind(this, _root, Layout);
        Hide();
    }
    private static Label Text(string text, int size)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        return label;
    }
    internal void Present(string topic, string title, string description, string screenshot)
    {
        // An uninitialized/replaced topic can never reveal an empty paper.
        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(description)) { Clear(); return; }
        _presentation++;
        _dismissing = false;
        Topic = topic;
        _close.ResetStamp();
        _scroll.ScrollVertical = 0;
        _heading.Text = title;
        _description.Text = description;
        _picture.Texture = GD.Load<Texture2D>(screenshot);
        _picture.Visible = _picture.Texture is not null;
        Show();
        _root.Show();
        Layout();
        _ = _paper.OpenAsync();
    }
    internal void SetAllowed(bool allowed)
    {
        bool show = allowed && Topic is not null && !string.IsNullOrWhiteSpace(_heading.Text)
            && !string.IsNullOrWhiteSpace(_description.Text) && UiHints.Enabled;
        _root.Visible = show;
        Visible = show;
    }
    internal void Clear()
    {
        _presentation++;
        _dismissing = false;
        Topic = null;
        _close.ResetStamp();
        _ = _paper.FoldAsync(true);
        _paper.Hide();
        _root.Hide();
        Hide();
    }
    private void Layout()
    {
        if (_paper is null || _layingOut) return;
        _layingOut = true;
        var viewport = UiScale.LogicalViewport(this);
        _picture.CustomMinimumSize = new(0, Math.Min(112, viewport.Y * .19f));
        PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _close, viewport, 330, .48f);
        _paper.Position = new(18, 18);
        _layingOut = false;
    }
}
