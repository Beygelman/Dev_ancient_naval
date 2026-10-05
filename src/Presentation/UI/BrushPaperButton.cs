using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Accessible native button with dry-brush ink, never a rectangular plaque.</summary>
internal partial class BrushPaperButton : Button
{
    private Color _ink = PaintedVoyageChoice.Burgundy;
    private bool _underline;
    private BrushInscriptionBackdrop? _inscription;
    internal Color Ink
    {
        get => _ink;
        set { _ink = value; _inscription?.QueueRedraw(); QueueRedraw(); }
    }
    // Kept as the existing public presentation contract; the stroke now passes
    // behind the inscription rather than beneath the bottom of its button.
    internal bool Underline
    {
        get => _underline;
        set { _underline = value; _inscription?.QueueRedraw(); }
    }
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
        AddThemeColorOverride("font_focus_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_hover_pressed_color", PapyrusStyle.Ink);
        AddThemeColorOverride("font_disabled_color", new Color(PapyrusStyle.Ink, .45f));
        _inscription = new BrushInscriptionBackdrop { Name = "InscriptionBrush", OwnerButton = this,
            MouseFilter = MouseFilterEnum.Ignore, ShowBehindParent = true };
        AddChild(_inscription);
        _inscription.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseEntered += BeginOutline;
        MouseExited += QueueRedraw;
        Resized += () => { QueueRedraw(); _inscription.QueueRedraw(); };
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

/// <summary>The native button text stays in front of the painted bristles.</summary>
internal partial class BrushInscriptionBackdrop : Control
{
    internal BrushPaperButton OwnerButton { get; init; } = null!;
    internal float StrokeCenterY => Size.Y * .5f;
    public override void _Draw()
    {
        if (!OwnerButton.Underline) return;
        float opacity = OwnerButton.Disabled ? .11f : .32f;
        float halfHeight = Math.Clamp(Size.Y * .19f, 6, 11);
        for (int bristle = 0; bristle < 17; bristle++)
        {
            float y = StrokeCenterY - halfHeight + bristle * halfHeight / 8;
            float left = 11 + bristle * 7 % 15;
            float right = Size.X - 12 - bristle * 11 % 19;
            if (right <= left) continue;
            DrawLine(new(left, y), new(right, y + MathF.Sin(bristle * 1.9f) * 1.4f),
                new Color(OwnerButton.Ink, opacity * (bristle % 4 == 0 ? .48f : 1)), 1.75f, true);
        }
    }
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
