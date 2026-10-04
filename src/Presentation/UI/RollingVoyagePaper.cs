using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Retained parchment texture and animated rolled lips over a native clipped viewport.</summary>
internal partial class RollingVoyagePaper : PanelContainer
{
    private int _lastScroll;
    private float _rollPhase, _remainingMotion;
    private static Texture2D? _agedPaper;
    internal ScrollContainer Scroll { get; private set; } = null!;
    internal void Bind(Control content)
    {
        Name = "VoyageSetupPaper";
        AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.99f));
        PapyrusGrain.Apply(this);
        _agedPaper ??= CreatePaper();
        Draw += () => DrawTextureRect(_agedPaper, new Rect2(Vector2.Zero, Size), false);
        Scroll = PapyrusModal.Wrap(this, content, "VoyageSetupScroll");
        Scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        Scroll.GetVScrollBar().ValueChanged += value =>
        {
            float direction = Math.Sign(value - _lastScroll);
            _lastScroll = (int)value;
            _rollPhase += direction * .27f;
            _remainingMotion = .3f;
            SetProcess(true);
            QueueRedraw();
        };
        var lips = new PaperLips { Paper = this };
        AddChild(lips);
        Resized += lips.QueueRedraw;
        Draw += lips.QueueRedraw;
        SetProcess(false);
    }
    private static Texture2D CreatePaper()
    {
        const int width = 192, height = 384;
        using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263 + 5903));
                hash = (hash ^ hash >> 13) * 1274126177;
                float edge = MathF.Pow(MathF.Abs(x / (width - 1f) * 2 - 1), 8);
                float fiber = .5f + .5f * MathF.Sin(y * .66f + MathF.Sin(x * .1f) * .7f);
                float spots = MathF.Max(0, MathF.Sin(x * .039f + y * .016f) * MathF.Sin(y * .031f - .8f));
                float alpha = .01f + edge * .13f + fiber * .025f + spots * .055f + (hash % 173 == 0 ? .15f : 0);
                image.SetPixel(x, y, new Color(.44f, .29f, .13f, alpha));
            }
        return ImageTexture.CreateFromImage(image);
    }
    public override void _Process(double delta)
    {
        _remainingMotion = Math.Max(0, _remainingMotion - (float)delta);
        _rollPhase += _remainingMotion * (float)delta * 4;
        QueueRedraw();
        if (_remainingMotion == 0) SetProcess(false);
    }
    private partial class PaperLips : Node2D
    {
        internal RollingVoyagePaper Paper { get; init; } = null!;
        public override void _Draw()
        {
            float w = Paper.Size.X;
            foreach (float y in new[] { 0f, Paper.Size.Y - 7 })
            {
                DrawRect(new Rect2(1, y, w - 2, 8), new Color("d3ba84"));
                DrawLine(new(2, y + 1), new(w - 2, y + 1), new Color("f1e3bd"), 2);
                DrawLine(new(2, y + 7), new(w - 2, y + 7), new Color("9b7849"), 1.2f);
                for (int i = 0; i < 5; i++)
                {
                    float x = 17 + i * (w - 34) / 4;
                    DrawLine(new(x, y + 2), new(x + MathF.Sin(Paper._rollPhase + i) * 8, y + 5), new Color(.43f, .29f, .14f, .18f));
                }
            }
        }
    }
}
