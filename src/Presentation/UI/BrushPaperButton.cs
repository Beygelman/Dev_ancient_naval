using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Accessible native button with dry-brush ink, never a rectangular plaque.</summary>
internal partial class BrushPaperButton : Button
{
    internal Color Ink { get; set; } = PaintedVoyageChoice.Burgundy;
    internal bool Underline { get; set; }
    internal bool Selected { get; set; }
    internal float StampProgress => _stamp;
    private float _outline = 1, _stamp = 1;
    private bool _stamping;
    private TaskCompletionSource? _completion;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.All;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty
            {
                ContentMarginLeft = 9,
                ContentMarginRight = 9,
                ContentMarginTop = 6,
                ContentMarginBottom = 6
            });
        AddThemeFontSizeOverride("font_size", 17);
        AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_hover_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_pressed_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_disabled_color", new Color(PapyrusStyle.Ink, .45f));
        MouseEntered += BeginOutline;
        MouseExited += QueueRedraw;
        Resized += QueueRedraw;
        SetProcess(false);
    }

    private void BeginOutline()
    {
        _outline = 0;
        SetProcess(true);
        QueueRedraw();
    }

    internal void ResetStamp()
    {
        var interrupted = _completion;
        _completion = null;
        _stamping = false;
        _stamp = 1;
        _outline = 1;
        SetProcess(false);
        interrupted?.TrySetResult();
        QueueRedraw();
    }

    internal Task StampAsync(bool instant = false)
    {
        if (_completion is not null) return _completion.Task;
        _stamping = true;
        _stamp = instant ? 1 : 0;
        QueueRedraw();
        if (instant) return Task.CompletedTask;
        _completion = new TaskCompletionSource();
        SetProcess(true);
        return _completion.Task;
    }

    public override void _Process(double delta)
    {
        _outline = Math.Min(1, _outline + (float)delta / .23f);
        if (_stamping) _stamp = Math.Min(1, _stamp + (float)delta / .28f);
        QueueRedraw();
        if (_stamping && _stamp >= 1)
        {
            var completion = _completion;
            _completion = null;
            completion?.TrySetResult();
        }
        if (_outline >= 1 && (!_stamping || _stamp >= 1)) SetProcess(false);
    }

    public override void _Draw()
    {
        float alpha = Disabled ? .25f : .9f;
        if (Underline)
        {
            float y = Size.Y - 5;
            for (int bristle = 0; bristle < 7; bristle++)
            {
                float left = 10 + bristle * 7 % 13;
                float right = Size.X - 11 - bristle * 9 % 16;
                DrawLine(new(left, y - bristle * .55f), new(right, y + MathF.Sin(bristle) * .9f),
                    new Color(Ink, alpha * (bristle % 3 == 0 ? .32f : .62f)), 1.8f, true);
            }
        }
        if (IsHovered() || Selected || HasFocus())
        {
            var center = Size * .5f;
            int count = Math.Max(2, (int)(60 * _outline));
            Span<Vector2> points = stackalloc Vector2[60];
            for (int stroke = 0; stroke < 3; stroke++)
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = i / 59f * Mathf.Tau;
                    float wobble = MathF.Sin(angle * 7 + stroke) * 1.3f;
                    points[i] = center + new Vector2(MathF.Cos(angle) * (Size.X * .47f + wobble),
                        MathF.Sin(angle) * (Size.Y * .42f + wobble));
                }
                DrawPolyline(points[..count], new Color(Ink, alpha * (.45f - stroke * .1f)), 1.5f, true);
            }
        }
        if (_stamping) HandStampArt.Draw(this, Size * .5f, _stamp, Ink);
    }

    public override void _ExitTree() => _completion?.TrySetCanceled();
}

internal static class HandStampArt
{
    internal static void Draw(CanvasItem canvas, Vector2 center, float progress, Color color)
    {
        float reveal = Math.Clamp(progress * 2.4f, 0, 1);
        var ink = new Color(color, reveal * .72f);
        canvas.DrawCircle(center + new Vector2(0, 5), 12, ink);
        for (int i = 0; i < 4; i++)
        {
            var from = center + new Vector2(-9 + i * 6, 1);
            var to = center + new Vector2(-11 + i * 7, -15 - (i is 1 or 2 ? 4 : 0));
            canvas.DrawLine(from, from.Lerp(to, reveal), ink, 4.5f, true);
            canvas.DrawLine(from + new Vector2(1, 0), from.Lerp(to, reveal) + new Vector2(.4f, 0),
                new Color(color.Lightened(.15f), reveal * .25f), .7f, true);
        }
    }
}
