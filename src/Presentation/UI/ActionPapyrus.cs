using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>One horizontal world banner: unfold once, wait, then burn before Core commits.</summary>
public partial class ActionPapyrus : Control
{
    private Texture2D _art = null!;
    private float _age, _consumeAge, _floatTime, _drawTime;
    private bool _hovered, _ready, _onScreen = true;
    private TaskCompletionSource? _completion;
    public string ArtworkPath { get; set; } = "";
    internal bool HasArtwork => _art is not null;
    internal float Reveal => 1 - Mathf.Pow(1 - _age, 3);
    internal bool ActivationEnabled { get; set; } = true;
    public bool IsConsuming => _completion is not null;
    public float ConsumptionProgress { get; private set; }
    public event Action? Pressed;
    private Vector2 FloatOffset => new(0, Mathf.Sin(_floatTime * 1.8f) * 2);
    public override void _Ready()
    {
        Size = SectorButton.PaperFootprint;
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
        bool fresh = ready && !_ready;
        _ready = ready;
        if (fresh) { _age = 0; ConsumptionProgress = 0; }
        Visible = ready && _onScreen;
        SetProcess(Visible);
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
    public override bool _HasPoint(Vector2 point) => ActivationEnabled && !IsConsuming && Reveal >= .95f
        && new Rect2(new Vector2(0, 4) + FloatOffset, new Vector2(Size.X, Size.Y - 8)).HasPoint(point);
    public override void _GuiInput(InputEvent input)
    {
        if (ActivationEnabled && !IsConsuming && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } or InputEventScreenTouch { Pressed: true })
        {
            AcceptEvent();
            Pressed?.Invoke();
        }
    }
    public override void _Process(double delta)
    {
        _floatTime += (float)delta;
        _drawTime += (float)delta;
        if (IsConsuming)
        {
            _consumeAge += (float)delta;
            ConsumptionProgress = Math.Clamp(_consumeAge / .42f, 0, 1);
        }
        else _age = Math.Min(1, _age + (float)delta / .38f);
        if (_drawTime >= 1f / 30 || IsConsuming) { _drawTime = 0; QueueRedraw(); }
        if (!IsConsuming || ConsumptionProgress < 1) return;
        var completed = _completion;
        _completion = null;
        _ready = false;
        Hide();
        SetProcess(false);
        completed!.TrySetResult();
    }
    public override void _Draw()
    {
        float reveal = Reveal, burn = ConsumptionProgress;
        float width = Size.X * reveal;
        var rect = new Rect2(new Vector2((Size.X - width) / 2, 4) + FloatOffset, new(width, Size.Y - 8));
        if (burn < .99f && reveal > .002f)
        {
            var paper = PapyrusStyle.Paper.Lightened(_hovered ? .05f : 0).Darkened(burn * .8f);
            DrawRect(rect, new Color(paper, 1 - burn));
            DrawRect(rect, new Color(PapyrusStyle.Bronze, 1 - burn), false, 1.2f);
            var artSize = _art.GetSize();
            var region = new Rect2(new Vector2(artSize.X * (1 - reveal) / 2, 0), new Vector2(artSize.X * reveal, artSize.Y));
            DrawTextureRectRegion(_art, rect, region, new Color(1, 1, 1, (1 - burn) * reveal));
            for (int i = 1; i <= 5; i++)
                DrawLine(rect.Position + new Vector2(4, i * rect.Size.Y / 6), rect.Position + new Vector2(width - 4, i * rect.Size.Y / 6), new Color(PapyrusStyle.FaintInk, .04f * (1 - burn)), .7f);
            foreach (float x in new[] { rect.Position.X, rect.End.X })
            {
                DrawLine(new(x, rect.Position.Y), new(x, rect.End.Y), new Color(PapyrusStyle.Bronze, 1 - burn), 4);
                DrawLine(new(x, rect.Position.Y), new(x, rect.End.Y), new Color(PapyrusStyle.Paper.Lightened(.1f), 1 - burn), 1.5f);
            }
        }
        if (burn <= 0) return;
        for (int i = 0; i < 24; i++)
        {
            float x = Size.X * ((i * .618f) % 1);
            var at = new Vector2(x, Size.Y - 8 - burn * (45 + i % 5 * 12)) + FloatOffset;
            float alpha = Math.Min(1, (1 - burn) * 3);
            DrawCircle(at, 2 + i % 4, new Color(1, .43f, .08f, alpha));
            DrawCircle(at + new Vector2(0, -4), 1.4f + i % 3, new Color(1, .82f, .26f, alpha));
        }
    }
    public override void _ExitTree() => _completion?.TrySetCanceled();
}
