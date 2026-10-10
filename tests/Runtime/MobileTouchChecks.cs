using System;
using System.Threading.Tasks;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Native synthetic gesture ownership; this is not an iOS device test.</summary>
public partial class MobileTouchChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool valid, string message)
    { if (!valid) throw new InvalidOperationException("MobileTouch: " + message); _checks++; }
    private async Task Frames(int count = 3)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Touch(int finger, Vector2 point, bool pressed, bool canceled = false) =>
        GetViewport().PushInput(new InputEventScreenTouch { Index = finger, Position = point,
            Pressed = pressed, Canceled = canceled }, true);
    private void Drag(int finger, Vector2 point) =>
        GetViewport().PushInput(new InputEventScreenDrag { Index = finger, Position = point }, true);

    public override async void _Ready()
    {
        try
        {
            await Frames();
            Game.MapInput.CancelGesture(); Game.MapInput.SetProcessInput(false);
            Game.MapInput.SetProcessUnhandledInput(false); Game.MapInput.SetProcess(false);
            foreach (Node child in Game.GetChildren()) if (child is CanvasLayer layer) layer.Hide();
            var layerFixture = new CanvasLayer { Layer = 100 }; AddChild(layerFixture);
            var panel = new PanelContainer { Position = new(30, 30), Size = new(180, 220),
                MouseFilter = Control.MouseFilterEnum.Stop };
            layerFixture.AddChild(panel);
            var input = new MapInput { Camera = Game.MapCamera, PointerEnabled = () => true,
                ZoomEnabled = () => true, KeyboardEnabled = () => false };
            AddChild(input); int taps = 0, holds = 0;
            input.Tapped += _ => taps++; input.Held += _ => holds++;
            await Frames();
            Vector2 sea = new(600, 380), other = new(800, 380), paper = new(90, 90);
            Check(input.TouchOverInterface(paper) && !input.TouchOverInterface(sea),
                "touch hit testing uses the finger position, not mouse hover");
            Touch(0, sea, true); Touch(0, sea, false);
            Check(taps == 1 && holds == 0, "a sea tap selects once");
            Touch(0, paper, true); Touch(0, paper, false);
            Check(taps == 1, "touches starting on paper do not select the sea behind it");
            Touch(0, sea, true);
            ulong heldAt = Time.GetTicksUsec();
            // MapInput deliberately measures a hold with the monotonic clock.
            // A SceneTreeTimer can inherit the large startup frame's delta and
            // expire sooner in real time when created from ProcessFrame.
            while ((Time.GetTicksUsec() - heldAt) / 1_000_000.0 < MapInput.SalvoHoldSeconds + .08)
                await Frames(1);
            Check((Time.GetTicksUsec() - heldAt) / 1_000_000.0 >= MapInput.SalvoHoldSeconds,
                "the held touch remains pressed for the real monotonic hold threshold");
            Touch(0, sea, false);
            Check(taps == 1 && holds == 1, $"a held sea press requests the existing salvo choice once (taps={taps}, holds={holds}, elapsed={(Time.GetTicksUsec()-heldAt)/1_000_000.0:F3}s, UI={input.TouchOverInterface(sea)})");
            Vector2 beforePan = Game.MapCamera.Position;
            Touch(0, sea, true); Drag(0, sea + new Vector2(50, 0)); Touch(0, sea + new Vector2(50, 0), false);
            Check(Game.MapCamera.Position != beforePan && taps == 1, "one-finger pan never becomes a tap");
            float beforeZoom = Game.MapCamera.Zoom.X;
            Touch(0, sea, true); Touch(1, other, true); Drag(1, other + new Vector2(60, 0));
            Touch(0, sea, false); Touch(1, other + new Vector2(60, 0), false);
            Check(Game.MapCamera.Zoom.X > beforeZoom && taps == 1, "two-finger pinch zooms without a release tap");
            input.ZoomEnabled = () => false; beforeZoom = Game.MapCamera.Zoom.X;
            Touch(0, sea, true); Touch(1, other, true); Drag(1, other + new Vector2(60, 0));
            Touch(0, sea, false); Touch(1, other + new Vector2(60, 0), false);
            Check(Game.MapCamera.Zoom.X == beforeZoom, "pinch obeys the same zoom permission as the wheel");
            input.ZoomEnabled = () => true;
            Touch(0, sea, true); Touch(1, paper, true);
            Vector2 stopped = Game.MapCamera.Position; beforeZoom = Game.MapCamera.Zoom.X;
            Drag(1, paper + new Vector2(50, 0)); Drag(0, sea + new Vector2(50, 0));
            Touch(1, paper, false); Touch(0, sea, false);
            Check(Game.MapCamera.Position == stopped && Game.MapCamera.Zoom.X == beforeZoom && taps == 1,
                "a new panel finger cancels the sea gesture rather than stealing the panel as a pinch");
            Touch(0, sea, true); Touch(0, sea, false, canceled: true);
            Drag(0, sea + new Vector2(70, 0)); Touch(0, sea, false);
            Check(taps == 1 && holds == 1, "canceled touches and orphan releases cannot activate anything");
            Touch(0, sea, true); input.Notification((int)Node.NotificationApplicationPaused);
            Touch(0, sea, false);
            Check(taps == 1 && holds == 1, "background suspension clears captured fingers before resuming");
            input.PointerEnabled = () => false;
            stopped = Game.MapCamera.Position;
            Touch(0, sea, true); Drag(0, sea + new Vector2(70, 0)); Touch(0, sea, false);
            Check(Game.MapCamera.Position == stopped && taps == 1, "modal pointer lock blocks touch commands and pan");
            var viewport = new Rect2(0, 0, 1420, 655);
            var stretch = new Transform2D(0, new Vector2(1.8f, 1.8f), 0, Vector2.Zero);
            var safe = UiScale.ConvertSafeArea(new Rect2(90, 0, 2376, 1116), stretch, viewport);
            Check(safe.Position.IsEqualApprox(new Vector2(50, 0))
                && safe.Size.IsEqualApprox(new Vector2(1320, 620)), "landscape safe pixels convert through the viewport stretch");
            Check(UiScale.ConvertSafeArea(new Rect2(-100, -100, 4000, 4000), stretch, viewport) == viewport,
                "safe rectangles are clipped to the actual viewport");
            Check(UiScale.ConvertSafeArea(new Rect2(4000, 4000, 10, 10), stretch, viewport) == viewport,
                "invalid native safe rectangles preserve a usable viewport");
            GD.Print($"PASS: {_checks} native synthetic mobile gesture and safe-area checks (not device/iOS-tested).");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
