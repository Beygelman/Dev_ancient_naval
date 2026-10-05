using System;
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>Static embossed faction wax with a tied cord above the selected object's paper.</summary>
internal partial class ObjectWaxSeal : Control
{
    internal ActionSymbol Symbol { get; set; }
    internal Color Wax { get; set; } = PapyrusStyle.Bronze;
    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, Vector2.One * 1.15f);
        var c = new Vector2(25, 29);
        var cord = new Color("79684d");
        DrawPolyline(new[] { new Vector2(9, -14), new(13, 0), new(22, 15), c }, cord, 1.8f, true);
        DrawArc(new(11, 3), 5, -.5f, 5.5f, 16, cord, 1.2f, true);
        DrawLine(new(9, 3), new(3, 12), cord, 1.4f, true);
        DrawCircle(c + new Vector2(3, 4), 24, new Color("574027", .20f));
        var edge = new Vector2[49];
        for (int i = 0; i < edge.Length; i++)
        {
            float a = i * Mathf.Tau / 48;
            float radius = 23 + 1.1f * MathF.Sin(a * 7) + .5f * MathF.Cos(a * 13);
            edge[i] = c + Vector2.FromAngle(a) * radius;
        }
        DrawColoredPolygon(edge, Wax.Darkened(.24f));
        DrawCircle(c - new Vector2(.7f, 1.7f), 20, Wax);
        DrawCircle(c, 17, Wax.Darkened(.10f));
        DrawArc(c - new Vector2(1, 1), 19, 3.35f, 5.4f, 30, new Color(Wax.Lightened(.42f), .75f), 2, true);
        DrawArc(c + new Vector2(1, 1), 18, .15f, 2.8f, 30, new Color(Wax.Darkened(.48f), .4f), 1.4f, true);
        for (int i = 0; i < 32; i++)
        {
            float a = i * 2.4f, radius = 11 + i % 9;
            var at = c + Vector2.FromAngle(a) * radius;
            DrawLine(at, at + new Vector2(.8f, -.3f), new Color(Wax.Lightened(.25f), .34f), .6f, true);
        }
        NavalGlyphArt.Draw(this, c + new Vector2(.8f, 1), Symbol, new Color(Wax.Lightened(.65f), .75f), 1.75f);
        NavalGlyphArt.Draw(this, c, Symbol, Wax.Darkened(.61f), 1.75f);
    }
}

/// <summary>A thin vertical margin ornament uses the observed object's nation, never a radar identity.</summary>
internal partial class ObjectCardOrnament : Control
{
    internal FleetColor? Nation { get; set; }
    internal Color Ink { get; set; }
    public override void _Draw()
    {
        // PanelContainer places this overlay in the 31px content inset. Draw back in the paper margin.
        const float x = -24;
        DrawLine(new(x, 7), new(x, Size.Y - 7), new Color(Ink, .32f), .8f, true);
        for (float y = 21; y < Size.Y - 10; y += 42)
        {
            if (Nation is { } faction) NationOrnament.DrawMotif(this, new(x, y), 9.5f, faction);
            else
            {
                DrawPolyline(new[] { new Vector2(x, y - 7), new(x + 5, y), new(x, y + 7), new(x - 5, y), new(x, y - 7) },
                    new Color(Ink, .74f), 1.2f, true);
                DrawLine(new(x - 4, y - 3), new(x + 4, y + 3), Ink, 1, true);
            }
        }
    }
}
