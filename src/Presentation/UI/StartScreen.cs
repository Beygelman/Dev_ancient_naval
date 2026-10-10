using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.Map;
using Godot;
using MapKind = DevAncientNaval.Core.World.WorldKind;

namespace DevAncientNaval.Presentation.UI;

public partial class StartScreen : CanvasLayer
{
    public event Action<FleetColor>? StartRequested;
    public event Action? ContinueRequested, ExitRequested;
    private Control _root = null!;
    private MenuHarborView _harbor = null!;
    private TextureRect _title = null!;
    private VBoxContainer _actions = null!, _colors = null!, _settings = null!;
    private RollingModalPaper _settingsPaper = null!;
    private ScrollContainer _settingsScroll = null!;
    private RollingVoyagePaper _setupPaper = null!;
    private Label _footer = null!, _notice = null!;
    private Button _continue = null!;
    private CheckBox _pirates = null!;
    private PaintedVoyageChoice _start = null!;
    private ColorRect _fade = null!;
    private readonly Dictionary<FleetColor, PaintedVoyageChoice> _swatches = new();
    private readonly Dictionary<int, PaintedVoyageChoice> _opponents = new();
    private readonly Dictionary<AiDifficulty, PaintedVoyageChoice> _difficultyButtons = new();
    private readonly Dictionary<MapSize, PaintedVoyageChoice> _sizes = new();
    private readonly Dictionary<MapKind, PaintedVoyageChoice> _worlds = new();
    private readonly List<(GridContainer Grid, int Columns)> _choiceRows = new();
    private bool _layingOut, _settingsClosing;
    public bool IsOpen => Visible;
    public bool Transitioning { get; private set; }
    public FleetColor SelectedColor { get; private set; } = FleetColor.Blue;
    public AiDifficulty Difficulty { get; private set; } = AiDifficulty.Admiral;
    public int OpponentCount { get; private set; } = 1;
    public MapKind WorldKind { get; private set; } = MapKind.Oceans;
    public MapSize MapSize { get; private set; } = MapSize.Sea;
    public bool IncludePirates => _pirates.ButtonPressed;
    internal int LayoutPasses { get; private set; }
    public void SetNotice(string text) => _notice.Text = text;
    internal void CompleteVoyage() => Transitioning = false;

