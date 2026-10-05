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
    private readonly VesselArt _vesselArt = new();
    private readonly Dictionary<int, SeaGeometryBatch> _farHullBatches = new();
    private void DrawShip(ShipSnapshot ship, Vector2 center, bool silhouette = false, bool wreckSource = false)
    {
        if (!silhouette && !wreckSource && Landscape?.FarSceneryActive == true)
        {
            if (!_farHullBatches.TryGetValue(ship.Id, out var batch)) _farHullBatches.Add(ship.Id, batch = new(256));
            FarFleetArt.Draw(Ink, batch, ship, center, BaseHeading(ship.Class, ship.Owner) + DeckAngle(ship.Id), FleetPalette.For(Battle, ship.Owner));
            if (ship.Id == SelectedId) Ink.DrawArc(center, 34 * ShipVisualProfile.For(ship.Class).Size, 0, Mathf.Tau, 24, new Color("ffe298"), 2, true);
            return;
        }
        if (ship.Class == ShipClass.Balloon)
        {
            var balloon = center + new Vector2(0, -62);
            var color = ship.IsAncient ? new Color("c7aa65") : FleetPalette.For(Battle, ship.Owner);
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
            }

            if (ship.BombCooldown == 0)
            {
                var bomb = balloon + new Vector2(14, 34);
                Ink.DrawCircle(bomb, 5, new Color("263b44"));
                Ink.DrawArc(bomb, 5, 0, Mathf.Tau, 14, new Color("f4db9b"), 1.4f, true);
                Ink.DrawLine(bomb + new Vector2(0, -5), bomb + new Vector2(3, -9), new Color("ffcb77"), 1.6f, true);
            }

            return;
        }

        if (ship.Class == ShipClass.Lighthouse)
        {
            DrawLighthouse(center, FleetPalette.For(Battle, ship.Owner), ship.Id, !silhouette && !wreckSource);
            if (ship.Id == SelectedId)
                Ink.DrawArc(center + LighthouseOffset, 20, 0, Mathf.Tau, 32, new Color("ffe298"), 1.7f, true);
            return;
        }

        var profile = ShipVisualProfile.For(ship.Class);
        float size = profile.Size;
        // Bob/roll use the retained hull canvas transform; floor geometry changes only with heading.
        var bob = Vector2.Zero;
        float roll = 0;
        float sinking = wreckSource ? 0 : _sinking.GetValueOrDefault(ship.Id);
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
        if (!silhouette && ship.Id == SelectedId)
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
            _cityShip.Draw(Ink, P, accent, _clock, sinking, Battle.ColorFor(ship.Owner), ship.Level,
                effects: !silhouette && !wreckSource);
        }
        else
        {
            _vesselArt.Draw(Ink, P, ship.Class, accent, ship.IsExhausted);
            if (ship.Class == ShipClass.PirateSchooner)
                DrawPirateFlag((x, y) => P(x, 0, -y));
            if (ship.Class == ShipClass.Togus)
                DrawMortarBarrel(ship, P(5, 0, 4), size, new Color("c9c4b4"));
        }

        if (ship.Class == ShipClass.Mothership && ship.HasMortar)
        {
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
    }
}
