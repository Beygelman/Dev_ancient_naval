using System;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Presentation-only health transitions. Observe only an already visible snapshot.</summary>
internal sealed class AmphoraHealthAnimation
{
    private bool _initialized;
    private double _health;
    private double _maximum;
    private float _born = -10;
    private int _previousStage;
    private bool _healing;

    internal int Stage => AmphoraBadgeArt.Stage(_health, _maximum);
    internal bool Active(float time) => time - _born < (_healing ? .85f : .55f);

    internal void Observe(double health, double maximum, float time)
    {
        if (!_initialized)
        {
            _initialized = true;
            _health = health;
            _maximum = maximum;
            _previousStage = Stage;
            return;
        }
        if (_health == health && _maximum == maximum)
            return;
        _previousStage = Stage;
        _healing = health > _health;
        _health = health;
        _maximum = maximum;
        _born = time;
    }

    internal AmphoraMotion Motion(float time)
    {
        float age = Math.Max(0, time - _born);
        if (!Active(time))
            return new(Stage, Stage, Vector2.Zero, 1, 0, false, 1);
        float duration = _healing ? .85f : .55f;
        float t = Math.Clamp(age / duration, 0, 1);
        // A short brittle impact differs from the gentle return of fragments.
        var shake = _healing ? Vector2.Zero : new Vector2(MathF.Sin(age * 83) * 2.7f, -MathF.Abs(MathF.Sin(age * 51)) * 1.7f) * MathF.Exp(-age * 11);
        float restore = _healing ? FleetView.MotionProgress(Math.Clamp(age / .5f, 0, 1)) : 1;
        float flash = _healing ? MathF.Pow(Math.Max(0, MathF.Sin(t * Mathf.Tau * 2)), 3) * (1 - t * .35f) : 0;
        return new(_previousStage, Stage, shake, restore, flash, _healing, t);
    }
}

internal readonly record struct AmphoraMotion(int PreviousStage, int Stage, Vector2 Shake, float Restore, float Flash, bool Healing, float Progress)
{
    internal static AmphoraMotion Still(double health, double maximum)
    {
        int stage = AmphoraBadgeArt.Stage(health, maximum);
        return new(stage, stage, Vector2.Zero, 1, 0, false, 1);
    }
}
