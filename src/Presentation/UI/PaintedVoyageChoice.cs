using System;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.UI;

internal enum VoyageMotif { None, Rival, Difficulty, World, Size, Hand }

/// <summary>Native accessible choice with ink artwork instead of a rectangular button skin.</summary>
internal partial class PaintedVoyageChoice : Button
{
    internal static readonly Color Burgundy = new("763343");
    internal VoyageMotif Motif { get; init; }
    internal int Variant { get; init; }
    internal bool Marked { get; private set; }
    private float _reveal = 1;
    internal void Mark(bool selected)
    {
        if (Marked == selected) return;
        Marked = selected;
        _reveal = selected ? 0 : 1;
        QueueRedraw();
        SetProcess(selected);
    }
    public override void _Ready()
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        AddThemeColorOverride("font_color", Burgundy);
        AddThemeColorOverride("font_hover_color", Burgundy.Lightened(.12f));
        AddThemeColorOverride("font_pressed_color", Burgundy);
        AddThemeFontSizeOverride("font_size", 14);
        Alignment = HorizontalAlignment.Center;
        if (Motif != VoyageMotif.None && Motif != VoyageMotif.Hand && Motif != VoyageMotif.Rival)
        {
            var caption = new Label { Text = Text, MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            Text = "";
            caption.AddThemeFontSizeOverride("font_size", 12);
            caption.AddThemeColorOverride("font_color", Burgundy);
            AddChild(caption);
            caption.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
            caption.OffsetTop = -30;
        }
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        Resized += QueueRedraw;
        SetProcess(false);
    }
    public override void _Process(double delta)
    {
        _reveal = Math.Min(1, _reveal + (float)delta / .24f);
        QueueRedraw();
        if (_reveal == 1) SetProcess(false);
    }
    public override void _Draw()
    {
        var ink = new Color(Burgundy, Motif == VoyageMotif.Rival && !Marked ? .18f : .86f);
        // Uneven translucent bristle strokes make the plate feel painted on the
        // same sheet; none of the native rectangular state backgrounds are used.
        if (Motif != VoyageMotif.Hand)
            for (int fiber = 0; fiber < 19; fiber++)
            {
                float y = Size.Y * .23f + fiber * Size.Y * .027f;
                float left = 7 + (fiber * 7 % 11);
                float right = Size.X - 8 - (fiber * 11 % 13);
                DrawLine(new(left, y), new(right, y + (fiber % 3 - 1) * .7f),
                    new Color(Burgundy, Marked ? .035f : .012f), 1.8f, true);
            }
        // Caption lives in the native button; the pictogram occupies the upper half.
        var origin = new Vector2(Size.X * .5f, Motif == VoyageMotif.None ? Size.Y * .5f : 30);
        DrawSetTransform(origin);
        switch (Motif)
        {
            case VoyageMotif.Rival: Rival(ink); break;
            case VoyageMotif.Difficulty: Difficulty(ink); break;
            case VoyageMotif.World: World(ink); break;
            case VoyageMotif.Size: SizeArt(ink); break;
            case VoyageMotif.Hand: Hand(ink, _reveal); break;
        }
        DrawSetTransform(Vector2.Zero);
        if (Motif != VoyageMotif.Hand && ((Marked && Motif != VoyageMotif.Rival) || HasFocus() || IsHovered()))
        {
            float reveal = Marked ? _reveal : 1;
            var center = Size * .5f;
            for (int strand = 0; strand < 3; strand++)
            {
                int count = Math.Max(2, (int)(58 * reveal));
                var points = new Vector2[count];
                for (int i = 0; i < count; i++)
                {
                    float angle = i / 57f * Mathf.Tau;
                    float wobble = MathF.Sin(angle * 7 + strand) * 1.7f;
                    points[i] = center + new Vector2(MathF.Cos(angle) * (Size.X * .47f + wobble),
                        MathF.Sin(angle) * (Size.Y * .45f + wobble));
                }
                float opacity = Marked ? (strand == 0 ? .78f : .27f) : (strand == 0 ? .27f : .08f);
                DrawPolyline(points, new Color(Burgundy, opacity), strand == 0 ? 2.1f : .8f, true);
            }
        }
    }
    private void Line(Vector2 a, Vector2 b, Color ink, float width = 1.3f)
    {
        DrawLine(a, b, ink, width, true);
        DrawLine(a + new Vector2(.7f, .45f), b + new Vector2(-.5f, .8f), new Color(ink, ink.A * .28f), .7f, true);
    }
    private void Rival(Color ink)
    {
        DrawCircle(new(0, -13), 5.8f, ink);
        DrawColoredPolygon(new[] { new Vector2(-10, 12), new Vector2(-6, -5), new Vector2(6, -5), new Vector2(10, 12) }, ink);
        Line(new(-13, 13), new(13, 13), ink, 2);
        Line(new(-7, 12), new(-8, 22), ink, 3);
        Line(new(7, 12), new(8, 22), ink, 3);
        Line(new(-6, -1), new(-15, 8), ink, 3);
        Line(new(6, -1), new(15, 8), ink, 3);
    }
    private void Difficulty(Color ink)
    {
        var centers = Variant switch { 0 => new[] { Vector2.Zero }, 1 => new[] { new Vector2(-8, 0), new Vector2(8, 0) },
            _ => new[] { new Vector2(-9, -6), new Vector2(9, -6), new Vector2(0, 9) } };
        foreach (var c in centers)
            for (int strand = 0; strand < 3; strand++)
                DrawArc(c + new Vector2(strand * .4f, strand * -.35f), 13, -.12f, Mathf.Tau - .12f, 33,
                    new Color(ink, ink.A * (strand == 0 ? 1 : .34f)), strand == 0 ? 1.8f : .7f, true);
        DrawCircle(Vector2.Zero, 2.4f, ink);
    }
    private void SizeArt(Color ink)
    {
        float radius = 8 + Variant * 4;
        var points = new Vector2[28];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i / 27f * Mathf.Tau;
            points[i] = new(MathF.Cos(a) * (radius + MathF.Sin(a * 3) * 2), MathF.Sin(a) * radius * .55f);
        }
        DrawPolyline(points, ink, 1.8f, true);
        for (int y = -1; y <= 1; y++)
            Line(new(-radius * .65f, y * 4), new(radius * .65f, y * 4 + 1), new Color(ink, .45f));
    }
    private void World(Color ink)
    {
        // Each miniature is an independent etched landscape, not a reused UI symbol.
        int islands = Variant switch { 0 => 2, 1 => 3, 2 => 2, _ => 1 };
        Line(new(-37, 17), new(37, 17), new Color(ink, .38f));
        DrawArc(new(20, -13), 5, Mathf.Pi, Mathf.Tau, 14, ink, 1.1f, true);
        for (int island = 0; island < islands; island++)
        {
            float x = islands == 1 ? 0 : (island - (islands - 1) * .5f) * (Variant == 1 ? 24 : 34);
            float w = Variant switch { 0 => 12 + island * 2, 1 => 11 + island * 2, 2 => 17, _ => 34 };
            var shore = new[] { new Vector2(x - w, 13), new Vector2(x - w * .75f, 8), new Vector2(x - w * .3f, 5),
                new Vector2(x + w * .25f, 7), new Vector2(x + w, 12), new Vector2(x + w * .55f, 15), new Vector2(x - w, 13) };
            DrawPolyline(shore, ink, 1.5f, true);
            int peaks = Variant >= 2 ? (Variant == 3 ? 4 : 2) : 1;
            for (int p = 0; p < peaks; p++)
            {
                float px = x + (p - (peaks - 1) * .5f) * 8;
                Line(new(px - 6, 8), new(px, -5 - p % 2 * 3), ink);
                Line(new(px, -5 - p % 2 * 3), new(px + 6, 8), ink);
                Line(new(px - 2, -1), new(px + 1, -2), new Color(ink, .5f));
            }
            int trees = Variant switch { 0 => 1, 1 => 2 + island % 2, 2 => 3 + island, _ => 6 };
            for (int tree = 0; tree < trees; tree++)
            {
                float tx = x - w * .7f + tree * w * 1.4f / Math.Max(1, trees - 1);
                float ty = tree % 2 * 2 + 5;
                Line(new(tx, ty + 5), new(tx, ty - 1), ink);
                Line(new(tx - 3, ty + 1), new(tx, ty - 4), ink);
                Line(new(tx, ty - 4), new(tx + 3, ty + 1), ink);
            }
            if (Variant >= 2 && island == 0)
            {
                for (int column = 0; column < 3; column++) Line(new(x + column * 4 - 4, 11), new(x + column * 4 - 4, 5), ink);
                Line(new(x - 5, 5), new(x + 5, 4), ink);
            }
        }
        for (int gull = 0; gull < Variant; gull++)
        {
            float x = -26 + gull * 13;
            Line(new(x - 3, -12 + gull % 2 * 3), new(x, -10 + gull % 2 * 3), ink);
            Line(new(x, -10 + gull % 2 * 3), new(x + 3, -12 + gull % 2 * 3), ink);
        }
    }
    internal void BeginHandprint() { _reveal = 0; SetProcess(true); }
    private void Hand(Color ink, float reveal)
    {
        if (!Marked) return;
        ink = new Color(ink, reveal * .65f);
        DrawCircle(new(0, 6), 11, ink);
        for (int i = 0; i < 4; i++)
            DrawLine(new(-9 + i * 6, 1), new(-11 + i * 7, -15 - (i is 1 or 2 ? 4 : 0)), ink, 4.5f, true);
    }
}
