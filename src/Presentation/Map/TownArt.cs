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

    private readonly Dictionary<int, TownHouse[]> _townHomes = new();
    internal static Vector2[] TownMills(Village town) => TownMills(town.Level);
    private static Vector2[] TownMills(int level) => level < 2 ? Array.Empty<Vector2>() : level < 4 ? new[]
    {
        new Vector2(-18, 0)
    }

    : new[]
    {
        new Vector2(-18, 0),
        new Vector2(18, 0)
    };
    internal TownHouse[] TownHomes(Village town) => TownHomes(town.Id);
    private TownHouse[] TownHomes(int townId)
    {
        if (!_townHomes.TryGetValue(townId, out var homes))
            _townHomes[townId] = homes = TownLayout.Build(Board.Seed ^ townId * 719);
        return homes;
    }
    private void DrawVillage(Node2D canvas, TownArtState town, Vector2 center)
    {
        var homes = TownHomes(town.Id);

        var accent = town.Accent;
        if (town.IsFortified) DrawTownWall(canvas, town, center, false);
        int count = Math.Min(homes.Length, 3 + town.Level * 2);
        bool shrineDrawn = false;
        void Shrine()
        {
            if (town.Owner is not { } owner) return;
            Vector2 Project(float x, float y, float z) => center + new Vector2((x - y) * .85f, (x + y) * .42f - z);
            Vector2 Monument(float x, float y, float z) => Project(x, y - 4, z * SanctuaryHeightScale(town.Level) + 1);
            if (owner == Side.Pirates) FactionSanctuaryArt.DrawPirate(canvas, Monument);
            else FactionSanctuaryArt.Draw(canvas, Monument, town.Monument);
        }
        for (int i = 0; i < count; i++)
        {
            var home = homes[i];
            if (!shrineDrawn && home.Position.Y >= -3) { Shrine(); shrineDrawn = true; }
            var p = center + home.Position;
            float h = home.Height + Math.Max(0, town.Level - 2) * 1.7f;
            float w = home.Width;
            var plaster = new Color(home.Style switch { 0 => "e2caa1", 1 => "e6d5b6", 2 => "d4c4aa", 3 => "ddd4bc", _ => "cfc3a5" });
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-w, 0), p + new Vector2(2, 4), p + new Vector2(2, 4 - h), p + new Vector2(-w, -h) }, plaster);
            canvas.DrawColoredPolygon(new[] { p + new Vector2(2, 4), p + new Vector2(w + 3, 0), p + new Vector2(w + 3, -h), p + new Vector2(2, 4 - h) }, plaster.Darkened(.2f));
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
        foreach (var mill in TownMills(town.Level))
        {
            var p = center + mill;
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-5, 0), p + new Vector2(6, 0), p + new Vector2(4, -16), p + new Vector2(-3, -16) }, new Color("d6c6a3"));
            canvas.DrawColoredPolygon(new[] { p + new Vector2(-6, -16), p + new Vector2(0, -22), p + new Vector2(6, -16) }, new Color("886d4c"));
        }

        if (town.Owner is not null)
            canvas.DrawLine(center + new Vector2(13, -24), center + VillageFlagOffset, new Color("eadfc2"), 2, true);
    }

    private void DrawVillageForeground(Node2D canvas, TownArtState town, Vector2 center)
    {
        if (town.IsFortified) DrawTownWall(canvas, town, center, true);
    }

    internal static float SanctuaryHeightScale(int level) => 1.4f + .3f * (level - 1);
    private void DrawTownFields(Node2D canvas, TownArtState town, Vector2 center)
    {
        foreach (var field in TownGround(town).Fields)
        {
            foreach (var shape in field.Shapes)
                canvas.DrawColoredPolygon(shape.Select(p => p + center).ToArray(), new Color("ad985f"));
            foreach (var basePoint in field.Stalks)
            {
                var p = basePoint + center;
                canvas.DrawLine(p, p + new Vector2(0,-4), new Color("e2bd62"), .8f);
                canvas.DrawLine(p + new Vector2(-1.1f,-3), p + new Vector2(1.1f,-5), new Color("f4d889"), 1.1f, true);
            }
        }
    }

    internal Vector2 PortAnchor(Village town) => Projection.GridToWorld(town.Position).Lerp(Projection.GridToWorld(Battle.PortBerth(town)), .5f);

    private void DrawPort(Node2D canvas, TownArtState town, Vector2 center)
    {
        var sea = Projection.GridToWorld(town.PortBerth) - Projection.GridToWorld(town.Position);
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
        FactionSanctuaryArt.Pyramid(canvas, P,0,0,8,5,4,town.Accent.Darkened(.2f));
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
