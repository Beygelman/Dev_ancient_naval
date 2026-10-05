using System;
using System.Collections.Generic;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Large finite fireworks or a bounded living fire along the lower screen.</summary>
internal partial class VictoryCelebration : Control
{
    private readonly record struct Spark(Vector2 Origin, Vector2 Velocity, float Born, float Life, Color Color);
    private readonly record struct BurstLight(Vector2 Origin, float Born, Color Color);
    private readonly List<Spark> _sparks = new(240);
    private readonly List<BurstLight> _lights = new(7);
    private readonly Vector2[] _flame = new Vector2[19];
    private readonly Random _random = new(9019);
    private float _age, _paintClock;
    private int _nextBurst;
    private bool _defeat;
    internal int ActiveSparkCount => _sparks.Count;
    internal int ActiveFlameCount => _defeat && IsProcessing() ? 24 : 0;

    public override void _Ready() => SetProcess(false);

    internal void Start()
    {
        Reset();
        _defeat = false;
        SetProcess(true);
        QueueRedraw();
    }

    internal void StartDefeat()
    {
        Reset();
        _defeat = true;
        SetProcess(true);
        QueueRedraw();
    }

    private void Reset()
    {
        _sparks.Clear();
        _lights.Clear();
        _age = _paintClock = 0;
        _nextBurst = 0;
    }

    internal void Stop()
    {
        Reset();
        _defeat = false;
        SetProcess(false);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
        {
            Stop();
            return;
        }
        _age += (float)delta;
        if (!_defeat)
        {
            _sparks.RemoveAll(spark => _age - spark.Born >= spark.Life);
            _lights.RemoveAll(light => _age - light.Born >= .6f);
            if (_nextBurst < 7 && _age >= .4f + _nextBurst * .78f) Burst(_nextBurst++);
        }
        _paintClock += (float)delta;
        if (_paintClock >= 1 / 30f)
        {
            _paintClock %= 1 / 30f;
            QueueRedraw();
        }
        if (!_defeat && _nextBurst == 7 && _sparks.Count == 0 && _lights.Count == 0)
            SetProcess(false);
    }

    private void Burst(int index)
    {
        bool left = index % 2 == 0;
        var origin = new Vector2(Size.X * (left ? .18f : .82f), Size.Y * (.20f + .10f * (index % 3)));
        var color = (index % 4) switch
        {
            1 => new Color("a8f2ff"),
            2 => new Color("fff2b3"),
            3 => new Color("ef90b6"),
            _ => new Color("ffd06c")
        };
        float scale = Math.Clamp(Size.Y / 720, .7f, 1.35f);
        _lights.Add(new(origin, _age, color));
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.Tau / 32 + (float)_random.NextDouble() * .045f;
            float speed = (125 + (float)_random.NextDouble() * 95) * scale;
            _sparks.Add(new(origin, Vector2.FromAngle(angle) * speed,
                _age, 1.6f + (float)_random.NextDouble() * .55f, color));
        }
    }

    public override void _Draw()
    {
        if (_defeat)
        {
            DrawBurningGround();
            return;
        }
        foreach (var light in _lights)
        {
            float t = (_age - light.Born) / .6f;
            float alpha = (1 - t) * .45f;
            DrawCircle(light.Origin, 26 + t * 42, new Color(light.Color, alpha * .18f));
            DrawArc(light.Origin, 14 + t * 108, 0, Mathf.Tau, 48, new Color(light.Color, alpha), 2, true);
        }
        foreach (var spark in _sparks)
        {
            float age = _age - spark.Born;
            float progress = age / spark.Life;
            var head = spark.Origin + spark.Velocity * age + new Vector2(0, 28 * age * age);
            float trailAge = Math.Max(0, age - .11f);
            var tail = spark.Origin + spark.Velocity * trailAge + new Vector2(0, 28 * trailAge * trailAge);
            float alpha = MathF.Pow(1 - progress, 1.2f);
            DrawLine(tail, head, new Color(spark.Color, alpha * .76f), 3.6f, true);
            DrawCircle(head, 12, new Color(spark.Color, alpha * .15f));
            DrawCircle(head, 6.3f, new Color(spark.Color, alpha * .46f));
            DrawCircle(head, 3.5f, new Color(spark.Color, alpha));
            DrawCircle(head, 1.8f, new Color(Colors.White, alpha));
        }
    }

    private void DrawBurningGround()
    {
        float appear = Math.Clamp(_age / .7f, 0, 1);
        // Layered ember haze leaves the text and parchment completely opaque in front.
        for (int strip = 0; strip < 9; strip++)
        {
            float y = Size.Y - (strip + 1) * 18;
            DrawRect(new(0, y, Size.X, 19), new Color(.59f, .16f, .035f,
                appear * (.23f - strip * .022f)));
        }
        var flame = _flame;
        for (int i = 0; i < 24; i++)
        {
            float x = (i + .5f) * Size.X / 24;
            float phase = _age * (1.3f + i % 5 * .17f) + i * 2.71f;
            float width = Math.Clamp(Size.X / 24 * .95f, 16, 68);
            float height = (60 + i * 29 % 91) * (.80f + MathF.Sin(phase) * .17f);
            float lean = MathF.Sin(phase * .7f) * width * .20f;
            var c = new Vector2(x, Size.Y + 12);
            var tip = new Vector2(lean - width * .04f + MathF.Sin(phase * 2.1f) * width * .08f, -height);
            for (int p = 0; p <= 9; p++)
                flame[p] = c + Bezier(new(-width * .56f, 0), new(-width * .70f, -height * .42f),
                    new(lean - width * .02f, -height * .72f), tip, p / 9f);
            for (int p = 1; p <= 9; p++)
                flame[9 + p] = c + Bezier(tip, new(lean + width * .05f, -height * .58f),
                    new(width * .64f, -height * .32f), new(width * .54f, 0), p / 9f);
            DrawColoredPolygon(flame, new Color(.97f, .22f, .035f, appear * .72f));
            for (int p = 0; p < flame.Length; p++)
                flame[p] = c + (flame[p] - c) * new Vector2(.49f, .66f);
            DrawColoredPolygon(flame, new Color(1, .63f, .14f, appear * .92f));
            DrawCircle(c + new Vector2(0, -11), width * .12f, new Color(1, .91f, .49f, appear));
        }
        for (int i = 0; i < 18; i++)
        {
            float phase = (_age * .12f + i * .618f) % 1;
            float x = (i + .4f) * Size.X / 18 + MathF.Sin(_age + i) * 14;
            var p = new Vector2(x, Size.Y - phase * (180 + i % 5 * 16));
            DrawLine(p, p + new Vector2(1, 5), new Color(1, .66f, .2f, appear * (1 - phase)), 1.6f, true);
        }
    }

    private static Vector2 Bezier(Vector2 from, Vector2 controlA, Vector2 controlB, Vector2 to, float t)
    {
        float u = 1 - t;
        return from * (u * u * u) + controlA * (3 * u * u * t)
            + controlB * (3 * u * t * t) + to * (t * t * t);
    }
}
