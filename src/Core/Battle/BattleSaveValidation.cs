using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
/// <summary>Rejects incomplete snapshots before collections or mesh indexes are rebuilt.</summary>
internal static class BattleSaveValidation
{
    internal static void ValidateEnvelope(BattleSave saved)
    {
        if (saved.Version != 1 || saved.Statistics is null || saved.Board is null || saved.Rules is null || saved.Board.Width < 1 || saved.Board.Height < 1 || (long)saved.Board.Width * saved.Board.Height > 20_000 || saved.Board.Land is null || saved.Ships is null || saved.Villages is null || saved.Fish is null || saved.Shoals is null || saved.Treasuries is null || saved.Whirlpools is null || saved.CaptureWaits is null || saved.TreasuryWaits is null || saved.PirateHomes is null || saved.Outcomes is null || saved.Income is null || saved.Vision is null || saved.Credits is null || saved.EverProduced is null || saved.Credits.Length is not (3 or BattleState.SideSlots) || saved.Credits.Any(n => n < 0) || saved.EverProduced.Length != saved.Credits.Length || saved.Round < 1 || saved.TurnSerial < 0 || saved.EventDraws is < 0 or > 10_000_000 || !Enum.IsDefined(saved.Board.Kind) || saved.Board.MapSize is { } mapSize && !Enum.IsDefined(mapSize) || !Enum.IsDefined(saved.Difficulty) || !Enum.IsDefined(saved.ActiveSide) || !Enum.IsDefined(saved.Color) || saved.Winner is { } winner && !Enum.IsDefined(winner) || saved.LastReward is { } reward && !Enum.IsDefined(reward))
            throw new ArgumentException("Invalid or unsupported saved game.");
        if (saved.PendingAwards is null || saved.PendingAwards.Length > 1000 || saved.PendingAwards.Any(a => a is null || string.IsNullOrWhiteSpace(a.Id) || a.Id.Length > 60 || !Enum.IsDefined(a.Kind) || a.Owner != Side.Player || a.Amount is < 0 or > 1000 || a.Turn < 0 || a.Turn > saved.TurnSerial) || saved.PendingAwards.Select(a => a.Id).Distinct().Count() != saved.PendingAwards.Length)
            throw new ArgumentException("Invalid saved pending rewards.");
        saved.Statistics.Validate();
        if (saved.DirectShipKills is null || saved.DirectShipKills.Length is not (0 or BattleState.SideSlots)
            || saved.DirectShipKills.Any(n => n < 0))
            throw new ArgumentException("Invalid saved direct ship losses.");
        if (saved.FlagshipSightings is null || saved.FlagshipSightings.Length > 20
            || saved.FlagshipSightings.Any(record => record is null
                || !BattleState.PlayableSides.Contains(record.Observer)
                || !BattleState.PlayableSides.Contains(record.Owner)
                || record.Observer == record.Owner || record.Turn < 0 || record.Turn > saved.TurnSerial)
            || saved.FlagshipSightings.Select(record => (record.Observer, record.Owner)).Distinct().Count() != saved.FlagshipSightings.Length)
            throw new ArgumentException("Invalid saved flagship observations.");
        if (saved.PersonalTurnStarts is null || saved.FlagshipKills is null
            || saved.PersonalTurnStarts.Length is not (0 or BattleState.SideSlots)
            || saved.FlagshipKills.Length is not (0 or BattleState.SideSlots)
            || saved.PersonalTurnStarts.Any(n => n < 0 || (long)n > (long)saved.TurnSerial + 1)
            || saved.FlagshipKills.Any(n => n is < 0 or > 4)
            || saved.PersonalTurnStarts.Length > 0 && saved.PersonalTurnStarts[(int)Side.Pirates] != 0
            || saved.FlagshipKills.Length > 0 && saved.FlagshipKills[(int)Side.Pirates] != 0)
            throw new ArgumentException("Invalid saved heavenly assistance progress.");
        if (saved.Factions is null || saved.FactionColors is null || saved.Factions.Length > 0 && (saved.Factions.Length is < 2 or > 5 || !saved.Factions.Contains(Side.Player) || saved.Factions.Any(side => !BattleState.PlayableSides.Contains(side)) || saved.Factions.Distinct().Count() != saved.Factions.Length))
            throw new ArgumentException("Invalid saved faction roster.");
        if (saved.FactionColors.Any(entry => entry is null || !BattleState.PlayableSides.Contains(entry.Side) || !Enum.IsDefined(entry.Color)) || saved.FactionColors.Select(entry => entry.Side).Distinct().Count() != saved.FactionColors.Length || saved.FactionColors.Select(entry => entry.Color).Distinct().Count() != saved.FactionColors.Length)
            throw new ArgumentException("Invalid saved fleet colors.");
    }

