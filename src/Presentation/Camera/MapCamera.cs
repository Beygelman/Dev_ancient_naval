using Godot;
using System;
using System.Threading.Tasks;

namespace DevAncientNaval.Presentation.Camera;

public partial class MapCamera : Camera2D
{
    public const float MinZoom = 0.16f;
    public const float MaxZoom = 2.5f;
    public const float KeyboardPanPixelsPerSecond = 500;
    public Rect2 MapBounds { get; set; }
    private TaskCompletionSource? _flightCompletion;
    private Vector2 _flightFrom, _flightTo;
    private ulong _flightStarted;
    private double _flightSeconds;
    public event Action? ViewChanged;
    public const double EncounterSeconds = .5, ActionSeconds = .25;
    internal static float FlightEase(float t) => t * t * t * (t * (t * 6 - 15) + 10);

    public Task FocusAsync(Vector2 target, double seconds)
    {
        CancelFlight();
        target = new(Mathf.Clamp(target.X, MapBounds.Position.X, MapBounds.End.X), Mathf.Clamp(target.Y, MapBounds.Position.Y, MapBounds.End.Y));
        _flightFrom = Position;
        _flightTo = target;
        _flightStarted = Time.GetTicksUsec();
        _flightSeconds = seconds;
        _flightCompletion = new TaskCompletionSource();
        SetProcess(true);
        return _flightCompletion.Task;
    }
    public override void _Ready() => SetProcess(false);
    public override void _Process(double delta)
    {
        if (_flightCompletion is null) return;
        float t = (float)Math.Clamp((Time.GetTicksUsec()-_flightStarted)/1_000_000.0/_flightSeconds,0,1);
        Position = _flightFrom.Lerp(_flightTo,FlightEase(t));
        ForceUpdateScroll();
        ViewChanged?.Invoke();
        if (t < 1) return;
        var completion = _flightCompletion;
        _flightCompletion = null;
        SetProcess(false);
        completion.TrySetResult();
    }
    public void CancelFlight()
    {
        var completion = _flightCompletion;
        _flightCompletion = null;
        SetProcess(false);
        completion?.TrySetResult();
    }

    public void FitBoard()
    {
        CancelFlight();
        var viewport = GetViewportRect().Size;
        float zoom = Mathf.Clamp(Mathf.Min((viewport.X - 72) / MapBounds.Size.X,
            (viewport.Y - 200) / MapBounds.Size.Y), MinZoom, MaxZoom);
        Zoom = Vector2.One * zoom;
        Position = MapBounds.GetCenter() + new Vector2(0, -10 / zoom);
        ForceUpdateScroll();
        ViewChanged?.Invoke();
    }

    public Vector2 ScreenToWorld(Vector2 screen) => GetViewport().GetCanvasTransform().AffineInverse() * screen;

    public void Pan(Vector2 screenDelta)
    {
        CancelFlight();
        Position -= screenDelta / Zoom;
        Constrain();
    }

    public void PanByKeys(Vector2 direction, double delta)
    {
        if (direction == Vector2.Zero || !double.IsFinite(delta) || delta <= 0) return;
        Pan(-direction.Normalized() * KeyboardPanPixelsPerSecond * (float)Math.Min(delta, .05));
    }

    public void ZoomAt(Vector2 screenAnchor, float factor)
    {
        if (!float.IsFinite(factor) || factor <= 0) return;
        CancelFlight();
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
        ViewChanged?.Invoke();
    }
    public override void _ExitTree() => CancelFlight();
}
