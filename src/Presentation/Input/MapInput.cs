using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Presentation.Camera;
using Godot;

namespace DevAncientNaval.Presentation.Input;

/// <summary>Start gestures only on unhandled map input. Once started, finish them
/// even over UI, so pointer releases cannot leave a stuck camera gesture.</summary>
public partial class MapInput : Node
{
    private const float DragThreshold = 12;
    private readonly Dictionary<int, Vector2> _touches = new();
    private Vector2 _start;
    private Vector2 _lastMouse;
    private bool _mouseDown;
    private bool _dragged;
    private double _pressedAt;
    private Vector2? _pendingHover;
    public const double SalvoHoldSeconds = .4;
    public MapCamera Camera { get; set; } = null!;
    public event Action<Vector2>? Tapped;
    public event Action<Vector2>? Held;
    public event Action<Vector2>? Hovered;
    public event Action? Canceled;

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            CancelGesture();
            Canceled?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }
        if ((_mouseDown && input is InputEventMouse) ||
            (_touches.Count > 0 && input is InputEventScreenTouch or InputEventScreenDrag))
            Handle(input);
    }

    public override void _UnhandledInput(InputEvent input) => Handle(input);
    public override void _Process(double delta)
    {
        if (_pendingHover is not { } point) return;
        _pendingHover = null;
        Hovered?.Invoke(point);
    }
    private static double Now => Time.GetTicksUsec() / 1_000_000.0;
    private void ReleaseAt(Vector2 point)
    {
        if (Now - _pressedAt >= SalvoHoldSeconds) Held?.Invoke(point);
        else Tapped?.Invoke(point);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut) CancelGesture();
    }

    public void CancelGesture()
    {
        _touches.Clear();
        _mouseDown = false;
        _dragged = false;
        _pendingHover = null;
    }

    private void Handle(InputEvent input)
    {
        switch (input)
        {
            case InputEventScreenTouch touch:
                HandleTouch(touch);
                break;
            case InputEventScreenDrag drag when _touches.ContainsKey(drag.Index):
                HandleDrag(drag);
                break;
            case InputEventMouseButton mouse when _touches.Count == 0 && mouse.Device != -1:
                if (mouse.ButtonIndex == MouseButton.Left)
                {
                    if (mouse.Pressed)
                    {
                        _mouseDown = true;
                        _dragged = false;
                        _start = _lastMouse = mouse.Position;
                        _pressedAt = Now;
                    }
                    else if (_mouseDown)
                    {
                        _mouseDown = false;
                        if (!_dragged && _start.DistanceTo(mouse.Position) < DragThreshold)
                            ReleaseAt(mouse.Position);
                    }
                }
                else if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
                    Camera.ZoomAt(mouse.Position, mouse.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1 / 1.15f);
                else return;
                break;
            case InputEventMouseMotion motion when _mouseDown && motion.Device != -1:
                if (!_dragged && _start.DistanceTo(motion.Position) >= DragThreshold)
                {
                    _dragged = true;
                    Camera.Pan(motion.Position - _lastMouse);
                }
                else if (_dragged) Camera.Pan(motion.Position - _lastMouse);
                _lastMouse = motion.Position;
                break;
            case InputEventMouseMotion motion when !_mouseDown && _touches.Count == 0:
                _pendingHover = motion.Position;
                return;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    private void HandleTouch(InputEventScreenTouch touch)
    {
        if (touch.Canceled)
        {
            CancelGesture();
            return;
        }
        if (touch.Pressed)
        {
            _mouseDown = false;
            if (_touches.Count == 0)
            {
                _start = touch.Position;
                _pressedAt = Now;
                _dragged = false;
            }
            else _dragged = true; // Pinching never becomes a tap when fingers lift.
            _touches[touch.Index] = touch.Position;
        }
        else if (_touches.Remove(touch.Index))
        {
            if (_touches.Count == 0 && !_dragged && _start.DistanceTo(touch.Position) < DragThreshold)
                ReleaseAt(touch.Position);
        }
    }

    private void HandleDrag(InputEventScreenDrag drag)
    {
        var old = _touches[drag.Index];
        if (_touches.Count >= 2)
        {
            var pair = _touches.Keys.OrderBy(index => index).Take(2).ToArray();
            var a = _touches[pair[0]];
            var b = _touches[pair[1]];
            var oldCenter = (a + b) / 2;
            float oldDistance = a.DistanceTo(b);
            _touches[drag.Index] = drag.Position;
            a = _touches[pair[0]];
            b = _touches[pair[1]];
            var newCenter = (a + b) / 2;
            Camera.Pan(newCenter - oldCenter);
            if (oldDistance > 1) Camera.ZoomAt(newCenter, a.DistanceTo(b) / oldDistance);
            _dragged = true;
        }
        else
        {
            if (!_dragged && _start.DistanceTo(drag.Position) >= DragThreshold)
            {
                _dragged = true;
                Camera.Pan(drag.Position - old);
            }
            else if (_dragged) Camera.Pan(drag.Position - old);
            _touches[drag.Index] = drag.Position;
        }
    }
}
