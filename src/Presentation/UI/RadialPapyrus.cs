using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>Symmetric parchment unfurling around an object; its length follows command count.</summary>
public partial class RadialPapyrus : Control
{
    private readonly Vector2[] _paper = new Vector2[130];
    private readonly Vector2[] _grainUv = new Vector2[130];
    private readonly Color[] _grainTint = { Colors.White };
    private readonly List<SectorButton> _commands = new(12);
    private float _time = 1;
    private float _span;
    private float _inner = SectorButton.Inner, _outer = SectorButton.Outer;
    private float _maximumSpan = Mathf.Pi;
    private SectorButton? _centeredCommand;
    private readonly List<float> _slotAngles = new(12);
    public Vector2 RingCenter { get; private set; } = SectorButton.Center;
    public float BandWidth => _outer - _inner;
    public int ActionCount => _commands.Count;
    internal float ArcLength => _span;
    public float Reveal { get; private set; } = 1;
    internal float TopInset => _inner * Mathf.Cos(_span / 2);

    internal void SetWrapping(float inner, float maximumSpan)
    {
        if (Math.Abs(_inner - inner) < .01f && Math.Abs(_maximumSpan - maximumSpan) < .001f) return;
        _inner = inner;
        _outer = inner + 58;
        _maximumSpan = maximumSpan;
        RingCenter = new(220, 220);
        Size = RingCenter * 2;
        PlaceCommands();
        ApplyReveal();
        QueueRedraw();
    }

    public override void _Ready()
    {
        SetProcess(false);
        VisibilityChanged += () => SetProcess(IsVisibleInTree() && _time < 1);
    }

    public void Configure(IReadOnlyList<SectorButton> commands, bool unfold, SectorButton? centeredCommand = null)
    {
        _commands.Clear();
        for (int i = 0; i < commands.Count; i++)
            _commands.Add(commands[i]);
        _centeredCommand = centeredCommand;
        PlaceCommands();
        _time = unfold ? 0 : 1;
        Reveal = unfold ? 0 : 1;
        ApplyReveal();
        SetProcess(unfold && IsVisibleInTree());
        QueueRedraw();
    }

    private void PlaceCommands()
    {
        _span = Math.Min(_maximumSpan, _commands.Count * SectorButton.SectorStep);
        float sweep = _span / Math.Max(1, _commands.Count);
        _slotAngles.Clear();
        for (int i = 0; i < _commands.Count; i++)
            _slotAngles.Add(Mathf.Pi / 2 - _span / 2 + (i + .5f) * sweep);
        // The same real left-to-right order drives ink, prices and numeric keys.
        // At a wider wrap the upper end slots may exchange order with the lower ones.
        _slotAngles.Sort((a, b) =>
        {
            int x = Mathf.Cos(a).CompareTo(Mathf.Cos(b));
            return x != 0 ? x : Mathf.Sin(a).CompareTo(Mathf.Sin(b));
        });
        int centered = _centeredCommand is null ? -1 : _commands.IndexOf(_centeredCommand);
        if (centered >= 0 && _commands.Count % 2 == 1)
        {
            int middle = _slotAngles.FindIndex(a => Math.Abs(a - Mathf.Pi / 2) < .001f);
            if (middle >= 0) (_slotAngles[centered], _slotAngles[middle]) = (_slotAngles[middle], _slotAngles[centered]);
        }
        for (int i = 0; i < _commands.Count; i++)
        {
            _commands[i].Size = Size;
            _commands[i].SetGeometry(RingCenter, _inner, _outer);
            _commands[i].SetSector(_slotAngles[i], sweep);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) { SetProcess(false); return; }
        _time = Math.Min(1, _time + (float)delta / .26f);
        Reveal = 1 - Mathf.Pow(1 - _time, 3);
        ApplyReveal();
        QueueRedraw();
        if (_time >= 1)
            SetProcess(false);
    }

    private void ApplyReveal()
    {
        if (_commands.Count == 0) return;
        for (int i = 0; i < _commands.Count; i++)
        {
            _commands[i].SetInkRightLimit(float.PositiveInfinity);
            _commands[i].SetReveal(Reveal);
        }
    }

    internal void RefreshInkBounds()
    {
        if (_commands.Count == 0) return;
        foreach (var command in _commands) command.SetInkRightLimit(float.PositiveInfinity);
    }

    public override void _Draw()
    {
        if (_commands.Count == 0 || Reveal < .002f)
            return;
        const int steps = 64;
        float span = Math.Max(.06f, _span * Reveal);
        float start = Mathf.Pi / 2 - span * .5f;
        float middle = (_inner + _outer) * .5f;
        float outer = middle + (_outer - middle) * Reveal;
        float inner = middle - (middle - _inner) * Reveal;
        for (int i = 0; i <= steps; i++)
        {
            float angle = start + span * i / steps;
            _paper[i] = RingCenter + Vector2.FromAngle(angle) * outer;
            _paper[_paper.Length - 1 - i] = RingCenter + Vector2.FromAngle(angle) * inner;
        }

        DrawColoredPolygon(_paper, new Color(PapyrusStyle.Paper, .98f));
        for (int i = 0; i < _paper.Length; i++) _grainUv[i] = _paper[i] / 64;
        TextureRepeat = TextureRepeatEnum.Enabled;
        DrawPolygon(_paper, _grainTint, _grainUv, PapyrusGrain.Texture);
        DrawArc(RingCenter, outer, start, start + span, 65, PapyrusStyle.Bronze, 1.2f, true);
        DrawArc(RingCenter, inner, start, start + span, 65, PapyrusStyle.Bronze, 1, true);
        for (int i = 1; i < _commands.Count; i++)
        {
            float angle = _commands[i].CenterAngle - _commands[i].Sweep * Reveal / 2;
            var direction = Vector2.FromAngle(angle);
            DrawLine(RingCenter + direction * (inner + 7 * Reveal), RingCenter + direction * (outer - 6 * Reveal), new Color(PapyrusStyle.FaintInk, .18f), 1, true);
        }

        for (int i = 0; i < 5; i++)
            DrawArc(RingCenter, Mathf.Lerp(inner, outer, (i + 1) / 6f), start + .02f, start + span - .02f, 49, new Color(PapyrusStyle.FaintInk, .035f), .8f, true);
        DrawRoll(start, inner, outer);
        DrawRoll(start + span, inner, outer);
    }

    private void DrawRoll(float angle, float inner, float outer)
    {
        var direction = Vector2.FromAngle(angle);
        float radius = ScrollRollArt.Radius(new(outer - inner, _span * (inner + outer) * .5f), Reveal);
        var normal = new Vector2(-direction.Y, direction.X);
        var from = RingCenter + direction * (inner - 2 * Reveal);
        var to = RingCenter + direction * (outer + 2 * Reveal);
        DrawLine(from, to, PapyrusStyle.Bronze, radius * 2, true);
        for (int strip = 0; strip < 9; strip++)
        {
            float t = strip / 8f;
            var tint = PapyrusStyle.Paper.Darkened(.20f).Lerp(PapyrusStyle.Paper.Lightened(.14f), MathF.Sin(t * Mathf.Pi));
            var offset = normal * ((t - .5f) * radius * 1.6f);
            DrawLine(from + offset, to + offset, tint, radius * .25f + .4f, true);
        }
    }
}
