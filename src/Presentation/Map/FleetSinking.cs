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
    private sealed record WreckArt(BoardTerrainLayer Root, WreckRenderer Geometry);
    private readonly Dictionary<int, WreckArt> _wreckArt = new();
    internal int WreckPieceCount => _wreckArt.Values.Sum(w => w.Geometry.Parts.Length);
    internal bool WrecksOpaque => _wreckArt.Values.All(w => w.Geometry.Opaque && w.Root.Modulate.A == 1);
    internal int WreckPartCount(WreckPartKind kind) => _wreckArt.Values.Sum(w => w.Geometry.Parts.Count(p => p.Kind == kind));
    internal int ImmersedWreckFaces => _wreckArt.Values.Sum(w => w.Geometry.ImmersedFaces);

    private WreckArt CreateWreck(ShipSnapshot ship, Vector2 center)
    {
        var parts = WreckModels.Build(ship, FleetPalette.For(Battle, ship.Owner), Battle.ColorFor(ship.Owner));
        var geometry = new WreckRenderer(parts, BaseHeading(ship.Class, ship.Owner) + DeckAngle(ship.Id),
            ShipVisualProfile.For(ship.Class), ship.Class == ShipClass.Mothership);
        var root = new BoardTerrainLayer
        {
            Name = "ImmersingWreck" + ship.Id,
            Position = center,
            DrawWorld = canvas => geometry.Draw(canvas)
        };
        AddChild(root);
        return new(root, geometry);
    }

    private void RemoveWreck(int id)
    {
        if (!_wreckArt.Remove(id, out var art)) return;
        art.Root.QueueFree();
    }

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
            _wreckArt[ship.Id] = CreateWreck(ship, center);
            EmitRipple(center, ShipVisualProfile.For(ship.Class).Size * 1.4f);
            float previous = 0;
            bool balloon = ship.Class == ShipClass.Balloon;
            float duration = ship.Class == ShipClass.Mothership ? 3.05f : balloon ? 2.7f : 2.2f;
            if (balloon) EmitSmoke(center + new Vector2(0, -42), new Vector2(1, -.5f), 1.2f, true);
            await TweenValue(duration, t =>
            {
                _sinking[ship.Id] = t;
                var art = _wreckArt[ship.Id];
                art.Geometry.Seconds = t * duration;
                art.Root.QueueRedraw();
                if (balloon)
                {
                    _fallingBalloons[ship.Id] = (center, t);
                    if (t >= .46f) CommitBalloonCrash(ship.Id);
                }
                if (t - previous > .14f) { EmitRipple(center, ShipVisualProfile.For(ship.Class).Size * (1.2f + t)); previous = t; }
            });
            RemoveWreck(ship.Id);
            _fallingBalloons.Remove(ship.Id);
            _sinking.Remove(ship.Id);
            _snapshots.Remove(ship.Id);
        }

        if (ship.Class == ShipClass.Balloon)
        {
            // Hidden crashes still resolve exactly once, without drawing their origin or victims.
            CommitBalloonCrash(ship.Id);
            await SinkBalloonVictims(ship.Id);
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

}
