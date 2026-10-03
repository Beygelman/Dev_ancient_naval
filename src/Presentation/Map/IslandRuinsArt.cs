using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.World;
using Godot;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Broken silhouettes share the fleet's upright masonry projection.
/// None of these decorative structures is an interactive Core object.</summary>
internal static class IslandRuinsArt
{
    internal static void Draw(CanvasItem canvas, Vector2 origin, float scale, int variant, bool sunken)
    {
        Vector2 P(float x, float y, float z) => origin + new Vector2((x - y) * .72f, (x + y) * .35f - z) * scale;
        var stone = new Color(sunken ? "7d9e96" : "c5c2a2");
        var dark = stone.Darkened(.32f);
        var trim = stone.Lightened(.14f);
        var moss = new Color(sunken ? "487769" : "6e8d62");
        void Box(float x, float y, float z, float w, float d, float h, Color color) =>
            FactionSanctuaryArt.Box(canvas, P, x, y, z, w, d, h, color);
        void Column(float x, float y, float height, bool intact)
        {
            Box(x, y, 0, 4, 4, 2, dark);
            Box(x, y, 2, 2.7f, 2.7f, height, stone);
            Box(x, y, 2 + height, 3.5f, 3.5f, 1.1f, trim);
            canvas.DrawLine(P(x - .55f, y + 1.4f, 3), P(x - .55f, y + 1.4f, height + 1), dark, .7f * scale, true);
            if (!intact)
                RuinTriangle(canvas, P(x - 1.3f, y - 1.3f, height + 2), P(x + 1.3f, y - 1.3f, height + 3.4f),
                    P(x + 1.3f, y + 1.3f, height + 1.8f), trim);
            if (!intact)
                RuinTriangle(canvas, P(x - 1.3f, y - 1.3f, height + 2), P(x + 1.3f, y + 1.3f, height + 1.8f),
                    P(x - 1.3f, y + 1.3f, height + 2.5f), trim.Darkened(.06f));
        }
        // Disconnected paving and scattered blocks have no opaque square base.
        for (int slab = 0; slab < 9; slab++)
            Box(-11 + slab % 3 * 10, -7 + slab / 3 * 7, -.7f, 6 + slab % 2 * 2, 4, .7f, stone.Darkened(.1f));
        switch (variant % 4)
        {
            case 0: // Shattered sanctuary with a surviving pediment fragment.
                Box(-8, -2, 0, 4, 15, 14, stone);
                Box(8, -5, 0, 4, 8, 8, stone);
                Column(-3, 8, 15, true);
                Column(6, 8, 7, false);
                Box(-8, -2, 14, 5, 15, 2, trim);
                // Both ends of the surviving roof plane share their ridge
                // height; the old skewed front tip folded across its own edge.
                canvas.DrawColoredPolygon(new[] { P(-10, -9, 16), P(-7, -9, 27), P(-7, 6, 27), P(-10, 6, 16) }, dark);
                canvas.DrawColoredPolygon(new[] { P(-10, -9, 16), P(-7, -9, 27), P(-1, -9, 17) }, trim);
                Box(0, 0, 0, 8, 5, 2, moss);
                break;
            case 1: // Fort wall, crenellated corner and a collapsed gate.
                Box(-10, -5, 0, 8, 8, 19, stone);
                Box(-10, -5, 19, 9, 9, 2, trim);
                for (int i = 0; i < 3; i++) Box(-13 + i * 3, -8, 21, 1.7f, 2, i == 1 ? 2 : 4, trim);
                Box(-4, -6, 0, 8, 3, 9, stone);
                Box(9, -6, 0, 6, 3, 5, stone);
                Box(-10, 5, 0, 3, 12, 7, stone);
                canvas.DrawLine(P(-10, -1, 10), P(-10, -1, 15), dark, 1.4f * scale, true);
                for (int beam = 0; beam < 3; beam++)
                    canvas.DrawLine(P(0 + beam * 2, 3, .5f), P(8, -1 + beam, 2), new Color("776c54"), 2 * scale, true);
                break;
            case 2: // Uneven colonnade and fallen, fluted drums.
                for (int column = 0; column < 5; column++)
                    Column(-12 + column * 6, -3, column is 0 or 1 ? 21 : column == 4 ? 13 : 5 + column, column < 2);
                Box(-9, -3, 23, 11, 4, 2, trim);
                for (int drum = 0; drum < 4; drum++)
                {
                    Box(1 + drum * 3, 6, 0, 2.5f, 4, 2.4f, stone);
                    canvas.DrawLine(P(1 + drum * 3, 4, 1.6f), P(1 + drum * 3, 8, 1.6f), trim, scale, true);
                }
                break;
            default: // Small dead town, broken roofs and a narrow bell tower.
                for (int house = 0; house < 4; house++)
                {
                    float x = -10 + house % 2 * 15, y = -5 + house / 2 * 12;
                    float h = 5 + house * 2;
                    Box(x, y, 0, 9, 7, h, stone.Darkened(house * .035f));
                    canvas.DrawColoredPolygon(new[] { P(x - 5, y - 4, h), P(x, y - 4, h + 4),
                        P(x + 2, y + 2, h + 1), P(x - 5, y + 4, h) }, new Color("887c62"));
                    canvas.DrawLine(P(x - 2, y + 3.6f, 1), P(x - 2, y + 3.6f, h - 1), dark, 1.8f * scale, true);
                }
                Box(-1, -9, 0, 4, 4, 27, stone);
                Box(-1, -9, 27, 5, 5, 1.5f, trim);
                canvas.DrawLine(P(-1, -6.8f, 18), P(-1, -6.8f, 23), dark, 1.5f * scale, true);
                break;
        }
        for (int block = 0; block < 6; block++)
            Box(-15 + block * 6, 9 + block % 2 * 2, 0, 2 + block % 3, 2, 1 + block % 2, stone.Darkened(.15f));
        for (int ivy = 0; ivy < 5; ivy++)
        {
            var a = P(-11 + ivy * 5, 7, 0);
            canvas.DrawLine(a, a + new Vector2(1, -3 - ivy % 3) * scale, moss, 1.2f * scale, true);
            canvas.DrawCircle(a + new Vector2(1, -2) * scale, 1.2f * scale, moss);
        }
        if (sunken)
        {
            // A translucent, irregular wash obscures submerged foundations;
            // the upper broken silhouette remains legible above the water.
            canvas.DrawColoredPolygon(new[] { P(-19, -12, 0), P(20, -11, 0), P(23, 9, 0), P(-18, 15, 0) }, new Color(.16f, .37f, .43f, .39f));
            canvas.DrawLine(P(-14, 10, 2), P(-3, 10, 2), new Color(.75f, .88f, .82f, .5f), .8f * scale, true);
            canvas.DrawLine(P(3, 7, 2), P(15, 7, 2), new Color(.75f, .88f, .82f, .5f), .8f * scale, true);
        }
    }
    private static void RuinTriangle(CanvasItem canvas, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (MathF.Abs((b - a).Cross(c - a)) > .001f)
            canvas.DrawPrimitive(new[] { a, b, c }, new[] { color }, Array.Empty<Vector2>());
    }
    internal static void DrawProw(CanvasItem canvas, Vector2 center, float scale)
    {
        Vector2 P(float x, float y) => center + new Vector2(x, y) * scale;
        canvas.DrawColoredPolygon(new[] { P(-22, 5), P(-9, 8), P(16, -3), P(22, -18), P(12, -7), P(-8, 2) }, new Color("7e795e"));
        canvas.DrawColoredPolygon(new[] { P(-22, 5), P(-8, 2), P(12, -7), P(18, -25), P(8, -18), P(2, -6) }, new Color("b9ad7f"));
        for (int plank = 0; plank < 5; plank++)
            canvas.DrawLine(P(-16 + plank * 5, 4 - plank * 1.4f), P(-8 + plank * 5, -3 - plank * 2.2f), new Color("6d6b55"), .8f * scale, true);
        canvas.DrawLine(P(18, -25), P(23, -31), new Color("c5c9a4"), 3 * scale, true);
        canvas.DrawColoredPolygon(new[] { P(19, -27), P(24, -34), P(28, -30), P(24, -26) }, new Color("adb79b"));
        canvas.DrawLine(P(-12, 1), P(-21, -9), new Color("7c856e"), 2 * scale, true);
        canvas.DrawColoredPolygon(new[] { P(-25, 6), P(-8, 11), P(23, 0), P(27, -9), P(11, -4) }, new Color(.17f, .39f, .44f, .4f));
        canvas.DrawLine(P(-19, 6), P(-6, 8), new Color(.75f, .88f, .83f, .5f), scale, true);
    }
}