    public override void _Ready()
    {
        Layer = 40;
        _root = new Control { Name = "HomeRoot", Theme = PapyrusStyle.ChartTheme(), MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _harbor = new MenuHarborView { Name = "LivingHarbor", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_harbor);
        _harbor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _title = Logo();
        _root.AddChild(_title);
        _actions = new VBoxContainer { Name = "StartActions" };
        _actions.AddThemeConstantOverride("separation", 12);
        _root.AddChild(_actions);
        _actions.AddChild(HomeButton("HomeNewGame", "New game", ShowColors));
        _continue = HomeButton("HomeContinue", "Continue", () => ContinueRequested?.Invoke());
        _actions.AddChild(_continue);
        _actions.AddChild(HomeButton("HomeSettings", "Settings", ShowSettings));
        _actions.AddChild(HomeButton("HomeExit", "Exit game", () => ExitRequested?.Invoke()));
        BuildSettings();
        BuildSetup();
        _notice = Heading("", 15);
        _notice.AddThemeColorOverride("font_color", new("edc69d"));
        _root.AddChild(_notice);
        _footer = Heading(GameIdentity.Signature, 14);
        _footer.Name = "HomeVersionSignature";
        _footer.Modulate = new Color(1, 1, 1, .42f);
        _footer.AddThemeColorOverride("font_color", new("e5edde"));
        _root.AddChild(_footer);
        _fade = new ColorRect { Name = "VoyageFade", Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_fade);
        _fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _fade.Hide();
        _root.Resized += Layout;
        _colors.MinimumSizeChanged += Layout;
        Language.Changed += UpdateCaptions;
        UiScale.Bind(this, _root, Layout);
        Choose(SelectedColor);
        ChooseOpponents(OpponentCount);
        ChooseDifficulty(Difficulty);
        ChooseSize(MapSize);
        ChooseWorld(WorldKind);
        ShowHome(false);
    }
    private static TextureRect Logo() => new() { Name = "AncientNavalTitle",
        Texture = GD.Load<Texture2D>("res://assets/ui/ancient-naval-title.png"),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new(0, 80) };
    private static Label Heading(string text, int size = 18)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        return label;
    }
    private static Button HomeButton(string name, string text, Action action)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new(0, 48),
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        PapyrusStyle.Button(button, 20);
        button.Pressed += action;
        return button;
    }
    private static PaintedVoyageChoice Choice(string name, string text, VoyageMotif motif, int variant, Action action)
    {
        var button = new PaintedVoyageChoice { Name = name, Text = text, Motif = motif, Variant = variant,
            CustomMinimumSize = new(motif == VoyageMotif.World ? 89 : 76, motif == VoyageMotif.None ? 46 : 86),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        button.Pressed += action;
        return button;
    }
    private GridContainer ChoiceRow(int columns)
    {
        var row = new GridContainer { Columns = columns, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("h_separation", 6);
        row.AddThemeConstantOverride("v_separation", 6);
        _colors.AddChild(row);
        _choiceRows.Add((row, columns));
        return row;
    }
    private void BuildSetup()
    {
        _colors = new VBoxContainer { Name = "ColorSelection" };
        _colors.AddThemeConstantOverride("separation", 8);
        _setupPaper = new RollingVoyagePaper();
        _root.AddChild(_setupPaper);
        _setupPaper.Bind(_colors);
        var setupLogo = Logo(); setupLogo.Name = "VoyageLogo"; _colors.AddChild(setupLogo);
        _colors.AddChild(Heading("CHART YOUR VOYAGE", 21));
        _colors.AddChild(Heading("Your fleet & its emblem"));
        var nationRow = new GridContainer { Columns = 3 };
        nationRow.AddThemeConstantOverride("h_separation", 9);
        nationRow.AddThemeConstantOverride("v_separation", 3);
        _colors.AddChild(nationRow);
        _choiceRows.Add((nationRow, 3));
        foreach (var color in Enum.GetValues<FleetColor>())
        {
            var swatch = Choice("FleetColor" + color, "", VoyageMotif.None, 0, () => Choose(color));
            swatch.CustomMinimumSize = new(82, 96);
            var crest = new FleetCrest { Faction = color, MouseFilter = Control.MouseFilterEnum.Ignore };
            swatch.AddChild(crest); crest.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
            crest.OffsetBottom = 38;
            var label = Heading(NationIdentity.Name(color), 12);
            label.Name = "NationCaption";
            label.AutoTranslateMode = Control.AutoTranslateModeEnum.Disabled;
            swatch.AddChild(label);
            label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
            label.OffsetTop = -54; label.OffsetBottom = -5;
            swatch.TooltipText = NationIdentity.Name(color);
            _swatches.Add(color, swatch); nationRow.AddChild(swatch);
        }
        _colors.AddChild(Heading("Waters to explore"));
        var sizes = ChoiceRow(Enum.GetValues<MapSize>().Length);
        foreach (var size in Enum.GetValues<MapSize>())
        {
            var button = Choice("MapSize" + size, size.ToString(), VoyageMotif.Size, (int)size, () => ChooseSize(size));
            _sizes.Add(size, button); sizes.AddChild(button);
        }
        _colors.AddChild(Heading("Rival fleets"));
        var rivals = ChoiceRow(4);
        for (int count = 1; count <= 4; count++)
        {
            int selected = count;
            var button = Choice("OpponentCount" + count, "", VoyageMotif.Rival, 0, () => ChooseOpponents(selected));
            button.CustomMinimumSize = new(68, 65);
            button.TooltipText = count == 1 ? "1 rival fleet" : count + " rival fleets";
            _opponents.Add(count, button); rivals.AddChild(button);
        }
        _colors.AddChild(Heading("Rival seamanship"));
        var difficulties = ChoiceRow(Enum.GetValues<AiDifficulty>().Length);
        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var button = Choice("Difficulty" + difficulty, difficulty.ToString(), VoyageMotif.Difficulty, (int)difficulty,
                () => ChooseDifficulty(difficulty));
            button.TooltipText = difficulty switch { AiDifficulty.Boatswain => "A forgiving sailor: simple attacks and modest fleets",
                AiDifficulty.Captain => "An experienced captain: the previous tactical rules",
                _ => "An admiral: coordinated guns, cautious scouts and economic recovery" };
            _difficultyButtons.Add(difficulty, button); difficulties.AddChild(button);
        }
        var pirateRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _colors.AddChild(pirateRow);
        _pirates = new CheckBox
        {
            Name = "IncludePirates",
            Text = "Include pirates",
            ButtonPressed = true,
            TooltipText = "Pirate bays and patrols appear at the start. Higher difficulty brings more pirates; disabling them keeps neutral towns and resources.",
            CustomMinimumSize = new(0, 38),
            FocusMode = Control.FocusModeEnum.All
        };
        _pirates.AddThemeFontSizeOverride("font_size", 16);
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            _pirates.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            _pirates.AddThemeColorOverride(state, PaintedVoyageChoice.Burgundy);
        pirateRow.AddChild(_pirates);
        _colors.AddChild(Heading("Shape of the world"));
        var worlds = ChoiceRow(4);
        foreach (var (kind, caption, variant) in new[] { (MapKind.SeaWorld, "Oceanic world", 0), (MapKind.Oceans, "Island chains", 1),
            (MapKind.Continents, "Continents", 2), (MapKind.Pangaea, "Pangaea", 3) })
        {
            var button = Choice("World" + kind, caption, VoyageMotif.World, variant, () => ChooseWorld(kind));
            _worlds.Add(kind, button); worlds.AddChild(button);
        }
        _start = Choice("StartBattle", "", VoyageMotif.Hand, 0, () =>
        {
            if (!Transitioning) StartRequested?.Invoke(SelectedColor);
        });
        _start.CustomMinimumSize = new(0, 64);
        _colors.AddChild(_start);
        _colors.AddChild(Choice("CancelColor", "Back", VoyageMotif.None, 0, () => { if (!Transitioning) ShowHome(!_continue.Disabled); }));
        _setupPaper.Hide();
    }
    private void BuildSettings()
    {
        _settingsPaper = new RollingModalPaper { Name = "HomeSettingsPaper" };
        _root.AddChild(_settingsPaper);
        _settings = new VBoxContainer(); _settings.AddThemeConstantOverride("separation", 12);
        _settingsScroll = PapyrusModal.Wrap(_settingsPaper, _settings, "HomeSettingsScroll");
        _settingsScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _settings.AddChild(Heading("Settings", 23));
        _settings.AddChild(new LanguageButtons());
        _settings.AddChild(new UiScaleSlider());
        _settings.AddChild(new UiHintsToggle());
        _settings.AddChild(HomeButton("CloseHomeSettings", "Back", () => _ = CloseSettingsAsync()));
        _settingsPaper.Hide();
    }
    private void ShowSettings()
    {
        if (Transitioning) return;
        _actions.Hide(); _setupPaper.Hide(); _settingsPaper.Show(); Layout();
        _ = _settingsPaper.OpenAsync();
    }
    private async Task CloseSettingsAsync()
    {
        if (_settingsClosing) return;
        _settingsClosing = true;
        try
        {
            await _settingsPaper.FoldAsync();
            ShowHome(!_continue.Disabled);
        }
        finally { _settingsClosing = false; }
    }
    private void Choose(FleetColor color)
    {
        if (Transitioning) return;
        SelectedColor = color; _harbor.FleetColor = color;
        foreach (var (key, button) in _swatches) button.Mark(key == color);
        UpdateCaptions();
    }
    private void UpdateCaptions()
    {
        foreach (var (color, swatch) in _swatches)
        {
            swatch.GetNode<Label>("NationCaption").Text = NationIdentity.Name(color);
            swatch.TooltipText = NationIdentity.Name(color) + " · " + Language.Translate(color.ToString());
        }
        _start.Text = Language.Translate("Embark with the {0} nation").Replace("{0}", NationIdentity.Name(SelectedColor));
        Layout();
    }
    private void ChooseDifficulty(AiDifficulty difficulty)
    {
        if (Transitioning) return;
        Difficulty = difficulty;
        foreach (var (key, button) in _difficultyButtons) button.Mark(key == difficulty);
    }
    private void ChooseSize(MapSize size)
    {
        if (Transitioning) return;
        MapSize = size;
        foreach (var (key, button) in _sizes) button.Mark(key == size);
    }
    private void ChooseWorld(MapKind world)
    {
        if (Transitioning) return;
        WorldKind = world;
        foreach (var (key, button) in _worlds) button.Mark(key == world);
    }
    private void ChooseOpponents(int count)
    {
        if (Transitioning) return;
        OpponentCount = Math.Clamp(count, 1, 4);
        foreach (var (key, button) in _opponents) button.Mark(key <= OpponentCount);
    }
    public void ShowHome(bool canContinue, string notice = "")
    {
        Transitioning = false; _fade.Hide();
        _start.Disabled = false; _start.Mark(false); _pirates.Disabled = false;
        Show(); _root.Show(); _continue.Disabled = !canContinue; _continue.Visible = canContinue;
        _actions.Show(); _colors.Show(); _setupPaper.Hide(); _settingsPaper.Hide(); _notice.Text = notice; Layout();
    }
    public void ShowColors()
    {
        if (Transitioning) return;
        Show(); _actions.Hide(); _settingsPaper.Hide(); _setupPaper.Show(); _notice.Text = "";
        _setupPaper.Scroll.ScrollVertical = 0;
        Layout();
        _ = _setupPaper.OpenAsync();
    }
    internal async Task CloseForVoyage(bool fast)
    {
        Transitioning = true;
        _start.Disabled = true; _pirates.Disabled = true;
        _start.Mark(true); _start.BeginHandprint();
        if (fast)
        {
            await _setupPaper.FoldAsync(instant: true);
            _fade.Show();
            return;
        }
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        await _setupPaper.FoldAsync();
        _fade.Modulate = new Color(1, 1, 1, 0); _fade.Show();
        var fade = CreateTween(); fade.TweenProperty(_fade, "modulate:a", 1f, .3);
        await ToSignal(fade, Tween.SignalName.Finished);
    }
    private void Layout()
    {
        if (_root is null || _footer is null || _layingOut) return;
        _layingOut = true; LayoutPasses++;
        var size = UiScale.LogicalViewport(this);
        float width = Math.Min(size.X - 32, Math.Min(420, Math.Max(260, size.X * .36f)));
        float x = Math.Max(16, size.X - width - 32);
        _title.Visible = !_setupPaper.Visible;
        _title.Position = new(Math.Max(16, x - 50), size.Y * .09f);
        _title.Size = new(Math.Min(width + 100, size.X - _title.Position.X - 16), size.Y * .23f);
        _actions.Position = new(x, size.Y * .39f); _actions.Size = new(width, 0);
        bool portrait = size.Y > size.X;
        if (portrait)
        {
            _title.Position = new((size.X - _title.Size.X) / 2, _title.Position.Y);
            _actions.Position = new((size.X - width) / 2, _actions.Position.Y);
        }
        float margin = Mathf.Clamp(size.Y * .045f, 14, 32);
        float paperWidth = Math.Min(420, Math.Max(1, size.X - margin * 2));
        float contentWidth = paperWidth - _setupPaper.GetThemeStylebox("panel").GetMinimumSize().X;
        foreach (var (grid, columns) in _choiceRows)
        {
            float itemWidth = 1;
            foreach (Control choice in grid.GetChildren())
                itemWidth = Math.Max(itemWidth, choice.GetCombinedMinimumSize().X);
            int separation = grid.GetThemeConstant("h_separation");
            int fitted = Math.Clamp((int)((contentWidth + separation) / (itemWidth + separation)), 1, columns);
            // Keep four-choice rows balanced when a phone can fit only three.
            if (columns == 4 && fitted == 3) fitted = 2;
            grid.Columns = fitted;
        }
        float height = Math.Max(100, size.Y - margin * 2);
        _setupPaper.CustomMinimumSize = new(paperWidth, 0);
        _setupPaper.Scroll.CustomMinimumSize = new(0, Math.Max(1, height - 24));
        _setupPaper.Size = new(paperWidth, height);
        _setupPaper.Position = new(size.X - paperWidth - margin, margin);
        if (portrait) _setupPaper.Position = new((size.X - paperWidth) / 2, margin);
        PapyrusModal.Layout(_settingsPaper, _settingsScroll, _settings, size);
        _notice.Position = new(x, size.Y * .82f); _notice.Size = new(width, 65);
        if (portrait) _notice.Position = new((size.X - width) / 2, _notice.Position.Y);
        float footerHeight = size.X < 540 ? 42 : 24;
        _footer.Position = new(22, size.Y - footerHeight - 8); _footer.Size = new(size.X - 44, footerHeight);
        _layingOut = false;
    }
    public override void _ExitTree() => Language.Changed -= UpdateCaptions;
}
