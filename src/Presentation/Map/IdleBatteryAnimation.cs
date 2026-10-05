using System;
using System.Collections.Generic;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    private readonly Dictionary<int, IdleBattery> _idleBatteries = new();
    private sealed class IdleBattery
    {
        internal float From = -1.15f, To = -1.15f, Start, Next, LastTick = -1;
        internal int Sequence;
    }

    private static float CosmeticUnit(int seed, int id, int sequence, uint salt)
    {
        uint hash = unchecked((uint)seed ^ (uint)id * 0x9e3779b9u ^ (uint)sequence * 0x85ebca6bu ^ salt);
        hash ^= hash >> 16;
        hash *= 0x7feb352du;
        hash ^= hash >> 15;
        hash *= 0x846ca68bu;
        hash ^= hash >> 16;
        return (hash & 0xffffff) / 16777216f;
    }

    internal static float IdleBatteryInterval(int seed, int id, int sequence) =>
        5 + CosmeticUnit(seed, id, sequence, 3811) * 5;

    internal float PassiveBatteryHeading(int id) => _barrelAngles.GetValueOrDefault(id, -1.15f);

    private void UpdateIdleBattery(ShipSnapshot ship)
    {
        if (ship.Class is not (ShipClass.AncientGun or ShipClass.CannonTower)) return;
        if (!_idleBatteries.TryGetValue(ship.Id, out var idle))
        {
            idle = new IdleBattery { Next = _clock + IdleBatteryInterval(Battle.Board.Seed, ship.Id, 0) };
            _idleBatteries[ship.Id] = idle;
        }
        // Gun-command aiming always owns the visible barrel during its choreography.
        if (TurningForShot)
        {
            idle.From = idle.To = PassiveBatteryHeading(ship.Id);
            idle.Next = _clock + IdleBatteryInterval(Battle.Board.Seed, ship.Id, idle.Sequence);
            return;
        }
        if (_clock - idle.LastTick < .075f) return;
        idle.LastTick = _clock;
        if (_clock >= idle.Next)
        {
            idle.From = PassiveBatteryHeading(ship.Id);
            idle.To = CosmeticUnit(Battle.Board.Seed, ship.Id, ++idle.Sequence, 9127) * Mathf.Tau;
            idle.Start = _clock;
            idle.Next = _clock + IdleBatteryInterval(Battle.Board.Seed, ship.Id, idle.Sequence);
        }
        float progress = Math.Clamp((_clock - idle.Start) / 2.2f, 0, 1);
        _barrelAngles[ship.Id] = Mathf.LerpAngle(idle.From, idle.To, MotionProgress(progress));
    }
}
