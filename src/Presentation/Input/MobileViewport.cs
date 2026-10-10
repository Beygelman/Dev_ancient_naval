using Godot;

namespace DevAncientNaval.Presentation.Input;

/// <summary>Readable handheld canvas sizes; desktop window scaling stays unchanged.</summary>
internal static class MobileViewport
{
    private static Vector2I? _landscapeBaseline;

    internal static Vector2I ChooseScaleSize(Vector2I windowSize, Vector2I landscapeBaseline) =>
        windowSize.X > 0 && windowSize.Y > windowSize.X
            ? new Vector2I(540, 960) : landscapeBaseline;

    internal static void Configure(Window window)
    {
        if (OS.GetName() is not ("iOS" or "Android")) return;
        _landscapeBaseline ??= window.ContentScaleSize;
        var requested = ChooseScaleSize(window.Size, _landscapeBaseline.Value);
        // SizeChanged can fire again after changing the canvas; a no-op makes
        // the update finite and retains each original landscape configuration.
        if (window.ContentScaleSize != requested) window.ContentScaleSize = requested;
    }
}
