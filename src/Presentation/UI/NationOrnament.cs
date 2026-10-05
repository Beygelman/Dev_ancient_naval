using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Nation-specific vector ink, reused on parchment and clay.</summary>
internal partial class NationOrnament : Control
{
    internal FleetColor Color { get; set; }
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        var ink = new Color(FleetPalette.Color(Color).Darkened(.25f), .85f);
        var center = Size * .5f;
        DrawMotif(this, center, Math.Min(24, Size.Y * .30f), Color);
        for (int i = 1; i <= 3; i++)
        {
            float span = Math.Min(Size.X * .12f, 38) * i + 35;
            foreach (int side in new[] { -1, 1 })
            {
                var at = center + new Vector2(side * span, 0);
                DrawMotif(this, at, 5 + i % 2, Color);
                DrawLine(at + new Vector2(-10, 14), at + new Vector2(10, 14), ink, 1, true);
            }
        }
    }
    internal static void DrawMotif(CanvasItem canvas, Vector2 c, float s, FleetColor color)
    {
        var ink = new Color(FleetPalette.Color(color).Darkened(.25f), .9f);
        Vector2 P(float x, float y) => c + new Vector2(x, y) * s;
        void Line(float x, float y, float a, float b, float w = 1) => canvas.DrawLine(P(x, y), P(a, b), ink, w, true);
        void Diamond(float x, float y, float size)
        {
            canvas.DrawPolyline(new[] { P(x, y - size), P(x + size * .55f, y), P(x, y + size), P(x - size * .55f, y), P(x, y - size) }, ink, 1.5f, true);
        }
        switch (color)
        {
            case FleetColor.Blue:
                canvas.DrawArc(c, s * .42f, 0, Mathf.Tau, 32, ink, 2, true);
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.Tau / 12;
                    Line(MathF.Cos(a) * .60f, MathF.Sin(a) * .60f, MathF.Cos(a), MathF.Sin(a));
                }
                break;
            case FleetColor.Purple:
                Line(-1, .7f, 0, -1); Line(0, -1, 1, .7f); Line(1, .7f, -1, .7f);
                Line(-.6f, .3f, 0, -.6f); Line(0, -.6f, .6f, .3f); Line(.6f, .3f, -.6f, .3f);
                Diamond(0, .7f, .23f);
                break;
            case FleetColor.Yellow:
                Diamond(0, -.30f, .68f);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * .35f;
                    canvas.DrawArc(P(x, .44f), s * .36f, Mathf.Pi, Mathf.Tau, 16, ink, 1.7f, true);
                }
                Line(-1, .75f, 1, .75f, 2);
                break;
            case FleetColor.White:
                // A weighty serif rune with a distinct bowl and diagonal leg.
                Line(-.55f, 1, -.55f, -1, 3.4f); Line(-.85f, -1, .10f, -1, 2.5f); Line(-.85f, 1, -.2f, 1, 2.5f);
                canvas.DrawArc(P(-.05f, -.45f), s * .57f, -Mathf.Pi / 2, Mathf.Pi / 2, 24, ink, 3, true);
                Line(-.55f, .12f, .03f, .12f, 3); Line(-.02f, .12f, .78f, 1, 3.4f); Line(.5f, 1, .98f, 1, 2.5f);
                break;
            case FleetColor.Green:
                Line(0, 1, 0, -.85f, 2.4f);
                for (int i = 0; i < 3; i++)
                {
                    float y = .15f - i * .4f;
                    Line(0, y, -.7f, y - .4f, 1.8f); Line(0, y, .7f, y - .4f, 1.8f);
                }
                Line(0, .7f, -.6f, 1); Line(0, .7f, .6f, 1);
                break;
            default:
                Diamond(0, 0, 1); Diamond(-.7f, .3f, .65f); Diamond(.7f, .3f, .65f);
                Line(0, -1, 0, 1); Line(-.7f, -.35f, -.7f, .95f); Line(.7f, -.35f, .7f, .95f);
                break;
        }
    }
}
