using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    private readonly Dictionary<int, SanctuaryTurnPulse> _shipTurnPulses = new();
    private BattleState? _shipPulseBattle;
    internal int ActiveSanctuaryPulseCount => _shipTurnPulses.Count;
    internal int ProcessingSanctuaryPulseCount => _shipTurnPulses.Values.Count(p => p.IsProcessing());

    /// <summary>Finite presentation-only brightening of the human flagship shrine.
    /// The child inherits actual hull movement, bob, bank, roll and native depth.</summary>
    public int PulseOwnedSanctuaries(Side owner)
    {
        if (owner != Side.Player) return 0;
        EnsureVisualBattle();
        if (!ReferenceEquals(_shipPulseBattle, Battle))
        {
            foreach (var old in _shipTurnPulses.Values.ToArray()) old.QueueFree();
            _shipTurnPulses.Clear();
            _shipPulseBattle = Battle;
        }
        var original = Battle;
        int created = 0;
        foreach (var ship in original.Ships)
        {
            if (ship.Owner != owner || ship.Definition.Class != ShipClass.Mothership ||
                ship.Health <= 0 || _sinking.ContainsKey(ship.Id)) continue;
            if (!_hulls.TryGetValue(ship.Id, out var hull))
            {
                ShowHull(ShipSnapshot.From(ship), Projection.GridToWorld(ship.Position));
                hull = _hulls[ship.Id];
            }
            int id = ship.Id;
            if (_shipTurnPulses.Remove(id, out var previous)) previous.QueueFree();
            var profile = ShipVisualProfile.For(ShipClass.Mothership);
            var pulse = new SanctuaryTurnPulse
            {
                Name = "TurnShrineFlare" + id,
                Nation = original.ColorFor(owner),
                Seed = id,
                Project = (x, y, z) => DeckProjection.Point(x, y - 2,
                    z * (1 + .1f * (ship.Level - 1)) + 6,
                    BaseHeading(ShipClass.Mothership, owner) + DeckAngle(id), profile.Size, profile.DeckWidth) + new Vector2(0, -5),
                CanDraw = () => ReferenceEquals(Battle, original) &&
                    original.Find(id) is { Owner: Side.Player, Health: > 0 } current &&
                    current.Definition.Class == ShipClass.Mothership && !_sinking.ContainsKey(id) &&
                    original.Vision.IsVisible(Side.Player, current.Position),
                Finished = finished =>
                {
                    if (_shipTurnPulses.TryGetValue(id, out var current) && ReferenceEquals(current, finished))
                        _shipTurnPulses.Remove(id);
                }
            };
            _shipTurnPulses[id] = pulse;
            hull.Canvas.AddChild(pulse);
            created++;
        }
        return created;
    }
}
