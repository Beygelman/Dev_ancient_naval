using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class BoardView
{
    internal int TownInterfaceDrawCount { get; private set; }
    private readonly Dictionary<int, AmphoraHealthAnimation> _townHealth = new();
    private readonly ulong _townAnimationEpoch = Time.GetTicksMsec();
    private float TownAnimationTime => (Time.GetTicksMsec() - _townAnimationEpoch) / 1000f;
    internal static Vector2 TownHealthAnchor(Vector2 center) => center + new Vector2(38, -31);

    private readonly Dictionary<int, (Vector2 Position, float Height, float Width)[]> _townHomes = new();
    internal static Vector2[] TownMills(Village town) => town.Level < 2 ? Array.Empty<Vector2>() : town.Level < 4 ? new[]
    {
        new Vector2(-24, -17)
    }

    : new[]
    {
        new Vector2(-29, -18),
        new Vector2(26, -8)
    };
    private void DrawVillage(Node2D canvas, Village town, Vector2 center)
    {
        if (!_townHomes.TryGetValue(town.Id, out var homes))
        {
            var random = new Random(Board.Seed ^ town.Id * 719);
            homes = Enumerable.Range(0, 16).Select(i => (new Vector2((float)random.NextDouble() * 42 - 21, (float)random.NextDouble() * 22 - 13), 10f + random.Next(13), 5f + random.Next(4))).OrderBy(h => h.Item1.Y).ToArray();
            _townHomes[town.Id] = homes;
        }

        var accent = FleetPalette.For(Battle, town.Owner);
        var ground = new[]
        {
            center + new Vector2(-30, -9),
            center + new Vector2(0, -23),
            center + new Vector2(33, -4),
            center + new Vector2(2, 17)
        };
        canvas.DrawColoredPolygon(ground, new Color("999b7a"));
        int count = 3 + town.Level * 2;
        bool shrineDrawn = false;
        void Shrine()
        {
            if (town.Owner is not { } owner) return;
            Vector2 Project(float x, float y, float z) => center + new Vector2((x - y) * .85f, (x + y) * .42f - z);
            FactionSanctuaryArt.Draw(canvas, (x, y, z) => Project(x, y - 4, z * (1.4f + .24f * (town.Level - 1)) + 1), Battle.ColorFor(owner));
        }
        for (int i = 0; i < count; i++)
        {
            var home = homes[i];
            if (!shrineDrawn && home.Position.Y >= -3) { Shrine(); shrineDrawn = true; }
            var p = center + home.Position;
            float h = home.Height + Math.Max(0, town.Level - 2) * 4;
            float w = home.Width;
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-w, 0), p + new Vector2(2, 4), p + new Vector2(2, 4 - h), p + new Vector2(-w, -h) }, new Color("e2caa1").Darkened(i % 4 * .035f));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(2, 4), p + new Vector2(w + 3, 0), p + new Vector2(w + 3, -h), p + new Vector2(2, 4 - h) }, new Color("ac9a77"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-w - 1, -h), p + new Vector2(0, -h - 5), p + new Vector2(w + 4, -h), p + new Vector2(2, 5 - h) }, accent.Darkened(.12f + i % 5 * .07f));
            // Upright doors, cornices, lintels and roof tiles distinguish the houses.
            canvas.DrawLine(p + new Vector2(-w, -h + 1), p + new Vector2(2, 4 - h + 1), new Color("f4e5c2"), 1, true);
            canvas.DrawLine(p + new Vector2(2, 4-h+1), p + new Vector2(w+3, -h+1), new Color("8b7660"), .7f, true);
            canvas.DrawRect(new Rect2(p + new Vector2(-w+2, -5), new Vector2(2.5f, 5)), new Color("745940"));
            for (int window = 0; window < Math.Max(1, (int)(h/8)); window++)
            {
                var at = p + new Vector2(-w + 2, -h + 4 + window * 7);
                canvas.DrawRect(new Rect2(at - new Vector2(.6f,.6f), new Vector2(3.2f,4.2f)), new Color("f4ddb4"));
                canvas.DrawRect(new Rect2(at, new Vector2(2,3)), new Color("596068"));
                canvas.DrawLine(at + new Vector2(1,0), at + new Vector2(1,3), new Color("ad9271"), .6f);
            }
            for (int tile = 0; tile < 3; tile++)
                canvas.DrawLine(p + new Vector2(-w+tile*3,-h), p + new Vector2(2+tile*3,3-h), accent.Darkened(.4f), .55f, true);
            if (i % 3 == 0)
            {
                canvas.DrawLine(p + new Vector2(-w+1,-h), p + new Vector2(-w+1,-h-4), new Color("a39278"), 2, true);
                canvas.DrawLine(p + new Vector2(-w+2,-5), p + new Vector2(1,-4), accent, 1.8f, true);
            }
            if (i % 4 == 1)
                canvas.DrawCircle(p + new Vector2(w+2,1), 1.8f, new Color("6a8361"));
        }

        if (!shrineDrawn) Shrine();
        foreach (var mill in TownMills(town))
        {
            var p = center + mill;
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-5, 9), p + new Vector2(6, 9), p + new Vector2(4, -7), p + new Vector2(-3, -7) }, new Color("d6c6a3"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-6, -7), p + new Vector2(0, -12), p + new Vector2(6, -7) }, new Color("886d4c"));
        }

        if (town.IsFortified)
        {
            var wall = new[]
            {
                center + new Vector2(-35, -8),
                center + new Vector2(-35, 10),
                center + new Vector2(0, 28),
                center + new Vector2(36, 10),
                center + new Vector2(36, -9)
            };
            bool stone = town.Level >= 3;
            if (stone)
                for (int segment = 1; segment < wall.Length; segment++)
                    canvas.DrawColoredPolygon(new[] { wall[segment - 1], wall[segment], wall[segment] + new Vector2(0, -6), wall[segment - 1] + new Vector2(0, -6) }, new Color("a6ab99"));
            else
                canvas.DrawPolyline(wall, new Color("826442"), 4, true);
            if (stone)
                for (int segment = 1; segment < wall.Length; segment++)
                {
                    canvas.DrawLine(wall[segment-1] + new Vector2(0,-3), wall[segment] + new Vector2(0,-3), new Color("7b8377"), .65f, true);
                    for (float t = .08f; t < 1; t += .18f)
                    {
                        var at = wall[segment-1].Lerp(wall[segment],t);
                        canvas.DrawLine(at,at+new Vector2(0,-3),new Color("7b8377"),.6f);
                    }
                }
            for (int i = 1; i < wall.Length; i++)
                for (float t = 0; t <= 1; t += .14f)
                {
                    var at = wall[i - 1].Lerp(wall[i], t);
                    if (stone)
                        canvas.DrawRect(new Rect2(at + new Vector2(-2, -8), new Vector2(4, 5)), new Color("d1cbb6"));
                    else
                        canvas.DrawLine(at, at + new Vector2(0, -7), new Color("b49a6b"), 2, true);
                }
        }

        DrawTownFields(canvas, town, center);
        if (town.HasPort)
            DrawPort(canvas, town, center);
        if (town.Owner is not null)
            canvas.DrawLine(center + new Vector2(13, -24), center + new Vector2(13, -47), new Color("eadfc2"), 2, true);
    }

    private static void DrawTownFields(Node2D canvas, Village town, Vector2 center)
    {
        int rows = 2 + town.Level / 2;
        for (int field = 0; field < rows; field++)
        {
            var at = center + new Vector2(-28 + field * 14, 36 + field % 2 * 4);
            canvas.DrawColoredPolygon(new[] { at, at + new Vector2(12,-5), at + new Vector2(21,0), at + new Vector2(9,6) }, new Color("aa8d50"));
            for (int stalk = 0; stalk < 8; stalk++)
            {
                var basePoint = at + new Vector2(4 + stalk % 4 * 3, stalk / 4 * 3);
                canvas.DrawLine(basePoint, basePoint + new Vector2(0,-5), new Color("e2bd62"), .8f);
                canvas.DrawLine(basePoint + new Vector2(-1.4f,-4), basePoint + new Vector2(1.4f,-6), new Color("f4d889"), 1.1f, true);
            }
        }
    }

    internal Vector2 PortAnchor(Village town) => Projection.GridToWorld(town.Position).Lerp(Projection.GridToWorld(Battle.PortBerth(town)), .5f);

    private void DrawPort(Node2D canvas, Village town, Vector2 center)
    {
        var sea = Projection.GridToWorld(Battle.PortBerth(town)) - Projection.GridToWorld(town.Position);
        var axis = sea.Normalized();
        var side = axis.Orthogonal();
        var shore = center + sea * .5f;
        canvas.DrawLine(center + new Vector2(0,2), shore, new Color("d5c29b"), 4, true);
        canvas.DrawLine(center + new Vector2(0,2), shore, new Color("9f8e72"), .7f, true);
        for (int pier = -1; pier <= 1; pier++)
        {
            var at = shore + side * pier * 7;
            var end = at + axis * (18 + (pier == 0 ? 7 : 0));
            canvas.DrawLine(at + new Vector2(2, 3), end + new Vector2(2, 3), new Color("435e60"), 7);
            canvas.DrawLine(at, end, new Color("b9a57c"), 5, true);
            for (int plank = 0; plank < 5; plank++)
            {
                var p = at.Lerp(end, plank / 4f);
                canvas.DrawLine(p - side * 3, p + side * 3, new Color("73664d"), 1);
            }

            var boat = end + side * 6;
            canvas.DrawColoredPolygon(new[] { boat - axis * 5, boat + side * 2, boat + axis * 5, boat - side * 2 }, new Color("735c41"));
            canvas.DrawLine(boat, boat + new Vector2(0, -6), new Color("ece0b3"), 1);
        }

        Vector2 P(float x, float y, float z) => shore + new Vector2(x-y,(x+y)*.45f-z);
        FactionSanctuaryArt.Box(canvas, P, 0,0,0,9,7,8,new Color("d5c098"));
        FactionSanctuaryArt.Pyramid(canvas, P,0,0,8,5,4,FleetPalette.For(Battle,town.Owner).Darkened(.2f));
        canvas.DrawCircle(shore + new Vector2(0,-4),2,new Color("f0e4bf"));
        canvas.DrawLine(shore + new Vector2(0,-6),shore + new Vector2(0,-2),new Color("726851"),1);
    }

    private void DrawVillageInterface(Node2D canvas, Village town)
    {
        TownInterfaceDrawCount++;
        var accent = FleetPalette.For(Battle, town.Owner);
        float width = ThemeDB.FallbackFont.GetStringSize(town.Name, fontSize: 13).X;
        canvas.DrawString(ThemeDB.FallbackFont, new Vector2(-width * .5f, 61), town.Name, fontSize: 13, modulate: Colors.Black);
        for (int i = 0; i < 5; i++)
            canvas.DrawRect(new Rect2(new Vector2(-18 + i * 8, 65), new Vector2(5, 4)), i < town.Level ? accent : new Color("475857"));
        if (!_townHealth.TryGetValue(town.Id, out var animation))
            _townHealth[town.Id] = animation = new AmphoraHealthAnimation();
        float time = TownAnimationTime;
        animation.Observe(town.Health, town.MaxHealth, time);
        AmphoraBadgeArt.Draw(canvas, new Vector2(38, -35), town.Health, town.MaxHealth, accent, null, animation.Motion(time));
    }
}
