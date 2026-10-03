using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.UI;
<<<<<<< Updated upstream
/// <summary>Symmetric parchment unfurling around an object; its length follows command count.</summary>
public partial class RadialPapyrus : Control
{
    private readonly Vector2[] _paper = new Vector2[130];
    private readonly Vector2[] _grainUv = new Vector2[130];
    private readonly Color[] _grainTint = { Colors.White };
=======

/// <summary>A continuous scroll unfurling equally to either side of its top.</summary>
public partial class RadialPapyrus : Control
{
    private readonly Vector2[] _paper = new Vector2[130];
>>>>>>> Stashed changes
    private readonly List<SectorButton> _commands = new(12);
    private float _time = 1;
    private float _span;
    public int ActionCount => _commands.Count;
<<<<<<< Updated upstream
    internal float ArcLength => _span;
    public float Reveal { get; private set; } = 1;
    internal float TopInset => SectorButton.Inner * Mathf.Cos(_span / 2);
=======
    public float Reveal { get; private set; } = 1;
>>>>>>> Stashed changes

    public void Configure(IReadOnlyList<SectorButton> commands, bool unfold)
    {
        _commands.Clear();
<<<<<<< Updated upstream
        for (int i = 0; i < commands.Count; i++)
            _commands.Add(commands[i]);
        _span = SectorButton.ArcLength(commands.Count);
        _time = unfold ? 0 : 1;
        Reveal = unfold ? 0 : 1;
        foreach (var command in _commands)
            command.SetReveal(Reveal);
=======
        for (int i = 0; i < commands.Count; i++) _commands.Add(commands[i]);
        _span = commands.Count * SectorButton.SectorStep;
        _time = unfold ? 0 : 1;
        Reveal = unfold ? 0 : 1;
        foreach (var command in _commands) command.SetReveal(Reveal);
>>>>>>> Stashed changes
        SetProcess(unfold);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time = Math.Min(1, _time + (float)delta / .26f);
        Reveal = 1 - Mathf.Pow(1 - _time, 3);
<<<<<<< Updated upstream
        foreach (var command in _commands)
            command.SetReveal(Reveal);
        QueueRedraw();
        if (_time >= 1)
            SetProcess(false);
=======
        foreach (var command in _commands) command.SetReveal(Reveal);
        QueueRedraw();
        if (_time >= 1) SetProcess(false);
>>>>>>> Stashed changes
    }

    public override void _Draw()
    {
<<<<<<< Updated upstream
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
        for (int i = 0; i < _paper.Length; i++) _grainUv[i] = _paper[i] / 64;
        TextureRepeat = TextureRepeatEnum.Enabled;
        DrawPolygon(_paper, _grainTint, _grainUv, PapyrusGrain.Texture);
        DrawArc(SectorButton.Center, outer, start, start + span, 65, PapyrusStyle.Bronze, 1.2f, true);
        DrawArc(SectorButton.Center, inner, start, start + span, 65, PapyrusStyle.Bronze, 1, true);
=======
        if (_commands.Count == 0) return;
        const int steps = 64;
        float span = Math.Max(.06f, _span * Reveal);
        float start = -Mathf.Pi / 2 - span * .5f;
        for (int i = 0; i <= steps; i++)
        {
            float angle = start + span * i / steps;
            _paper[i] = SectorButton.Center + Vector2.FromAngle(angle) * SectorButton.Outer;
            _paper[_paper.Length - 1 - i] = SectorButton.Center + Vector2.FromAngle(angle) * SectorButton.Inner;
        }
        DrawColoredPolygon(_paper, new Color(PapyrusStyle.Paper, .98f));
        DrawArc(SectorButton.Center, SectorButton.Outer, start, start + span, 65, PapyrusStyle.Bronze, 1.2f, true);
        DrawArc(SectorButton.Center, SectorButton.Inner, start, start + span, 65, PapyrusStyle.Bronze, 1, true);
>>>>>>> Stashed changes
        for (int i = 1; i < _commands.Count; i++)
        {
            float angle = start + span * i / _commands.Count;
            var direction = Vector2.FromAngle(angle);
<<<<<<< Updated upstream
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
=======
            DrawLine(SectorButton.Center + direction * 58, SectorButton.Center + direction * 97, new Color(PapyrusStyle.FaintInk, .18f), 1, true);
        }
        for (int i = 0; i < 5; i++)
            DrawArc(SectorButton.Center, 56 + i * 10, start + .02f, start + span - .02f, 49, new Color(PapyrusStyle.FaintInk, .035f), .8f, true);
        DrawRoll(start);
        DrawRoll(start + span);
    }

    private void DrawRoll(float angle)
    {
        var direction = Vector2.FromAngle(angle);
        DrawLine(SectorButton.Center + direction * 49, SectorButton.Center + direction * 105, PapyrusStyle.Bronze, 4, true);
        DrawLine(SectorButton.Center + direction * 49, SectorButton.Center + direction * 105, new Color("f2e2bd"), 1.5f, true);
>>>>>>> Stashed changes
    }
}
