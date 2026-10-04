using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    // Selection and camera changes retain command-derived guidance. This exact
    // immutable signature prevents repeated navigation searches without hashing
    // mutable ships or consulting hidden opponent health.
    internal int ReadyQueryRebuilds { get; private set; }
    private BattleState? _readyCachedBattle;
    private (int Turn, int Credits, long Vision, bool Creative, int Used, int Capacity, int Fish, int Shoals)? _readyKey;
    private ReadyShipState[] _readyShipStates = Array.Empty<ReadyShipState>();
    private ReadyTownState[] _readyTownStates = Array.Empty<ReadyTownState>();
    private System.Collections.Generic.IReadOnlyList<ReadyActionObject> _readyCached = Array.Empty<ReadyActionObject>();
    private sealed record ReadyShipState(int Id, GridPosition Position, double Health, int Level, int Resources,
        int Movement, int Trade, int Attacks, int Kills, int Pending, int Cooldown, int Flags);
    private sealed record ReadyTownState(int Id, GridPosition Position, double Health, int Level, int Turns,
        bool Produced, bool Repaired, bool Attacked, bool Port, bool Fortified);
    private System.Collections.Generic.IReadOnlyList<ReadyActionObject> CachedReadyActions(BattleState battle)
    {
        var key = (battle.TurnSerial, battle.Credits(Side.Player), battle.Vision.Revision, battle.Creative,
            battle.FleetUsed(Side.Player), battle.FleetCapacity(Side.Player), battle.FishSpots.Count, battle.Shoals.Count);
        var ships = battle.OwnShips(Side.Player).Select(s => new ReadyShipState(s.Id, s.Position, s.Health, s.Level,
            s.Resources, s.MovementSpentUnits, s.TradeStreak, s.AttacksUsed, s.Kills, s.PendingUpgradeLevel,
            s.BombCooldown, (s.IsVeteran ? 1 : 0) | (s.HasRepaired ? 2 : 0) | (s.HasMoved ? 4 : 0)
                | (s.IsExhausted ? 8 : 0) | (s.HasProduced ? 16 : 0) | (s.MovementLocked ? 32 : 0)
                | (s.HasMortar ? 64 : 0) | (s.HasRadar ? 128 : 0) | (s.SecondAttackUpgrade ? 256 : 0)
                | (s.MobilityUpgrade ? 512 : 0) | (s.ShipwrightUpgrade ? 1024 : 0) | (s.VisionUpgrade ? 2048 : 0)
                | (s.RestorationUpgrade ? 4096 : 0) | (s.FirepowerUpgrade ? 8192 : 0))).ToArray();
        var towns = battle.Villages.Where(v => v.Owner == Side.Player).Select(v => new ReadyTownState(v.Id, v.Position,
            v.Health, v.Level, v.TurnsOwned, v.HasProduced, v.HasRepaired, v.HasAttacked, v.HasPort, v.IsFortified)).ToArray();
        if (ReferenceEquals(_readyCachedBattle, battle) && _readyKey == key && _readyShipStates.SequenceEqual(ships)
            && _readyTownStates.SequenceEqual(towns)) return _readyCached;
        _readyCachedBattle = battle; _readyKey = key; _readyShipStates = ships; _readyTownStates = towns;
        ReadyQueryRebuilds++;
        return _readyCached = battle.ReadyActions(Side.Player);
    }
}

