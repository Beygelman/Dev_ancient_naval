using System;
using System.Linq;
<<<<<<< Updated upstream
using System.Collections.Generic;
=======
>>>>>>> Stashed changes
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
<<<<<<< Updated upstream
    private readonly CityShipArt _cityShip = new();
    private readonly VesselArt _vesselArt = new();
    private void DrawShip(ShipSnapshot ship, Vector2 center, bool silhouette = false)
=======
    private void DrawShip(ShipSnapshot ship, Vector2 center)
>>>>>>> Stashed changes
    {
        if (ship.Class == ShipClass.Balloon)
        {
            var balloon = center + new Vector2(0, -62);
            var color = ship.IsAncient ? new Color("c7aa65") : FleetPalette.For(Battle, ship.Owner);
<<<<<<< Updated upstream
            if (!silhouette) Ink.DrawCircle(center, 12, new Color(0, 0, 0, 0.2f));
            Ink.DrawLine(balloon + new Vector2(-12, 10), balloon + new Vector2(-6, 32), color, 2, true);
            Ink.DrawLine(balloon + new Vector2(12, 10), balloon + new Vector2(6, 32), color, 2, true);
            DrawBalloonEnvelope(balloon, color);
            Ink.DrawArc(balloon, 20, 0, Mathf.Tau, 32, new Color("f8ecc7"), 2, true);
            DrawBalloonBasket(balloon, color);
            if (ship.Id == SelectedId)
                Ink.DrawArc(balloon, 25, 0, Mathf.Tau, 32, new Color("ffe298"), 2, true);
            for (int turn = 0; turn < 3; turn++)
                Ink.DrawCircle(balloon + new Vector2(-8 + turn * 8, 46), 2.6f, turn < 3 - ship.BombCooldown ? new Color("fff0b8") : new Color("455b63"));
            if (ship.IsAncient)
            {
                Ink.DrawArc(balloon, 13, 0, Mathf.Tau, 6, new Color("6a734f"), 2, true);
                Ink.DrawLine(balloon + new Vector2(-18, 0), balloon + new Vector2(18, 0), new Color("f9dfa5"), 2, true);
=======
            DrawCircle(center, 12, new Color(0, 0, 0, 0.2f));
            DrawLine(balloon + new Vector2(-12, 10), balloon + new Vector2(-6, 32), color, 2, true);
            DrawLine(balloon + new Vector2(12, 10), balloon + new Vector2(6, 32), color, 2, true);
            DrawCircle(balloon, 20, color);
            DrawArc(balloon, 20, 0, Mathf.Tau, 32, new Color("f8ecc7"), 2, true);
            DrawLine(balloon + new Vector2(0, -19), balloon + new Vector2(0, 18), new Color("e2f6dd"), 4, true);
            DrawRect(new Rect2(balloon + new Vector2(-7, 29), new Vector2(14, 9)), new Color("baa478"));
            if (ship.Id == SelectedId)
                DrawArc(balloon, 25, 0, Mathf.Tau, 32, new Color("ffe298"), 2, true);
            for (int turn = 0; turn < 3; turn++)
                DrawCircle(balloon + new Vector2(-8 + turn * 8, 46), 2.6f, turn < 3 - ship.BombCooldown ? new Color("fff0b8") : new Color("455b63"));
            if (ship.IsAncient)
            {
                DrawArc(balloon, 13, 0, Mathf.Tau, 6, new Color("6a734f"), 2, true);
                DrawLine(balloon + new Vector2(-18, 0), balloon + new Vector2(18, 0), new Color("f9dfa5"), 2, true);
>>>>>>> Stashed changes
            }

            if (ship.BombCooldown == 0)
            {
                var bomb = balloon + new Vector2(14, 34);
<<<<<<< Updated upstream
                Ink.DrawCircle(bomb, 5, new Color("263b44"));
                Ink.DrawArc(bomb, 5, 0, Mathf.Tau, 14, new Color("f4db9b"), 1.4f, true);
                Ink.DrawLine(bomb + new Vector2(0, -5), bomb + new Vector2(3, -9), new Color("ffcb77"), 1.6f, true);
            }

            return;
        }

        if (ship.Class == ShipClass.Lighthouse)
        {
            DrawLighthouse(center, FleetPalette.For(Battle, ship.Owner));
            if (ship.Id == SelectedId)
                Ink.DrawArc(center + LighthouseOffset, 20, 0, Mathf.Tau, 32, new Color("ffe298"), 1.7f, true);
=======
                DrawCircle(bomb, 5, new Color("263b44"));
                DrawArc(bomb, 5, 0, Mathf.Tau, 14, new Color("f4db9b"), 1.4f, true);
                DrawLine(bomb + new Vector2(0, -5), bomb + new Vector2(3, -9), new Color("ffcb77"), 1.6f, true);
            }

            DrawString(ThemeDB.FallbackFont, balloon + new Vector2(23, 6), ship.Health.ToString("0"),
                fontSize: 16, modulate: new Color(ship.Owner == Side.Player ? "85e6a0" : "f05d55"));
>>>>>>> Stashed changes
            return;
        }

        var profile = ShipVisualProfile.For(ship.Class);
        float size = profile.Size;
<<<<<<< Updated upstream
        // Bob/roll use the retained hull canvas transform; floor geometry changes only with heading.
        var bob = Vector2.Zero;
        float roll = 0;
        float sinking = _sinking.GetValueOrDefault(ship.Id);
        bob.Y += sinking * 32;
        roll += sinking * .13f;
        float yaw = BaseHeading(ship.Class, ship.Owner) + DeckAngle(ship.Id) + roll;
        Vector2 P(float x, float y, float z) => center + bob + DeckProjection.Point(x, y, z, yaw, size, profile.DeckWidth) + new Vector2(0, -5);
        Vector2 Point(float x, float y) => P(x, y, 0);
        Vector2 StructurePoint(float x, float y) => center + new Vector2(x, y) * size;
        var accent = FleetPalette.For(Battle, ship.Owner);
        Ink.DrawSetTransform(center + new Vector2(0, 4), 0, new Vector2(1, 0.45f));
        if (!silhouette) Ink.DrawCircle(Vector2.Zero, 30 * size, new Color(0, 0, 0, 0.3f));
        Ink.DrawSetTransform(Vector2.Zero);
        if (ship.Id == SelectedId)
            Ink.DrawArc(center, 34 * size, 0, Mathf.Tau, 40, new Color("ffe298"), 2, true);
        if (ship.Class == ShipClass.AncientGun)
        {
            DrawAncientTower(StructurePoint, accent);
            DrawMortarBarrel(ship, center + new Vector2(0, -30), size, new Color("c4b780"));
        }
        else if (ship.Class == ShipClass.CannonTower)
        {
            DrawCannonTower(StructurePoint, accent);
            DrawMortarBarrel(ship, center + new Vector2(0, -20), size, new Color("b7a77a"));
        }
        else if (ship.Class == ShipClass.FishingDock)
        {
            DrawReefHarbor(P, accent);
        }
        else if (ship.Class == ShipClass.Mothership)
        {
            _cityShip.Draw(Ink, P, accent, _clock, sinking, Battle.ColorFor(ship.Owner), ship.Level);
        }
        else
        {
            _vesselArt.Draw(Ink, P, ship.Class, accent, ship.IsExhausted);
            if (ship.Class == ShipClass.PirateSchooner)
                DrawPirateFlag((x, y) => P(x, 0, -y));
            if (ship.Class == ShipClass.Togus)
                DrawMortarBarrel(ship, P(5, 0, 4), size, new Color("c9c4b4"));
=======
        var (bob, roll) = HullMotion(ship);
        float facing = ship.Owner == Side.Player ? 1 : -1;
        float deckAngle = DeckAngle(ship.Id);
        Vector2 Point(float x, float y)
        {
            float height = y < -10 ? y + 4 : 0;
            float floor = y < -10 ? -4 : y;
            return center + bob + new Vector2(x * facing * size, floor * size * profile.DeckWidth).Rotated(deckAngle+roll) + new Vector2(0,height*size-5);
        }
        var accent = FleetPalette.For(Battle, ship.Owner);
        var hull = new[]
        {
            Point(-25, -1),
            Point(-19, -10),
            Point(14, -3),
            Point(29, 9),
            Point(12, 14),
            Point(-23, 6)
        };
        DrawSetTransform(center + new Vector2(0, 4), 0, new Vector2(1, 0.45f));
        DrawCircle(Vector2.Zero, 30 * size, new Color(0, 0, 0, 0.3f));
        DrawSetTransform(Vector2.Zero);
        if (ship.Id == SelectedId)
            DrawArc(center, 34 * size, 0, Mathf.Tau, 40, new Color("ffe298"), 2, true);
        if (ship.Class is not (ShipClass.Mothership or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.FishingDock))
        {
            DrawColoredPolygon(hull, accent.Darkened(.6f));
            DrawPolyline(hull.Append(hull[0]).ToArray(), accent, 2, true);
        }

        if (ship.Class == ShipClass.AncientGun)
        {
            DrawAncientTower(Point, accent);
            DrawMortarBarrel(ship,center+new Vector2(0,-30),size,new Color("c4b780"));
        }
        else if (ship.Class == ShipClass.CannonTower)
        {
            DrawCannonTower(Point,accent);
            DrawMortarBarrel(ship,center+new Vector2(0,-20),size,new Color("b7a77a"));
        }
        else if (ship.Class == ShipClass.FishingDock)
        {
            DrawFishingHarbor(Point, accent);
        }
        else if (ship.Class == ShipClass.Togus)
        {
            DrawColoredPolygon(new[] { Point(-18, -6), Point(11, -3), Point(22, 7), Point(-18, 4) }, new Color("a89c77"));
            DrawCircle(Point(0, 0), 11, accent.Darkened(.15f));
            DrawMortarBarrel(ship,Point(0,0),size,new Color("c9c4b4"));
        }
        else if (ship.Class == ShipClass.Mothership)
        {
            Vector2 TownPoint(float x, float y, float z) => center + bob
                + new Vector2(x * size, y * size * .7f).Rotated(deckAngle + roll)
                + new Vector2(0, -z * size - 5);
            foreach (float offset in new[] { -17f, 17f })
            {
                var twinHull = new[]
                {
                    TownPoint(-39, offset - 3, -3), TownPoint(-29, offset - 8, -3),
                    TownPoint(24, offset - 7, -3), TownPoint(42, offset, -3),
                    TownPoint(28, offset + 7, -3), TownPoint(-36, offset + 4, -3)
                };
                DrawColoredPolygon(twinHull, accent.Darkened(.62f));
                DrawPolyline(twinHull.Append(twinHull[0]).ToArray(), accent, 1.5f, true);
            }
            FloatingTownArt.Draw(this,TownPoint,accent,_clock,false);
        }
        else
        {
            int masts = ship.Class == ShipClass.Kolonel ? 3 : ship.Class is ShipClass.Invader or ShipClass.PirateSchooner ? 2 : 1;
            for (int i = 0; i < masts; i++)
            {
                float x = -10 + i * 10;
                DrawLine(Point(x, 0), Point(x, -26), accent, 2, true);
                DrawColoredPolygon(new[] { Point(x + 1, -26), Point(x + 1, -6), Point(x + 14, -8) }, new Color(ship.Class == ShipClass.PirateSchooner ? "353b42" : ship.IsExhausted ? "82959b" : "f2e5c5"));
            }

            if (ship.Class == ShipClass.PirateSchooner)
                DrawPirateFlag(Point);
            if (ship.Class == ShipClass.Fishing)
            {
                DrawCircle(Point(13, 3), 7, new Color("c6bf93"));
                for (int x = 7; x <= 19; x += 4)
                    DrawLine(Point(x, -3), Point(x, 10), new Color("746e54"), 1);
            }
>>>>>>> Stashed changes
        }

        if (ship.Class == ShipClass.Mothership && ship.HasMortar)
        {
<<<<<<< Updated upstream
            Ink.DrawCircle(Point(10, 0), 6, new Color("cfc3a0"));
            DrawMortarBarrel(ship, Point(10, 0), size * .65f, new Color("dfd3b1"));
        }

        if (ship.IsVeteran && ship.Class is ShipClass.CannonTower or ShipClass.AncientGun)
        {
            Ink.DrawLine(StructurePoint(-10,-20), StructurePoint(10,-20), new Color("783541"), 2.2f, true);
            Ink.DrawLine(StructurePoint(-10,-16), StructurePoint(10,-16), new Color("783541"), 1.4f, true);
        }
        else if (ship.IsVeteran)
        {
            // Painted stern ribbons identify veteran crews without bolting an oversized structure to the deck.
            var burgundy = new Color("783541");
            for (int edge = 0; edge < 2; edge++)
            {
                float side = edge == 0 ? -5 : 5;
                DrawProjectedPolygon(new[] { P(-22,side,2), P(-9,side,2), P(-9,side,3.5f), P(-22,side,3.5f) }, burgundy);
                Ink.DrawLine(P(-22,side,5), P(-9,side,5), burgundy, 1.2f, true);
            }
        }

        if (!silhouette && !_sinking.ContainsKey(ship.Id))
        {
            for (int slot = 0; slot < ship.ProgressGoal; slot++)
            {
                var point = center + new Vector2((slot - (ship.ProgressGoal - 1) * .5f) * 9, ShipVisualProfile.ProgressY(ship.Class));
                var rect = new Rect2(point - new Vector2(4, 3), new Vector2(8, 6));
                Ink.DrawRect(rect, slot < ship.Progress ? new Color(ship.Class == ShipClass.Mothership ? "83e9ba" : "ffd66e") : new Color("17333e"));
                Ink.DrawRect(rect, new Color("b2c8bc"), false, 1);
            }
        }

        if (!silhouette && ship.IsExhausted)
            Ink.DrawCircle(center + new Vector2(28, 14), 4, new Color("c8c4b4"));
=======
            DrawCircle(Point(10, 0), 6, new Color("cfc3a0"));
            DrawMortarBarrel(ship,Point(10,0),size*.65f,new Color("dfd3b1"));
        }

        if (ship.IsVeteran)
        {
            // Raised quarterdeck at the stern, with a visible step and rail.
            DrawColoredPolygon(new[] { Point(-27, -3), Point(-13, -1), Point(-13, -10), Point(-27, -12) }, new Color("887f65"));
            DrawColoredPolygon(new[] { Point(-28, -12), Point(-17, -17), Point(-7, -8), Point(-15, -5) }, new Color("ddd0a2"));
            DrawLine(Point(-28, -13), Point(-16, -7), new Color("ebe4c4"), 2, true);
            DrawLine(Point(-28, -13), Point(-28, -20), new Color("d6d9d3"), 1.8f, true);
            DrawLine(Point(-16, -7), Point(-16, -14), new Color("d6d9d3"), 1.8f, true);
            DrawLine(Point(-28, -20), Point(-16, -14), new Color("d6d9d3"), 1.8f, true);
            // Silver figurehead at the bow: body, upturned head and spread wings.
            DrawLine(Point(25, 5), Point(30, -5), new Color("cdd9dd"), 3.2f, true);
            DrawCircle(Point(31, -7), 2.6f * size, new Color("f0f4ef"));
            DrawColoredPolygon(new[] { Point(30, -4), Point(21, -13), Point(25, -3), Point(34, 1), Point(39, -6) }, new Color("bacbd1"));
        }

        var hpPosition = center + new Vector2(-22, 20);
        DrawRect(new Rect2(hpPosition, new Vector2(44, 5)), new Color("10212b"));
        var healthColor = new Color(ship.Owner == Side.Player ? "85e6a0" : "f05d55");
        DrawRect(new Rect2(hpPosition, new Vector2(44f * (float)(ship.Health / ship.MaxHealth), 5)), healthColor);
        var hpText = ship.Health.ToString("0");
        var hpAt = center + new Vector2(28 * size, -26 * size);
        DrawRect(new Rect2(hpAt + new Vector2(-3, -17), new Vector2(hpText.Length * 13 + 6, 23)), new Color(0.02f, 0.07f, 0.1f, 0.62f));
        DrawString(ThemeDB.FallbackFont, hpAt, hpText, fontSize: 22, modulate: healthColor);
        for (int slot = 0; slot < ship.ProgressGoal; slot++)
        {
            var rect = new Rect2(center + new Vector2(-ship.ProgressGoal * 6 + slot * 12, 30), new Vector2(9, 6));
            DrawRect(rect, slot < ship.Progress ? new Color(ship.Class == ShipClass.Mothership ? "83e9ba" : "ffd66e") : new Color(0.03f, 0.09f, 0.12f, 0.7f));
            DrawRect(rect, new Color(0.65f, 0.82f, 0.86f, 0.8f), false, 1);
        }

        if (ship.IsExhausted)
            DrawCircle(center + new Vector2(28, 14), 4, new Color("c8c4b4"));
>>>>>>> Stashed changes
    }
}
