using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private float _clock, _wakeClock, _sailingPitch, _sailingBank;
    private BattleState? _visualBattle;
    private readonly List<Smoke> _smoke = new();
    private readonly List<Ripple> _ripples = new();
    private readonly List<Debris> _debris = new();
    private readonly List<WaterMist> _mist = new();
    private sealed record Debris(Vector2 Origin, Vector2 Velocity, float Born, float Size);
    private sealed record WaterMist(Vector2 Center, float Born, float Strength);
    private readonly Dictionary<int, Impulse> _impulses = new();
    private readonly List<int> _expiredImpulses = new();
    private readonly List<(Vector2 Point, Vector2 Previous, float Radius, Vector2 Shadow)> _projectiles = new();
    private readonly SeaGeometryBatch _waterEffectsBatch = new();
    private sealed record Smoke(Vector2 Origin, Vector2 Velocity, float Radius, float Born, float Lifetime, bool Dark);
    private sealed record Ripple(Vector2 Center, float Strength, float Born, float Angle, bool Wake);
    private sealed record Impulse(Vector2 Push, float Born, float Duration, float Roll);
    internal int ActiveProjectileCount => _projectiles.Count;
    internal int EffectCount => _smoke.Count + _ripples.Count + _debris.Count + _mist.Count;
    internal Vector2 AnimatedPosition => _movingPosition;

    internal static float MotionProgress(float t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (10 + t * (-15 + 6 * t));
    }

    private void EnsureVisualBattle()
    {
        if (!ReferenceEquals(_visualBattle, Battle))
        {
            _visualBattle = Battle;
            foreach (var hull in _hulls.Values)
            {
                hull.Canvas.GetParent()?.RemoveChild(hull.Canvas);
                hull.Badge.GetParent()?.RemoveChild(hull.Badge);
                hull.Canvas.QueueFree();
                hull.Badge.QueueFree();
            }
            _hulls.Clear();
            _smoke.Clear();
            _ripples.Clear();
            _debris.Clear();
            _mist.Clear();
            _impulses.Clear();
            _deckAngles.Clear();
            _barrelAngles.Clear();
            _incomeLabels.Clear();
            _clock = 0;
        }
    }

    public override void _Process(double delta)
    {
        EnsureVisualBattle();
        _clock += (float)delta;
        _incomeLabels.RemoveAll(label => _clock - label.Born > 2.2f);
        _smoke.RemoveAll(p => _clock - p.Born > p.Lifetime);
        _ripples.RemoveAll(p => _clock - p.Born > 1.8f);
        _debris.RemoveAll(p => _clock - p.Born > .9f);
        _mist.RemoveAll(p => _clock - p.Born > 2.1f);
        if (_impulses.Count > 0)
        {
            _expiredImpulses.Clear();
            foreach (var(id, impulse)in _impulses)
            {
                if (_clock - impulse.Born > impulse.Duration)
                {
                    _expiredImpulses.Add(id);
                }
            }

            foreach (int id in _expiredImpulses)
            {
                _impulses.Remove(id);
            }
        }

        QueueRedraw(); // Only the small fleet/effect layer, never the map mesh.
    }

    private (Vector2 Offset, float Roll) HullMotion(ShipSnapshot ship)
    {
        if (ship.Class is ShipClass.FishingDock or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)
            return (Vector2.Zero, 0);
        float size = ShipVisualProfile.For(ship.Class).Size;
        float phase = _clock * (1.4f / size) + ship.Id * 2.37f;
        var offset = new Vector2(MathF.Sin(phase * .7f) * .25f, MathF.Sin(phase) * 1.6f);
        float roll = MathF.Sin(phase * .83f) * .025f / size;
        if (ship.Id == _movingId)
        {
            roll += _sailingPitch + _sailingBank;
            offset.Y += MathF.Abs(_sailingPitch) * 20;
        }

        if (_impulses.TryGetValue(ship.Id, out var impulse))
        {
            float t = Math.Clamp((_clock - impulse.Born) / impulse.Duration, 0, 1);
            float spring = MathF.Exp(-t * 5) * MathF.Cos(t * 9);
            offset += impulse.Push * spring;
            roll += impulse.Roll * spring;
        }

        return (offset, roll);
    }

    private void PushHull(int id, Vector2 direction, float amount, float size)
    {
        _impulses[id] = new(direction * amount * 1.4f / size, _clock, .7f + size * .2f, direction.X * .075f / size);
    }

    private void EmitRipple(Vector2 center, float strength, float angle = 0, bool wake = false)
    {
        if (strength <= 0)
            return;
        if (_ripples.Count >= 100)
            _ripples.RemoveAt(0);
        _ripples.Add(new(center, strength, _clock, angle, wake));
    }

    private void EmitSmoke(Vector2 center, Vector2 direction, float strength, bool impact)
    {
        for (int i = 0; i < (impact ? 8 : 6); i++)
        {
            if (_smoke.Count >= 180)
                _smoke.RemoveAt(0);
            float side = MathF.Sin(i * 4.1f + _clock * 3);
            var velocity = direction * (12 + i * 3) + direction.Orthogonal() * side * 8 + new Vector2(2, -10);
            _smoke.Add(new(center, velocity, (2.4f + i * .38f) * strength, _clock, 1.1f + i * .11f, impact));
        }
    }

    private void GunEffect(ShipSnapshot ship, Vector2 muzzle, Vector2 direction, bool mortar)
    {
        var profile = ShipVisualProfile.For(ship.Class);
        PushHull(ship.Id, -direction, mortar ? 4.8f : 3.5f, profile.Size);
        EmitSmoke(muzzle, direction, mortar ? 1.3f : .85f, false);
        EmitRipple(Projection.GridToWorld(ship.Position), profile.Wake * (mortar ? 1.1f : .8f));
    }

    private void HitEffect(ShipSnapshot? target, Vector2 point, Vector2 direction, bool heavy)
    {
        float scale = target is null ? .9f : ShipVisualProfile.For(target.Class).Size;
        if (target is not null)
            PushHull(target.Id, direction, heavy ? 5.5f : 3.8f, scale);
        EmitSmoke(point, direction * .25f, heavy ? 1.35f : .9f, true);
        if (_mist.Count >= 32)
            _mist.RemoveAt(0);
        _mist.Add(new(target is null ? point : Projection.GridToWorld(target.Position), _clock, scale));
        for (int i = 0; i < 7; i++)
        {
            if (_debris.Count >= 100)
                _debris.RemoveAt(0);
            float phase = i * 2.4f + _clock * 11;
            _debris.Add(new(point, new Vector2(MathF.Cos(phase) * (20 + i * 5), -25 - i * 7), _clock, 1.5f + i % 3));
        }

        if (target is not null)
            EmitRipple(Projection.GridToWorld(target.Position), scale * (heavy ? 1.2f : .75f));
    }

    private void DrawWaterEffects()
    {
        _waterEffectsBatch.Clear();
        foreach (var ball in _projectiles)
        {
            Ink.DrawSetTransform(ball.Shadow, 0, new Vector2(1.2f, .5f));
            Ink.DrawCircle(Vector2.Zero, ball.Radius * 1.35f, new Color(0, .04f, .06f, .24f));
            Ink.DrawSetTransform(Vector2.Zero);
        }

        foreach (var mist in _mist)
        {
            float t = (_clock - mist.Born) / 2.1f;
            Ink.DrawSetTransform(mist.Center, 0, new Vector2(1, .4f));
            for (int i = 0; i < 6; i++)
                Ink.DrawCircle(Vector2.FromAngle(i * Mathf.Tau / 6) * (6 + t * 19) * mist.Strength, (4 + t * 9) * mist.Strength, new Color(.8f, .85f, .79f, (1 - t) * .11f));
            Ink.DrawSetTransform(Vector2.Zero);
        }

        foreach (var ripple in _ripples)
        {
            float age = (_clock - ripple.Born) / 1.8f;
            float radius = (6 + 25 * age) * ripple.Strength;
            float alpha = Math.Min(1, age * 8) * (1 - age) * .25f;
            if (ripple.Wake)
            {
                // Two straight divergent arms leave a Kelvin-style V behind the keel.
                var forward = Vector2.FromAngle(ripple.Angle);
                var back = -forward;
                var side = forward.Orthogonal();
                float length = (12 + age * 26) * ripple.Strength;
                var origin = ripple.Center + back * age * 7;
                for (int arm = -1; arm <= 1; arm += 2)
                {
                    var start = origin + side * arm * (3 + age * 4) * ripple.Strength;
                    var end = origin + back * length + side * arm * length * .36f;
                    _waterEffectsBatch.Line(start, end, new Color(.77f, .93f, .94f, alpha));
                    _waterEffectsBatch.Line(start + back * 4, end + back * 4, new Color(.64f, .84f, .87f, alpha * .35f));
                }
            }
            else
            {
                var previous = ripple.Center + new Vector2(radius, 0);
                for (int step = 1; step <= 32; step++)
                {
                    float angle = step * Mathf.Tau / 32;
                    var next = ripple.Center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .48f);
                    _waterEffectsBatch.Line(previous, next, new Color(.8f, .93f, .94f, alpha));
                    previous = next;
                }
            }
        }
        _waterEffectsBatch.Submit(Ink, 1.15f);
    }

    private void DrawAirEffects()
    {
        foreach (var chip in _debris)
        {
            float age = _clock - chip.Born;
            var point = chip.Origin + chip.Velocity * age + new Vector2(0, 65 * age * age);
            Ink.DrawSetTransform(point, age * 9 + chip.Size);
            Ink.DrawRect(new Rect2(-chip.Size, -1, chip.Size * 2, 1.7f), new Color(.76f, .60f, .36f, 1 - age / .9f));
            Ink.DrawSetTransform(Vector2.Zero);
        }

        foreach (var smoke in _smoke)
        {
            float age = _clock - smoke.Born, t = age / smoke.Lifetime;
            var point = smoke.Origin + smoke.Velocity * (1 - MathF.Exp(-age * 1.6f)) / 1.6f + new Vector2(0, -age * age * 3);
            float alpha = MathF.Sin(Math.Min(1, t * 7) * Mathf.Pi * .5f) * (1 - t) * .38f;
            var color = smoke.Dark ? new Color(.42f, .47f, .46f, alpha) : new Color(.8f, .81f, .72f, alpha);
            Ink.DrawCircle(point, smoke.Radius * (1 + t * 2.1f), color);
        }

        foreach (var ball in _projectiles)
        {
            Ink.DrawLine(ball.Previous, ball.Point, new Color(.8f, .76f, .61f, .38f), ball.Radius * .8f, true);
            Ink.DrawCircle(ball.Point, ball.Radius + 1, new Color(.89f, .73f, .45f, .2f));
            Ink.DrawCircle(ball.Point, ball.Radius, new Color("263239"));
            Ink.DrawCircle(ball.Point + new Vector2(-.7f, -.8f), ball.Radius * .36f, new Color("a0a29a"));
        }
    }
}
