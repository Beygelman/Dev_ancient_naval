using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

internal enum NavigationMarkerKind { Capture, Treasury, OwnAttack }

/// <summary>Main supplies eligible owned/action coordinates; this HUD never queries hidden world state.</summary>
internal readonly record struct OffscreenNavigationTarget(int Id, GridPosition Cell, NavigationMarkerKind Kind);

/// <summary>World actions remain on their targets; these separate compass pointers only move the camera.</summary>
internal partial class OffscreenNavigationHud : CanvasLayer
{
    private Control _root = null!;
    private NavigationGuideRing _ring = null!;
    private MothershipCompassButton _compass = null!;
    private readonly Dictionary<(NavigationMarkerKind Kind, int Id), OffscreenNavigationArrow> _arrows = new();
    private readonly List<(NavigationMarkerKind Kind, int Id)> _removed = new();
    private readonly List<MarkerPlacement> _placements = new();
    private readonly List<SpreadBlock> _spreadBlocks = new();
    private float[] _placedAngles = Array.Empty<float>();
    private Func<GridPosition, Vector2>? _projectScreen;
    private FleetColor _nation;
    private bool _sessionVisible, _hasMothership;
    private float _phase, _drawAge;
    internal Func<bool> CanInteract { get; set; } = () => false;
    internal event Action<OffscreenNavigationTarget>? TargetRequested;
    internal event Action? MothershipRequested;
    internal Vector2 CircleCenter { get; private set; }
    internal float CircleRadius { get; private set; }
    internal int VisibleMarkerCount { get; private set; }
    internal Button CompassButton => _compass;
    internal OffscreenNavigationArrow? Marker(NavigationMarkerKind kind, int id) =>
        _arrows.TryGetValue((kind, id), out var arrow) ? arrow : null;

    public override void _Ready()
    {
        // Utility arrows sit beneath the main HUD and its modal papers. Their
        // native hit regions still isolate clicks from the sea beneath them.
        Layer = 0;
        _root = new Control { Name = "OffscreenNavigation", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root);
        _ring = new NavigationGuideRing { Name = "NavigationGuideCircle", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_ring);
        _compass = new MothershipCompassButton { Name = "FindMothershipCompass", Size = new(64, 64),
            MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = "Find your Mothership", FocusMode = Control.FocusModeEnum.None,
            ZIndex = 2 };
        _root.AddChild(_compass);
        _compass.Pressed += () => { if (_sessionVisible && _hasMothership && CanInteract()) MothershipRequested?.Invoke(); };
        _root.VisibilityChanged += UpdateProcessing;
        UiScale.Bind(this, _root, Layout);
        _root.Hide();
        SetProcess(false);
    }

    internal void SetTargets(IReadOnlyList<OffscreenNavigationTarget> targets, FleetColor nation,
        bool sessionVisible, bool hasMothership)
    {
        _nation = nation;
        _sessionVisible = sessionVisible;
        _hasMothership = hasMothership;
        foreach (var arrow in _arrows.Values) arrow.Current = false;
        foreach (var target in targets)
        {
            var key = (target.Kind, target.Id);
            if (!_arrows.TryGetValue(key, out var arrow))
            {
                arrow = new OffscreenNavigationArrow { Name = "Navigation" + target.Kind + target.Id,
                    Size = new(80, 80), MouseFilter = Control.MouseFilterEnum.Stop, FocusMode = Control.FocusModeEnum.None };
                _arrows.Add(key, arrow);
                _root.AddChild(arrow);
                arrow.Pressed += () =>
                {
                    if (_sessionVisible && arrow.Current && arrow.IsVisibleInTree() && CanInteract())
                        TargetRequested?.Invoke(arrow.Target);
                };
            }
            arrow.Current = true;
            arrow.Configure(target, nation);
        }
        _removed.Clear();
        foreach (var (key, arrow) in _arrows)
            if (!arrow.Current) { arrow.Hide(); _removed.Add(key); }
        foreach (var key in _removed)
        {
            _arrows[key].QueueFree();
            _arrows.Remove(key);
        }
        _compass.SetNation(nation);
        _compass.Visible = sessionVisible && hasMothership;
        _compass.Disabled = !CanInteract();
        _root.Visible = sessionVisible;
        Layout();
    }

