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
    private Label _heading = null!, _currency = null!, _built = null!, _sunk = null!, _nations = null!, _footnote = null!;
    private Button _home = null!;
    private Label _story = null!;
    private VictoryCelebration _celebration = null!;
    private float _age;

    public event Action? HomeRequested, ExitRequested, CloseRequested;
    public bool IsOpen => Visible;
    internal int ShowCount { get; private set; }
    internal int ActiveSparkCount => _celebration?.ActiveSparkCount ?? 0;
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
        _paper = new PanelContainer { Name = "VictoryPaper", MouseFilter = Control.MouseFilterEnum.Stop };
        var parchment = PapyrusStyle.Panel(.95f);
        parchment.BgColor = new Color("192c32");
        parchment.BorderColor = new Color("b89a61");
        parchment.ContentMarginLeft = parchment.ContentMarginRight = 38;
        parchment.ContentMarginTop = parchment.ContentMarginBottom = 28;
        _paper.AddThemeStyleboxOverride("panel", parchment);
        center.AddChild(_paper);
        var column = new VBoxContainer { Name = "VictoryContent" };
        column.AddThemeConstantOverride("separation", 17);
        _paper.AddChild(column);

        _heading = Text("VICTORY", 66, new Color("f8e7b0"));
        _heading.Name = "VictoryHeading";
        _heading.AddThemeFontOverride("font", new SystemFont { FontNames = new[] { "Georgia", "Times New Roman" } });
        _heading.AddThemeColorOverride("font_shadow_color", new Color(1, .77f, .30f, .34f));
        _heading.AddThemeConstantOverride("shadow_outline_size", 10);
        _heading.AddThemeConstantOverride("shadow_offset_x", 0);
        _heading.AddThemeConstantOverride("shadow_offset_y", 0);
        column.AddChild(_heading);

        var story = _story = Text("You preserved your people.\nThe ship of new hope sails on.", 20, new Color("e9d9b8"));
        story.Name = "VictoryStory";
        story.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(story);
        column.AddChild(Text("—   THE VOYAGE REMEMBERED   —", 13, new Color("bca779")));
        var ledger = new GridContainer { Name = "VictoryStatistics", Columns = 2 };
        ledger.AddThemeConstantOverride("h_separation", 28);
        ledger.AddThemeConstantOverride("v_separation", 18);
        column.AddChild(ledger);
        _currency = Statistic(ledger, "Currency earned", "VictoryCurrency");
        _nations = Statistic(ledger, "Enemy flagships sunk", "VictoryNations");
        _built = Statistic(ledger, "Ships built", "VictoryBuilt");
        _sunk = Statistic(ledger, "Enemy vessels sunk", "VictorySunk");
        _footnote = Text("", 13, new Color("b9b7a5"));
        _footnote.Name = "VictoryFootnote";
        _footnote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_footnote);

        column.AddChild(Action("InspectMap", "View the map", () => CloseRequested?.Invoke()));
        var buttons = new HBoxContainer { Name = "VictoryActions", Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 14);
        column.AddChild(buttons);
        _home = Action("VictoryHome", "Return to menu", () => HomeRequested?.Invoke());
        buttons.AddChild(_home);
        buttons.AddChild(Action("VictoryExit", "Exit game", () => ExitRequested?.Invoke()));
        GetViewport().SizeChanged += Layout;
        Layout();
        Close();
    }

    private static Label Text(string text, int size, Color color)
    {
        var label = new Label
        {
            Text = text,
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
        cell.AddChild(Text(caption, 15, new Color("c5baa0")));
        var amount = Text("0", 30, new Color("f6e3b3"));
        amount.Name = name;
        cell.AddChild(amount);
        return amount;
    }

    private static Button Action(string name, string text, System.Action action)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new(192, 47) };
        PapyrusStyle.Button(button, 17);
        button.Pressed += action;
        return button;
    }

    private void Layout()
    {
        float width = GetViewport().GetVisibleRect().Size.X;
        _paper.CustomMinimumSize = new(Math.Clamp(width - 44, 380, 650), 0);
        _heading.AddThemeFontSizeOverride("font_size", width < 560 ? 46 : 66);
    }

    public void ShowVictory(VoyageStatistics statistics, int round) => ShowOutcome(statistics, round, true);

    public void ShowOutcome(VoyageStatistics statistics, int round, bool won)
    {
        _heading.Text = won ? "VICTORY" : "DEFEAT";
        _story.Text = won ? "You preserved your people.\nThe ship of new hope sails on." : "Your flagship has fallen.\nYour voyage will be remembered.";
        _currency.Text = statistics.CurrencyEarned.ToString("N0") + " Thors";
        _built.Text = statistics.ShipsBuilt.ToString("N0");
        _sunk.Text = statistics.EnemyShipsDestroyed.ToString("N0");
        _nations.Text = statistics.NationsDefeated.ToString("N0");
        _footnote.Text = $"Turn {round} · Ships and flagships sunk by your fleet.\nIncome includes rewards; the starting reserve is excluded.";
        _age = 0;
        ShowCount++;
        Visible = true;
        _paper.Modulate = new Color(1, 1, 1, 0);
        _shade.Modulate = new Color(1, 1, 1, 0);
        if (won) _celebration.Start(); else _celebration.Stop();
        SetProcess(true);
        SetProcessInput(true);
        _home.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        SetProcess(false);
        SetProcessInput(false);
        _celebration?.Stop();
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float fade = Math.Clamp(_age / .65f, 0, 1);
        fade = fade * fade * (3 - 2 * fade);
        _shade.Modulate = new Color(1, 1, 1, fade);
        _paper.Modulate = new Color(1, 1, 1, Math.Clamp((_age - .12f) / .65f, 0, 1));
        float pulse = _age < 7 ? .30f + .10f * MathF.Sin(_age * 2.2f) : .32f;
        _heading.AddThemeColorOverride("font_shadow_color", new Color(1, .77f, .30f, pulse));
        if (_age >= 8)
            SetProcess(false);
    }

    public override void _Input(InputEvent input)
    {
        if (IsOpen && input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            CloseRequested?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree() => GetViewport().SizeChanged -= Layout;
}

internal partial class VictoryCelebration : Control
{
    private readonly record struct Spark(Vector2 Origin, Vector2 Velocity, float Born, float Life, Color Color);
    private readonly List<Spark> _sparks = new(240);
    private readonly Random _random = new(9019);
    private float _age, _paintClock;
    private int _nextBurst;
    internal int ActiveSparkCount => _sparks.Count;

    internal void Start()
    {
        _sparks.Clear();
        _age = _paintClock = 0;
        _nextBurst = 0;
        SetProcess(true);
        QueueRedraw();
    }

    internal void Stop()
    {
        _sparks.Clear();
        SetProcess(false);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        _sparks.RemoveAll(spark => _age - spark.Born >= spark.Life);
        if (_nextBurst < 7 && _age >= .4f + _nextBurst * .78f)
            Burst(_nextBurst++);
        _paintClock += (float)delta;
        if (_paintClock >= 1 / 30f)
        {
            _paintClock = 0;
            QueueRedraw();
        }
        if (_nextBurst == 7 && _sparks.Count == 0)
            SetProcess(false);
    }

    private void Burst(int index)
    {
        bool left = index % 2 == 0;
        var origin = new Vector2(Size.X * (left ? .14f : .86f), Size.Y * (.18f + .11f * (index % 3)));
        var color = (index % 3) switch
        {
            1 => new Color("8ec4ca"),
            2 => new Color("e6d9ac"),
            _ => new Color("f4c873")
        };
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.Tau / 32 + (float)_random.NextDouble() * .045f;
            float speed = 38 + (float)_random.NextDouble() * 65;
            _sparks.Add(new(origin, new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                _age, 1.4f + (float)_random.NextDouble() * .6f, color));
        }
    }

    public override void _Draw()
    {
        foreach (var spark in _sparks)
        {
            float age = _age - spark.Born;
            float progress = age / spark.Life;
            var head = spark.Origin + spark.Velocity * age + new Vector2(0, 18 * age * age);
            float trailAge = Math.Max(0, age - .055f);
            var tail = spark.Origin + spark.Velocity * trailAge + new Vector2(0, 18 * trailAge * trailAge);
            float alpha = MathF.Pow(1 - progress, 1.4f);
            DrawLine(tail, head, new Color(spark.Color, alpha * .65f), 1.5f, true);
            DrawCircle(head, 1.9f, new Color(spark.Color, alpha));
        }
    }
}
