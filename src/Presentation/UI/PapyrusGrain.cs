using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>A small, shared cosmetic texture; its deterministic grain never consumes battle RNG.</summary>
internal static class PapyrusGrain
{
    private static Texture2D? _texture;
    internal static Texture2D Texture => _texture ??= Create();
    internal const float FamilyWidth = SectorButton.Outer * 2;
    internal const float UprightWidth = FamilyWidth;

    private static Texture2D Create()
    {
        const int size = 192;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263 + 9317));
                hash = (hash ^ (hash >> 13)) * 1274126177;
                // Cross-laid plant fibres, irregular stains and pin-sized abrasions.
                // Three incommensurate waves avoid obvious checkerboard/repeating stripes.
                float horizontal = .5f + .5f * Mathf.Sin(y * .72f + Mathf.Sin(x * .071f) * .9f);
                float vertical = .5f + .5f * Mathf.Sin(x * 1.13f + Mathf.Sin(y * .097f) * 1.5f);
                float mottling = Mathf.Max(0, Mathf.Sin(x * .041f + y * .017f)
                    * Mathf.Sin(y * .033f - x * .014f));
                float alpha = ((hash >> 24) / 255f) * .028f
                    + horizontal * .034f + vertical * .021f + mottling * .073f;
                if (hash % 173 == 0) alpha += .15f;
                if (hash % 311 == 0)
                    image.SetPixel(x, y, new Color(1, .97f, .82f, .19f));
                else image.SetPixel(x, y, new Color(.44f, .29f, .12f, alpha));
            }
        var texture = ImageTexture.CreateFromImage(image);
        image.Dispose();
        return texture;
    }

    internal static void Apply(PanelContainer paper)
    {
        paper.Draw += () => paper.DrawTextureRect(Texture, new Rect2(Vector2.Zero, paper.Size), true);
    }

    internal static void Draw(CanvasItem canvas, Rect2 rect)
    {
        if (rect.Size.X <= 0 || rect.Size.Y <= 0) return;
        canvas.DrawRect(rect, new Color(PapyrusStyle.Paper, .99f));
        canvas.DrawTextureRect(Texture, rect, true);
        // Worn edges are relative to each actual sheet, rather than baked into a tiled texture.
        for (int strip = 0; strip < 6; strip++)
        {
            float inset = strip * 1.4f;
            var rim = new Rect2(rect.Position + Vector2.One * inset, rect.Size - Vector2.One * inset * 2);
            if (rim.Size.X <= 0 || rim.Size.Y <= 0) break;
            canvas.DrawRect(rim, new Color(PapyrusStyle.Bronze, .08f - strip * .01f), false, 1.5f);
        }
        canvas.DrawRect(rect, new Color(PapyrusStyle.Bronze, .55f), false, 1);
    }
}
