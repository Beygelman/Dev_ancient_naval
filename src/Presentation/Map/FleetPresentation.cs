using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    private readonly Dictionary<Tween, Action> _visualTweens = new();
    private bool _skipAnimation, _opponentReplay;
    private double _animationRate = 1;
    private PresentedCommand? _animationPresentation;
    internal bool HasCommandAnimation => _visualTweens.Count > 0;

    // Complete callbacks on the scene thread. Finishing a Godot tween alone does
    // not emit Finished, which would strand awaiting orders and their cleanup.
    public void FinishAnimation()
    {
        _skipAnimation = true;
        foreach (var finish in _visualTweens.Values.ToArray())
            finish();
    }

    private Task FocusAction(GridPosition cell) => !_skipAnimation && _opponentReplay && FocusTarget is not null
        ? FocusTarget(cell) : Task.CompletedTask;

    private async Task TweenValue(double duration, Action<float> update)
    {
        if (_skipAnimation)
        {
            update(1);
            return;
        }
        var completion = new TaskCompletionSource();
        var tween = CreateTween();
        bool finished = false;
        void Finish()
        {
            if (finished) return;
            finished = true;
            tween.Kill();
            _visualTweens.Remove(tween);
            try { update(1); }
            catch (Exception error) { completion.TrySetException(error); return; }
            completion.TrySetResult();
        }
        _visualTweens.Add(tween, Finish);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            update(t);
            QueueRedraw();
        }), 0f, 1f, duration / _animationRate);
        tween.Finished += Finish;
        await completion.Task;
    }
}