    internal static void ValidateContents(BattleSave saved, GameBoard board, BattleRules rules)
    {
        if (saved.Encounters is null || saved.Encounters.Any(e => e is null || e.Side is Side.Player or Side.Pirates || !Enum.IsDefined(e.Side) || !board.Contains(e.Position))
            || saved.Encounters.Select(e => e.Side).Distinct().Count() != saved.Encounters.Length)
            throw new ArgumentException("Invalid saved nation encounters.");
        PendingAwardValidation.Validate(saved, board, rules);
        if (saved.Ships.Any(s => s is null || s.Id <= 0 || !Enum.IsDefined(s.Owner) || !Enum.IsDefined(s.Kind) || !board.Contains(s.Position) || !double.IsFinite(s.Health) || s.Health <= 0 || s.Level is < 1 or > 5 || s.BombCooldown < 0 || s.BombCooldown > rules.Balloon.CooldownTurns || s.Kills < 0 || s.Resources < 0 || s.MovementSpentUnits < 0 || s.TradeStreak is < 0 or > 100 || s.AttacksUsed < 0))
            throw new ArgumentException("Invalid saved fleet.");
        if (saved.Ships.Any(s => s.ConstructionPrice is < 0))
            throw new ArgumentException("Invalid saved ship construction price.");
        if (saved.Villages.Any(v => v is null || v.Id <= 0 || !board.Contains(v.Position) || v.Owner is { } owner && !Enum.IsDefined(owner) || v.Level is < 1 or > 5 || !double.IsFinite(v.Health) || v.Health < 0 || v.Health > v.Level * 5 || v.TurnsOwned < 0 || v.Port && v.Level < 3) || saved.Treasuries.Any(t => t is null || t.Id <= 0 || !board.Contains(t.Position)))
            throw new ArgumentException("Invalid saved settlements or treasuries.");
        if (!saved.PiratesEnabled && (saved.ActiveSide == Side.Pirates || saved.Winner == Side.Pirates
            || saved.Ships.Any(ship => ship.Owner == Side.Pirates)
            || saved.Villages.Any(village => village.Owner == Side.Pirates)
            || saved.PirateHomes.Length > 0))
            throw new ArgumentException("A pirate-free voyage contains pirate objects.");
        if (saved.Villages.Any(v => v.PortCell is { } cell && (!v.Port || !board.Contains(cell)
            || board.GetTile(cell).Terrain == TerrainType.Land
            || !(rules.DiagonalVillageBerths ? board.GetSurrounding(v.Position) : board.GetNeighbors(v.Position)).Contains(cell))))
            throw new ArgumentException("Invalid saved port berth.");
        if (saved.FactionNames is null || saved.FactionNames.Length > 4 || saved.FactionNames.Any(e => e is null || e.Side == Side.Player || e.Side == Side.Pirates || !Enum.IsDefined(e.Side) || !WorldNames.Captains.Contains(e.Name)) || saved.FactionNames.Select(e => e.Side).Distinct().Count() != saved.FactionNames.Length || saved.FactionNames.Select(e => e.Name).Distinct().Count() != saved.FactionNames.Length || saved.Villages.Any(v => v.Name is null || v.Name.Length > 60 || v.Name.Any(char.IsControl)))
            throw new ArgumentException("Invalid saved world identities.");
        var roster = saved.Factions.Length == 0 ? new[]
        {
            Side.Player,
            Side.Enemy
        }

        : saved.Factions;
        if (saved.FlagshipSightings.Any(record => !roster.Contains(record.Observer)
            || !roster.Contains(record.Owner) || !board.Contains(record.Position)
            || !saved.Vision.Any(vision => vision is not null && vision.Side == record.Observer
                && vision.Explored is not null && vision.Explored.Contains(record.Position))))
            throw new ArgumentException("Saved flagship observations do not match the board and roster.");
        if (saved.PersonalTurnStarts.Where((n, index) => n > 0 && !roster.Contains((Side)index)).Any()
            || saved.FlagshipKills.Where((n, index) => n > 0 && !roster.Contains((Side)index)).Any()
            || saved.FlagshipKills.Any(n => n > roster.Length - 1)
            || saved.FlagshipKills.Sum() > roster.Count(side => !saved.Ships.Any(ship => ship.Owner == side && ship.Kind == ShipClass.Mothership)))
            throw new ArgumentException("Saved heavenly assistance does not match the fleet roster.");
        if (saved.Encounters.Any(e => !roster.Contains(e.Side)))
            throw new ArgumentException("Saved nation encounters do not match the roster.");
        if (saved.FactionNames.Length > 0 && (saved.FactionNames.Length != roster.Length - 1 || saved.FactionNames.Any(e => !roster.Contains(e.Side))))
            throw new ArgumentException("Saved captain identities do not match their roster.");
        if (saved.FactionColors.Length > 0 && (saved.FactionColors.Length != roster.Length || roster.Any(side => !saved.FactionColors.Any(entry => entry.Side == side)) || saved.FactionColors.First(entry => entry.Side == Side.Player).Color != saved.Color))
            throw new ArgumentException("Saved fleet colors do not match their roster.");
        if (roster.Any(side => (int)side >= saved.Credits.Length))
            throw new ArgumentException("Saved currency slots do not cover every fleet.");
        if (saved.Ships.Any(ship => ship.Owner != Side.Pirates && !roster.Contains(ship.Owner)) || saved.Ships.Where(ship => ship.Kind == ShipClass.Mothership).GroupBy(ship => ship.Owner).Any(group => group.Count() > 1) || saved.ActiveSide != Side.Pirates && !roster.Contains(saved.ActiveSide))
            throw new ArgumentException("Saved fleet is outside its faction roster.");
        var ids = saved.Ships.Select(s => s.Id).Concat(saved.Villages.Select(v => v.Id)).Concat(saved.Treasuries.Select(t => t.Id)).ToArray();
        if (ids.Distinct().Count() != ids.Length || saved.NextId <= ids.DefaultIfEmpty(0).Max())
            throw new ArgumentException("Invalid save identifiers.");
        if (saved.Fish.Any(p => !board.Contains(p)) || saved.Shoals.Any(p => !board.Contains(p)) || saved.Whirlpools.Any(w => w is null || !board.Contains(w.Position) || w.Cells is null || w.Cells.Any(p => !board.Contains(p))))
            throw new ArgumentException("Invalid saved sea features.");
        var ships = saved.Ships.ToDictionary(s => s.Id);
        var villages = saved.Villages.Select(v => v.Id).ToHashSet();
        var treasuries = saved.Treasuries.Where(t => !t.IsCollected).Select(t => t.Id).ToHashSet();
        foreach (var wait in saved.CaptureWaits.Concat(saved.TreasuryWaits))
            if (wait is null || !Enum.IsDefined(wait.Side) || !board.Contains(wait.Position) || wait.Since < 0 || wait.Since > saved.TurnSerial || !ships.TryGetValue(wait.Ship, out var ship) || ship.Owner != wait.Side)
                throw new ArgumentException("Invalid saved waiting crew.");
        if (saved.CaptureWaits.Any(w => !villages.Contains(w.Target)) || saved.TreasuryWaits.Any(w => !treasuries.Contains(w.Target)) || saved.CaptureWaits.Select(w => (w.Target, w.Side, w.Ship)).Distinct().Count() != saved.CaptureWaits.Length || saved.TreasuryWaits.Select(w => w.Target).Distinct().Count() != saved.TreasuryWaits.Length)
            throw new ArgumentException("Invalid saved capture or plunder progress.");
        if (saved.PirateHomes.Any(h => h is null || !board.Contains(h.Position) || !ships.TryGetValue(h.Ship, out var ship) || ship.Owner != Side.Pirates) || saved.PirateHomes.Select(h => h.Ship).Distinct().Count() != saved.PirateHomes.Length || saved.Outcomes.Any(o => o is null || !treasuries.Contains(o.Treasury) || !Enum.IsDefined(o.Reward)) || saved.Outcomes.Select(o => o.Treasury).Distinct().Count() != saved.Outcomes.Length)
            throw new ArgumentException("Invalid saved sea-event progress.");
        if (saved.Income.Any(i => i is null || string.IsNullOrWhiteSpace(i.Id) || !Enum.IsDefined(i.Owner) || i.Amount < 0 || i.BoundShipId is { } shipId && !ships.ContainsKey(shipId)) || saved.Income.Select(i => i.Id).Distinct().Count() != saved.Income.Length)
            throw new ArgumentException("Invalid saved income sources.");
        if (!saved.PiratesEnabled && (saved.Income.Any(i => i.Owner == Side.Pirates)
            || saved.CaptureWaits.Concat(saved.TreasuryWaits).Any(wait => wait.Side == Side.Pirates)))
            throw new ArgumentException("A pirate-free voyage contains pirate progress.");
    }
}
