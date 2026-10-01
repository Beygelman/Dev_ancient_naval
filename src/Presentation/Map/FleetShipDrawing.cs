using System;
using System.Linq;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private readonly CityShipArt _cityShip = new();
    private void DrawShip(ShipSnapshot ship, Vector2 center)
    {
        if (ship.Class == ShipClass.Balloon)
        {
            var balloon = center + new Vector2(0, -62);
            var color = ship.IsAncient ? new Color("c7aa65") : FleetPalette.For(Battle, ship.Owner);
            Ink.DrawCircle(center, 12, new Color(0, 0, 0, 0.2f));
            Ink.DrawLine(balloon + new Vector2(-12, 10), balloon + new Vector2(-6, 32), color, 2, true);
            Ink.DrawLine(balloon + new Vector2(12, 10), balloon + new Vector2(6, 32), color, 2, true);
            Ink.DrawCircle(balloon, 20, color);
            Ink.DrawArc(balloon, 20, 0, Mathf.Tau, 32, new Color("f8ecc7"), 2, true);
            Ink.DrawLine(balloon + new Vector2(0, -19), balloon + new Vector2(0, 18), new Color("e2f6dd"), 4, true);
            Ink.DrawRect(new Rect2(balloon + new Vector2(-7, 29), new Vector2(14, 9)), new Color("baa478"));
            if (ship.Id == SelectedId)
                Ink.DrawArc(balloon, 25, 0, Mathf.Tau, 32, new Color("ffe298"), 2, true);
            for (int turn = 0; turn < 3; turn++)
                Ink.DrawCircle(balloon + new Vector2(-8 + turn * 8, 46), 2.6f, turn < 3 - ship.BombCooldown ? new Color("fff0b8") : new Color("455b63"));
            if (ship.IsAncient)
            {
                Ink.DrawArc(balloon, 13, 0, Mathf.Tau, 6, new Color("6a734f"), 2, true);
                Ink.DrawLine(balloon + new Vector2(-18, 0), balloon + new Vector2(18, 0), new Color("f9dfa5"), 2, true);
            }

            if (ship.BombCooldown == 0)
            {
                var bomb = balloon + new Vector2(14, 34);
                Ink.DrawCircle(bomb, 5, new Color("263b44"));
                Ink.DrawArc(bomb, 5, 0, Mathf.Tau, 14, new Color("f4db9b"), 1.4f, true);
                Ink.DrawLine(bomb + new Vector2(0, -5), bomb + new Vector2(3, -9), new Color("ffcb77"), 1.6f, true);
            }

            Ink.DrawString(ThemeDB.FallbackFont, HealthAnchor(center, ship.Class), ship.Health.ToString("0"), fontSize: 16, modulate: new Color(ship.Owner == Side.Player ? "85e6a0" : "f05d55"));
            return;
        }

        var profile = ShipVisualProfile.For(ship.Class);
        float size = profile.Size;
        var(bob, roll) = HullMotion(ship);
        float sinking = _sinking.GetValueOrDefault(ship.Id);
        bob.Y += sinking * 32;
        roll += sinking * .13f;
        float yaw = BaseHeading(ship.Class, ship.Owner) + DeckAngle(ship.Id) + roll;
        Vector2 P(float x, float y, float z) => center + bob + DeckProjection.Point(x, y, z, yaw, size, profile.DeckWidth) + new Vector2(0, -5);
        Vector2 Point(float x, float y) => P(x, y, 0);
        Vector2 StructurePoint(float x, float y) => center + new Vector2(x, y) * size;
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
        Ink.DrawSetTransform(center + new Vector2(0, 4), 0, new Vector2(1, 0.45f));
        Ink.DrawCircle(Vector2.Zero, 30 * size, new Color(0, 0, 0, 0.3f));
        Ink.DrawSetTransform(Vector2.Zero);
        if (ship.Id == SelectedId)
            Ink.DrawArc(center, 34 * size, 0, Mathf.Tau, 40, new Color("ffe298"), 2, true);
        if (ship.Class is not (ShipClass.Mothership or ShipClass.AncientGun or ShipClass.CannonTower or ShipClass.FishingDock))
        {
            DrawProjectedPolygon(hull, accent.Darkened(.6f));
            Ink.DrawPolyline(hull.Append(hull[0]).ToArray(), accent, 2, true);
        }

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
        else if (ship.Class == ShipClass.Togus)
        {
            DrawProjectedPolygon(new[] { Point(-18, -6), Point(11, -3), Point(22, 7), Point(-18, 4) }, new Color("a89c77"));
            Ink.DrawCircle(Point(0, 0), 11, accent.Darkened(.15f));
            DrawMortarBarrel(ship, Point(0, 0), size, new Color("c9c4b4"));
        }
        else if (ship.Class == ShipClass.Mothership)
        {
            _cityShip.Draw(Ink, P, accent, _clock, sinking);
        }
        else
        {
            int masts = ship.Class == ShipClass.Kolonel ? 3 : ship.Class is ShipClass.Invader or ShipClass.PirateSchooner ? 2 : 1;
            for (int i = 0; i < masts; i++)
            {
                float x = -10 + i * 10;
                Ink.DrawLine(P(x, 0, 0), P(x, 0, 26), new Color("b69e71"), 2, true);
                DrawProjectedPolygon(new[] { P(x + 1, 0, 26), P(x + 1, 0, 6), P(x + 14, 0, 8) }, new Color(ship.Class == ShipClass.PirateSchooner ? "353b42" : ship.IsExhausted ? "82959b" : "f2e5c5"));
            }

            if (ship.Class == ShipClass.PirateSchooner)
                DrawPirateFlag((x, y) => P(x, 0, -y));
            if (ship.Class == ShipClass.Fishing)
            {
                Ink.DrawCircle(Point(13, 3), 7, new Color("c6bf93"));
                for (int x = 7; x <= 19; x += 4)
                    Ink.DrawLine(Point(x, -3), Point(x, 10), new Color("746e54"), 1);
            }
        }

        if (ship.Class == ShipClass.Mothership && ship.HasMortar)
        {
            Ink.DrawCircle(Point(10, 0), 6, new Color("cfc3a0"));
            DrawMortarBarrel(ship, Point(10, 0), size * .65f, new Color("dfd3b1"));
        }

        if (ship.IsVeteran && ship.Class is ShipClass.CannonTower or ShipClass.AncientGun)
        {
            Ink.DrawArc(center + new Vector2(0, -21 * size), 12 * size, Mathf.Pi, Mathf.Tau, 12, new Color("e2e9df"), 2, true);
            Ink.DrawCircle(center + new Vector2(0, -34 * size), 3 * size, new Color("e9d39b"));
        }
        else if (ship.IsVeteran)
        {
            DrawProjectedPolygon(new[] { P(-25, -5, 4), P(-12, -5, 4), P(-12, 5, 4), P(-25, 5, 4) }, new Color("d5c69a"));
            DrawProjectedPolygon(new[] { P(-25, 5, 0), P(-12, 5, 0), P(-12, 5, 4), P(-25, 5, 4) }, new Color("8e805f"));
            Ink.DrawLine(P(-25, 5, 8), P(-12, 5, 8), new Color("dce2d6"), 1.4f, true);
            Ink.DrawLine(P(-25, 5, 4), P(-25, 5, 8), new Color("dce2d6"), 1.4f, true);
            Ink.DrawLine(P(-12, 5, 4), P(-12, 5, 8), new Color("dce2d6"), 1.4f, true);
            Ink.DrawLine(P(27, 0, 1), P(31, 0, 8), new Color("cedbdf"), 2.5f, true);
            Ink.DrawCircle(P(31, 0, 9), 2 * size, new Color("f1f3e9"));
            DrawProjectedPolygon(new[] { P(30, 0, 6), P(25, -5, 10), P(28, 0, 7), P(34, 5, 10) }, new Color("b9cbd1"));
        }

        if (!_sinking.ContainsKey(ship.Id))
        {
            var healthColor = new Color(ship.Owner == Side.Player ? "85e6a0" : "f05d55");
            var hpAt = HealthAnchor(center);
            Ink.DrawString(ThemeDB.FallbackFont, hpAt, ship.Health.ToString("0"), fontSize: 22, modulate: healthColor);
            for (int slot = 0; slot < ship.ProgressGoal; slot++)
            {
                float angle = Mathf.Pi / 2 + (slot - (ship.ProgressGoal - 1) * .5f) * .22f;
                var point = center + Vector2.FromAngle(angle) * 39;
                var rect = new Rect2(point - new Vector2(4, 3), new Vector2(8, 6));
                Ink.DrawRect(rect, slot < ship.Progress ? new Color(ship.Class == ShipClass.Mothership ? "83e9ba" : "ffd66e") : new Color("17333e"));
                Ink.DrawRect(rect, new Color("b2c8bc"), false, 1);
            }
        }

        if (ship.IsExhausted)
            Ink.DrawCircle(center + new Vector2(28, 14), 4, new Color("c8c4b4"));
    }
}
