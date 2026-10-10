using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Left-unrolled paper stamps and rolls back into its fixed right-hand spindle.</summary>
internal partial class EndTurnPaper : Button
{
    private float _stamp = 1, _unroll = 1;
    private bool _humanTurn = true, _opening, _folding;
    private Label _caption = null!;
    private Control _captionViewport = null!;
    private TaskCompletionSource? _completion;
    internal float StampProgress => _stamp;
    internal float UnrollProgress => _unroll;
    internal Color NationInk { get; set; } = PaintedVoyageChoice.Burgundy;
    public override void _Ready()
    {
        CustomMinimumSize = new(188, 54);
        FocusMode = FocusModeEnum.None;
        ClipContents = true;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty { ContentMarginLeft = 12, ContentMarginRight = 30 });
        AddThemeFontSizeOverride("font_size", 17);
        AddThemeColorOverride("font_color", new Color(PapyrusStyle.Ink, 0));
        AddThemeColorOverride("font_hover_color", new Color(PapyrusStyle.Ink, 0));
        AddThemeColorOverride("font_pressed_color", new Color(PapyrusStyle.Ink, 0));
        AddThemeColorOverride("font_disabled_color", new Color(PapyrusStyle.Ink, 0));
        _captionViewport = new Control { Name = "EndTurnRevealViewport", ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_captionViewport);
        _caption = new Label { Name = "EndTurnCaption", Text = Text,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        _caption.AddThemeFontSizeOverride("font_size", 17);
        _caption.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        _captionViewport.AddChild(_caption);
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        Resized += () => { UpdateCaptionReveal(); QueueRedraw(); };
        UpdateCaptionReveal();
        SetProcess(false);
    }
    internal void SetHumanTurn(bool active, bool instant)
    {
        if (_humanTurn == active) return;
        _humanTurn = active;
        if (!active)
        {
            Hide();
            return;
        }
        _stamp = 1;
        _unroll = instant ? 1 : 0;
        _opening = !instant;
        Show();
        UpdateCaptionReveal();
        SetProcess(_opening);
        QueueRedraw();
    }
    internal Task StampAsync(bool instant)
    {
        if (_completion is not null) return _completion.Task;
        _opening = false;
        _folding = false;
        _stamp = instant ? 1 : 0;
        _unroll = 1;
        UpdateCaptionReveal();
        QueueRedraw();
        if (instant) return Task.CompletedTask;
        _completion = new TaskCompletionSource();
        SetProcess(true);
        return _completion.Task;
    }
    internal Task FoldAsync(bool instant)
    {
        if (_completion is not null) return _completion.Task;
        _opening = false;
        _folding = true;
        if (instant)
        {
            _unroll = 0;
            UpdateCaptionReveal();
            QueueRedraw();
            return Task.CompletedTask;
        }
        _completion = new TaskCompletionSource();
        SetProcess(true);
        return _completion.Task;
    }
    public override void _Process(double delta)
    {
        if (_opening)
        {
            _unroll = Math.Min(1, _unroll + (float)delta / .35f);
            if (_unroll >= 1) _opening = false;
        }
        else if (_completion is not null)
        {
            if (_folding) _unroll = Math.Max(0, _unroll - (float)delta / .32f);
            else _stamp = Math.Min(1, _stamp + (float)delta / .28f);
        }
        UpdateCaptionReveal();
        QueueRedraw();
        if (_opening || _completion is not null && (_folding ? _unroll > 0 : _stamp < 1)) return;
        SetProcess(false);
        var completion = _completion;
        _completion = null;
        completion?.TrySetResult();
    }
    private void UpdateCaptionReveal()
    {
        if (_captionViewport is null) return;
        float eased = _unroll * _unroll * (3 - 2 * _unroll);
        float width = Math.Max(1, Size.X - 22);
        float left = width * (1 - eased);
        _captionViewport.Position = new(left, 0);
        _captionViewport.Size = new(Math.Max(0, width - left), Size.Y);
        _caption.Position = new(-left, 0);
        _caption.Size = new(width, Size.Y);
        _caption.Scale = Vector2.One;
    }
    public override void _Draw()
    {
        float eased = _unroll * _unroll * (3 - 2 * _unroll);
        float spindleX = Size.X - 22;
        float left = spindleX * (1 - eased);
        var paper = PapyrusStyle.Paper.Lightened(IsHovered() ? .05f : 0);
        var rect = new Rect2(left, 5, Math.Max(1, spindleX - left + 10), Size.Y - 10);
        PapyrusGrain.Draw(this, rect);
        float radius = ScrollRollArt.Radius(new(Size.Y, Size.X), _unroll);
        var roll = new Rect2(spindleX + 10 - radius, 0, radius * 2, Size.Y);
        for (int strip = 0; strip < 12; strip++)
        {
            float t = strip / 11f;
            var tint = paper.Darkened(.22f).Lerp(paper.Lightened(.16f), MathF.Sin(t * Mathf.Pi));
            DrawRect(new(roll.Position.X + t * roll.Size.X, 0, roll.Size.X / 11 + .7f, Size.Y), tint);
        }
        DrawLine(roll.Position, new(roll.Position.X, roll.End.Y), PapyrusStyle.Bronze, 2, true);
        DrawLine(roll.Position + new Vector2(4, 0), new(roll.Position.X + 4, roll.End.Y), paper.Lightened(.14f), 3, true);
        DrawArc(new(Size.X - 12, 5), radius, Mathf.Pi, Mathf.Tau, 20, PapyrusStyle.Bronze, 1, true);
        DrawArc(new(Size.X - 12, Size.Y - 5), radius, 0, Mathf.Pi, 20, PapyrusStyle.Bronze, 1, true);
        if (_completion is not null && _unroll > .65f)
            HandStampArt.Draw(this, Size * .5f, _stamp, NationInk);
    }
    public override void _ExitTree() => _completion?.TrySetCanceled();
}

