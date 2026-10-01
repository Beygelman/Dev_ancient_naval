using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>A continuous scroll gaining thickness and unfurling upward from its bottom.</summary>
public partial class RadialPapyrus : Control
{
    private readonly Vector2[] _paper = new Vector2[130];
    private readonly List<SectorButton> _commands = new(12);
    private float _time = 1;
    private float _span;
    public int ActionCount => _commands.Count;
    public float Reveal { get; private set; } = 1;

    public void Configure(IReadOnlyList<SectorButton> commands, bool unfold)
    {
        _commands.Clear();
        for (int i = 0; i < commands.Count; i++)
            _commands.Add(commands[i]);
        _span = commands.Count * SectorButton.SectorStep;
        _time = unfold ? 0 : 1;
        Reveal = unfold ? 0 : 1;
        foreach (var command in _commands)
            command.SetReveal(Reveal);
        SetProcess(unfold);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time = Math.Min(1, _time + (float)delta / .26f);
        Reveal = 1 - Mathf.Pow(1 - _time, 3);
        foreach (var command in _commands)
            command.SetReveal(Reveal);
        QueueRedraw();
        if (_time >= 1)
            SetProcess(false);
    }

    public override void _Draw()
    {
        if (_commands.Count == 0 || Reveal < .002f)
            return;
        const int steps = 64;
        float span = Math.Max(.06f, _span * Reveal);
        float start = Mathf.Pi / 2 - span * .5f;
        float middle = (SectorButton.Inner + SectorButton.Outer) * .5f;
        float outer = middle + (SectorButton.Outer - middle) * Reveal;
        float inner = middle - (middle - SectorButton.Inner) * Reveal;
        for (int i = 0; i <= steps; i++)
        {
            float angle = start + span * i / steps;
            _paper[i] = SectorButton.Center + Vector2.FromAngle(angle) * outer;
            _paper[_paper.Length - 1 - i] = SectorButton.Center + Vector2.FromAngle(angle) * inner;
        }

        DrawColoredPolygon(_paper, new Color(PapyrusStyle.Paper, .98f));
        DrawArc(SectorButton.Center, outer, start, start + span, 65, PapyrusStyle.Bronze, 1.2f, true);
        DrawArc(SectorButton.Center, inner, start, start + span, 65, PapyrusStyle.Bronze, 1, true);
        for (int i = 1; i < _commands.Count; i++)
        {
            float angle = start + span * i / _commands.Count;
            var direction = Vector2.FromAngle(angle);
            DrawLine(SectorButton.Center + direction * (inner + 7 * Reveal), SectorButton.Center + direction * (outer - 6 * Reveal), new Color(PapyrusStyle.FaintInk, .18f), 1, true);
        }

        for (int i = 0; i < 5; i++)
            DrawArc(SectorButton.Center, Mathf.Lerp(inner, outer, (i + 1) / 6f), start + .02f, start + span - .02f, 49, new Color(PapyrusStyle.FaintInk, .035f), .8f, true);
        DrawRoll(start, inner, outer);
        DrawRoll(start + span, inner, outer);
    }

    private void DrawRoll(float angle, float inner, float outer)
    {
        var direction = Vector2.FromAngle(angle);
        DrawLine(SectorButton.Center + direction * (inner - 2 * Reveal), SectorButton.Center + direction * (outer + 2 * Reveal), PapyrusStyle.Bronze, 4, true);
        DrawLine(SectorButton.Center + direction * (inner - 2 * Reveal), SectorButton.Center + direction * (outer + 2 * Reveal), new Color("f2e2bd"), 1.5f, true);
    }
}
