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
        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263 + 9317));
                hash = (hash ^ (hash >> 13)) * 1274126177;
                float alpha = ((hash >> 24) / 255f) * .042f;
                if (y % 11 == 3 && (x + y * 7) % 19 < 13) alpha += .05f;
                if (hash % 131 == 0) alpha += .105f;
                image.SetPixel(x, y, new Color(.42f, .28f, .12f, alpha));
            }
        var texture = ImageTexture.CreateFromImage(image);
        image.Dispose();
        return texture;
    }

    internal static void Apply(PanelContainer paper)
    {
        paper.Draw += () => paper.DrawTextureRect(Texture, new Rect2(Vector2.Zero, paper.Size), true);
    }
}
