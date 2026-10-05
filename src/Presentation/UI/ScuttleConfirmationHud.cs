using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>A reversible choice; only Main may submit the eventual Core command.</summary>
internal partial class ScuttleConfirmationHud : CanvasLayer
{
    private Control _root = null!;
    private RollingModalPaper _paper = null!;
    private VBoxContainer _body = null!;
    private ScrollContainer _scroll = null!;
    private HBoxContainer _footer = null!;
    private Label _ship = null!, _refund = null!;
    private BrushPaperButton _yes = null!, _no = null!;
    private TaskCompletionSource<bool>? _choice;
    private bool _closing, _instant;
    internal bool IsOpen => _choice is not null;

    public override void _Ready()
    {
        Layer = 54;
        _root = new Control { Name = "ScuttleConfirmation", Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .48f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _paper = new RollingModalPaper { Name = "ScuttleConfirmationPaper", MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_paper);
        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 12);
        _footer = new HBoxContainer();
        _footer.AddThemeConstantOverride("separation", 12);
        _scroll = PapyrusModal.WrapWithFooter(_paper, _body, _footer, "ScuttleConfirmationScroll");
        var title = new Label { Text = "Scuttle this ship?", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 23);
        title.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _body.AddChild(title);
        var glyph = new ActionGlyph { Name = "ScuttleConfirmationGlyph", Symbol = ActionSymbol.Scuttle,
            CustomMinimumSize = new(76, 76), InkScale = 2.2f, SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _body.AddChild(glyph);
        _ship = new Label { Name = "ScuttleShipName", HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _ship.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _body.AddChild(_ship);
        var warning = new Label { Text = "The vessel will sink. This order cannot be undone.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        warning.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _body.AddChild(warning);
        _refund = new Label { Name = "ScuttleRefund", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _refund.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _body.AddChild(_refund);
        _no = new BrushPaperButton { Name = "CancelScuttle", Text = "Keep this ship",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _yes = new BrushPaperButton { Name = "ConfirmScuttle", Text = "Scuttle ship", Underline = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _footer.AddChild(_no); _footer.AddChild(_yes);
        _no.Pressed += () => _ = CloseAsync(false);
        _yes.Pressed += () => _ = CloseAsync(true);
        _body.MinimumSizeChanged += Layout;
        UiScale.Bind(this, _root, Layout);
        _root.Hide(); SetProcessInput(false);
    }

    private void Layout() => PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _footer, UiScale.LogicalViewport(this));

    internal async Task<bool> Ask(string shipName, int refund, FleetColor nation, bool instant)
    {
        if (IsOpen) return false;
        _instant = instant;
        _choice = new TaskCompletionSource<bool>();
        var choice = _choice;
        _closing = false;
        _ship.Text = shipName;
        _refund.Text = $"Dismantling returns {refund} Thors.";
        _paper.Nation = nation;
        _yes.Disabled = _no.Disabled = true;
        _root.Show(); SetProcessInput(true); Layout();
        try { await _paper.OpenAsync(instant); }
        catch (TaskCanceledException) { CancelImmediately(); }
        if (ReferenceEquals(_choice, choice) && !_closing)
        { _yes.Disabled = _no.Disabled = false; _no.GrabFocus(); }
        return await choice.Task;
    }

    private async Task CloseAsync(bool accepted)
    {
        if (_choice is not { } choice || _closing || accepted && _yes.Disabled) return;
        _closing = true;
        _yes.Disabled = _no.Disabled = true;
        try { await _paper.FoldAsync(_instant); }
        catch (TaskCanceledException) { accepted = false; }
        if (!ReferenceEquals(_choice, choice)) return;
        _choice = null;
        _root.Hide(); SetProcessInput(false);
        choice.TrySetResult(accepted);
    }

    internal void CancelImmediately()
    {
        var choice = _choice;
        _choice = null;
        _closing = false;
        _root?.Hide(); SetProcessInput(false);
        choice?.TrySetResult(false);
    }

    public override void _Input(InputEvent input)
    {
        if (!IsOpen) return;
        if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }
            || input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        { GetViewport().SetInputAsHandled(); _ = CloseAsync(false); }
    }
    public override void _ExitTree() => CancelImmediately();
}
