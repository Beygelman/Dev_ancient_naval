using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>A finite cosmetic turn-start flare, attached to the real art canvas.
/// Offscreen/hidden flares have no frame processing; their one expiry timer still
/// releases them. They never redraw the retained town/ship or terrain artwork.</summary>
internal partial class SanctuaryTurnPulse : Node2D
{
    internal const float Duration = 1.1f;
    internal Func<float, float, float, Vector2> Project { get; init; } = null!;
    internal Func<bool> CanDraw { get; init; } = null!;
    internal FleetColor Nation { get; init; }
    internal int Seed { get; init; }
    internal Action<SanctuaryTurnPulse>? Finished { get; init; }
    internal int DrawCount { get; private set; }
    private SceneTreeTimer? _timer;
    private ulong _born;
    private float _progress, _paintTime;
    private bool _onScreen, _finished;

    public override void _Ready()
    {
        _born = Time.GetTicksMsec();
        SetProcess(false);
        var bounds = new VisibleOnScreenNotifier2D
        {
            Name = "ShrineFlareVisibility", Rect = new Rect2(-70, -125, 140, 150)
        };
        bounds.ScreenEntered += () => { _onScreen = true; RefreshProcessing(); };
        bounds.ScreenExited += () => { _onScreen = false; RefreshProcessing(); };
        AddChild(bounds);
        VisibilityChanged += RefreshProcessing;
        _timer = GetTree().CreateTimer(Duration + .02, processAlways: true, ignoreTimeScale: true);
        _timer.Timeout += Complete;
        if (!CanDraw()) Complete();
    }

    private void RefreshProcessing()
    {
        if (_finished) return;
        bool process = _onScreen && IsVisibleInTree() && CanDraw();
        SetProcess(process);
        if (process) { _paintTime = -1; QueueRedraw(); }
    }

    public override void _Process(double delta)
    {
        if (!CanDraw()) { Complete(); return; }
        _progress = (Time.GetTicksMsec() - _born) / (Duration * 1000f);
        if (_progress >= 1) { Complete(); return; }
        if (_progress - _paintTime < 1 / (30f * Duration)) return;
        _paintTime = _progress;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_finished || !_onScreen || !CanDraw()) return;
        DrawCount++;
        FactionSanctuaryArt.DrawTurnPulse(this, Project, Nation, _progress, Seed);
    }

    private void Complete()
    {
        if (_finished) return;
        _finished = true;
        SetProcess(false);
        Visible = false;
        Finished?.Invoke(this);
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (_timer is not null && GodotObject.IsInstanceValid(_timer)) _timer.Timeout -= Complete;
        VisibilityChanged -= RefreshProcessing;
        if (_finished) return;
        _finished = true;
        Finished?.Invoke(this);
    }
}
