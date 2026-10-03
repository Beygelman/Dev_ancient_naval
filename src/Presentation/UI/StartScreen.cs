using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
<<<<<<< Updated upstream
using DevAncientNaval.Core.World;
using MapKind = DevAncientNaval.Core.World.WorldKind;
=======
>>>>>>> Stashed changes
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class StartScreen : CanvasLayer
{
    public event Action<FleetColor>? StartRequested;
    public event Action? ContinueRequested, ExitRequested;
<<<<<<< Updated upstream
    private Control _root = null !;
    private MenuHarborView _harbor = null !;
    private TextureRect _title = null !;
    private VBoxContainer _actions = null !, _colors = null !;
    private Button _continue = null !, _start = null !;
    private Label _footer = null !, _notice = null !, _colorTitle = null !;
    private FleetColor _selected = FleetColor.Blue;
    private readonly Dictionary<FleetColor, Button> _swatches = new();
    private readonly Dictionary<int, Button> _opponents = new();
    private Label _opponentNote = null !;
    public bool IsOpen => Visible;
    public FleetColor SelectedColor => _selected;
    public AiDifficulty Difficulty { get; private set; } = AiDifficulty.Admiral;
    private readonly Dictionary<AiDifficulty, Button> _difficultyButtons = new();
    public int OpponentCount { get; private set; } = 3;
    public MapKind WorldKind { get; private set; } = MapKind.Oceans;
    private OptionButton _worldChoice = null!;
    private PanelContainer _setupPaper = null!;
    private ScrollContainer _setupScroll = null!;
    private Label _worldHint = null!;
    private bool _layingOut;
=======
    private Control _root = null!;
    private MenuHarborView _harbor = null!;
    private TextureRect _title = null!;
    private VBoxContainer _actions = null!, _colors = null!;
    private Button _continue = null!, _start = null!;
    private Label _footer = null!, _notice = null!, _colorTitle = null!;
    private FleetColor _selected = FleetColor.Blue;
    private readonly Dictionary<FleetColor, Button> _swatches = new();
    private readonly Dictionary<int, Button> _opponents = new();
    private Label _opponentNote = null!;
    public bool IsOpen => Visible;
    public FleetColor SelectedColor => _selected;
    public int OpponentCount { get; private set; } = 3;
>>>>>>> Stashed changes

    public void SetNotice(string text) => _notice.Text = text;
    public override void _Ready()
    {
        Layer = 40;
        _root = new Control
        {
            Name = "HomeRoot",
            Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _harbor = new MenuHarborView
        {
            Name = "LivingHarbor",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_harbor);
        _harbor.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _title = new TextureRect
        {
            Name = "AncientNavalTitle",
            Texture = GD.Load<Texture2D>("res://assets/ui/ancient-naval-title.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_title);
        _actions = new VBoxContainer
        {
            Name = "StartActions"
        };
        _actions.AddThemeConstantOverride("separation", 12);
        _root.AddChild(_actions);
        _actions.AddChild(MakeButton("HomeNewGame", "New game", ShowColors));
        _continue = MakeButton("HomeContinue", "Continue", () => ContinueRequested?.Invoke());
        _actions.AddChild(_continue);
        _actions.AddChild(MakeButton("HomeExit", "Exit game", () => ExitRequested?.Invoke()));
<<<<<<< Updated upstream
        _actions.AddChild(new LanguageButtons());
        _actions.AddChild(new UiScaleSlider { OverArtwork = true });
=======
>>>>>>> Stashed changes
        _colors = new VBoxContainer
        {
            Name = "ColorSelection"
        };
<<<<<<< Updated upstream
        _colors.AddThemeConstantOverride("separation", 7);
        _setupPaper = new PanelContainer { Name = "VoyageSetupPaper" };
        _setupPaper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.97f));
        PapyrusGrain.Apply(_setupPaper);
        _root.AddChild(_setupPaper);
        _setupScroll = PapyrusModal.Wrap(_setupPaper, _colors, "VoyageSetupScroll");
        var heading = new Label { Text = "CHART YOUR VOYAGE", HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        heading.AddThemeFontSizeOverride("font_size", 25);
        heading.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(heading);
        var divider = new HSeparator();
        divider.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = PapyrusStyle.Bronze, Thickness = 1 });
        _colors.AddChild(divider);
        _colorTitle = new Label
        {
            Text = "Your fleet & its emblem",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _colorTitle.AddThemeFontSizeOverride("font_size", 23);
        _colorTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(_colorTitle);
        var row = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        row.AddThemeConstantOverride("h_separation", 5);
        row.AddThemeConstantOverride("v_separation", 5);
        _colors.AddChild(row);
        foreach (var color in Enum.GetValues<FleetColor>())
        {
            var swatch = MakeButton("FleetColor" + color, "", () => Choose(color));
            swatch.CustomMinimumSize = new(68, 66);
            swatch.AddChild(new FleetCrest { Faction = color, Position = new(0, 6), Size = new(68, 32), MouseFilter = Control.MouseFilterEnum.Ignore });
            var colorLabel = new Label { Text = color.ToString(), Position = new(0, 41), Size = new(68, 18), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            colorLabel.AddThemeFontSizeOverride("font_size", 12);
            colorLabel.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
            swatch.AddChild(colorLabel);
=======
        _colors.AddThemeConstantOverride("separation", 11);
        _root.AddChild(_colors);
        _colorTitle = new Label
        {
            Text = "Choose your fleet color",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _colorTitle.AddThemeFontSizeOverride("font_size", 23);
        _colorTitle.AddThemeColorOverride("font_color", new Color("f1e1ba"));
        _colors.AddChild(_colorTitle);
        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 10);
        _colors.AddChild(row);
        foreach (var color in Enum.GetValues<FleetColor>())
        {
            var swatch = MakeButton("FleetColor" + color, "●", () => Choose(color));
            swatch.CustomMinimumSize = new(58, 54);
>>>>>>> Stashed changes
            swatch.TooltipText = color.ToString();
            swatch.AddThemeColorOverride("font_color", FleetPalette.Color(color));
            swatch.AddThemeColorOverride("font_hover_color", FleetPalette.Color(color));
            swatch.AddThemeColorOverride("font_pressed_color", FleetPalette.Color(color));
            swatch.AddThemeColorOverride("font_outline_color", PapyrusStyle.Ink);
            swatch.AddThemeConstantOverride("outline_size", 2);
            _swatches[color] = swatch;
            row.AddChild(swatch);
        }

        var opponentTitle = new Label
        {
            Text = "Rival fleets",
<<<<<<< Updated upstream
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        opponentTitle.AddThemeFontSizeOverride("font_size", 19);
        opponentTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(opponentTitle);
        var rivals = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
=======
            HorizontalAlignment = HorizontalAlignment.Center
        };
        opponentTitle.AddThemeFontSizeOverride("font_size", 19);
        opponentTitle.AddThemeColorOverride("font_color", new Color("f1e1ba"));
        _colors.AddChild(opponentTitle);
        var rivals = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
>>>>>>> Stashed changes
        rivals.AddThemeConstantOverride("separation", 10);
        _colors.AddChild(rivals);
        for (int count = 1; count <= 4; count++)
        {
            int choice = count;
            var button = MakeButton("OpponentCount" + count, count.ToString(), () => ChooseOpponents(choice));
<<<<<<< Updated upstream
            button.CustomMinimumSize = new(50, 44);
            _opponents[count] = button;
            rivals.AddChild(button);
        }

        _opponentNote = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _opponentNote.AddThemeFontSizeOverride("font_size", 14);
        _opponentNote.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
        _colors.AddChild(_opponentNote);
        var difficultyTitle = new Label { Text = "Rival seamanship", HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        difficultyTitle.AddThemeFontSizeOverride("font_size", 19);
        difficultyTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(difficultyTitle);
        var difficultyRow = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        difficultyRow.AddThemeConstantOverride("separation", 6);
        _colors.AddChild(difficultyRow);
        foreach (var choice in Enum.GetValues<AiDifficulty>())
        {
            var button = MakeButton("Difficulty" + choice, choice.ToString(), () => ChooseDifficulty(choice));
            button.CustomMinimumSize = new(88, 42);
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            button.AddThemeFontSizeOverride("font_size", 16);
            button.TooltipText = choice switch { AiDifficulty.Boatswain => "A forgiving sailor: simple attacks and modest fleets", AiDifficulty.Captain => "An experienced captain: the previous tactical rules", _ => "An admiral: coordinated guns, cautious scouts and economic recovery" };
            _difficultyButtons[choice] = button;
            difficultyRow.AddChild(button);
        }
        ChooseDifficulty(Difficulty);
        var worldTitle = new Label { Text = "Shape of the world", HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        worldTitle.AddThemeFontSizeOverride("font_size", 19);
        worldTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(worldTitle);
        _worldChoice = new OptionButton { Name = "WorldGeneration", CustomMinimumSize = new(0, 42) };
        PapyrusStyle.Button(_worldChoice, 18);
        AddWorldChoice("Sea World", MapKind.SeaWorld);
        AddWorldChoice("Oceans", MapKind.Oceans);
        AddWorldChoice("Continents", MapKind.Continents);
        AddWorldChoice("Pangaea", MapKind.Pangaea);
        _worldChoice.Selected = 1;
        _worldChoice.ItemSelected += index =>
        {
            WorldKind = (MapKind)_worldChoice.GetItemId((int)index);
            UpdateWorldHint();
        };
        _colors.AddChild(_worldChoice);
        _worldHint = new Label { Name = "WorldDescription", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(0, 34) };
        _worldHint.AddThemeFontSizeOverride("font_size", 13);
        _worldHint.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
        _colors.AddChild(_worldHint);
        UpdateWorldHint();
        _start = MakeButton("StartBattle", "Sail with the blue fleet", () => StartRequested?.Invoke(_selected));
        _start.CustomMinimumSize = new(0, 48);
        _colors.AddChild(_start);
        var back = MakeButton("CancelColor", "Back", () =>
        {
            _colors.Hide();
            _actions.Show();
            Layout();
        });
        back.CustomMinimumSize = new(0, 44);
        _colors.AddChild(back);
=======
            button.CustomMinimumSize = new(62, 44);
            _opponents[count] = button;
            rivals.AddChild(button);
        }
        _opponentNote = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _opponentNote.AddThemeFontSizeOverride("font_size", 14);
        _opponentNote.AddThemeColorOverride("font_color", new Color("e1cca0"));
        _colors.AddChild(_opponentNote);

        _start = MakeButton("StartBattle", "Sail with the blue fleet", () => StartRequested?.Invoke(_selected));
        _colors.AddChild(_start);
        _colors.AddChild(MakeButton("CancelColor", "Back", () =>
        {
            _colors.Hide();
            _actions.Show();
        }));
>>>>>>> Stashed changes
        _colors.Hide();
        _notice = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _notice.AddThemeFontSizeOverride("font_size", 15);
        _notice.AddThemeColorOverride("font_color", new("edc69d"));
        _root.AddChild(_notice);
        _footer = new Label
        {
            Text = "GitHub: Beygelman  @Ancient_Naval_v0.13 30.09.2026",
            Modulate = new Color(1, 1, 1, .42f)
        };
        _footer.AddThemeFontSizeOverride("font_size", 14);
<<<<<<< Updated upstream
        _footer.AddThemeColorOverride("font_color", new Color(.9f, .93f, .84f, 1));
        _root.AddChild(_footer);
        _root.Resized += Layout;
        _colors.MinimumSizeChanged += Layout;
        Language.Changed += Layout;
        Choose(_selected);
        ChooseOpponents(OpponentCount);
        UiScale.Bind(this, _root, Layout);
=======
        _root.AddChild(_footer);
        _root.Resized += Layout;
        Choose(_selected);
        ChooseOpponents(OpponentCount);
>>>>>>> Stashed changes
        Layout();
    }

    private static Button MakeButton(string name, string text, Action action)
    {
        var b = new Button
        {
            Name = name,
            Text = text,
<<<<<<< Updated upstream
            CustomMinimumSize = new(0, 56),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
=======
            CustomMinimumSize = new(340, 56),
>>>>>>> Stashed changes
            FocusMode = Control.FocusModeEnum.All
        };
        PapyrusStyle.Button(b, 21);
        b.Pressed += action;
        return b;
    }

    private void Choose(FleetColor color)
    {
        _selected = color;
        _harbor.FleetColor = color;
        _start.Text = $"Sail with the {color.ToString().ToLowerInvariant()} fleet";
<<<<<<< Updated upstream
        foreach (var(choice, b)in _swatches)
        {
            b.AddThemeStyleboxOverride("normal", PapyrusStyle.Panel(choice == color ? 1 : .45f));
            b.Modulate = choice == color ? Colors.White : new Color(1,1,1,.75f);
        }
    }

    private void ChooseDifficulty(AiDifficulty difficulty)
    {
        Difficulty = difficulty;
        foreach (var (choice, button) in _difficultyButtons)
            button.Text = choice == difficulty ? $"‹ {choice} ›" : choice.ToString();
    }

    private void AddWorldChoice(string title, MapKind kind) => _worldChoice.AddItem(title, (int)kind);

    private void UpdateWorldHint()
    {
        _worldChoice.TooltipText = WorldKind switch
        {
            MapKind.SeaWorld => "Open sea, mountain islets and chains of one to three land tiles",
            MapKind.Continents => "Several broad islands, shallow winding rivers and island chains",
            MapKind.Pangaea => "A central great land with river channels, lakes and sheltered bays",
            _ => "The familiar scattered archipelagos and wide ocean passages"
        };
        if (_worldHint is not null) _worldHint.Text = _worldChoice.TooltipText;
=======
        foreach (var (choice, b) in _swatches)
            b.Text = choice == color ? "◆" : "●";
>>>>>>> Stashed changes
    }

    private void ChooseOpponents(int count)
    {
        OpponentCount = Math.Clamp(count, 1, 4);
<<<<<<< Updated upstream
        foreach (var(choice, button)in _opponents)
=======
        foreach (var (choice, button) in _opponents)
>>>>>>> Stashed changes
        {
            button.Text = choice == OpponentCount ? $"‹ {choice} ›" : choice.ToString();
            button.TooltipText = $"{choice} rival fleet{(choice == 1 ? "" : "s")}";
        }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
        _opponentNote.Text = OpponentCount switch
        {
            1 => "A close sea · one rival captain",
            2 => "A wider sea · two rival captains",
            3 => "The great sea · three rival captains",
            _ => "An open expanse · four rival captains"
        };
    }

    public void ShowHome(bool canContinue, string notice = "")
    {
        Show();
        _root.Show();
        _continue.Disabled = !canContinue;
<<<<<<< Updated upstream
        _continue.Visible = canContinue;
=======
>>>>>>> Stashed changes
        _actions.Show();
        _colors.Hide();
        _notice.Text = notice;
        Layout();
    }

    public void ShowColors()
    {
        Show();
        _actions.Hide();
        _colors.Show();
        _notice.Text = "";
        Layout();
    }

    internal int LayoutPasses { get; private set; }

    private void Layout()
    {
<<<<<<< Updated upstream
        if (_root is null || _footer is null || _layingOut)
            return;
        _layingOut = true;
        LayoutPasses++;
        var size = UiScale.LogicalViewport(this);
        float width = Math.Min(Math.Clamp(size.X * .37f, 300, 470), Math.Max(220, size.X - 32));
        float x = Mathf.Clamp(size.X * .73f - width * .5f, 16, Math.Max(16, size.X - width - 16));
        bool choosing = _colors.Visible;
        _title.Position = new(x - 90, choosing ? 8 : size.Y * .105f);
        _title.Size = new(width + 180, choosing ? Math.Min(90, size.Y * .13f) : size.Y * .23f);
        _actions.Position = new(x, Math.Min(size.Y * .395f, Math.Max(24, size.Y - _actions.GetCombinedMinimumSize().Y - 42)));
        _actions.Size = new(width, 0);
        PapyrusModal.Layout(_setupPaper, _setupScroll, _colors, size);
        _setupPaper.Scale = Vector2.One;
        _setupPaper.Position = new(Math.Max(12, size.X - _setupPaper.Size.X - 24), (size.Y - _setupPaper.Size.Y) / 2);
        _setupPaper.Visible = choosing;
        _notice.Position = new(x, size.Y * .82f);
        _notice.Size = new(width, 65);
        _footer.Position = new(22, size.Y - 32);
        _layingOut = false;
    }
    public override void _ExitTree() => Language.Changed -= Layout;
=======
        if (_root is null || _footer is null)
            return;
        LayoutPasses++;
        var size = GetViewport().GetVisibleRect().Size;
        float width = Math.Clamp(size.X * .37f, 330, 470);
        float x = Math.Clamp(size.X * .73f - width * .5f, 20, size.X - width - 20);
        _title.Position = new(x - 90, size.Y * .105f);
        _title.Size = new(width + 180, size.Y * .23f);
        _actions.Position = new(x, size.Y * .395f);
        _actions.Size = new(width, 0);
        _colors.Position = new(x, size.Y * .32f);
        _colors.Size = new(width, 0);
        _notice.Position = new(x, size.Y * .82f);
        _notice.Size = new(width, 65);
        _footer.Position = new(22, size.Y - 32);
    }
>>>>>>> Stashed changes
}
