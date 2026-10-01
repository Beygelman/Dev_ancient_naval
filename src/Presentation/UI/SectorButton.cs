using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>One ink command on the unfolding parchment arc.</summary>
public partial class SectorButton : Button
{
    public float CenterAngle { get; private set; }
    public float Sweep { get; private set; } = SectorStep;

    public const float Inner = 51, Outer = 103, SectorStep = .49f;
    public static readonly Vector2 Center = new(124, 124);
    private float _offset;
    private float _reveal = 1;
    private readonly Vector2[] _polygon = new Vector2[34];
    private ActionGlyph? _glyph;
    private Label? _badge;
    public void AttachInk(ActionGlyph glyph, Label badge)
    {
        _glyph = glyph;
        _badge = badge;
        PlaceInk();
    }

    public void SetSector(int index, int count)
    {
        Sweep = SectorStep;
        _offset = (index - (count - 1) * .5f) * SectorStep;
        SetReveal(1);
    }

    public Vector2 IconCenter => Center + Vector2.FromAngle(CenterAngle) * 77;

    public void SetReveal(float progress)
    {
        _reveal = Mathf.Clamp(progress, 0, 1);
        CenterAngle = Mathf.Pi / 2 + _offset * _reveal;
        PlaceInk();
        QueueRedraw();
    }

    private void PlaceInk()
    {
        if (_glyph is not null)
            _glyph.Position = IconCenter - _glyph.Size * .5f - new Vector2(0, 4);
        if (_badge is not null)
            _badge.Visible = false;
        Modulate = new Color(1, 1, 1, _reveal);
    }

    public override bool _HasPoint(Vector2 point)
    {
        if (_reveal < .95f)
            return false;
        var offset = point - Center;
        float radius = offset.Length();
        float angle = Mathf.Wrap(offset.Angle() - CenterAngle, -Mathf.Pi, Mathf.Pi);
        return radius >= Inner && radius <= Outer && Math.Abs(angle) <= Sweep / 2;
    }

    public override void _Draw()
    {
        if (!IsHovered() || Disabled)
            return;
        const int steps = 16;
        float start = CenterAngle - Sweep / 2 + 0.025f, span = Sweep - 0.05f;
        for (int i = 0; i <= steps; i++)
        {
            float angle = start + span * i / steps;
            _polygon[i] = Center + Vector2.FromAngle(angle) * Outer;
            _polygon[_polygon.Length - 1 - i] = Center + Vector2.FromAngle(angle) * Inner;
        }

        DrawColoredPolygon(_polygon, new Color(PapyrusStyle.Bronze, .19f));
    }
}
