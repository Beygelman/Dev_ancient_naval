using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.UI;
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
    private readonly HashSet<Key> _panKeys = new();
    public const double SalvoHoldSeconds = .4;
    public MapCamera Camera { get; set; } = null!;
    public event Action<Vector2>? Tapped;
    public event Action<Vector2>? Held;
    public event Action<Vector2>? Hovered;
    public event Action? Canceled;
    public Func<bool>? KeyboardEnabled { get; set; }
    public Func<bool>? PointerEnabled { get; set; }
    public Func<bool>? ZoomEnabled { get; set; }
    internal bool PointerOverInterface()
    {
        // Native scroll events can bubble at endpoints. The hovered control is
        // still authoritative even if that event reaches _UnhandledInput.
        for (Node? node = GetViewport().GuiGetHoveredControl(); node is not null; node = node.GetParent())
            if (node is Control control && control.IsVisibleInTree()
                && (control is ScrollContainer or PanelContainer || control.MouseFilter == Control.MouseFilterEnum.Stop))
                return true;
        return false;
    }
    internal bool TouchOverInterface(Vector2 point) => TouchOverInterface(GetTree().Root, point);
    private static bool TouchOverInterface(Node node, Vector2 point)
    {
        if (node is Control control)
        {
            if (!control.IsVisibleInTree()) return false;
            var local = control.GetGlobalTransformWithCanvas().AffineInverse() * point;
            // Curved native hit regions leave exposed sea inside their rectangle,
            // especially on a narrow phone. Keep mouse and finger ownership equal.
            // Direct typed calls also work in the reflection-free NativeAOT player.
            bool contains = control switch
            {
                SectorButton sector => sector._HasPoint(local),
                ActionPapyrus paper => paper._HasPoint(local),
                ReadyActionJug relic => relic._HasPoint(local),
                OffscreenNavigationArrow arrow => arrow._HasPoint(local),
                MothershipCompassButton compass => compass._HasPoint(local),
                _ => new Rect2(Vector2.Zero, control.Size).HasPoint(local)
            };
            // Touch has no hovered Control; hit-test its actual position. Respect
            // clipping so offscreen scroll children do not claim the sea behind it.
            if (!contains && control.ClipContents) return false;
            if (contains && (control is ScrollContainer or PanelContainer
                || control.MouseFilter == Control.MouseFilterEnum.Stop)) return true;
        }
        foreach (Node child in node.GetChildren())
            if (TouchOverInterface(child, point)) return true;
        return false;
    }
    public Func<bool>? GameplayShortcutsEnabled { get; set; }
    public Func<InputEventKey, bool>? CommandShortcut { get; set; }
    public event Action? EndTurnRequested;
    public event Action? RepairRequested;

    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey key && HandleKeyboard(key))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            CancelGesture();
            Canceled?.Invoke();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is InputEventScreenTouch { Pressed: true } newTouch
            && !_touches.ContainsKey(newTouch.Index) && TouchOverInterface(newTouch.Position))
        {
            // Never steal a new panel finger in _Input, before GUI dispatch.
            // Cancel the sea gesture without consuming the panel's own event.
            CancelGesture();
            return;
        }
        if ((_mouseDown && input is InputEventMouse) ||
            (input is InputEventScreenTouch touch && _touches.ContainsKey(touch.Index)) ||
            (input is InputEventScreenDrag drag && _touches.ContainsKey(drag.Index)))
            Handle(input);
    }

    public override void _UnhandledInput(InputEvent input) => Handle(input);
    public override void _Process(double delta)
    {
        if (KeyboardEnabled?.Invoke() != true)
            _panKeys.Clear();
        else if (_panKeys.Count > 0)
        {
            var direction = Vector2.Zero;
            if (_panKeys.Contains(Key.Left) || _panKeys.Contains(Key.A)) direction.X--;
            if (_panKeys.Contains(Key.Right) || _panKeys.Contains(Key.D)) direction.X++;
            if (_panKeys.Contains(Key.Up) || _panKeys.Contains(Key.W)) direction.Y--;
            if (_panKeys.Contains(Key.Down) || _panKeys.Contains(Key.S)) direction.Y++;
            Camera.PanByKeys(direction, delta);
        }
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
        if (what == NotificationApplicationFocusOut || what == NotificationApplicationPaused) CancelGesture();
    }

    public void CancelGesture()
    {
        _touches.Clear();
        _mouseDown = false;
        _dragged = false;
        _pendingHover = null;
        _panKeys.Clear();
    }

    private bool HandleKeyboard(InputEventKey input)
    {
        Key key = input.PhysicalKeycode != Key.None ? input.PhysicalKeycode : input.Keycode;
        bool pan = key is Key.Left or Key.Right or Key.Up or Key.Down or Key.W or Key.A or Key.S or Key.D;
        if (!input.Pressed)
            return pan && _panKeys.Remove(key);
        if (input.Echo || input.AltPressed || input.CtrlPressed || input.MetaPressed
            || KeyboardEnabled?.Invoke() != true)
            return false;
        if (pan)
        {
            _panKeys.Add(key);
            return true;
        }
        if ((GameplayShortcutsEnabled ?? KeyboardEnabled)?.Invoke() != true) return false;
        if (CommandShortcut?.Invoke(input) == true) return true;
        if (key == Key.Space)
        {
            EndTurnRequested?.Invoke();
            return true;
        }
        if (key == Key.R)
        {
            RepairRequested?.Invoke();
            return true;
        }
        return false;
    }

    private void Handle(InputEvent input)
    {
        if (PointerEnabled?.Invoke() == false)
        {
            CancelGesture();
            return;
        }
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
                {
                    if (ZoomEnabled?.Invoke() == false || PointerOverInterface())
                    { GetViewport().SetInputAsHandled(); return; }
                    Camera.ZoomAt(mouse.Position, mouse.ButtonIndex == MouseButton.WheelUp ? 1.15f : 1 / 1.15f);
                }
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
            if (TouchOverInterface(touch.Position)) return;
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
            if (oldDistance > 1 && ZoomEnabled?.Invoke() != false)
                Camera.ZoomAt(newCenter, a.DistanceTo(b) / oldDistance);
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
