using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

public partial class VoyageWelcome : CanvasLayer
{
    private Control _root = null!;
    private ColorRect _veil = null!;
    private RollingModalPaper _paper = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _body = null!;
    private Label _faith = null!, _nation = null!;
    private NationOrnament _ornament = null!;
    private BrushPaperButton _sail = null!;
    private TaskCompletionSource? _accept;
    private int _ceremony;
    public bool IsOpen => Visible;
    internal bool InstantAnimations { get; set; }
    public override void _Ready()
    {
        Layer = 52;
        _root = new Control { Theme = PapyrusStyle.ChartTheme(), MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _veil = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_veil);
        _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _paper = new RollingModalPaper { Name = "EldersPapyrus" };
        _root.AddChild(_paper);
        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 13);
        _sail = new BrushPaperButton { Name = "AcceptVoyage", Text = "Set sail", Underline = true, CustomMinimumSize = new(0, 48) };
        _scroll = PapyrusModal.WrapWithFooter(_paper, _body, _sail, "EldersScroll");
        _scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _ornament = new NationOrnament { Name = "NationWelcomeOrnament", CustomMinimumSize = new(0, 72) };
        _body.AddChild(_ornament);
        _nation = Text("", 20);
        _body.AddChild(_nation);
        _body.AddChild(Text("The elders bless your voyage", 22));
        _faith = Text("", 16);
        _body.AddChild(_faith);
        _body.AddChild(Text("Carry our faith to the settlements of this new land. Defeat those who have chosen another path, and you shall be rewarded, brave captain!", 16));
        _sail.Pressed += async () => await Accept();
        _body.MinimumSizeChanged += Layout;
        UiScale.Bind(this, _root, Layout);
        Hide();
    }
    private async Task Accept()
    {
        if (_accept is null || _sail.Disabled) return;
        _sail.Disabled = true;
        int ceremony = _ceremony;
        var receipt = _accept;
        await _sail.StampAsync(InstantAnimations);
        await _paper.FoldAsync(InstantAnimations);
        if (ceremony != _ceremony) return;
        _accept = null;
        Hide();
        receipt.TrySetResult();
    }
    private static Label Text(string source, int size)
    {
        var label = new Label
        {
            Text = source,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }
    private void Layout() => PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _sail, UiScale.LogicalViewport(this));
    internal void BeginDescent()
    {
        _paper.Hide();
        _veil.Color = Colors.Black;
        _veil.Modulate = Colors.White;
        Show();
    }
    internal async Task RevealSea()
    {
        var fade = CreateTween();
        fade.TweenProperty(_veil, "modulate:a", 0f, 1.25);
        await ToSignal(fade, Tween.SignalName.Finished);
    }
    internal async Task Welcome(FleetColor color)
    {
        _ceremony++;
        _faith.Text = color switch
        {
            FleetColor.Blue => "May the golden dome's celestial light guide your sails.",
            FleetColor.Purple => "May the moonlit spirits of our silver shrine guard your voyage.",
            FleetColor.Yellow => "May the Rose Crown's living light renew your courage.",
            FleetColor.White => "May the sacred crimson R shine above your fleet.",
            FleetColor.Green => "May the great tree and its ancient roots shelter your people.",
            _ => "May the crimson crystals and mountain spirits lend you their fire."
        };
        _nation.Text = NationIdentity.Name(color);
        _ornament.Color = color;
        _ornament.QueueRedraw();
        _sail.Ink = FleetPalette.Color(color).Darkened(.18f);
        _sail.Disabled = true;
        _accept = new TaskCompletionSource();
        var receipt = _accept;
        _veil.Color = new Color(0, 0, 0, .25f);
        _veil.Modulate = Colors.White;
        _scroll.ScrollVertical = 0;
        Show();
        Layout();
        await _paper.OpenAsync(InstantAnimations);
        _sail.Disabled = false;
        await receipt.Task;
    }
    internal void Close()
    {
        _ceremony++;
        _accept?.TrySetResult();
        _accept = null;
        Hide();
    }
}
