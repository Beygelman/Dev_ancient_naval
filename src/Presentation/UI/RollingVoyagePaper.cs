using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Retained parchment texture and animated rolled lips over a native clipped viewport.</summary>
internal partial class RollingVoyagePaper : RollingModalPaper
{
    private int _lastScroll;
    private float _rollPhase, _remainingMotion;
    internal ScrollContainer Scroll { get; private set; } = null!;
    internal void Bind(Control content)
    {
        Name = "VoyageSetupPaper";
        Scroll = PapyrusModal.Wrap(this, content, "VoyageSetupScroll");
        Scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        Scroll.GetVScrollBar().ValueChanged += value =>
        {
            float direction = Math.Sign(value - _lastScroll);
            _lastScroll = (int)value;
            _rollPhase += direction * .27f;
            _remainingMotion = .3f;
            SetProcess(true);
        };
        VisibilityChanged += () =>
        {
            if (IsVisibleInTree()) return;
            _remainingMotion = 0;
            SetProcess(false);
        };
        SetProcess(false);
    }
    public override void _Process(double delta)
    {
        _remainingMotion = Math.Max(0, _remainingMotion - (float)delta);
        _rollPhase += _remainingMotion * (float)delta * 4;
        RollTexturePhase = _rollPhase;
        if (_remainingMotion == 0) SetProcess(false);
    }
}