/// <summary>The legacy type name stays internal; the ready counter is now a nation monument.</summary>
internal partial class ReadyActionJug : Button
{
    private static readonly Vector2[] MonumentShadow = MakeShadow();
    internal int Count { get; private set; }
    internal Vector2 PrintedCountCenter => new(Size.X * .5f + 4, Size.Y - 47);
    internal bool CounterPointerInput { get; private set; }
    internal float ActivityProgress => _activity;
    internal bool HumanTurnActive => _humanTurn;
    internal float EffectPhase => _clock;
    private Color _clay;
    private FleetColor _nation;
    private bool _humanTurn;
    private float _activity, _clock, _redrawClock;
    private ReadyNationRadiance _radiance = null!;
    internal event Action? CounterRequested;

    public override void _GuiInput(InputEvent input)
    {
        // The upper monument ends the turn; its inset numeral still cycles crews.
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
        {
            // The event has the actual local finger/mouse coordinate. A global
            // mouse query can still point elsewhere during touch or pushed input.
            CounterPointerInput = mouse.Position.Y >= PrintedCountCenter.Y - 20;
            if (CounterPointerInput)
            {
                if (mouse.Pressed && !Disabled) CounterRequested?.Invoke();
                AcceptEvent();
            }
        }
    }

    internal void Update(int count, Color clay, FleetColor nation)
    {
        if (Count == count && _clay == clay && _nation == nation) return;
        Count = count;
        _clay = clay;
        _nation = nation;
        if (_radiance is not null) _radiance.Nation = nation;
        QueueRedraw();
        _radiance?.QueueRedraw();
    }

