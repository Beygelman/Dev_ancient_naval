using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

public partial class VoyageWelcome : CanvasLayer
{
    private Control _root = null!;
    private ColorRect _veil = null!;
    private PanelContainer _paper = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _body = null!;
    private Label _faith = null!;
    private Button _sail = null!;
    private TaskCompletionSource? _accept;
    public bool IsOpen => Visible;
    public override void _Ready()
    {
        Layer = 52;
        _root = new Control { Theme = PapyrusStyle.ChartTheme(), MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _veil = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_veil); _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _paper = new PanelContainer { Name = "EldersPapyrus" };
        _paper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.99f)); PapyrusGrain.Apply(_paper);
        _root.AddChild(_paper);
        _body = new VBoxContainer(); _body.AddThemeConstantOverride("separation", 15);
        _scroll = PapyrusModal.Wrap(_paper, _body, "EldersScroll");
        _body.AddChild(Text("The elders bless your voyage", 22));
        _faith = Text("", 16); _body.AddChild(_faith);
        _body.AddChild(Text("Carry our faith to the settlements of this new land. Defeat those who have chosen another path, and you shall be rewarded, brave captain!", 16));
        _sail = new Button { Name = "AcceptVoyage", Text = "Set sail", CustomMinimumSize = new(0, 44) };
        PapyrusStyle.Button(_sail, 18);
        _sail.Pressed += () => { if (_accept is null) return; var receipt = _accept; _accept = null; Hide(); receipt.TrySetResult(); };
        _body.AddChild(_sail);
        _body.MinimumSizeChanged += Layout;
        UiScale.Bind(this, _root, Layout);
        Hide();
    }
    private static Label Text(string source, int size)
    {
        var label = new Label { Text = source, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }
    private void Layout() => PapyrusModal.Layout(_paper, _scroll, _body, UiScale.LogicalViewport(this));
    internal void BeginDescent()
    {
        _paper.Hide(); _veil.Color = Colors.Black; _veil.Modulate = Colors.White; Show();
    }
    internal async Task RevealSea()
    {
        var fade = CreateTween(); fade.TweenProperty(_veil, "modulate:a", 0f, .7);
        await ToSignal(fade, Tween.SignalName.Finished);
    }
    internal Task Welcome(FleetColor color)
    {
        _faith.Text = color switch
        {
            FleetColor.Blue => "May the golden dome's celestial light guide your sails.",
            FleetColor.Purple => "May the moonlit spirits of our silver shrine guard your voyage.",
            FleetColor.Yellow => "May the rose pyramid's radiant sun shine upon your course.",
            FleetColor.White => "May the sacred black cross stand firm above your fleet.",
            FleetColor.Green => "May the great tree and its ancient roots shelter your people.",
            _ => "May the crimson crystals and mountain spirits lend you their fire."
        };
        _accept = new TaskCompletionSource();
        _veil.Color = new Color(0, 0, 0, .25f); _veil.Modulate = Colors.White;
        _paper.Show(); Layout();
        _paper.PivotOffset = _paper.Size * .5f; _paper.Scale = new Vector2(1, .02f);
        CreateTween().TweenProperty(_paper, "scale:y", 1f, .35).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        return _accept.Task;
    }
    internal void Close()
    {
        _accept?.TrySetResult(); _accept = null; Hide();
    }
}
