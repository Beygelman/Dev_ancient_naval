using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;
public partial class FleetView
{
    private readonly Dictionary<int, float> _sinking = new();
    private readonly HashSet<int> _playedWrecks = new();
    internal int SinkingCount => _sinking.Count;

    private async Task Sink(ShipSnapshot ship, bool visible)
    {
        if (!_playedWrecks.Add(ship.Id))
            return;
        if (visible)
        {
            _snapshots[ship.Id] = ship with
            {
                Health = 0
            };
            _suppressed.Add(ship.Id);
            _sinking[ship.Id] = 0;
            var center = Projection.GridToWorld(ship.Position);
            EmitRipple(center, ShipVisualProfile.For(ship.Class).Size * 1.4f);
            await TweenValue(ship.Class == ShipClass.Mothership ? 1.65 : .95, t => _sinking[ship.Id] = t);
            _sinking.Remove(ship.Id);
            _snapshots.Remove(ship.Id);
        }

        if (ship.Class != ShipClass.Mothership)
            return;
        var followers = Battle.OwnShips(ship.Owner).Select(s => (Ship: ShipSnapshot.From(s), Visible: s.Owner == Side.Player || Battle.Vision.IsVisible(Side.Player, s.Position))).ToArray();
        // The flagship wreck finishes first. Its fleet then sinks together.
        foreach (var follower in followers)
            _suppressed.Add(follower.Ship.Id);
        Battle.CompleteFleetCollapse(ship.Owner);
        await Task.WhenAll(followers.Select(follower => Sink(follower.Ship, follower.Visible)));
    }

    private void DrawWreck(ShipSnapshot ship, Vector2 center)
    {
        float t = _sinking[ship.Id];
        float size = ShipVisualProfile.For(ship.Class).Size;
        // Broken hull slabs lean apart while a water-colored veil climbs upward.
        if (ship.Class == ShipClass.Mothership)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var p = center + new Vector2(side * (12 + t * 22) * size, t * 15);
                Ink.DrawColoredPolygon(new[] { p + new Vector2(-17, -9), p + new Vector2(17, -6), p + new Vector2(13, 7), p + new Vector2(-15, 5) }, new Color(.57f, .43f, .27f, 1 - t));
            }
        }

        Ink.DrawSetTransform(center + new Vector2(0, 10), 0, new Vector2(1, .43f));
        Ink.DrawCircle(Vector2.Zero, 37 * size, new Color(.19f, .35f, .42f, t * .86f));
        Ink.DrawArc(Vector2.Zero, (28 + t * 20) * size, 0, Mathf.Tau, 32, new Color(.70f, .88f, .89f, (1 - t) * .65f), 1.6f, true);
        Ink.DrawSetTransform(Vector2.Zero);
    }
}
