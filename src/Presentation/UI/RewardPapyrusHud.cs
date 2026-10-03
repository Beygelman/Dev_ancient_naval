using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Displays one pending Core receipt. Only an explicit claim requests its award.</summary>
public partial class RewardPapyrusHud : CanvasLayer
{
    private Control _root = null!;
    private PanelContainer _paper = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _body = null!;
    private bool _layingOut;
    private Label _title = null!, _nation = null!, _first = null!, _second = null!;
    private GuidanceLabel _guidance = null!;
    private Button _claim = null!;
    private ColorRect _accent = null!;
    private string? _pendingId;
    private bool _submitted;
    private float _age = 1;
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
        _paper = new PanelContainer { Name = "RewardPapyrus", CustomMinimumSize = new(PapyrusModal.Width, 0) };
        _paper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.99f));
        PapyrusGrain.Apply(_paper);
        _root.AddChild(_paper);
        var body = new VBoxContainer();
        _body = body;
        body.AddThemeConstantOverride("separation", 12);
        _scroll = PapyrusModal.Wrap(_paper, body, "RewardScroll");
        _accent = new ColorRect { CustomMinimumSize = new(0, 4), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(_accent);
        _title = Text("", 19, true); _title.Name = "RewardTitle"; body.AddChild(_title);
        _nation = Text("", 16, true); _nation.Name = "RewardNation"; body.AddChild(_nation);
        var coin = new CoinIcon { CustomMinimumSize = new(36, 36), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        body.AddChild(coin);
        _first = Text("", 14); _first.Name = "RewardFirstSentence"; body.AddChild(_first);
        _second = Text("", 14); _second.Name = "RewardSecondSentence"; body.AddChild(_second);
        _claim = new Button { Name = "ClaimReward", CustomMinimumSize = new(174, 44),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, FocusMode = Control.FocusModeEnum.None };
        PapyrusStyle.Button(_claim, 16);
        _claim.Pressed += RequestClaim;
        body.AddChild(_claim);
        _guidance = new GuidanceLabel { Name = "RewardGuidance", Text = "Accept the reward to continue." };
        _root.AddChild(_guidance);
        _paper.Resized += Layout;
        _body.MinimumSizeChanged += Layout;
        _root.Resized += Layout;
        UiScale.Bind(this, _root, Layout);
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
            _age = 0;
            _submitted = false;
            _claim.Disabled = true;
        }
        Show();
        _paper.ResetSize();
        Layout();
        SetProcess(_age < 1);
    }

    private void RequestClaim()
    {
        if (_pendingId is null || _submitted || _age < 1) return;
        _submitted = true;
        _claim.Disabled = true;
        ClaimRequested?.Invoke(_pendingId);
    }

    public void RejectClaim()
    {
        _submitted = false;
        _claim.Disabled = _age < 1;
    }

    public void Close()
    {
        _pendingId = null;
        _submitted = false;
        Hide();
        SetProcess(false);
    }

    private void Layout()
    {
        if (_paper is null || !_paper.IsInsideTree() || _layingOut) return;
        _layingOut = true;
        var viewport = UiScale.LogicalViewport(this);
        PapyrusModal.Layout(_paper, _scroll, _body, viewport);
        float scale = 1;
        _paper.PivotOffset = _paper.Size * .5f;
        _paper.Position = (viewport - _paper.Size) * .5f;
        _guidance.Position = _paper.Position + new Vector2(0, _paper.Size.Y + 8);
        _guidance.Size = new Vector2(_paper.Size.X, 36);
        float reveal = 1 - Mathf.Pow(1 - _age, 3);
        _paper.Scale = new Vector2(scale, Math.Max(.01f, reveal) * scale);
        _layingOut = false;
    }

    public override void _Process(double delta)
    {
        _age = Math.Min(1, _age + (float)delta / .3f);
        Layout();
        if (_age < 1) return;
        _claim.Disabled = _submitted;
        SetProcess(false);
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
