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
    public int ShortcutNumber { get; internal set; }
    public Vector2 RingCenter { get; private set; } = Center;
    public float InnerRadius { get; private set; } = Inner;
    public float OuterRadius { get; private set; } = Outer;
    private readonly Vector2[] _polygon = new Vector2[34];
    private ActionGlyph? _glyph;
    private Label? _badge;
    private IReadOnlyList<Vector2>? _worldTargetPoints;
    private float _worldTargetRadiusSquared;
    private float _inkRightLimit = float.PositiveInfinity;
    private Vector2 _iconCenter = Center + new Vector2(IconRadius, 0);
    public int? Cost { get; set; }
    internal void SetGeometry(Vector2 center, float inner, float outer)
    {
        RingCenter = center;
        InnerRadius = inner;
        OuterRadius = outer;
        PlaceInk();
        QueueRedraw();
    }
    public void AttachInk(ActionGlyph glyph, Label badge)
    {
        _glyph = glyph;
        _badge = badge;
        PlaceInk();
    }

    public void SetSector(int index, int count)
    {
        float sweep = ArcLength(count) / Math.Max(1, count);
        SetSector(Mathf.Pi / 2 - ((count - 1) * .5f - index) * sweep, sweep);
    }

    internal void SetSector(float angle, float sweep)
    {
        Sweep = sweep;
        _offset = Mathf.Pi / 2 - angle;
        SetReveal(1);
    }

    // A lower semicircle keeps its first command at the right edge even in the shipyard.
    internal static float ArcLength(int count) => Math.Min(Mathf.Pi, count * SectorStep);

    internal void SetInkRightLimit(float limit)
    {
        if (_inkRightLimit == limit) return;
        _inkRightLimit = limit;
        PlaceInk();
        QueueRedraw();
    }

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
    internal Vector2 UnfoldedIconCenter => RingCenter + Vector2.FromAngle(Mathf.Pi / 2 - _offset)
        * (InnerRadius + (OuterRadius - InnerRadius) * .54f);

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
        float inkRadius = InnerRadius + (OuterRadius - InnerRadius) * .54f;
        var ordinary = RingCenter + Vector2.FromAngle(CenterAngle) * inkRadius;
        if (ordinary.X <= _inkRightLimit && !OverlapsWorldTarget(ordinary))
            return ordinary;
        var best = ordinary;
        float bestClearance = -1;
        // Move ink within its existing command wedge rather than covering a target's
        // priority input hole. Command areas, parchment size and glyph size stay intact.
        for (int band = 0; band < 3; band++)
            for (int side = 0; side < 3; side++)
            {
                float radius = band == 0 ? inkRadius : band == 1 ? InnerRadius + 16 : OuterRadius - 16;
                float angle = CenterAngle + (side == 0 ? 0 : side == 1 ? .25f : -.25f) * Sweep;
                var candidate = RingCenter + Vector2.FromAngle(angle) * radius;
                if (candidate.X > _inkRightLimit) continue;
                float clearance = float.MaxValue;
                if (_worldTargetPoints is not null)
                    foreach (var target in _worldTargetPoints)
                        clearance = Math.Min(clearance, candidate.DistanceSquaredTo(target));
                if (clearance > bestClearance)
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
        if (_reveal < 1 || GetParent() is RadialPapyrus paper && paper.IsProcessing())
            return false;
        // A nearby enemy's tile remains selectable even when it lies beneath the ring.
        // These points carry target coordinates only, including anonymous radar contacts.
        if (OverlapsWorldTarget(point))
            return false;
        var offset = point - RingCenter;
        float radius = offset.Length();
        float angle = Mathf.Wrap(offset.Angle() - CenterAngle, -Mathf.Pi, Mathf.Pi);
        return radius >= InnerRadius && radius <= OuterRadius + 12 && Math.Abs(angle) <= Sweep / 2;
    }

    public override void _Draw()
    {
        if (Cost is { } cost)
        {
            // Prices follow the outer rim, so neighboring commands cannot stack
            // one price on another icon as the paper wraps around its hull.
            var direction = (IconCenter - RingCenter).Normalized();
            var at = RingCenter + direction * (OuterRadius - 1);
            string text = cost.ToString();
            float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 12).X;
            DrawStyleBox(PapyrusStyle.Panel(.9f), new Rect2(at - new Vector2(width / 2 + 11, 9), new Vector2(width + 22, 18)));
            CoinIcon.DrawCoin(this, at - new Vector2(width / 2 + 3, 0), 5.5f);
            DrawString(ThemeDB.FallbackFont, at + new Vector2(3 - width / 2, 4), text, fontSize: 12, modulate: PapyrusStyle.Ink);
        }
        if (ShortcutNumber > 0)
        {
            var at = RingCenter + Vector2.FromAngle(CenterAngle) * (InnerRadius + 8);
            string text = ShortcutNumber.ToString();
            float width = ThemeDB.FallbackFont.GetStringSize(text, fontSize: 11).X;
            DrawString(ThemeDB.FallbackFont, at + new Vector2(-width / 2, 4), text,
                fontSize: 11, modulate: Disabled ? new Color(PapyrusStyle.FaintInk, .55f) : PapyrusStyle.Ink);
        }
        if (!IsHovered() || Disabled)
            return;
        const int steps = 16;
        float start = CenterAngle - Sweep / 2 + 0.025f, span = Sweep - 0.05f;
        for (int i = 0; i <= steps; i++)
        {
            float angle = start + span * i / steps;
            _polygon[i] = RingCenter + Vector2.FromAngle(angle) * OuterRadius;
            _polygon[_polygon.Length - 1 - i] = RingCenter + Vector2.FromAngle(angle) * InnerRadius;
        }

        DrawColoredPolygon(_polygon, new Color(PapyrusStyle.Bronze, .19f));
    }
}