    internal void SetHumanTurn(bool active, bool instant)
    {
        if (_humanTurn == active && !instant) return;
        _humanTurn = active;
        if (instant) _activity = active ? 1 : 0;
        UpdateRadiance();
        UpdateProcessing();
    }

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        _radiance = new ReadyNationRadiance { Name = "ReadyNationRadiance", Nation = _nation,
            MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_radiance);
        _radiance.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        Resized += () => { QueueRedraw(); _radiance.QueueRedraw(); };
        VisibilityChanged += UpdateProcessing;
        UpdateRadiance();
        UpdateProcessing();
    }

    private void UpdateProcessing() => SetProcess(IsVisibleInTree() && (_humanTurn || _activity > .001f));

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) { SetProcess(false); return; }
        float next = Mathf.MoveToward(_activity, _humanTurn ? 1 : 0, (float)delta / .48f);
        if (next != _activity) { _activity = next; UpdateRadiance(); }
        _clock += (float)delta;
        _redrawClock += (float)delta;
        if (_redrawClock >= 1f / 30)
        {
            _redrawClock = 0;
            _radiance.Clock = _clock;
            _radiance.QueueRedraw();
        }
        UpdateProcessing();
    }

    private void UpdateRadiance()
    {
        // End-turn fades the light without shrinking or moving the object/count.
        SelfModulate = Colors.White.Lerp(new Color(.63f, .65f, .68f), 1 - _activity);
        if (_radiance is not null) _radiance.Modulate = new Color(1, 1, 1, _activity);
    }

    public override bool _HasPoint(Vector2 point)
    {
        // Keep the paper inscription below it fully clickable.
        return point.X >= 18 && point.X <= Size.X - 18 && point.Y >= 10 && point.Y <= Size.Y - 10;
    }

    internal static Vector2 GroundAnchor(Vector2 size) => new(size.X * .5f, size.Y - 48);
    internal static Vector2 Project(float x, float y, float z) => new((x - y) * .92f, (x + y) * .42f - z);
    internal const float MonumentScale = 3.75f;

    private static Vector2[] MakeShadow()
    {
        var points = new Vector2[32];
        for (int i = 0; i < points.Length; i++)
            points[i] = Vector2.FromAngle(i * Mathf.Tau / points.Length) * new Vector2(19, 4) + new Vector2(0, 6);
        return points;
    }

    public override void _Draw()
    {
        var anchor = GroundAnchor(Size);
        DrawSetTransform(anchor, 0, Vector2.One * MonumentScale);
        DrawColoredPolygon(MonumentShadow, new Color(.02f, .04f, .045f, .32f));
        var stone = new Color("cfbc96");
        for (int step = 0; step < 3; step++)
            FactionSanctuaryArt.Box(this, Project, 0, 0, step * 1.1f - 3.3f,
                17 - step * 2, 14 - step * 1.8f, 1.1f, stone.Darkened(step * .035f));
        FactionSanctuaryArt.Draw(this, Project, _nation);
        var metal = _clay.Darkened(.28f);
        // Inlaid front frieze, stairs and bronze feet add depth at HUD scale.
        foreach (int side in new[] { -1, 1 })
        {
            FactionSanctuaryArt.Box(this, Project, side * 6.6f, 5.6f, -.4f, 1.4f, 1.4f, 2.2f, metal);
            for (int step = 0; step < 3; step++)
                DrawLine(Project(side * 1.4f, 6.3f + step * .9f, -step * .9f),
                    Project(side * 4.4f, 6.3f + step * .9f, -step * .9f), stone.Lightened(.12f), .35f, true);
        }
        if (_nation is not FleetColor.Green and not FleetColor.Red)
        {
            for (int window = -1; window <= 1; window++)
            {
                var at = Project(window * 2.4f, 3.6f, 10.5f);
                DrawLine(at, Project(window * 2.4f, 3.6f, 12.4f), new Color("70614e"), .65f, true);
                DrawLine(at + new Vector2(.18f, 0), Project(window * 2.4f, 3.6f, 12.2f) + new Vector2(.18f, 0),
                    new Color("e6d9b9"), .20f, true);
            }
        }
        DrawSetTransform(PrintedCountCenter, .10f, new Vector2(1, .94f));
        var countInk = _clay.Darkened(.48f);
        DrawColoredPolygon(new[] { new Vector2(-22, -15), new Vector2(22, -17),
            new Vector2(23, 15), new Vector2(-22, 17) }, new Color(countInk, .98f));
        DrawPolyline(new[] { new Vector2(-22, -15), new Vector2(22, -17), new Vector2(23, 15) },
            new Color("c6b17d"), 1.1f, true);
        DrawLine(new(-21, 16), new(22, 14), new Color("68573d"), 2, true);
        string text = Count.ToString();
        int fontSize = Count >= 100 ? 24 : 29;
        float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: fontSize).X;
        DrawString(ThemeDB.FallbackFont, new(-width / 2 + .7f, 10.7f), text,
            fontSize: fontSize, modulate: countInk.Darkened(.4f));
        DrawString(ThemeDB.FallbackFont, new(-width / 2, 10), text,
            fontSize: fontSize, modulate: new Color("f4e5b6"));
        DrawSetTransform(Vector2.Zero);
    }
}

/// <summary>Thirty-Hz bounded cosmetic shrine light; the parent stops it while hidden or dormant.</summary>
internal partial class ReadyNationRadiance : Control
{
    internal FleetColor Nation { get; set; }
    internal float Clock { get; set; }
    public override void _Ready() => SetProcess(false);
    public override void _Draw()
    {
        if (Modulate.A <= .001f) return;
        DrawSetTransform(ReadyActionJug.GroundAnchor(Size), 0, Vector2.One * ReadyActionJug.MonumentScale);
        FactionSanctuaryArt.DrawEffects(this, ReadyActionJug.Project, Nation, Clock);
        var tint = FleetPalette.Color(Nation).Lightened(.36f);
        // A slow inlaid halo and six deterministic glints enrich the same nation ritual.
        float pulse = .55f + .18f * MathF.Sin(Clock * 1.2f);
        DrawArc(new(0, 4), 14, 0, Mathf.Tau, 48, new Color(tint, pulse * .30f), .38f, true);
        for (int light = 0; light < 6; light++)
        {
            float phase = (Clock * .13f + light / 6f) % 1;
            float angle = light * Mathf.Tau / 6 + phase * .4f;
            var at = new Vector2(MathF.Cos(angle) * 11, MathF.Sin(angle) * 4 - phase * 21);
            float alpha = MathF.Sin(phase * Mathf.Pi) * .50f;
            DrawLine(at - new Vector2(.7f, 0), at + new Vector2(.7f, 0), new Color(tint, alpha), .32f, true);
            DrawLine(at - new Vector2(0, 1.1f), at + new Vector2(0, 1.1f), new Color(tint, alpha), .32f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }
}
