using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>World-anchored scroll opening down from its top center. Core commits after burning.</summary>
public partial class ActionPapyrus : Control
{
    private Texture2D _art = null!;
    private float _age, _consumeAge, _floatTime;
    private bool _hovered;
    private TaskCompletionSource? _completion;
    private bool _ready, _onScreen = true;
    public string ArtworkPath { get; set; } = "";
    internal bool HasArtwork => _art is not null;
    public bool IsConsuming => _completion is not null;
    public float ConsumptionProgress { get; private set; }
    public event Action? Pressed;
    private Vector2 Hinge => new(Size.X / 2, 4 + Mathf.Sin(_floatTime * 1.8f) * 3);
    private float Reveal => 1 - Mathf.Pow(1 - _age, 3);
    private float Roll => IsConsuming ? 1 - Mathf.SmoothStep(0, .65f, ConsumptionProgress) : 1;
    public override void _Ready()
    {
        Size = new(300, 162);
        MouseFilter = MouseFilterEnum.Stop;
        _art = GD.Load<Texture2D>(ArtworkPath);
        MouseEntered += () => { _hovered = true; QueueRedraw(); };
        MouseExited += () => { _hovered = false; QueueRedraw(); };
        Hide();
        SetProcess(false);
    }
    public void SetReady(bool ready)
    {
        if (IsConsuming) return;
        _ready = ready;
        if (!ready) { Hide(); SetProcess(false); return; }
        if (Visible) return;
        _age = 0;
        ConsumptionProgress = 0;
        Visible = _onScreen;
        SetProcess(_onScreen);
    }
    public void SetOnScreen(bool onScreen)
    {
        _onScreen = onScreen;
        if (IsConsuming) return;
        Visible = _ready && onScreen;
        SetProcess(Visible);
    }
    public Task ConsumeAsync()
    {
        if (_completion is not null) return _completion.Task;
        _completion = new TaskCompletionSource();
        _age = 1;
        _consumeAge = 0;
        ConsumptionProgress = 0;
        Show();
        SetProcess(true);
        return _completion.Task;
    }
    public override bool _HasPoint(Vector2 point)
    {
        var offset = point - Hinge;
        return !IsConsuming && Reveal >= .95f && offset.Y >= 0 && offset.Length() is >= 74 and <= 152;
    }
    public override void _GuiInput(InputEvent input)
    {
        if (!IsConsuming && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } or InputEventScreenTouch { Pressed: true })
        {
            AcceptEvent();
            Pressed?.Invoke();
        }
    }
    public override void _Process(double delta)
    {
        _floatTime += (float)delta;
        if (IsConsuming)
        {
            _consumeAge += (float)delta;
            ConsumptionProgress = Math.Clamp(_consumeAge / .95f, 0, 1);
        }
        else _age = Math.Min(1, _age + (float)delta / .38f);
        QueueRedraw();
        if (IsConsuming && ConsumptionProgress >= 1)
        {
            var completed = _completion;
            _completion = null;
            Hide();
            SetProcess(false);
            completed!.TrySetResult();
        }
    }
    public override void _Draw()
    {
        const int steps = 64;
        float spread = Reveal * Roll;
        float span = Mathf.Pi * Math.Max(.01f, spread), start = Mathf.Pi / 2 - span / 2;
        float inner = 112 - 38 * spread, outer = 112 + 38 * spread;
        float burn = IsConsuming ? Mathf.SmoothStep(.62f, .94f, ConsumptionProgress) : 0;
        var paper = PapyrusStyle.Paper.Lightened(_hovered ? .05f : 0).Darkened(burn * .95f);
        if (burn < .99f)
        {
            DrawArc(Hinge, outer, start, start + span, 65, new Color(PapyrusStyle.Bronze, 1 - burn), 1, true);
            DrawArc(Hinge, inner, start, start + span, 65, new Color(PapyrusStyle.Bronze, 1 - burn), 1, true);
            for (int i = 0; i < steps; i++)
            {
                float u0 = i / (float)steps, u1 = (i + 1) / (float)steps;
                var a = Vector2.FromAngle(start + span * i / steps);
                var b = Vector2.FromAngle(start + span * (i + 1) / steps);
                var points = new[] { Hinge + a * inner, Hinge + b * inner, Hinge + b * outer, Hinge + a * outer };
                var uv = new[] { new Vector2(1-u0, 0), new Vector2(1-u1, 0), new Vector2(1-u1, 1), new Vector2(1-u0, 1) };
                if (spread > .002f)
                {
                    DrawPrimitive(points, new[] { new Color(paper, (1-burn)*.97f) }, Array.Empty<Vector2>());
                    DrawPrimitive(points, new[] { new Color(1, 1, 1, spread * (1 - burn)) }, uv, _art);
                }
            }
            for (int i = 1; i < 5; i++)
                DrawArc(Hinge, Mathf.Lerp(inner, outer, i / 5f), start, start + span, 48, new Color(PapyrusStyle.FaintInk, .035f * (1-burn)), .7f, true);
            foreach (float angle in new[] { start, start + span })
            {
                var ray = Vector2.FromAngle(angle);
                DrawLine(Hinge + ray * inner, Hinge + ray * outer, new Color(PapyrusStyle.Bronze, 1-burn), 4, true);
                DrawLine(Hinge + ray * inner, Hinge + ray * outer, new Color(PapyrusStyle.Paper.Lightened(.1f), 1-burn), 1.5f, true);
            }
        }
        if (burn > 0)
            for (int i = 0; i < 18; i++)
            {
                var at = Hinge + new Vector2(Mathf.Sin(i * 2.4f) * burn * 25, 112 - burn * (25 + i % 5 * 12));
                DrawCircle(at, 1 + i % 3 * .4f, new Color(1, .5f + i % 3 * .1f, .12f, (1 - ConsumptionProgress) * 2));
            }
    }
    public override void _ExitTree() => _completion?.TrySetCanceled();
}
