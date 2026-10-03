using System;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    // A square on the ground becomes this closed diamond in the sea's
    // isometric projection. Back and front halves bracket the houses/mills.
    internal static Vector2[] TownWallCorners => new[]
    {
        new Vector2(0, -35), new Vector2(39, -8),
        new Vector2(0, 19), new Vector2(-39, -8)
    };
    internal static float TownWallHeight(int level) => 11 + level * .8f;

    private void DrawTownWall(Node2D canvas, TownArtState town, Vector2 center, bool front)
    {
        var corners = VillagePlacement(town).Compact ? new[]
            { new Vector2(0,-3), new Vector2(13,-1), new Vector2(0,1), new Vector2(-13,-1) } : TownWallCorners;
        float height = TownWallHeight(town.Level);
        var stone = new Color(town.Owner == Side.Pirates ? "69716c" : "beb99d");
        var joint = stone.Darkened(.27f);
        var cap = stone.Lightened(.18f);
        foreach (int edge in front ? new[] { 1, 2 } : new[] { 3, 0 })
        {
            var a = center + corners[edge];
            var b = center + corners[(edge + 1) % 4];
            var lift = new Vector2(0, -height);
            var shade = stone.Darkened(edge % 2 == 0 ? .12f : .23f);
            canvas.DrawColoredPolygon(new[] { a, b, b + lift, a + lift }, shade);
            for (int course = 1; course <= 3; course++)
            {
                var rise = lift * course / 4;
                canvas.DrawLine(a + rise, b + rise, joint, .65f, true);
                for (int block = 0; block < 8; block++)
                {
                    var p = a.Lerp(b, (block + (course % 2) * .5f) / 8f) + rise;
                    canvas.DrawLine(p, p + lift / 4, joint, .55f, true);
                }
            }
            canvas.DrawLine(a + lift, b + lift, cap, 2.2f, true);
            for (int merlon = 0; merlon <= 8; merlon++)
            {
                var p = a.Lerp(b, merlon / 8f) + lift;
                canvas.DrawRect(new Rect2(p + new Vector2(-1.8f, -3), new Vector2(3.6f, 3.5f)), cap);
            }
        }
        // The rear tower is behind the rooftops; three foreground towers
        // remain in front of the mill blades and directly behind the wheat.
        foreach (int corner in front ? new[] { 3, 1, 2 } : new[] { 0 })
        {
            var at = center + corners[corner];
            Vector2 P(float x, float y, float z) => at + new Vector2((x - y) * .65f, (x + y) * .32f - z);
            float top = height + 5;
            FactionSanctuaryArt.Box(canvas, P, 0, 0, 0, 7, 7, top, stone);
            FactionSanctuaryArt.Box(canvas, P, 0, 0, top, 8, 8, 1.3f, cap);
            foreach (float x in new[] { -3f, 3f })
                foreach (float y in new[] { -3f, 3f })
                    FactionSanctuaryArt.Box(canvas, P, x, y, top + 1.3f, 2, 2, 3, cap);
            canvas.DrawLine(P(0, 3.6f, 6), P(0, 3.6f, 10), joint, 1.1f, true);
            canvas.DrawLine(P(3.6f, 0, 6), P(3.6f, 0, 10), joint, 1.1f, true);
        }
        if (front)
        {
            var gate = center + corners[2].Lerp(corners[1], .48f);
            canvas.DrawColoredPolygon(new[] { gate + new Vector2(-3.3f, 0), gate + new Vector2(3.3f, -4.5f),
                gate + new Vector2(3.3f, -11), gate + new Vector2(0, -12), gate + new Vector2(-3.3f, -8) }, new Color("625848"));
            canvas.DrawLine(gate + new Vector2(0, -1), gate + new Vector2(0, -10), new Color("b4a078"), .8f, true);
        }
    }
}
