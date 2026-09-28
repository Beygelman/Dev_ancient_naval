using Godot;

namespace DevAncientNaval.Presentation.Camera;

public partial class MapCamera : Camera2D
{
    public const float MinZoom = 0.25f;
    public const float MaxZoom = 2.5f;
    public Rect2 MapBounds { get; set; }

    public void FitBoard()
    {
        var viewport = GetViewportRect().Size;
        float zoom = Mathf.Clamp(Mathf.Min((viewport.X - 72) / MapBounds.Size.X,
            (viewport.Y - 320) / MapBounds.Size.Y), MinZoom, MaxZoom);
        Zoom = Vector2.One * zoom;
        Position = MapBounds.GetCenter() + new Vector2(0, 36 / zoom);
        ForceUpdateScroll();
    }

    public Vector2 ScreenToWorld(Vector2 screen) => GetViewport().GetCanvasTransform().AffineInverse() * screen;

    public void Pan(Vector2 screenDelta)
    {
        Position -= screenDelta / Zoom;
        Constrain();
    }

    public void ZoomAt(Vector2 screenAnchor, float factor)
    {
        if (!float.IsFinite(factor) || factor <= 0) return;
        var before = ScreenToWorld(screenAnchor);
        Zoom = Vector2.One * Mathf.Clamp(Zoom.X * factor, MinZoom, MaxZoom);
        ForceUpdateScroll();
        Position += before - ScreenToWorld(screenAnchor);
        Constrain();
    }

    private void Constrain()
    {
        Position = new Vector2(Mathf.Clamp(Position.X, MapBounds.Position.X, MapBounds.End.X),
            Mathf.Clamp(Position.Y, MapBounds.Position.Y, MapBounds.End.Y));
        ForceUpdateScroll();
    }
}