    internal void PositionMarkers(Func<GridPosition, Vector2> projectScreen)
    {
        _projectScreen = projectScreen;
        Layout();
    }

    private void Layout()
    {
        if (_root is null || _ring is null || _compass is null) return;
        var viewport = UiScale.LogicalViewport(this);
        CircleCenter = viewport * .5f;
        CircleRadius = Math.Min(viewport.X, viewport.Y) * .23f;
        _ring.Size = viewport;
        _ring.SetCircle(CircleCenter, CircleRadius, _nation);
        _compass.Position = new((viewport.X - _compass.Size.X) / 2, viewport.Y - _compass.Size.Y - 14);
        VisibleMarkerCount = 0;
        _placements.Clear();
        bool disabled = !CanInteract();
        foreach (var arrow in _arrows.Values)
        {
            if (!_sessionVisible || _projectScreen is null) { arrow.Hide(); continue; }
            var projected = _projectScreen(arrow.Target.Cell);
            var offset = UiScale.ScreenToUi(projected) - CircleCenter;
            bool outside = float.IsFinite(offset.X) && float.IsFinite(offset.Y)
                && offset.LengthSquared() > CircleRadius * CircleRadius;
            if (!outside) { arrow.Hide(); continue; }
            var direction = offset.Normalized();
            arrow.SetDirection(direction);
            _placements.Add(new(arrow, Mathf.PosMod(direction.Angle(), Mathf.Tau)));
            arrow.Disabled = disabled;
            arrow.SetPhase(_phase);
            arrow.Show();
            VisibleMarkerCount++;
        }
        SpreadNearbyMarkers();
        _ring.Visible = _sessionVisible && VisibleMarkerCount > 0;
        UpdateProcessing();
    }

    private readonly record struct MarkerPlacement(OffscreenNavigationArrow Arrow, float Bearing);
    private readonly record struct SpreadBlock(int First, int Last, double Sum)
    {
        internal int Count => Last - First + 1;
        internal double Mean => Sum / Count;
    }

