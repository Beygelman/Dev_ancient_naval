using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>One ink command on the unfolding parchment arc.</summary>
public partial class SectorButton : Button
{
    public float CenterAngle { get; private set; }
    public float Sweep { get; private set; } = SectorStep;

    public const float Inner = 67, Outer = 103, SectorStep = Mathf.Pi / 4;
    public const float IconRadius = (Inner + Outer) * .5f;
    public static readonly Vector2 Center = new(124, 124);
    public static readonly Vector2 PaperFootprint = new(Outer * 2, Outer);
    private float _offset;
    private float _reveal = 1;
    private readonly Vector2[] _polygon = new Vector2[34];
    private ActionGlyph? _glyph;
    private Label? _badge;
    private IReadOnlyList<Vector2>? _worldTargetPoints;
    private float _worldTargetRadiusSquared;
    private Vector2 _iconCenter = Center + new Vector2(IconRadius, 0);
    private static readonly float[] IconRadii = { IconRadius, Inner + 7, Outer - 7 };
    public int? Cost { get; set; }
    public void AttachInk(ActionGlyph glyph, Label badge)
    {
        _glyph = glyph;
        _badge = badge;
        PlaceInk();
    }

    public void SetSector(int index, int count)
    {
        Sweep = ArcLength(count) / Math.Max(1, count);
        _offset = ((count - 1) * .5f - index) * Sweep;
        SetReveal(1);
    }

    // The tiny seam keeps the annulus a simple polygon when all eight sectors are shown.
    internal static float ArcLength(int count) => Math.Min(Mathf.Tau - .035f, count * SectorStep);

    internal void SetWorldTargetHitExclusions(IReadOnlyList<Vector2> points, float radius)
    {
        _worldTargetPoints = points;
        _worldTargetRadiusSquared = radius * radius;
        if (FindInkCenter() != _iconCenter)
        {
            PlaceInk();
            QueueRedraw();
        }
    }

    public Vector2 IconCenter => _iconCenter;

    public void SetReveal(float progress)
    {
        _reveal = Mathf.Clamp(progress, 0, 1);
        CenterAngle = Mathf.Pi / 2 - _offset * _reveal;
        PlaceInk();
        QueueRedraw();
    }

    private void PlaceInk()
    {
        _iconCenter = FindInkCenter();
        if (_glyph is not null)
            _glyph.Position = IconCenter - _glyph.Size * .5f;
        if (_badge is not null)
            _badge.Visible = false;
        Modulate = new Color(1, 1, 1, _reveal);
    }

    private Vector2 FindInkCenter()
    {
        var ordinary = Center + Vector2.FromAngle(CenterAngle) * IconRadius;
        if (!OverlapsWorldTarget(ordinary))
            return ordinary;
        var best = ordinary;
        float bestClearance = -1;
        // Move ink within its existing command wedge rather than covering a target's
        // priority input hole. Command areas, parchment size and glyph size stay intact.
        foreach (float radius in IconRadii)
            for (int side = 0; side < 3; side++)
            {
                float angle = CenterAngle + (side == 0 ? 0 : side == 1 ? .25f : -.25f) * Sweep;
                var candidate = Center + Vector2.FromAngle(angle) * radius;
                float clearance = float.MaxValue;
                foreach (var target in _worldTargetPoints!)
                    clearance = Math.Min(clearance, candidate.DistanceSquaredTo(target));
                if (clearance > _worldTargetRadiusSquared && clearance > bestClearance)
                {
                    best = candidate;
                    bestClearance = clearance;
                }
            }
        return best;
    }

    private bool OverlapsWorldTarget(Vector2 point)
    {
        if (_worldTargetPoints is not null)
            foreach (var target in _worldTargetPoints)
                if (point.DistanceSquaredTo(target) <= _worldTargetRadiusSquared)
                    return true;
        return false;
    }

    public override bool _HasPoint(Vector2 point)
    {
        if (_reveal < .95f)
            return false;
        // A nearby enemy's tile remains selectable even when it lies beneath the ring.
        // These points carry target coordinates only, including anonymous radar contacts.
        if (OverlapsWorldTarget(point))
            return false;
        var offset = point - Center;
        float radius = offset.Length();
        float angle = Mathf.Wrap(offset.Angle() - CenterAngle, -Mathf.Pi, Mathf.Pi);
        return radius >= Inner && radius <= Outer && Math.Abs(angle) <= Sweep / 2;
    }

    public override void _Draw()
    {
        if (Cost is { } cost)
        {
            var at = IconCenter + new Vector2(0, -23);
            string text = cost.ToString();
            float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 12).X;
            DrawStyleBox(PapyrusStyle.Panel(.9f), new Rect2(at - new Vector2(width / 2 + 11, 9), new Vector2(width + 22, 18)));
            CoinIcon.DrawCoin(this, at - new Vector2(width / 2 + 3, 0), 5.5f);
            DrawString(ThemeDB.FallbackFont, at + new Vector2(3 - width / 2, 4), text, fontSize: 12, modulate: PapyrusStyle.Ink);
        }
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
