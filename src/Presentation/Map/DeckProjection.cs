using Godot;

namespace DevAncientNaval.Presentation.Map;
/// <summary>Rotate the horizontal deck first; height stays vertical on screen.</summary>
internal static class DeckProjection
{
    internal const float Aspect = .62f;
    internal static Vector2 Point(float x, float y, float z, float yaw, float size = 1, float width = 1)
    {
        var floor = new Vector2(x, y * width).Rotated(yaw) * size;
        return new(floor.X, floor.Y * Aspect - z * size);
    }

    internal static float Heading(Vector2 screenDirection) => new Vector2(screenDirection.X, screenDirection.Y / Aspect).Angle();
}