    private void SpreadNearbyMarkers()
    {
        int count = _placements.Count;
        if (count == 0) return;
        const float minimumHitDistance = 62;
        var compassOffset = _compass.Position + _compass.Size * .5f - CircleCenter;
        float compassDistance = compassOffset.Length();
        bool reserveCompass = _compass.Visible && compassDistance > .001f && CircleRadius > .001f
            && Math.Abs(compassDistance - CircleRadius) < minimumHitDistance;
        float allowedStart = 0, allowedArc = Mathf.Tau;
        if (reserveCompass)
        {
            // The compass and an arrow need separate native hit circles. Keep
            // the requested guide radius, but exclude its intersection with a
            // 62px clearance circle around the compass using the cosine rule.
            float cosine = (CircleRadius * CircleRadius + compassDistance * compassDistance
                - minimumHitDistance * minimumHitDistance) / (2 * CircleRadius * compassDistance);
            float halfForbidden = MathF.Acos(Mathf.Clamp(cosine, -1, 1));
            allowedStart = Mathf.PosMod(compassOffset.Angle() + halfForbidden, Mathf.Tau);
            allowedArc = Mathf.Tau - 2 * halfForbidden;
            for (int i = 0; i < count; i++)
            {
                var placement = _placements[i];
                float relative = Mathf.PosMod(placement.Bearing - allowedStart, Mathf.Tau);
                if (relative > allowedArc)
                    relative = relative - allowedArc <= Mathf.Tau - relative ? allowedArc : 0;
                _placements[i] = placement with { Bearing = allowedStart + relative };
            }
        }
        _placements.Sort(static (a, b) =>
        {
            int order = a.Bearing.CompareTo(b.Bearing);
            if (order != 0) return order;
            order = a.Arrow.Target.Kind.CompareTo(b.Arrow.Target.Kind);
            return order != 0 ? order : a.Arrow.Target.Id.CompareTo(b.Arrow.Target.Id);
        });
        // Cut the circular ordering at its largest free gap. Reserve a 62px
        // chord for independent 60px hit circles when room permits; dense
        // populations share the available circle without dropping a target.
        int start = 0;
        if (!reserveCompass)
        {
            float largestGap = -1;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                float gap = _placements[next].Bearing + (next == 0 ? Mathf.Tau : 0) - _placements[i].Bearing;
                if (gap > largestGap) { largestGap = gap; start = next; }
            }
        }
        float capacityStep = Mathf.Tau * .96f / count;
        if (reserveCompass) capacityStep = Math.Min(capacityStep, allowedArc / Math.Max(1, count - 1));
        float step = count == 1 ? 0 : Math.Min(2 * MathF.Asin(Math.Min(1, 31 / Math.Max(31, CircleRadius))), capacityStep);
        if (_placedAngles.Length < count) _placedAngles = new float[count];
        _spreadBlocks.Clear();
        float previous = -1;
        for (int i = 0; i < count; i++)
        {
            float bearing = _placements[(start + i) % count].Bearing;
            while (bearing < previous) bearing += Mathf.Tau;
            previous = bearing;
            _spreadBlocks.Add(new(i, i, bearing - i * step));
            // Merge only overlapping groups, centering each fan on its true
            // bearings rather than always pushing its arrows to one side.
            while (_spreadBlocks.Count > 1)
            {
                var right = _spreadBlocks[^1];
                var left = _spreadBlocks[^2];
                if (left.Mean <= right.Mean) break;
                _spreadBlocks.RemoveAt(_spreadBlocks.Count - 1);
                _spreadBlocks[^1] = new(left.First, right.Last, left.Sum + right.Sum);
            }
        }
        foreach (var group in _spreadBlocks)
            for (int i = group.First; i <= group.Last; i++) _placedAngles[i] = (float)group.Mean + i * step;
        float firstBase = _placedAngles[0];
        float lastBase = _placedAngles[count - 1] - (count - 1) * step;
        // Reserve the closing chord too: a narrow compass exclusion must not
        // let the first and last arrows overlap across the circular seam.
        float available = reserveCompass ? Math.Min(allowedArc, Mathf.Tau - step) - (count - 1) * step
            : Mathf.Tau - count * step;
        float lower = (firstBase + lastBase - available) / 2;
        if (reserveCompass)
            lower = Mathf.Clamp(lower, allowedStart,
                allowedStart + Math.Max(0, allowedArc - (count - 1) * step - available));
        for (int i = 0; i < count; i++)
        {
            float angle = _placedAngles[i];
            if (reserveCompass || lastBase - firstBase > available)
                angle = Mathf.Clamp(angle - i * step, lower, lower + available) + i * step;
            var arrow = _placements[(start + i) % count].Arrow;
            arrow.Position = CircleCenter + Vector2.FromAngle(angle) * CircleRadius - arrow.Size / 2;
        }
    }

    private void UpdateProcessing() => SetProcess(_root is not null && _root.IsVisibleInTree() && VisibleMarkerCount > 0);

    public override void _Process(double delta)
    {
        if (!_root.IsVisibleInTree() || VisibleMarkerCount == 0) { SetProcess(false); return; }
        _drawAge += (float)delta;
        if (_drawAge < 1f / 30) return;
        _phase = (_phase + _drawAge) % 60;
        _drawAge = 0;
        bool disabled = !CanInteract();
        foreach (var arrow in _arrows.Values)
            if (arrow.IsVisibleInTree()) { arrow.Disabled = disabled; arrow.SetPhase(_phase); }
        _compass.Disabled = disabled;
    }

    public override void _ExitTree() => SetProcess(false);
}