public partial class BoardView
{
    private delegate bool SceneryLandTest(Vector2 p, out GridPosition cell);
    internal int DecorativeRuinCount => _scenery.Count(p => p.Kind == 3);
    internal IEnumerable<int> DecorativeRuinVariants => _scenery.Where(p => p.Kind == 3).Select(p => (int)p.Shade);
    private void AddDecorativeRuins(SceneryLandTest landPoint)
    {
        var random = new Random(Board.Seed ^ 0x7283D);
        var candidates = Board.Tiles.Where(t => t.Terrain == TerrainType.Land &&
                Battle.Villages.All(v => !Board.InRadius(v.Position, t.Position, 2)))
            .Select(t => t.Position).OrderBy(_ => random.Next()).ToArray();
        int wanted = Math.Clamp(candidates.Length / 32, 1, 24);
        var chosen = new List<Vector2>();
        foreach (var cell in candidates)
        {
            var p = Projection.GridToWorld(cell);
            if (!landPoint(p, out _) || CosmeticRiverAt(p) || chosen.Any(old => old.DistanceSquaredTo(p) < 10_000)) continue;
            float size = 14 + (float)random.NextDouble() * 9;
            bool Fits(float s) => new[] { new Vector2(-25, -8), new Vector2(25, -8), new Vector2(0, 16),
                    new Vector2(0, -17), new Vector2(-14, 10), new Vector2(14, 10) }
                .All(v => landPoint(p + v * (s / 18), out _) && !CosmeticRiverAt(p + v * (s / 18)));
            while (size > 5 && !Fits(size)) size *= .85f;
            if (!Fits(size)) continue;
            int variant = chosen.Count % 4;
            _scenery.Add(new(cell, p, size, 3, variant));
            chosen.Add(p);
            if (chosen.Count >= wanted) break;
        }
    }
}
