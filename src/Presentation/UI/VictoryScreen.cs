using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>A modal result, with a finite, cosmetic celebration. No simulation
/// randomness, timers or statistics mutations belong to this scene.</summary>
public partial class VictoryScreen : CanvasLayer
{
    private Control _root = null!;
    private ColorRect _shade = null!;
    private PanelContainer _paper = null!;
    private VBoxContainer _footer = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _body = null!;
    private Label _heading = null!, _currency = null!, _built = null!, _sunk = null!, _nations = null!, _footnote = null!;
    private Button _home = null!;
    private Label _story = null!;
    private VictoryCelebration _celebration = null!;
    private float _age;
    private bool _layingOut;
    private bool _closing;
    private Color _nationInk = PaintedVoyageChoice.Burgundy;
    internal bool InstantAnimations { get; set; }

    public event Action? HomeRequested, ExitRequested, CloseRequested;
    public bool IsOpen => Visible;
    internal int ShowCount { get; private set; }
    internal int ActiveSparkCount => _celebration?.ActiveSparkCount ?? 0;
    internal int ActiveFlameCount => _celebration?.ActiveFlameCount ?? 0;
    internal string StatisticsText => $"{_currency.Text}|{_built.Text}|{_sunk.Text}|{_nations.Text}";

    public override void _Ready()
    {
        Layer = 60;
        _root = new Control { Name = "VictoryRoot", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _shade = new ColorRect
        {
            Name = "VictoryShade",
            Color = new Color(.012f, .027f, .034f, .8f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _root.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _celebration = new VictoryCelebration
        {
            Name = "VictoryFireworks",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_celebration);
        _celebration.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var center = new CenterContainer { Name = "VictoryCenter", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(center);
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _paper = new RollingModalPaper { Name = "VictoryPaper", MouseFilter = Control.MouseFilterEnum.Stop,
            CeremonialDecorations = true };
        center.AddChild(_paper);
        var column = new VBoxContainer { Name = "VictoryContent" };
        _body = column;
        column.AddThemeConstantOverride("separation", 17);
        _footer = new VBoxContainer();
        _footer.AddThemeConstantOverride("separation", 6);
        _scroll = PapyrusModal.WrapWithFooter(_paper, column, _footer, "VictoryScroll");
        _scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;

        _heading = Text("VICTORY", 66, PapyrusStyle.Ink);
        _heading.Name = "VictoryHeading";
        _heading.AddThemeFontOverride("font", new SystemFont { FontNames = new[] { "Georgia", "Times New Roman" } });
        _heading.AddThemeColorOverride("font_shadow_color", new Color(1, .77f, .30f, .34f));
        _heading.AddThemeConstantOverride("shadow_outline_size", 10);
        _heading.AddThemeConstantOverride("shadow_offset_x", 0);
        _heading.AddThemeConstantOverride("shadow_offset_y", 0);
        column.AddChild(_heading);

        var story = _story = Text("You preserved your people.\nThe ship of new hope sails on.", 18, PapyrusStyle.Ink);
        story.Name = "VictoryStory";
        story.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(story);
        column.AddChild(Text("—   THE VOYAGE REMEMBERED   —", 13, PapyrusStyle.FaintInk));
        var ledger = new GridContainer { Name = "VictoryStatistics", Columns = 2 };
        ledger.AddThemeConstantOverride("h_separation", 28);
        ledger.AddThemeConstantOverride("v_separation", 18);
        column.AddChild(ledger);
        _currency = Statistic(ledger, "Currency earned", "VictoryCurrency");
        _nations = Statistic(ledger, "Enemy flagships sunk", "VictoryNations");
        _built = Statistic(ledger, "Ships built", "VictoryBuilt");
        _sunk = Statistic(ledger, "Enemy vessels sunk", "VictorySunk");
        _footnote = Text("", 13, PapyrusStyle.FaintInk);
        _footnote.Name = "VictoryFootnote";
        _footnote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_footnote);

        _footer.AddChild(Action("InspectMap", "View the map", async () =>
        {
            if (_closing) return;
            await FoldOutcome();
            CloseRequested?.Invoke();
        }));
        var buttons = new VBoxContainer { Name = "VictoryActions" };
        buttons.AddThemeConstantOverride("separation", 6);
        _footer.AddChild(buttons);
        _home = Action("VictoryHome", "Return to menu", async () =>
        {
            if (_closing) return;
            await FoldOutcome();
            HomeRequested?.Invoke();
        });
        buttons.AddChild(_home);
        buttons.AddChild(Action("VictoryExit", "Exit game", async () =>
        {
            if (_closing) return;
            await FoldOutcome();
            ExitRequested?.Invoke();
        }));
        UiScale.Bind(this, _root, Layout);
        _body.MinimumSizeChanged += Layout;
        Layout();
        Close();
    }

    private static Label Text(string text, int size, Color color)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label Statistic(GridContainer ledger, string caption, string name)
    {
        var cell = new VBoxContainer { Name = name + "Cell", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        cell.AddThemeConstantOverride("separation", 3);
        ledger.AddChild(cell);
        cell.AddChild(Text(caption, 15, PapyrusStyle.FaintInk));
        var amount = Text("0", 30, PapyrusStyle.Ink);
        amount.Name = name;
        cell.AddChild(amount);
        return amount;
    }

    private Button Action(string name, string text, System.Action action)
    {
        var button = new BrushPaperButton { Name = name, Text = text, CustomMinimumSize = new(0, 47),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, Ink = _nationInk, Underline = name == "InspectMap" };
        button.Pressed += action;
        return button;
    }

    private void Layout()
    {
        if (_layingOut || _heading is null) return;
        _layingOut = true;
        var viewport = UiScale.LogicalViewport(this);
        _heading.AddThemeFontSizeOverride("font_size", 42);
        PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _footer, viewport);
        _layingOut = false;
    }

    public void ShowVictory(VoyageStatistics statistics, int round) => ShowOutcome(statistics, round, true);

    public void ShowOutcome(VoyageStatistics statistics, int round, bool won, string? narration = null, Color? nationColor = null)
    {
        _heading.Text = won ? "VICTORY" : "DEFEAT";
        _story.Text = narration ?? (won ? "You preserved your people.\nThe ship of new hope sails on." : "Your flagship has fallen.\nYour voyage will be remembered.");
        _nationInk = nationColor?.Darkened(.25f) ?? PaintedVoyageChoice.Burgundy;
        ((RollingModalPaper)_paper).SetNation(nationColor ?? new Color("73bac6"));
        foreach (var button in DescendantButtons(_paper))
        {
            button.Disabled = false;
            button.Ink = _nationInk;
            button.ResetStamp();
        }
        _currency.Text = statistics.CurrencyEarned.ToString("N0") + " Thors";
        _built.Text = statistics.ShipsBuilt.ToString("N0");
        _sunk.Text = statistics.EnemyShipsDestroyed.ToString("N0");
        _nations.Text = statistics.NationsDefeated.ToString("N0");
        _footnote.Text = $"Turn {round} · Ships and flagships sunk by your fleet.\nIncome includes rewards; the starting reserve is excluded.";
        _scroll.ScrollVertical = 0;
        _age = 0;
        _closing = false;
        ShowCount++;
        Visible = true;
        _paper.Modulate = new Color(1, 1, 1, InstantAnimations ? 1 : 0);
        _shade.Modulate = new Color(1, 1, 1, InstantAnimations ? 1 : 0);
        Layout();
        _ = ((RollingModalPaper)_paper).OpenAsync(InstantAnimations);
        if (won) _celebration.Start(); else _celebration.StartDefeat();
        SetProcess(true);
        SetProcessInput(true);
        _home.GrabFocus();
    }

    private static IEnumerable<BrushPaperButton> DescendantButtons(Node root)
    {
        foreach (Node node in root.GetChildren())
        {
            if (node is BrushPaperButton button) yield return button;
            foreach (var child in DescendantButtons(node)) yield return child;
        }
    }

    private async System.Threading.Tasks.Task FoldOutcome()
    {
        if (_closing) return;
        _closing = true;
        foreach (var button in DescendantButtons(_paper)) button.Disabled = true;
        await ((RollingModalPaper)_paper).FoldAsync(InstantAnimations);
    }

    public void Close()
    {
        _paper?.Hide();
        Visible = false;
        _closing = false;
        SetProcess(false);
        SetProcessInput(false);
        _celebration?.Stop();
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float fade = InstantAnimations ? 1 : Math.Clamp(_age / .65f, 0, 1);
        fade = fade * fade * (3 - 2 * fade);
        _shade.Modulate = new Color(1, 1, 1, fade);
        _paper.Modulate = new Color(1, 1, 1, InstantAnimations ? 1 : Math.Clamp((_age - .12f) / .65f, 0, 1));
        float pulse = _age < 7 ? .30f + .10f * MathF.Sin(_age * 2.2f) : .32f;
        _heading.AddThemeColorOverride("font_shadow_color", new Color(1, .77f, .30f, pulse));
        if (_age >= 8)
            SetProcess(false);
    }

    public override void _Input(InputEvent input)
    {
        if (IsOpen && !_closing && input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            CloseByEscape();
            GetViewport().SetInputAsHandled();
        }
    }

    private async void CloseByEscape()
    {
        await FoldOutcome();
        CloseRequested?.Invoke();
    }

}
