using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.World;
using MapKind = DevAncientNaval.Core.World.WorldKind;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class StartScreen : CanvasLayer
{
    public event Action<FleetColor>? StartRequested;
    public event Action? ContinueRequested, ExitRequested;
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
    private Label _worldHint = null!;

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
        _colors = new VBoxContainer
        {
            Name = "ColorSelection"
        };
        _colors.AddThemeConstantOverride("separation", 7);
        _setupPaper = new PanelContainer { Name = "VoyageSetupPaper" };
        _setupPaper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.97f));
        _root.AddChild(_setupPaper);
        _setupPaper.AddChild(_colors);
        var heading = new Label { Text = "CHART YOUR VOYAGE", HorizontalAlignment = HorizontalAlignment.Center };
        heading.AddThemeFontSizeOverride("font_size", 25);
        heading.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(heading);
        var divider = new HSeparator();
        divider.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = PapyrusStyle.Bronze, Thickness = 1 });
        _colors.AddChild(divider);
        _colorTitle = new Label
        {
            Text = "Your fleet & its emblem",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _colorTitle.AddThemeFontSizeOverride("font_size", 23);
        _colorTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(_colorTitle);
        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 5);
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
            HorizontalAlignment = HorizontalAlignment.Center
        };
        opponentTitle.AddThemeFontSizeOverride("font_size", 19);
        opponentTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(opponentTitle);
        var rivals = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        rivals.AddThemeConstantOverride("separation", 10);
        _colors.AddChild(rivals);
        for (int count = 1; count <= 4; count++)
        {
            int choice = count;
            var button = MakeButton("OpponentCount" + count, count.ToString(), () => ChooseOpponents(choice));
            button.CustomMinimumSize = new(62, 44);
            _opponents[count] = button;
            rivals.AddChild(button);
        }

        _opponentNote = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _opponentNote.AddThemeFontSizeOverride("font_size", 14);
        _opponentNote.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
        _colors.AddChild(_opponentNote);
        var difficultyTitle = new Label { Text = "Rival seamanship", HorizontalAlignment = HorizontalAlignment.Center };
        difficultyTitle.AddThemeFontSizeOverride("font_size", 19);
        difficultyTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(difficultyTitle);
        var difficultyRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        difficultyRow.AddThemeConstantOverride("separation", 6);
        _colors.AddChild(difficultyRow);
        foreach (var choice in Enum.GetValues<AiDifficulty>())
        {
            var button = MakeButton("Difficulty" + choice, choice.ToString(), () => ChooseDifficulty(choice));
            button.CustomMinimumSize = new(106, 42);
            button.AddThemeFontSizeOverride("font_size", 16);
            button.TooltipText = choice switch { AiDifficulty.Boatswain => "A forgiving sailor: simple attacks and modest fleets", AiDifficulty.Captain => "An experienced captain: the previous tactical rules", _ => "An admiral: coordinated guns, cautious scouts and economic recovery" };
            _difficultyButtons[choice] = button;
            difficultyRow.AddChild(button);
        }
        ChooseDifficulty(Difficulty);
        var worldTitle = new Label { Text = "Shape of the world", HorizontalAlignment = HorizontalAlignment.Center };
        worldTitle.AddThemeFontSizeOverride("font_size", 19);
        worldTitle.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _colors.AddChild(worldTitle);
        _worldChoice = new OptionButton { Name = "WorldGeneration", CustomMinimumSize = new(340, 42) };
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
        _worldHint = new Label { Name = "WorldDescription", HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(400, 34) };
        _worldHint.AddThemeFontSizeOverride("font_size", 13);
        _worldHint.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
        _colors.AddChild(_worldHint);
        UpdateWorldHint();
        _start = MakeButton("StartBattle", "Sail with the blue fleet", () => StartRequested?.Invoke(_selected));
        _start.CustomMinimumSize = new(340, 48);
        _colors.AddChild(_start);
        var back = MakeButton("CancelColor", "Back", () =>
        {
            _colors.Hide();
            _actions.Show();
            Layout();
        });
        back.CustomMinimumSize = new(340, 44);
        _colors.AddChild(back);
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
        _footer.AddThemeColorOverride("font_color", new Color(.9f, .93f, .84f, 1));
        _root.AddChild(_footer);
        _root.Resized += Layout;
        Choose(_selected);
        ChooseOpponents(OpponentCount);
        Layout();
    }

    private static Button MakeButton(string name, string text, Action action)
    {
        var b = new Button
        {
            Name = name,
            Text = text,
            CustomMinimumSize = new(340, 56),
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
    }

    private void ChooseOpponents(int count)
    {
        OpponentCount = Math.Clamp(count, 1, 4);
        foreach (var(choice, button)in _opponents)
        {
            button.Text = choice == OpponentCount ? $"‹ {choice} ›" : choice.ToString();
            button.TooltipText = $"{choice} rival fleet{(choice == 1 ? "" : "s")}";
        }

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
        if (_root is null || _footer is null)
            return;
        LayoutPasses++;
        var size = GetViewport().GetVisibleRect().Size;
        float width = Math.Clamp(size.X * .37f, 330, 470);
        float x = Math.Clamp(size.X * .73f - width * .5f, 20, size.X - width - 20);
        bool choosing = _colors.Visible;
        _title.Position = new(x - 90, choosing ? 8 : size.Y * .105f);
        _title.Size = new(width + 180, choosing ? Math.Min(90, size.Y * .13f) : size.Y * .23f);
        _actions.Position = new(x, size.Y * .395f);
        _actions.Size = new(width, 0);
        float panelHeight = Math.Max(580, _colors.GetCombinedMinimumSize().Y + 24);
        float choicesScale = Math.Clamp((size.Y - 132) / panelHeight, .6f, 1);
        _setupPaper.Scale = Vector2.One * choicesScale;
        _setupPaper.Position = new((size.X - 500 * choicesScale) / 2, Math.Max(80, (size.Y - panelHeight * choicesScale) / 2));
        _setupPaper.Size = new(500, panelHeight);
        _setupPaper.Visible = choosing;
        _notice.Position = new(x, size.Y * .82f);
        _notice.Size = new(width, 65);
        _footer.Position = new(22, size.Y - 32);
    }
}
