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
    private readonly Dictionary<int, Vector2> _portShoreAnchors = new();
    private IsometricProjection? _portShoreProjection;
    internal static Vector2[] TownMills(Village town) => TownMills(town.Level);
    internal Vector2[] VillageMills(Village town) => VillagePlacement(town).Compact ? Array.Empty<Vector2>() : TownMills(town);
    internal void DrawSanctuaryOverlay(Node2D canvas, Village town, Vector2 center, float time)
    {
        if (town.Owner is null) return;
        Vector2 Monument(float x, float y, float z) => center + new Vector2((x - y + 4) * .85f,
            (x + y - 4) * .42f - z * SanctuaryHeightScale(town.Level) - 1);
        if (town.Owner == Side.Pirates)
            FactionSanctuaryArt.DrawPirateEffects(canvas, Monument, time, town.Id);
        else
            FactionSanctuaryArt.DrawEffects(canvas, Monument, Battle.ColorFor(town.Owner.Value), time, town.Id);
    }
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
        var homes = TownVisibleHomes(TownHomes(town.Id), town.Level);
        if (VillagePlacement(town).Compact)
            homes = homes.Take(3).Select((h, i) => h with { Position = new[]
                { new Vector2(0, -4), new Vector2(-7, -2), new Vector2(7, -2) }[i] }).ToArray();

        var accent = town.Accent;
        DrawTownPlaza(canvas, town, center);
        if (town.IsFortified) DrawTownWall(canvas, town, center, false);
        int count = homes.Length;
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
            float h = TownHouseHeight(home, town.Level);
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
        foreach (var mill in VillagePlacement(town).Compact ? Array.Empty<Vector2>() : TownMills(town.Level))
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

    internal static TownHouse[] TownVisibleHomes(TownHouse[] homes, int level)
    {
        // Early homes flank the sanctuary instead of all disappearing behind
        // its taller centre. Later growth fills the existing street lattice.
        var founders = new[] { new Vector2(-24, -16), new Vector2(24, -16),
            new Vector2(-30, -4), new Vector2(30, -4), new Vector2(0, -28) };
        var priority = founders.Select(target => homes.MinBy(h => h.Position.DistanceSquaredTo(target)))
            .Distinct().Concat(homes).Distinct().Take(TownHouseCount(level, homes.Length));
        return priority.OrderBy(h => h.Position.Y).ThenBy(h => h.Position.X).ToArray();
    }

    internal static int TownHouseCount(int level, int available) => Math.Min(available, 2 + level * 3);
    internal static float TownHouseHeight(TownHouse home, int level) => home.Height + Math.Max(0, level - 1) * 3.5f;
    internal static float SanctuaryHeightScale(int level) => 1.65f + .2f * (level - 1);
    internal static Vector2[] TownPlaza(int seed) => Enumerable.Range(0, 28).Select(i =>
    {
        float angle = i * Mathf.Tau / 28;
        float irregular = 1 + .065f * MathF.Sin(angle * 3 + seed * .017f) + .035f * MathF.Cos(angle * 5);
        return new Vector2(3.4f, -1.7f) + new Vector2(MathF.Cos(angle) * 12, MathF.Sin(angle) * 4.6f) * irregular;
    }).ToArray();
    private void DrawTownPlaza(Node2D canvas, TownArtState town, Vector2 center)
    {
        var edge = TownPlaza(town.Id);
        if(VillagePlacement(town).Compact)edge=edge.Select(p=>new Vector2(p.X*.72f,p.Y*.65f)).ToArray();
        canvas.DrawColoredPolygon(edge.Select(p => p + center).ToArray(), new Color("c9c0a4"));
        canvas.DrawPolyline(edge.Append(edge[0]).Select(p => p + center).ToArray(), new Color("a8a187"), .7f, true);
        // Irregular paving joints frame the church without filling its open
        // floor with another rectangular backdrop.
        for (int joint = 0; joint < 14; joint++)
        {
            var a = edge[joint * 2];
            var b = a.Lerp(new Vector2(3.4f, -1.7f), .24f);
            canvas.DrawLine(center + a, center + b, new Color("b4ad93"), .55f, true);
        }
    }
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

    internal Vector2 PortAnchor(Village town) => Projection.GridToWorld(town.Position) + PortShore(ObserveTownArt(town));

    private Vector2 PortShore(TownArtState town)
    {
        EnsureIslandGeometry();
        if (!ReferenceEquals(_portShoreProjection, Projection))
        {
            _portShoreProjection = Projection;
            _portShoreAnchors.Clear();
        }
        if (_portShoreAnchors.TryGetValue(town.Id, out var cached)) return cached;
        var origin = Projection.GridToWorld(town.Position);
        var sea = Projection.GridToWorld(town.PortBerth);
        // Follow the actual curving island edge, rather than placing a pier
        // at an assumed square-cell midpoint which may still be inland.
        var edge = _beaches.SelectMany(b => Enumerable.Range(1, b.Edge.Length - 1)
                .Select(i => (A: b.Edge[i - 1], B: b.Edge[i])))
            .Select(e => Geometry2D.SegmentIntersectsSegment(origin, sea, e.A, e.B))
            .Where(hit => hit.VariantType != Variant.Type.Nil)
            .Select(hit => hit.AsVector2())
            .OrderBy(p => p.DistanceSquaredTo(origin)).ToArray();
        return _portShoreAnchors[town.Id] = (edge.Length > 0 ? edge[0] : origin.Lerp(sea, .5f)) - origin;
    }

    internal Vector2[] PortRoad(Village town) => PortRoad(ObserveTownArt(town));
    private Vector2[] PortRoad(TownArtState town)
    {
        var start = VillagePlacement(town).Point(new Vector2(3.4f, -1.7f));
        var end = PortShore(town);
        var middle = start.Lerp(end, .5f) + (end - start).Orthogonal().Normalized() * 2.5f;
        return Enumerable.Range(0, 17).Select(i =>
        {
            float t = i / 16f;
            return start * (1 - t) * (1 - t) + middle * (2 * t * (1 - t)) + end * t * t;
        }).ToArray();
    }
    private void DrawPortRoad(Node2D canvas, TownArtState town, Vector2 center) =>
        canvas.DrawPolyline(PortRoad(town).Select(p => p + center).ToArray(), new Color("d5c29b"), 4, true);

    private void DrawPort(Node2D canvas, TownArtState town, Vector2 center)
    {
        var sea = Projection.GridToWorld(town.PortBerth) - Projection.GridToWorld(town.Position);
        var axis = sea.Normalized();
        var side = axis.Orthogonal();
        var shore = center + PortShore(town);
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
        var placement = VillagePlacement(town);
        float below = placement.Offset.Y + (placement.Compact ? 19 : 26);
        canvas.DrawString(ThemeDB.FallbackFont, new Vector2(placement.Offset.X - width * .5f, below), town.Name, fontSize: 13, modulate: Colors.Black);
        for (int i = 0; i < 5; i++)
            canvas.DrawRect(new Rect2(new Vector2(placement.Offset.X -18 + i * 8, below + 4), new Vector2(5, 4)), i < town.Level ? accent : new Color("475857"));
        if (!_townHealth.TryGetValue(town.Id, out var animation))
            _townHealth[town.Id] = animation = new AmphoraHealthAnimation();
        float time = TownAnimationTime;
        animation.Observe(town.Health, town.MaxHealth, time);
        AmphoraBadgeArt.Draw(canvas, placement.Offset + new Vector2(38, -35), town.Health, town.MaxHealth, accent, null, animation.Motion(time));
    }
}
