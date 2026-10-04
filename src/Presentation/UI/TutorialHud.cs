using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Nonmodal upper-left advice; its controls intercept only their own paper.</summary>
internal partial class TutorialHud : CanvasLayer
{
    private Control _root = null!;
    private PanelContainer _paper = null!;
    private VBoxContainer _body = null!;
    private ScrollContainer _scroll = null!;
    private Label _heading = null!, _description = null!;
    private TextureRect _picture = null!;
    private Button _close = null!;
    private bool _layingOut;
    internal string? Topic { get; private set; }
    internal bool IsOpen => Visible && Topic is not null;
    internal event Action? Dismissed;
    public override void _Ready()
    {
        Layer = 32;
        Name = "TutorialAdvice";
        _root = new Control { Name = "TutorialAdviceRoot", Theme = PapyrusStyle.ChartTheme(), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);
        _paper = new PanelContainer { Name = "TutorialAdvicePaper", MouseFilter = Control.MouseFilterEnum.Stop };
        _paper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.96f));
        PapyrusGrain.Apply(_paper);
        _root.AddChild(_paper);
        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 8);
        _scroll = PapyrusModal.Wrap(_paper, _body, "TutorialAdviceScroll");
        var top = new HBoxContainer();
        _body.AddChild(top);
        _heading = Text("", 17);
        _heading.Name = "TutorialAdviceTitle";
        _heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(_heading);
        _close = new Button { Name = "CloseTutorialAdvice", Text = "×", TooltipText = "Close advice",
            CustomMinimumSize = new(30, 30), FocusMode = Control.FocusModeEnum.None };
        PapyrusStyle.Button(_close, 19);
        top.AddChild(_close);
        _close.Pressed += () =>
        {
            Clear();
            Dismissed?.Invoke();
        };
        _picture = new TextureRect { Name = "TutorialAdviceScreenshot", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new(0, 138) };
        _body.AddChild(_picture);
        _description = Text("", 13);
        _description.Name = "TutorialAdviceDescription";
        _body.AddChild(_description);
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
        Topic = topic;
        _heading.Text = title;
        _description.Text = description;
        _picture.Texture = GD.Load<Texture2D>(screenshot);
        _picture.Visible = _picture.Texture is not null;
        Show();
        Layout();
    }
    internal void SetAllowed(bool allowed) => Visible = allowed && Topic is not null && UiHints.Enabled;
    internal void Clear()
    {
        Topic = null;
        Hide();
    }
    private void Layout()
    {
        if (_paper is null || _layingOut) return;
        _layingOut = true;
        var viewport = UiScale.LogicalViewport(this);
        _picture.CustomMinimumSize = new(0, Math.Min(138, viewport.Y * .24f));
        PapyrusModal.Layout(_paper, _scroll, _body, viewport);
        _paper.Position = new(18, 18);
        _layingOut = false;
    }
}
