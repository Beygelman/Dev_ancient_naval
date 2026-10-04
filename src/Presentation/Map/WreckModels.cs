using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

internal static class WreckModels
{
    internal static WreckPart[] Build(ShipSnapshot ship, Color accent, FleetColor faction)
    {
        var parts = new List<WreckPart>();
        void Add(WreckPartKind kind, WreckMesh mesh, Vector3 pivot, Vector3 drift, Vector3 spin, float release, float sink = 22)
            => parts.Add(new(kind, pivot, drift, spin, release, sink, mesh.Faces));
        if (ship.Class == ShipClass.Balloon)
        {
            var basket = new WreckMesh(); basket.Box(0, 0, 37, 13, 10, 9, new("90714c"));
            Add(WreckPartKind.House, basket, new(0, 0, 37), new(3, 7, 0), new(.8f, .5f, .4f), .04f, 58);
            for (int panel = 0; panel < 6; panel++)
            {
                var cloth = new WreckMesh();
                for (int band = 0; band < 8; band++)
                {
                    Vector3 Point(int edge, int tier)
                    {
                        float lat = -Mathf.Pi / 2 + tier * Mathf.Pi / 8, lon = (panel + edge) * Mathf.Tau / 6;
                        return new(MathF.Cos(lon) * MathF.Cos(lat) * 28, MathF.Sin(lon) * MathF.Cos(lat) * 28, 88 + MathF.Sin(lat) * 29);
                    }
                    cloth.Cloth(panel % 2 == 0 ? accent : new("e7d8b4"), Point(0, band), Point(1, band), Point(1, band + 1), Point(0, band + 1));
                }
                Add(WreckPartKind.Deck, cloth, new(0, 0, 88), new(MathF.Cos(panel) * 14, MathF.Sin(panel) * 14, -8),
                    new(.3f + panel * .08f, .6f, .2f), panel * .04f, 48);
            }
        }
        else if (ship.Class == ShipClass.FishingDock)
        {
            for (int i = 0; i < 3; i++)
            {
                var deck = new WreckMesh(); deck.Box(-18 + i * 18, 0, 0, 17, 24, 4, new("bca77c"));
                Add(WreckPartKind.Deck, deck, new(-18 + i * 18, 0, 0), new((i - 1) * 10, 5, 0), new(.55f, (i - 1) * .4f, .2f), i * .07f);
            }
            var hut = new WreckMesh(); hut.Box(-8, 0, 4, 12, 10, 10, new("e7d8b4"), true); hut.Pyramid(-8, 0, 14, 7, 5, accent);
            Add(WreckPartKind.House, hut, new(-8, 0, 4), new(-10, 7, 3), new(.9f, .6f, .3f), .13f);
            var post = new WreckMesh(); post.Cylinder(new(16, 0, 0), new(16, 0, 20), .7f, new("90714c"));
            Add(WreckPartKind.Mast, post, new(16, 0, 0), new(9, 8, 0), new(1.5f, .4f, .2f), .1f);
        }
        else if (ship.Class == ShipClass.Mothership)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var keel = new WreckMesh();
                // Rounded pontoons detach independently; the port pontoon floods first.
                var outline = WreckMesh.Ellipse(0, side * 21, 7, 7, 24)
                    .Select(p => new Vector2(p.X + MathF.Sign(p.X) * 35, p.Y)).ToArray();
                keel.Extrude(outline, -1, 8, new("7b806b"), accent.Darkened(.45f));
                Add(WreckPartKind.Keel, keel, new(0, side * 21, -2), new(1, side * 14, 0), new(side * .85f, .12f, side * .12f), side < 0 ? .05f : .46f, side < 0 ? 30 : 16);
            }
            for (int level = 0; level < 3; level++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float rx = 36 - level * 10, ry = 23 - level * 6.5f, cx = level == 1 ? -3 : 0, cy = -level, z = level * 3;
                    var polygon = WreckMesh.Ellipse(cx, cy, rx, ry, 32);
                    var half = new List<Vector2>();
                    for (int i = 0; i < polygon.Length; i++)
                    {
                        var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length];
                        bool inside = a.Y * side >= 0, next = b.Y * side >= 0;
                        if (inside) half.Add(a);
                        if (inside != next) half.Add(a.Lerp(b, -a.Y / (b.Y - a.Y)));
                    }
                    var deck = new WreckMesh(); deck.Extrude(half.ToArray(), z, 3, new(level == 1 ? "ccb88d" : "ddcaa0"), new("ac9976"));
                    Add(WreckPartKind.Deck, deck, new(cx, side * ry * .45f, z), new(side * 3, side * (8 + level * 2), 0),
                        new(side * (.42f + level * .13f), .07f, side * .08f), .18f + level * .08f + (side > 0 ? .28f : 0), side < 0 ? 24 : 15);
                }
            int count = Math.Min(CityShipArt.Homes.Length, 3 + ship.Level * 3);
            for (int i = 0; i < count; i++)
            {
                var home = CityShipArt.Homes[i]; float height = home.H + (ship.Level - 3) * 1.1f;
                var mesh = new WreckMesh(); mesh.Box(home.X, home.Y, home.Z, home.W, home.D, height,
                    new Color(i % 3 == 0 ? "eadbb5" : i % 3 == 1 ? "dbc29b" : "f0e3c5"), true);
                mesh.Box(home.X, home.Y, home.Z + height, home.W + .5f, home.D + .5f, .6f, accent.Darkened(.12f));
                Add(WreckPartKind.House, mesh, new(home.X, home.Y, home.Z), new(home.X * .45f, home.Y * .55f, 5 + i % 3),
                    new((i % 2 == 0 ? 1 : -1) * 1.2f, .6f - i % 3 * .6f, (i % 3 - 1) * .8f), .22f + i % 5 * .075f, 24 + i % 4 * 3);
            }
            Sanctuary(parts, faction, ship.Level);
            if (ship.HasMortar) Gun(parts, new(10, 0, 3), 1, .28f, true);
        }
        else if (ship.Class is ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.Lighthouse)
        {
            for (int level = 0; level < 3; level++)
            {
                var stone = new WreckMesh(); stone.Box(0, 0, level * 12, 15 - level * 2, 13 - level * 2, 12, new("d2c3a4"), true);
                Add(WreckPartKind.Rubble, stone, new(0, 0, level * 12), new((level - 1) * 12, 7 + level * 3, 0), new(.8f, level * .4f, .1f), level * .08f, 29);
            }
            if (ship.Class != ShipClass.Lighthouse) Gun(parts, new(0, 0, 35), 1, .12f, true);
        }
        else
        {
            float length = ship.Class == ShipClass.Kolonel ? 34 : ship.Class == ShipClass.Invader ? 31 : 29;
            float width = ship.Class == ShipClass.Togus ? 12 : ship.Class == ShipClass.Kolonel ? 11 : 10;
            var outline = WreckMesh.Ellipse(0, 0, length, width)
                .Select(p => new Vector2(p.X, p.Y * (1 - MathF.Max(0, p.X / length) * .22f))).ToArray();
            for (int side = -1; side <= 1; side += 2)
            {
                var half = new List<Vector2>();
                foreach (var p in outline) if (p.Y * side >= -.0001f) half.Add(p);
                var hull = new WreckMesh(); hull.Extrude(half.ToArray(), 1, 8, new("dcc69b"), new("90714c"));
                Add(WreckPartKind.Keel, hull, new(0, side * width * .5f, 0), new(-3, side * 6, 0),
                    new(side * .75f, ship.Id % 2 == 0 ? .36f : -.6f, .07f), side < 0 ? .04f : .3f, side < 0 ? 23 : 17);
            }
            var cabin = new WreckMesh(); cabin.Box(-length * .61f, 0, 5, 12, 9, 9, new("e7d8b4"), true);
            cabin.Box(-length * .61f, 0, 14, 12, 9, 1, accent);
            Add(WreckPartKind.House, cabin, new(-length * .61f, 0, 5), new(-9, 7, 4), new(.7f, .8f, .3f), .24f);
            if (ship.Class == ShipClass.Fishing)
            {
                for (int i = 0; i < 2; i++)
                {
                    var home = new WreckMesh(); home.Box(-2 + i * 14, i == 0 ? 4 : -3, 1.5f, 9 - i, 7, 10 - i * 2, new("e7d8b4"), true);
                    Add(WreckPartKind.House, home, new(-2 + i * 14, i == 0 ? 4 : -3, 2), new(i * 6 - 2, 9 - i * 16, 4), new(1, .8f, .2f), .15f + i * .1f);
                }
            }
            else
            {
                int masts = ship.Class == ShipClass.Kolonel ? 3 : ship.Class is ShipClass.Invader or ShipClass.PirateSchooner ? 2 : 1;
                for (int i = 0; i < masts; i++)
                {
                    float x = masts == 1 ? 1 : -6 + i * 15, height = ship.Class == ShipClass.Togus ? 25 : ship.Class == ShipClass.Kolonel ? 43 - i * 3 : ship.Class == ShipClass.Invader ? 38 - i * 4 : 35;
                    var mast = new WreckMesh(); mast.Cylinder(new(x, 0, 1), new(x, 0, height + 3), .75f, new("8f7652"));
                    mast.Cylinder(new(x, -width, height - 3), new(x, width, height - 3), .55f, new("b69968"));
                    for (int strip = 0; strip < 6; strip++)
                    {
                        Vector3 Sheet(int j, bool top)
                        {
                            float f = j / 6f; return new(x + (top ? .6f : MathF.Sin(f * Mathf.Pi) * 4), (f * 2 - 1) * width,
                            top ? height - 4 + MathF.Sin(f * Mathf.Pi) : 11 + MathF.Sin(f * Mathf.Pi) * 2);
                        }
                        mast.Cloth(new Color(ship.Class == ShipClass.PirateSchooner ? "404645" : "f1e6c8").Darkened(strip * .014f),
                            Sheet(strip, false), Sheet(strip + 1, false), Sheet(strip + 1, true), Sheet(strip, true));
                    }
                    Add(WreckPartKind.Mast, mast, new(x, 0, 1), new(i * 4 - 4, 12 - i * 7, 2), new(i % 2 == 0 ? 1.8f : -1.65f, .4f, .15f), .1f + i * .13f, 18);
                }
                if (ship.Class == ShipClass.Togus) Gun(parts, new(5, 0, 3), 1, .21f, true);
                else
                {
                    int guns = ship.Class == ShipClass.Kolonel ? 3 : ship.Class == ShipClass.Invader ? 2 : 1;
                    for (int i = 0; i < guns; i++) for (int side = -1; side <= 1; side += 2) Gun(parts, new(-7 + i * 10, side * (width - 4), 1.4f), side, .17f + i * .05f);
                }
            }
        }
        return parts.ToArray();
    }

    private static void Gun(List<WreckPart> parts, Vector3 origin, int side, float release, bool mortar = false)
    {
        var mesh = new WreckMesh(); mesh.Box(origin.X, origin.Y, origin.Z, 4, 4, 1.6f, new("86654a"));
        var a = origin + Vector3.Back * 2.4f; var b = a + new Vector3(0, side * (mortar ? 6 : 8), mortar ? 6 : 0);
        mesh.Cylinder(a, b, mortar ? 2.7f : 1.35f, new("555d58"));
        mesh.Cylinder(b, b + (b - a).Normalized() * .3f, mortar ? 1.9f : .9f, new("253638"));
        parts.Add(new(WreckPartKind.Gun, origin, new(4, side * 18, 3), new(side * 2.4f, 1.6f, .4f), release, 35, mesh.Faces));
    }

    private static void Sanctuary(List<WreckPart> parts, FleetColor faction, int level)
    {
        float scale = 1 + .1f * (level - 1); float baseZ = 6;
        var tower = new WreckMesh(); tower.Box(0, -2, baseZ, 8, 7, 16 * scale, new("f1e6cf"), true);
        if (faction is FleetColor.Green or FleetColor.Red)
        { tower = new WreckMesh(); tower.Box(0, -2, baseZ, 6, 5, 2, new("b49c74")); }
        parts.Add(new(WreckPartKind.Sanctuary, new(0, -2, baseZ), new(-5, 10, 0), new(1.1f, .5f, .2f), .26f, 22, tower.Faces));
        var top = new WreckMesh(); float z = baseZ + 16 * scale;
        switch (faction)
        {
            case FleetColor.Blue:
                for (int band = 0; band < 5; band++)
                {
                    float a = band * Mathf.Pi / 10, b = (band + 1) * Mathf.Pi / 10;
                    var low = WreckMesh.Ellipse(0, -2, MathF.Cos(a) * 4, MathF.Cos(a) * 4, 12);
                    var high = WreckMesh.Ellipse(0, -2, MathF.Cos(b) * 4, MathF.Cos(b) * 4, 12);
                    for (int i = 0; i < 12; i++)
                    {
                        int j = (i + 1) % 12; top.Face(new("d3ad4d"), new(low[i].X, low[i].Y, z + MathF.Sin(a) * 4),
                        new(low[j].X, low[j].Y, z + MathF.Sin(a) * 4), new(high[j].X, high[j].Y, z + MathF.Sin(b) * 4), new(high[i].X, high[i].Y, z + MathF.Sin(b) * 4));
                    }
                }
                break;
            case FleetColor.Purple: top.Pyramid(0, -2, z, 5, 6, new("c4cbd1")); break;
            case FleetColor.Yellow: top.Pyramid(0, -2, z, 6, 8, new("d98cac")); top.Pyramid(0, -2, z + 8, 1.8f, 4, new("fff5ef")); break;
            case FleetColor.White:
                top.Box(-2, -2, z, 1.6f, 1.5f, 11, new("aa3047"));
                top.Box(0, -2, z + 9, 5, 1.5f, 2, new("aa3047")); top.Box(0, -2, z + 5, 5, 1.5f, 1.7f, new("aa3047"));
                top.Box(2, -2, z + 6, 1.5f, 1.5f, 4, new("aa3047"));
                top.Cylinder(new(-.3f, -2, z + 5), new(3, -2, z), .9f, new("aa3047")); break;
            case FleetColor.Green:
                z = baseZ + 6; top.Box(0, -2, z, 2, 2, 10, new("584b36"));
                for (int tier = 0; tier < 3; tier++) top.Pyramid(0, -2, z + 4 + tier * 4, 8 - tier * 1.6f, 8, new("235740")); break;
            case FleetColor.Red:
                z = baseZ + 2; for (int i = 0; i < 3; i++) top.Pyramid((i - 1) * 3, -2 + (i % 2) * 2, z, 3 - i * .3f, 12 + i * 4, new(i == 1 ? "d95a6b" : "bc3c56")); break;
        }
        parts.Add(new(WreckPartKind.Sanctuary, new(0, -2, z), new(11, -7, 2), new(-1.2f, -.8f, .5f), .12f, 28, top.Faces));
        for (int i = 0; i < 3; i++)
        {
            var rubble = new WreckMesh(); rubble.Box(i - 1, -2, baseZ + 8 + i * 4, 2, 2, 2, new("dfd2b6"));
            parts.Add(new(WreckPartKind.Rubble, new(i - 1, -2, baseZ + 8 + i * 4), new((i - 1) * 16, -8 + i * 5, 8),
                new(1 + i * .4f, -.8f + i, .3f), .24f + i * .06f, 32, rubble.Faces));
        }
    }
}
