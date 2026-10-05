using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>One bounded, pendulous three-dimensional ornament, with no simulation randomness.</summary>
internal partial class NationBeads : Node2D
{
    private float _time, _paintClock;
    internal FleetColor Nation { get; set; }
    internal float ArtScale { get; set; } = 1;
    internal float SwingPhase => _time;
    internal Vector2 PendantPosition => Project(1);

    internal void SetSwinging(bool enabled)
    {
        SetProcess(enabled);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _paintClock += (float)delta;
        if (_paintClock < 1f / 30) return;
        _paintClock = 0;
        QueueRedraw();
    }

    private Vector2 Project(float along)
    {
        float swing = MathF.Sin(_time * 1.65f) * .14f;
        float depth = MathF.Sin(_time * 1.13f + .7f) * .12f;
        float length = 76 * ArtScale;
        return new Vector2(MathF.Sin(swing) * length * along + MathF.Sin(along * 2.6f) * 3 * depth,
            MathF.Cos(swing) * length * along * (1 + depth * .15f));
    }

    public override void _Draw()
    {
        float scale = ArtScale;
        Color thread = Nation switch
        {
            FleetColor.Red => new Color("242024"),
            FleetColor.Purple => new Color("715091"),
            FleetColor.Yellow or FleetColor.White => new Color("f0eadb"),
            FleetColor.Green => new Color("47613b"),
            _ => new Color("b18836")
        };
        Span<Vector2> cord = stackalloc Vector2[17];
        for (int i = 0; i < cord.Length; i++) cord[i] = Project(i / 16f);
        DrawPolyline(cord, thread.Darkened(.25f), (Nation == FleetColor.Red ? 2.5f : 1.5f) * scale, true);
        DrawPolyline(cord, new Color(thread.Lightened(.3f), .65f), .6f * scale, true);
        for (int i = 0; i < 9; i++)
        {
            float t = .12f + i * .084f;
            Vector2 at = Project(t);
            float twist = MathF.Cos(_time * 1.13f + i * .61f);
            float depth = .88f + twist * .12f;
            switch (Nation)
            {
                case FleetColor.Red:
                    Pearl(at, 3.7f * scale * depth, new Color("ede8d7"));
                    if (i % 3 == 1) Shell(at + new Vector2(3 * scale, 1), 5 * scale, new Color("d6c399"), twist);
                    break;
                case FleetColor.Blue:
                    Pearl(at, 4.2f * scale * depth, new Color("dec15f"));
                    break;
                case FleetColor.Purple:
                    if (i >= 3) Triangle(at, 5 * scale * depth, new Color("b9a0d4"), twist);
                    else Pearl(at, 2.1f * scale * depth, new Color("78519d"));
                    break;
                case FleetColor.White:
                    Pearl(at, 3.9f * scale * depth, new Color("e9eee5"));
                    break;
                case FleetColor.Yellow:
                    Pearl(at, 3.5f * scale * depth, new Color("a6aaa2"));
                    break;
                default:
                    Pearl(at, 3.5f * scale * depth, new Color("95845a"));
                    if (i % 3 == 1) Leaf(at, 5.2f * scale, twist);
                    break;
            }
        }
        Vector2 pendant = PendantPosition;
        float turn = MathF.Sin(_time * 1.13f + .5f);
        float flatten = .72f + .23f * MathF.Abs(MathF.Cos(_time * 1.13f + .5f));
        DrawSetTransform(pendant, MathF.Sin(_time * 1.65f) * .10f, new Vector2(scale * flatten, scale));
        switch (Nation)
        {
            case FleetColor.Red: Crystal(Vector2.Zero, 10); break;
            case FleetColor.Blue:
                Pearl(Vector2.Zero, 7, new Color("efd276"));
                for (int i = 0; i < 8; i++)
                {
                    var ray = Vector2.FromAngle(i * Mathf.Tau / 8);
                    DrawLine(ray * 8, ray * 11, new Color("d6aa48"), 1.3f, true);
                }
                break;
            case FleetColor.Purple: Triangle(Vector2.Zero, 12, new Color("b596d7"), turn); break;
            case FleetColor.White:
                DrawCircle(new(1.5f, 1.8f), 10, new Color("a8afa7"));
                DrawCircle(Vector2.Zero, 10, new Color("e8e9dd"));
                DrawArc(Vector2.Zero, 9, 0, Mathf.Tau, 30, new Color("c2c3b8"), .7f, true);
                DrawRune();
                break;
            case FleetColor.Yellow:
                Shell(Vector2.Zero, 11, new Color("e9dfc4"), turn);
                DrawBird();
                break;
            default:
                Leaf(Vector2.Zero, 11, turn);
                Leaf(new(4, 6), 8, -turn);
                break;
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void Pearl(Vector2 at, float radius, Color baseColor)
    {
        DrawCircle(at + new Vector2(radius * .15f, radius * .18f), radius, baseColor.Darkened(.3f));
        DrawCircle(at + new Vector2(-radius * .13f, -radius * .16f), radius * .87f, baseColor);
        DrawCircle(at + new Vector2(-radius * .31f, -radius * .39f), radius * .26f, baseColor.Lightened(.65f));
        DrawArc(at, radius * .75f, .2f, 1.3f, 9, new Color(baseColor.Darkened(.4f), .55f), .7f, true);
    }

    private void Shell(Vector2 at, float size, Color tint, float twist)
    {
        float w = size * (.65f + MathF.Abs(twist) * .22f);
        var basePoint = at + new Vector2(0, size * .52f);
        DrawColoredPolygon(new[] { basePoint, at + new Vector2(-w, -size * .2f),
            at + new Vector2(-w * .45f, -size * .65f), at + new Vector2(w * .55f, -size * .65f),
            at + new Vector2(w, -size * .14f) }, tint);
        for (int i = -2; i <= 2; i++)
            DrawLine(basePoint, at + new Vector2(i * w / 2.5f, -size * .49f), tint.Darkened(.3f), .7f, true);
    }

    private void Triangle(Vector2 at, float size, Color tint, float turn)
    {
        float width = size * (.58f + MathF.Abs(turn) * .16f);
        Vector2 top = at + new Vector2(0, -size * .7f);
        Vector2 left = at + new Vector2(-width, size * .52f);
        Vector2 right = at + new Vector2(width, size * .52f);
        DrawColoredPolygon(new[] { top, left, right }, tint.Darkened(.22f));
        DrawColoredPolygon(new[] { top, left, at }, tint.Lightened(.23f));
        DrawLine(top, at, tint.Lightened(.6f), .7f, true);
    }

    private void Crystal(Vector2 at, float size)
    {
        Vector2 top = at + new Vector2(0, -size);
        Vector2 right = at + new Vector2(size * .55f, 0);
        Vector2 bottom = at + new Vector2(0, size);
        Vector2 left = at + new Vector2(-size * .55f, 0);
        DrawColoredPolygon(new[] { top, right, bottom, left }, new Color("a92c47"));
        DrawColoredPolygon(new[] { top, at, left }, new Color("f18b96"));
        DrawColoredPolygon(new[] { at, right, bottom }, new Color("762038"));
        DrawLine(top, at, new Color("ffe3df"), 1.1f, true);
    }

    private void Leaf(Vector2 at, float size, float turn)
    {
        float w = size * (.37f + MathF.Abs(turn) * .15f);
        DrawColoredPolygon(new[] { at + new Vector2(0, -size), at + new Vector2(w, -size * .2f),
            at + new Vector2(0, size * .65f), at + new Vector2(-w, 0) }, new Color("679657"));
        DrawLine(at + new Vector2(0, -size * .8f), at + new Vector2(0, size * .85f), new Color("a8bf74"), .8f, true);
    }

    private void DrawRune()
    {
        var red = new Color("a73744");
        DrawLine(new(-4, 6), new(-4, -6), red, 2.4f, true);
        DrawLine(new(-6, -6), new(1, -6), red, 1.8f, true);
        DrawLine(new(-6, 6), new(-2, 6), red, 1.8f, true);
        DrawArc(new(0, -2.8f), 3.5f, -Mathf.Pi / 2, Mathf.Pi / 2, 16, red, 2.1f, true);
        DrawLine(new(-4, .7f), new(0, .7f), red, 2.1f, true);
        DrawLine(new(0, .7f), new(5, 6), red, 2.4f, true);
        DrawLine(new(3, 6), new(6, 6), red, 1.8f, true);
    }

    private void DrawBird()
    {
        var ink = new Color("75614d");
        DrawArc(new(0, -1), 3, 2.7f, 5.7f, 18, ink, 1.1f, true);
        DrawLine(new(2.5f, -2.6f), new(6, -1.6f), ink, 1.1f, true);
        DrawLine(new(6, -1.6f), new(2.2f, -.5f), ink, 1.1f, true);
        DrawCircle(new(1, -2), .7f, ink);
        DrawLine(new(-2, 0), new(-1, 4), ink, 1.1f, true);
    }
}
