using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Bounded parchment with retained grain, rolled lips and symmetric open/close motion.</summary>
internal partial class RollingModalPaper : PanelContainer
{
    private Tween? _motion;
    private TaskCompletionSource? _completion;
    internal bool IsAnimating { get; private set; }
    internal float RollProgress => Scale.Y;
    public override void _Ready()
    {
        AddThemeStyleboxOverride("panel", PapyrusStyle.Panel(.98f));
        PapyrusGrain.Apply(this);
        var lips = new RolledLips { Name = "PapyrusRolledEdges", Paper = this };
        AddChild(lips);
        Resized += () => { PivotOffset = Size * .5f; lips.QueueRedraw(); };
        PivotOffset = Size * .5f;
    }

    internal Task OpenAsync(bool instant = false)
    {
        Show();
        PivotOffset = Size * .5f;
        Scale = new(1, instant ? 1 : .045f);
        Modulate = Colors.White;
        return Animate(1, instant);
    }

    internal Task FoldAsync(bool instant = false) => Animate(.045f, instant);

    private Task Animate(float target, bool instant)
    {
        _motion?.Kill();
        _completion?.TrySetResult();
        _completion = null;
        if (instant)
        {
            Scale = new(1, target);
            IsAnimating = false;
            return Task.CompletedTask;
        }
        IsAnimating = true;
        _completion = new TaskCompletionSource();
        var completion = _completion;
        _motion = CreateTween();
        _motion.TweenProperty(this, "scale:y", target, .34).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _motion.TweenCallback(Callable.From(() =>
        {
            IsAnimating = false;
            if (ReferenceEquals(_completion, completion)) _completion = null;
            completion.TrySetResult();
        }));
        return completion.Task;
    }

    public override void _ExitTree()
    {
        _motion?.Kill();
        _completion?.TrySetCanceled();
    }

    private partial class RolledLips : Node2D
    {
        internal RollingModalPaper Paper { get; init; } = null!;
        public override void _Draw()
        {
            float width = Paper.Size.X;
            foreach (float y in new[] { -3f, Paper.Size.Y - 7 })
            {
                DrawRect(new Rect2(0, y, width, 10), new Color("ceb37d"));
                DrawLine(new(1, y + 1), new(width - 1, y + 1), new Color("f2e2b5"), 2.4f, true);
                DrawLine(new(1, y + 8), new(width - 1, y + 8), new Color("927043"), 1.1f, true);
                DrawArc(new(1, y + 5), 5, -Mathf.Pi / 2, Mathf.Pi / 2, 12, PapyrusStyle.Bronze, 1, true);
                DrawArc(new(width - 1, y + 5), 5, Mathf.Pi / 2, Mathf.Pi * 1.5f, 12, PapyrusStyle.Bronze, 1, true);
            }
        }
    }
}