internal partial class NavigationGuideRing : Control
{
    private Vector2 _center;
    private float _radius;
    private FleetColor _nation;
    internal void SetCircle(Vector2 center, float radius, FleetColor nation)
    {
        if (_center == center && _radius == radius && _nation == nation) return;
        _center = center; _radius = radius; _nation = nation;
        QueueRedraw();
    }
    public override void _Draw()
    {
        // Retained concentric fringes: light starts at the ring and fades outward.
        for (int fringe = 0; fringe < 12; fringe++)
        {
            float fade = 1 - fringe / 12f;
            DrawArc(_center, _radius + fringe * 1.7f, 0, Mathf.Tau, 120,
                new Color(FleetPalette.Color(_nation).Lightened(.25f), .045f * fade * fade), 2.4f, true);
        }
        DrawArc(_center, _radius, 0, Mathf.Tau, 120, new Color(PapyrusStyle.Paper, .10f), .8f, true);
    }
}

internal partial class OffscreenNavigationArrow : Button
{
    internal bool Current { get; set; }
    internal OffscreenNavigationTarget Target { get; private set; }
    internal Vector2 Direction { get; private set; } = Vector2.Up;
    private FleetColor _nation;
    private Color _light;
    private float _phase;
    public override void _Ready()
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        SetProcess(false);
    }
    internal void Configure(OffscreenNavigationTarget target, FleetColor nation)
    {
        bool changed = Target != target || _nation != nation;
        Target = target; _nation = nation;
        _light = target.Kind switch
        {
            NavigationMarkerKind.Capture => new("65b7ed"),
            NavigationMarkerKind.Treasury => new("edc16c"),
            _ => new("ef7864")
        };
        TooltipText = target.Kind switch
        {
            NavigationMarkerKind.Capture => "Claim this harbor",
            NavigationMarkerKind.Treasury => "Awaken the sunken treasury",
            _ => "Under attack"
        };
        if (changed) QueueRedraw();
    }
    internal void SetDirection(Vector2 direction)
    {
        if (Direction == direction) return;
        Direction = direction;
        QueueRedraw();
    }
    internal void SetPhase(float phase) { _phase = phase; QueueRedraw(); }
    public override bool _HasPoint(Vector2 point) => point.DistanceSquaredTo(Size * .5f) <= 30 * 30;
    public override void _Draw()
    {
        float opacity = Disabled ? .36f : IsHovered() ? 1 : .84f;
        DrawSetTransform(Size * .5f, Direction.Angle());
        Vector2 P(float x, float y) => new(x, y);
        // Short outward strips provide the directional halo without a shader,
        // full-screen blending layer, or unbounded particle population.
        for (int strand = 0; strand < 9; strand++)
        {
            float y = (strand - 4) * 1.8f;
            float alpha = opacity * (.12f - Math.Abs(strand - 4) * .017f);
            DrawLine(P(-30, y * .45f), P(24, y), new Color(_light, alpha), 3.2f, true);
        }
        var parchment = new Color(PapyrusStyle.Paper, opacity);
        var ink = new Color(FleetPalette.Color(_nation).Darkened(.37f), opacity);
        DrawColoredPolygon(new[] { P(-5, -9), P(24, 0), P(-5, 9), P(2, 0) }, parchment);
        DrawColoredPolygon(new[] { P(1, -4), P(20, 0), P(1, 4), P(6, 0) }, ink);
        DrawLine(P(-5, -9), P(24, 0), new Color(PapyrusStyle.Bronze.Lightened(.30f), opacity), 1.3f, true);
        DrawLine(P(24, 0), P(-5, 9), new Color(PapyrusStyle.Bronze.Darkened(.32f), opacity), 1.4f, true);
        DrawLine(P(-20, 0), P(2, 0), ink, 2, true);
        NationOrnament.DrawMotif(this, P(-13, 0), 5.5f, _nation);
        for (int rune = 0; rune < 2; rune++)
        {
            float x = 8 + rune * 4;
            DrawLine(P(x, -2), P(x + 2, 0), new Color(PapyrusStyle.Paper, opacity * .75f), .65f, true);
            DrawLine(P(x + 2, 0), P(x, 2), new Color(PapyrusStyle.Paper, opacity * .75f), .65f, true);
        }
        // Six deterministic sparks drift from the circle's inside to its outside.
        for (int spark = 0; spark < 6; spark++)
        {
            float t = Mathf.PosMod(_phase * .16f + spark / 6f + (Target.Id % 11) * .037f, 1);
            float y = Mathf.Sin(spark * 2.1f) * 9 * (.4f + t * .6f);
            float alpha = MathF.Sin(t * Mathf.Pi) * opacity * .64f;
            var at = P(-30 + 61 * t, y);
            DrawCircle(at, 1 + spark % 2 * .35f, new Color(_light.Lightened(.22f), alpha));
            DrawLine(at - new Vector2(3, 0), at, new Color(_light, alpha * .6f), .6f, true);
        }
    }
}

