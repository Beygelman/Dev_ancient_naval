<<<<<<< Updated upstream
using DevAncientNaval.Core.Battle;
using Godot;

namespace DevAncientNaval.Presentation.UI;
/// <summary>Static generated harbor artwork; no hidden background animation.</summary>
public partial class MenuHarborView : Control
{
    public FleetColor FleetColor { get; set; } = FleetColor.Blue;

    public override void _Ready()
    {
        var artwork = new TextureRect
        {
            Name = "CityShipArtwork",
            Texture = GD.Load<Texture2D>("res://assets/ui/menu-harbor-0.16.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(artwork);
        artwork.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect
        {
            Color = new Color(.01f, .04f, .06f, .16f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
=======
using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.Map;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>A living close-up of the same terraced town carried by the flagship.</summary>
public partial class MenuHarborView : Control
{
    public FleetColor FleetColor { get; set; } = FleetColor.Blue;
    private float _time;
    private readonly Vector2[] _waveLine = new Vector2[35];
    private readonly Vector2[] _bird = new Vector2[5];
    private readonly Vector2[] _hullTop = new Vector2[6];
    private readonly Vector2[] _hullSide = new Vector2[4];
    private readonly Vector2[] _hullOutline = new Vector2[7];

    public override void _Ready()
    {
        var water = new ColorRect
        {
            Name = "WaterSurface",
            ShowBehindParent = true,
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/ui/menu-sea.gdshader") }
        };
        AddChild(water);
        water.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float sx = Size.X / 1280, sy = Size.Y / 720;
        var screenScale = new Vector2(sx, sy);
        DrawSetTransform(Vector2.Zero, 0, screenScale);
        DrawSea();
        var boat = new Vector2(315, 533 + MathF.Sin(_time * .65f) * 4);
        float rock = MathF.Sin(_time * .48f) * .008f;
        // A broad, faint shadow follows the two keels without obscuring the waves.
        DrawSetTransform(new Vector2(boat.X * sx, (boat.Y + 54) * sy), rock, new(sx, sy * .34f));
        DrawCircle(Vector2.Zero, 350, new Color(.015f, .06f, .075f, .23f));
        DrawSetTransform(new Vector2(boat.X * sx, boat.Y * sy), rock, new(sx * 10, sy * 10));
        var accent = FleetPalette.Color(FleetColor);
        DrawKeel(-15, accent);
        DrawKeel(16, accent);
        FloatingTownArt.Draw(this, TownPoint, accent, _time, detailed: true);
        DrawSetTransform(Vector2.Zero, 0, screenScale);
        DrawBirds();
        DrawSetTransform(Vector2.Zero);
        // The harbor stays visible behind a quiet wash at the edge of the chart.
        DrawPolygon(
            new[] { new Vector2(Size.X * .49f, 0), new Vector2(Size.X, 0), Size, new Vector2(Size.X * .49f, Size.Y) },
            new[] { new Color(0, 0, 0, 0), new Color(.01f, .04f, .06f, .70f), new Color(.01f, .04f, .06f, .70f), new Color(0, 0, 0, 0) });
    }

    private static Vector2 TownPoint(float x, float y, float z) => new(x, y * .7f - z);

    private void DrawKeel(float shift, Color accent)
    {
        _hullTop[0] = TownPoint(-39, shift, -1);
        _hullTop[1] = TownPoint(-34, shift - 5, -1);
        _hullTop[2] = TownPoint(29, shift - 4, -1);
        _hullTop[3] = TownPoint(41, shift + 1, -1);
        _hullTop[4] = TownPoint(29, shift + 7, -1);
        _hullTop[5] = TownPoint(-35, shift + 7, -1);
        _hullSide[0] = _hullTop[5];
        _hullSide[1] = _hullTop[4];
        _hullSide[2] = TownPoint(29, shift + 7, -7);
        _hullSide[3] = TownPoint(-35, shift + 7, -7);
        DrawColoredPolygon(_hullSide, new Color("365963"));
        DrawColoredPolygon(_hullTop, new Color("667b77"));
        _hullTop.CopyTo(_hullOutline, 0);
        _hullOutline[^1] = _hullTop[0];
        DrawPolyline(_hullOutline, new Color("d9ca9f"), .6f, true);
        DrawLine(TownPoint(-33, shift + 7, -3), TownPoint(27, shift + 7, -3), accent.Darkened(.24f), 1.25f, true);
        DrawLine(TownPoint(-33, shift + 7, -4.6f), TownPoint(27, shift + 7, -4.6f), new Color("b39354"), .23f, true);
        for (int i = 0; i < 15; i++)
        {
            float x = -32 + i * 4;
            DrawLine(TownPoint(x, shift + 7, -1.2f), TownPoint(x, shift + 7, -6.8f), new Color(.12f, .2f, .19f, .22f), .15f, true);
        }
        // An ivory prow ornament makes both forward noses readable.
        var prow = TownPoint(37, shift + 2, 0);
        DrawLine(prow, prow + new Vector2(3, -2), new Color("ede1bd"), .55f, true);
        DrawArc(prow + new Vector2(3, -2), 1.1f, -Mathf.Pi, -.1f, 12, new Color("dcc38a"), .35f, true);
    }

    private void DrawSea()
    {
        for (int row = 0; row < 30; row++)
        {
            for (int i = 0; i < _waveLine.Length; i++)
            {
                float x = i * 40 - 30;
                _waveLine[i] = new(x, row * 29 + MathF.Sin(x * .013f + _time * .42f + row * .71f) * 5 + MathF.Cos(x * .03f - _time * .23f) * 2);
            }
            DrawPolyline(_waveLine, new Color(.56f, .78f, .79f, .035f + row % 3 * .022f), 1.5f, true);
        }
        for (int i = 0; i < 40; i++)
        {
            float x = i * 137 % 1310, y = i * 89 % 760;
            float pulse = (MathF.Sin(_time * .65f + i) + 1) * .5f;
            DrawLine(new(x, y), new(x + 14 + pulse * 24, y - 2), new Color(.7f, .87f, .84f, .09f * pulse), 1, true);
        }
        for (int i = 0; i < 3; i++)
        {
            float phase = Mathf.PosMod(_time * .09f + i * .31f, 1);
            DrawArc(new Vector2(345, 590), 75 + phase * 210, .03f, 2.9f, 36, new Color(.75f, .88f, .85f, .11f * (1 - phase)), 1.3f, true);
        }
    }

    private void DrawBirds()
    {
        for (int i = 0; i < 11; i++)
        {
            float a = _time * .095f + i * .67f;
            var water = new Vector2(320 + MathF.Cos(a) * (280 + i % 3 * 28), 480 + MathF.Sin(a) * 120);
            DrawSetTransform(new(water.X * Size.X / 1280, water.Y * Size.Y / 720), 0, new(Size.X / 1280, Size.Y / 720 * .3f));
            DrawCircle(Vector2.Zero, 6, new Color(.015f, .07f, .1f, .15f));
            DrawSetTransform(Vector2.Zero, 0, new(Size.X / 1280, Size.Y / 720));
            var bird = water + new Vector2(0, -145 - i % 3 * 35);
            float wing = MathF.Sin(_time * 3 + i) * 5;
            _bird[0] = bird + new Vector2(-11, wing);
            _bird[1] = bird + new Vector2(-5, -2);
            _bird[2] = bird;
            _bird[3] = bird + new Vector2(5, -2);
            _bird[4] = bird + new Vector2(11, wing);
            DrawPolyline(_bird, new Color("f1efdd"), 2.3f, true);
        }
>>>>>>> Stashed changes
    }
}
