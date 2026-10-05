using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Displays one pending Core receipt. Only an explicit claim requests its award.</summary>
public partial class RewardPapyrusHud : CanvasLayer
{
    private Control _root = null!;
    private RollingModalPaper _paper = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _body = null!;
    private bool _layingOut;
    private Label _title = null!, _nation = null!, _first = null!, _second = null!;
    private GuidanceLabel _guidance = null!;
    private BrushPaperButton _claim = null!;
    private ColorRect _accent = null!;
    private string? _pendingId;
    private bool _submitted;
    private bool _opening;
    private int _ceremony;
    internal bool InstantAnimations { get; set; }
    internal FleetColor Nation { get; set; }
    public bool IsOpen => Visible;
    public string? PendingId => _pendingId;
    public event Action<string>? ClaimRequested;

    public override void _Ready()
    {
        Layer = 48;
        _root = new Control { Name = "RewardPapyrusRoot", Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .24f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _paper = new RollingModalPaper { Name = "RewardPapyrus", CeremonialDecorations = true,
            CustomMinimumSize = new(PapyrusModal.Width, 0) };
        _root.AddChild(_paper);
        var body = new VBoxContainer();
        _body = body;
        body.AddThemeConstantOverride("separation", 12);
        _accent = new ColorRect { CustomMinimumSize = new(0, 4), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(_accent);
        _title = Text("", 19, true); _title.Name = "RewardTitle"; body.AddChild(_title);
        _nation = Text("", 16, true); _nation.Name = "RewardNation"; body.AddChild(_nation);
        var coin = new CoinIcon { CustomMinimumSize = new(36, 36), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        body.AddChild(coin);
        _first = Text("", 14); _first.Name = "RewardFirstSentence"; body.AddChild(_first);
        _second = Text("", 14); _second.Name = "RewardSecondSentence"; body.AddChild(_second);
        _claim = new BrushPaperButton { Name = "ClaimReward", CustomMinimumSize = new(174, 44),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, Underline = true };
        _claim.Pressed += RequestClaim;
        _scroll = PapyrusModal.WrapWithFooter(_paper, body, _claim, "RewardScroll");
        _guidance = new GuidanceLabel { Name = "RewardGuidance", Text = "Accept the reward to continue." };
        _root.AddChild(_guidance);
        _paper.Resized += Layout;
        _body.MinimumSizeChanged += Layout;
        _root.Resized += Layout;
        UiScale.Bind(this, _root, Layout);
        _paper.Hide();
        Hide();
        SetProcess(false);
    }

    public void ShowNation(string id, string nationName, Color nationColor,
        string firstSentence = "Among these sails, faith is woven into every voyage.",
        string secondSentence = "Their people seek heavenly favor beyond familiar shores.", int amount = 5)
    {
        Configure(id, "A nation beyond the horizon", nationName, nationColor, firstSentence, secondSentence, amount);
    }

    public void ShowHeavenly(string id, int amount,
        string description = "Heaven honors the faith carried by our cities and our flagship.")
    {
        Configure(id, "An offering from heaven", "", new Color("b9954e"), description,
            "Accept this gift before continuing your voyage.", amount);
    }

    private void Configure(string id, string title, string nation, Color accent, string first, string second, int amount)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A pending reward needs its Core identity.", nameof(id));
        bool fresh = _pendingId != id || !IsOpen;
        _pendingId = id;
        _title.Text = title;
        _nation.Text = nation;
        _nation.Visible = nation.Length > 0;
        _accent.Color = accent;
        _first.Text = first;
        _second.Text = second;
        _claim.Text = $"Claim {amount} Thors";
        if (fresh)
        {
            _ceremony++;
            _opening = true;
            _submitted = false;
            _claim.Disabled = true;
            _claim.ResetStamp();
        }
        _paper.Nation = Nation;
        _claim.Ink = FleetPalette.Color(Nation).Darkened(.25f);
        Show();
        _paper.ResetSize();
        Layout();
        if (fresh) _ = Open(_ceremony);
    }

    private async Task Open(int ceremony)
    {
        await _paper.OpenAsync(InstantAnimations);
        if (ceremony != _ceremony || !IsInsideTree()) return;
        _opening = false;
        _claim.Disabled = _submitted;
    }

    private async void RequestClaim()
    {
        if (_pendingId is null || _submitted || _opening) return;
        int ceremony = _ceremony;
        string id = _pendingId;
        _submitted = true;
        _claim.Disabled = true;
        await _claim.StampAsync(InstantAnimations);
        if (ceremony != _ceremony || !IsInsideTree()) return;
        await _paper.FoldAsync(InstantAnimations);
        if (ceremony != _ceremony || !IsInsideTree()) return;
        ClaimRequested?.Invoke(id);
    }

    public void RejectClaim()
    {
        _submitted = false;
        _claim.ResetStamp();
        _opening = true;
        _ = Open(_ceremony);
    }

    public void Close()
    {
        _ceremony++;
        _pendingId = null;
        _submitted = false;
        _opening = false;
        _claim.ResetStamp();
        _ = _paper.FoldAsync(true);
        _paper.Hide();
        Hide();
        SetProcess(false);
    }

    private void Layout()
    {
        if (_paper is null || !_paper.IsInsideTree() || _layingOut) return;
        _layingOut = true;
        var viewport = UiScale.LogicalViewport(this);
        PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _claim, viewport);
        _paper.Position = (viewport - _paper.Size) * .5f;
        _guidance.Position = _paper.Position + new Vector2(0, _paper.Size.Y + 8);
        _guidance.Size = new Vector2(_paper.Size.X, 36);
        _layingOut = false;
    }

    private static Label Text(string text, int fontSize, bool centered = false)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = Vector2.Zero,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = centered ? HorizontalAlignment.Center : HorizontalAlignment.Left };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        return label;
    }
}
