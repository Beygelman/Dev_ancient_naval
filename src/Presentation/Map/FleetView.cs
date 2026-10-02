using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView : Node2D
{
    public BattleState Battle { get; set; } = null !;
    public IsometricProjection Projection { get; set; } = null !;
    public int? SelectedId { get; set; }
    public Vector2? ProjectilePosition { get; private set; }
    public Func<Core.Grid.GridPosition, System.Threading.Tasks.Task>? FocusTarget { get; set; }

    private readonly Dictionary<int, ShipSnapshot> _snapshots = new();
    private readonly HashSet<int> _suppressed = new();
    private int _movingId;
    private bool _movingVisible;
    private Vector2 _movingPosition;
    private ShipSnapshot? _movingShip;
    private Vector2? _muzzle, _impact;
    private float _impactSize;
    private string _feedback = "";
    private Color _feedbackColor = new("ff8f85");
    private Vector2 _feedbackPosition;
    private float _feedbackRise;
    private Vector2? _blastCenter;
    private float _blastProgress;
    private readonly Dictionary<int, (Vector2 Position, string Text)> _blastDamage = new();
    internal IReadOnlyCollection<int> AnimatedShipIds => _snapshots.Keys;
    internal string CombatFeedback => _feedback;

    public override void _Draw()
    {
        using var trace = DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Fleet.Draw");
        using (Diagnostics.PerformanceTrace.Measure("Fleet.Water.Draw"))
            DrawWaterEffects();
        using (Diagnostics.PerformanceTrace.Measure("Fleet.Hulls.Refresh"))
            RefreshHullCanvases();
    }

    private void DrawFrontEffects()
    {
        if (_muzzle is { } muzzle)
        {
            Ink.DrawCircle(muzzle, 10, new Color(1, 0.65f, 0.2f, 0.6f));
            Ink.DrawCircle(muzzle, 5, new Color("fff3b0"));
        }

        DrawAirEffects();
        if (_projectiles.Count == 0 && ProjectilePosition is { } projectile)
        {
            Ink.DrawCircle(projectile + new Vector2(-3, -2), 6, new Color(1, 0.7f, 0.3f, 0.3f));
            Ink.DrawCircle(projectile, 3.5f, new Color("ffebae"));
        }

        if (_impact is { } impact)
        {
            Ink.DrawCircle(impact, _impactSize, new Color(1, 0.57f, 0.2f, Math.Max(0, 0.85f - _impactSize / 38)));
            Ink.DrawArc(impact, _impactSize + 4, 0, Mathf.Tau, 24, new Color("ffdb9d"), 2, true);
        }

        if (_blastCenter is { } blast)
        {
            Ink.DrawSetTransform(blast, 0, new Vector2(1, .5f));
            Ink.DrawCircle(Vector2.Zero, 20 + 100 * _blastProgress, new Color(1, .66f, .25f, .25f * (1 - _blastProgress)));
            Ink.DrawArc(Vector2.Zero, 20 + 100 * _blastProgress, 0, Mathf.Tau, 48, new Color(1, .85f, .55f, 1 - _blastProgress), 3, true);
            Ink.DrawSetTransform(Vector2.Zero);
        }

        foreach (var label in _blastDamage.Values)
            DrawHealthFeedback(label.Position, label.Text, new Color("ff8f85"));
        if (_feedback.Length > 0)
            DrawHealthFeedback(_feedbackPosition, _feedback, _feedbackColor);
        DrawIncome();
    }

    private void DrawHealthFeedback(Vector2 anchor, string text, Color color)
    {
        var font = ThemeDB.FallbackFont;
        float width = font.GetStringSize(text, fontSize: 23).X;
        Ink.DrawString(font, anchor + new Vector2(-width / 2, -22 - _feedbackRise), text, fontSize: 23, modulate: color);
    }

    // Rebuilt from current visibility on every draw: no stale fog or health cache.
    // The list retains capacity; the sequence index preserves OrderBy's stable ties.
    internal readonly record struct DrawEntry(ShipSnapshot Ship, Vector2 Center, int Sequence);
    private readonly List<DrawEntry> _drawOrder = new();
    private static readonly Comparison<DrawEntry> DepthComparison = (left, right) =>
    {
        int depth = left.Center.Y.CompareTo(right.Center.Y);
        return depth != 0 ? depth : left.Sequence.CompareTo(right.Sequence);
    };
    internal IReadOnlyList<DrawEntry> PrepareDrawOrder()
    {
        _drawOrder.Clear();
        for (int i = 0; i < Battle.Ships.Count; i++)
        {
            var ship = Battle.Ships[i];
            if (ship.Owner != Side.Player && !Battle.Vision.IsVisible(Side.Player, ship.Position))
            {
                continue;
            }

            if (_suppressed.Contains(ship.Id) || ship.Id == _movingId || _snapshots.ContainsKey(ship.Id))
            {
                continue;
            }

            AddDrawEntry(ShipSnapshot.From(ship));
        }

        foreach (var snapshot in _snapshots.Values)
        {
            AddDrawEntry(snapshot);
        }

        _drawOrder.Sort(DepthComparison);
        return _drawOrder;
    }

    private void AddDrawEntry(ShipSnapshot snapshot)
    {
        _drawOrder.Add(new(snapshot, Projection.GridToWorld(snapshot.Position), _drawOrder.Count));
    }
}
