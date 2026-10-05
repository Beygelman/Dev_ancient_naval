using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.Map;

public partial class BoardView
{
    private readonly Dictionary<int, SanctuaryTurnPulse> _townTurnPulses = new();
    private BattleState? _townPulseBattle;
    internal int ActiveSanctuaryPulseCount => _townTurnPulses.Count;
    internal int ProcessingSanctuaryPulseCount => _townTurnPulses.Values.Count(p => p.IsProcessing());

    /// <summary>Presentation only. Main invokes this once at the real human turn
    /// boundary after the current world has been shown, never from Refresh.</summary>
    public int PulseOwnedSanctuaries(Side owner)
    {
        // The API is intentionally human-only: no hidden opponent shrine may
        // turn into a turn/faction beacon through this cosmetic presentation.
        if (owner != Side.Player || _depthGroup is null) return 0;
        if (!ReferenceEquals(_townPulseBattle, Battle))
        {
            foreach (var old in _townTurnPulses.Values.ToArray()) old.QueueFree();
            _townTurnPulses.Clear();
            _townPulseBattle = Battle;
        }
        var original = Battle;
        int created = 0;
        foreach (var town in original.Villages)
        {
            if (town.Owner != owner || town.Health <= 0 ||
                !original.Vision.IsVisible(Side.Player, town.Position)) continue;
            var parent = _depthGroup.GetNodeOrNull<Node2D>("Town" + town.Id);
            var placement = VillagePlacement(town);
            if (parent is null || placement.Scale <= 0) continue;
            if (_townTurnPulses.Remove(town.Id, out var previous)) previous.QueueFree();
            int id = town.Id;
            var pulse = new SanctuaryTurnPulse
            {
                Name = "TurnShrineFlare" + id,
                Transform = placement.Transform(Vector2.Zero),
                Nation = original.ColorFor(owner),
                Seed = id,
                Project = (x, y, z) => new Vector2((x - y + 4) * .85f,
                    (x + y - 4) * .42f - z * SanctuaryHeightScale(town.Level) - 1),
                CanDraw = () => ReferenceEquals(Battle, original) &&
                    original.VillageAt(town.Position) is { Owner: Side.Player, Health: > 0 } current &&
                    current.Id == id && original.Vision.IsVisible(Side.Player, current.Position),
                Finished = finished =>
                {
                    if (_townTurnPulses.TryGetValue(id, out var current) && ReferenceEquals(current, finished))
                        _townTurnPulses.Remove(id);
                }
            };
            _townTurnPulses[id] = pulse;
            parent.AddChild(pulse);
            created++;
        }
        return created;
    }
}