internal partial class MothershipCompassButton : Button
{
    private FleetColor _nation;
    internal void SetNation(FleetColor nation) { if (_nation == nation) return; _nation = nation; QueueRedraw(); }
    public override void _Ready()
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        MouseEntered += QueueRedraw; MouseExited += QueueRedraw;
        SetProcess(false);
    }
    public override bool _HasPoint(Vector2 point) => point.DistanceSquaredTo(Size * .5f) <= 29 * 29;
    public override void _Draw()
    {
        var center = Size * .5f;
        float opacity = Disabled ? .40f : 1;
        DrawCircle(center + new Vector2(1, 2), 26, new Color("18333b", opacity * .45f));
        DrawCircle(center, 25, new Color(PapyrusStyle.Bronze.Darkened(.28f), opacity));
        DrawCircle(center + new Vector2(0, -.6f), 23, new Color(PapyrusStyle.Paper, opacity * .94f));
        DrawArc(center, 24, Mathf.Pi, Mathf.Tau, 40, new Color(PapyrusStyle.Paper.Lightened(.16f), opacity), 1.5f, true);
        DrawArc(center, 20, 0, Mathf.Tau, 48, new Color(PapyrusStyle.FaintInk, opacity * .50f), .9f, true);
        for (int tick = 0; tick < 16; tick++)
        {
            var direction = Vector2.FromAngle(tick * Mathf.Tau / 16);
            DrawLine(center + direction * (tick % 4 == 0 ? 16 : 18), center + direction * 21,
                new Color(PapyrusStyle.Ink, opacity * .63f), tick % 4 == 0 ? 1.2f : .7f, true);
        }
        var north = Vector2.FromAngle(-Mathf.Pi * .36f);
        var side = new Vector2(-north.Y, north.X);
        var nation = new Color(FleetPalette.Color(_nation).Darkened(.30f), opacity);
        DrawColoredPolygon(new[] { center + north * 18, center + side * 4, center, center - side * 4 }, nation);
        DrawColoredPolygon(new[] { center - north * 15, center + side * 4, center, center - side * 4 },
            new Color(PapyrusStyle.Bronze.Darkened(.16f), opacity));
        DrawLine(center - north * 15, center + north * 18, new Color(PapyrusStyle.Paper.Lightened(.10f), opacity * .72f), .7f, true);
        DrawCircle(center, 3, new Color(PapyrusStyle.Bronze, opacity));
        DrawCircle(center - new Vector2(.7f, .7f), 1.1f, new Color(PapyrusStyle.Paper, opacity));
        if (IsHovered() && !Disabled) DrawArc(center, 28, 0, Mathf.Tau, 56,
            new Color(FleetPalette.Color(_nation), .56f), 1.1f, true);
    }
}
