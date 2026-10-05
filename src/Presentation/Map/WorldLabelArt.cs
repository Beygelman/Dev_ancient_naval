using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>World labels retain a higher-resolution font sample rather than
/// enlarging a 13-pixel glyph with the map. Their logical size and world anchor
/// remain fixed; camera movement never redraws an observed/fogged interface.</summary>
internal static class WorldLabelArt
{
    internal const int FontSize = 13, SampleScale = 3;
    internal const int SampleFontSize = FontSize * SampleScale;

    internal static Vector2 LogicalSize(string text) =>
        ThemeDB.FallbackFont.GetStringSize(text, fontSize: SampleFontSize) / SampleScale;

    internal static void DrawCentered(Node2D canvas, Vector2 baseline, string text)
    {
        var sampleWidth = ThemeDB.FallbackFont.GetStringSize(text, fontSize: SampleFontSize).X;
        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One / SampleScale);
        canvas.DrawString(ThemeDB.FallbackFont,
            baseline * SampleScale - new Vector2(sampleWidth * .5f, 0), text,
            fontSize: SampleFontSize, modulate: Colors.Black);
        canvas.DrawSetTransform(Vector2.Zero);
    }
}
