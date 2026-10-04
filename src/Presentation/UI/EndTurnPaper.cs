using System;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Left-unrolled paper stamps and rolls back into its fixed right-hand spindle.</summary>
internal partial class EndTurnPaper : Button
{
    private float _stamp = 1, _unroll = 1;
    private bool _humanTurn = true, _opening, _folding;
    private Label _caption = null!;
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
        _caption = new Label { Name = "EndTurnCaption", Text = Text,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        _caption.AddThemeFontSizeOverride("font_size", 17);
        _caption.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        AddChild(_caption);
        _caption.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _caption.OffsetRight = -22;
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        Resized += QueueRedraw;
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
        _caption.Modulate = new Color(1, 1, 1, Math.Clamp((_unroll - .25f) / .75f, 0, 1));
        QueueRedraw();
        if (_opening || _completion is not null && (_folding ? _unroll > 0 : _stamp < 1)) return;
        SetProcess(false);
        var completion = _completion;
        _completion = null;
        completion?.TrySetResult();
    }
    public override void _Draw()
    {
        float eased = _unroll * _unroll * (3 - 2 * _unroll);
        float spindleX = Size.X - 22;
        float left = spindleX * (1 - eased);
        var paper = PapyrusStyle.Paper.Lightened(IsHovered() ? .05f : 0);
        var rect = new Rect2(left, 5, Math.Max(1, spindleX - left + 10), Size.Y - 10);
        DrawStyleBox(PapyrusStyle.Panel(), rect);
        DrawTextureRect(PapyrusGrain.Texture, rect, true, new Color(1, 1, 1, .62f));
        var roll = new Rect2(spindleX, 0, 20, Size.Y);
        DrawRect(roll, paper.Darkened(.08f));
        DrawLine(roll.Position, new(roll.Position.X, roll.End.Y), PapyrusStyle.Bronze, 2, true);
        DrawLine(roll.Position + new Vector2(4, 0), new(roll.Position.X + 4, roll.End.Y), paper.Lightened(.14f), 3, true);
        DrawArc(new(Size.X - 12, 5), 9, Mathf.Pi, Mathf.Tau, 14, PapyrusStyle.Bronze, 1, true);
        DrawArc(new(Size.X - 12, Size.Y - 5), 9, 0, Mathf.Pi, 14, PapyrusStyle.Bronze, 1, true);
        if (_completion is not null && _unroll > .65f)
            HandStampArt.Draw(this, Size * .5f, _stamp, NationInk);
    }
    public override void _ExitTree() => _completion?.TrySetCanceled();
}

internal partial class ReadyActionJug : Button
{
    internal int Count { get; private set; }
    private Color _clay;
    private FleetColor _nation;
    internal void Update(int count, Color clay, FleetColor nation)
    {
        if (Count == count && _clay == clay && _nation == nation) return;
        Count = count;
        _clay = clay;
        _nation = nation;
        QueueRedraw();
    }
    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
    }
    public override void _Draw()
    {
        var center = Size * .5f + new Vector2(0, 3);
        var shape = new[] { new Vector2(-8, -28), new Vector2(8, -28), new Vector2(8, -17), new Vector2(19, -8),
            new Vector2(20, 8), new Vector2(13, 24), new Vector2(-13, 24), new Vector2(-20, 8), new Vector2(-19, -8), new Vector2(-8, -17) };
        DrawSetTransform(center);
        DrawColoredPolygon(shape, _clay.Darkened(IsHovered() ? .15f : .25f));
        DrawLine(new(-8, -28), new(8, -28), _clay.Lightened(.25f), 3, true);
        DrawArc(new(-17, -13), 9, 1.5f, 4.6f, 14, _clay.Darkened(.1f), 3, true);
        DrawArc(new(17, -13), 9, -1.5f, 1.6f, 14, _clay.Darkened(.1f), 3, true);
        DrawPolyline(new[] { new Vector2(-15, -5), new Vector2(-13, 11), new Vector2(-8, 20) }, _clay.Lightened(.2f), 2, true);
        for (int grain = 0; grain < 26; grain++)
        {
            float x = -14 + grain * 11 % 29;
            float y = -7 + grain * 17 % 26;
            DrawLine(new(x, y), new(x + 1 + grain % 3, y + .4f), new Color(_clay.Lightened(.32f), .2f), .6f, true);
        }
        var pattern = new Color(PapyrusStyle.Paper, .66f);
        for (int i = -2; i <= 2; i++)
        {
            float x = i * 6.1f;
            float y = -9;
            if (_nation == FleetColor.Blue)
            {
                DrawArc(new(x, y), 2, 0, Mathf.Tau, 12, pattern, .8f, true);
                DrawLine(new(x, y - 3), new(x, y - 4.5f), pattern, .7f, true);
            }
            else if (_nation is FleetColor.Red or FleetColor.Purple)
            {
                DrawPolyline(new[] { new Vector2(x, y - 4), new Vector2(x + 2.5f, y), new Vector2(x, y + 3), new Vector2(x - 2.5f, y), new Vector2(x, y - 4) }, pattern, .9f, true);
            }
            else if (_nation == FleetColor.Green)
            {
                DrawLine(new(x, y + 3), new(x, y - 3), pattern, .8f, true);
                DrawLine(new(x, y), new(x - 2.4f, y - 2), pattern, .8f, true);
                DrawLine(new(x, y + 1), new(x + 2.4f, y - 1), pattern, .8f, true);
            }
            else if (_nation == FleetColor.Yellow)
            {
                DrawArc(new(x - 1.3f, y), 2.4f, -.5f, 2.4f, 9, pattern, .9f, true);
                DrawArc(new(x + 1.3f, y), 2.4f, .7f, 3.6f, 9, pattern, .9f, true);
            }
            else
            {
                DrawLine(new(x - 1.5f, y + 3), new(x - 1.5f, y - 4), pattern, 1.1f, true);
                DrawArc(new(x - 1, y - 2), 2, -Mathf.Pi / 2, Mathf.Pi / 2, 8, pattern, 1, true);
                DrawLine(new(x - 1, y), new(x + 2, y + 3), pattern, 1.1f, true);
            }
        }
        DrawLine(new(-14, 19), new(14, 19), pattern, 1.1f, true);
        DrawLine(new(-12, 22), new(12, 22), new Color(PapyrusStyle.Paper, .42f), .8f, true);
        string text = Count.ToString();
        float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 22).X;
        DrawString(ThemeDB.FallbackFont, new(-width / 2, 13), text, fontSize: 22, modulate: PapyrusStyle.Paper);
        DrawSetTransform(Vector2.Zero);
    }
}
