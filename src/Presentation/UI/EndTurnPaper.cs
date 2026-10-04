using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>A left-unrolled parchment with a bounded, four-finger ink stamp.</summary>
internal partial class EndTurnPaper : Button
{
    private float _stamp = 1;
    private TaskCompletionSource? _completion;
    internal float StampProgress => _stamp;
    public override void _Ready()
    {
        CustomMinimumSize = new(188, 54);
        FocusMode = FocusModeEnum.None;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty { ContentMarginLeft = 12, ContentMarginRight = 30 });
        AddThemeFontSizeOverride("font_size", 17);
        AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_hover_color", PapyrusStyle.Ink);
        var caption = new Label { Name = "EndTurnCaption", Text = Text,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        caption.AddThemeFontSizeOverride("font_size", 17);
        caption.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        AddChild(caption);
        caption.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        caption.OffsetRight = -22;
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        Resized += QueueRedraw;
        SetProcess(false);
    }
    internal Task StampAsync(bool instant)
    {
        if (_completion is not null) return _completion.Task;
        _stamp = instant ? 1 : 0;
        QueueRedraw();
        if (instant) return Task.CompletedTask;
        _completion = new TaskCompletionSource();
        SetProcess(true);
        return _completion.Task;
    }
    public override void _Process(double delta)
    {
        _stamp = Math.Min(1, _stamp + (float)delta / .32f);
        QueueRedraw();
        if (_stamp < 1) return;
        SetProcess(false);
        var completion = _completion;
        _completion = null;
        completion?.TrySetResult();
    }
    public override void _Draw()
    {
        var paper = PapyrusStyle.Paper.Lightened(IsHovered() ? .05f : 0);
        if (Disabled) paper = paper.Darkened(.12f);
        var rect = new Rect2(0, 5, Size.X - 12, Size.Y - 10);
        DrawStyleBox(PapyrusStyle.Panel(), rect);
        DrawTextureRect(PapyrusGrain.Texture, rect, true, new Color(1, 1, 1, .52f));
        var roll = new Rect2(Size.X - 22, 0, 20, Size.Y);
        DrawRect(roll, paper.Darkened(.08f));
        DrawLine(roll.Position, new(roll.Position.X, roll.End.Y), PapyrusStyle.Bronze, 2, true);
        DrawLine(roll.Position + new Vector2(4, 0), new(roll.Position.X + 4, roll.End.Y), paper.Lightened(.14f), 3, true);
        DrawArc(new(Size.X - 12, 5), 9, Mathf.Pi, Mathf.Tau, 14, PapyrusStyle.Bronze, 1, true);
        DrawArc(new(Size.X - 12, Size.Y - 5), 9, 0, Mathf.Pi, 14, PapyrusStyle.Bronze, 1, true);
        if (_stamp >= 1 && _completion is null) return;
        float alpha = Math.Min(1, _stamp * 3) * .58f;
        var ink = new Color(new Color("783541"), alpha);
        var center = Size * .5f;
        DrawCircle(center + new Vector2(0, 5), 12, ink);
        for (int i = 0; i < 4; i++)
            DrawLine(center + new Vector2(-9 + i * 6, 1), center + new Vector2(-11 + i * 7, -15 - (i is 1 or 2 ? 4 : 0)), ink, 4.5f, true);
    }
    public override void _ExitTree() => _completion?.TrySetCanceled();
}

internal partial class ReadyActionJug : Control
{
    internal int Count { get; private set; }
    private Color _clay;
    internal void Update(int count, Color clay)
    {
        if (Count == count && _clay == clay) return;
        Count = count; _clay = clay; QueueRedraw();
    }
    public override void _Draw()
    {
        var center = Size * .5f + new Vector2(0, 3);
        var shape = new[] { new Vector2(-8, -28), new Vector2(8, -28), new Vector2(8, -17), new Vector2(19, -8),
            new Vector2(20, 8), new Vector2(13, 24), new Vector2(-13, 24), new Vector2(-20, 8), new Vector2(-19, -8), new Vector2(-8, -17) };
        DrawSetTransform(center);
        DrawColoredPolygon(shape, _clay.Darkened(.25f));
        DrawLine(new(-8, -28), new(8, -28), _clay.Lightened(.25f), 3, true);
        DrawArc(new(-17, -13), 9, 1.5f, 4.6f, 14, _clay.Darkened(.1f), 3, true);
        DrawArc(new(17, -13), 9, -1.5f, 1.6f, 14, _clay.Darkened(.1f), 3, true);
        DrawPolyline(new[] { new Vector2(-15, -5), new Vector2(-13, 11), new Vector2(-8, 20) }, _clay.Lightened(.08f), 2, true);
        string text = Count.ToString();
        float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 23).X;
        DrawString(ThemeDB.FallbackFont, new(-width / 2, 12), text, fontSize: 23, modulate: PapyrusStyle.Paper);
        DrawSetTransform(Vector2.Zero);
    }
}
