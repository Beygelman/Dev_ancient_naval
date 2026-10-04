using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

[Flags]
public enum ReadyActionKind
{
    None = 0, Move = 1, Attack = 2, Build = 4, Upgrade = 8,
    Collect = 16, Dock = 32, Capture = 64, Loot = 128
}

/// <summary>A useful owned object, counted once even when it has several legal orders.</summary>
public sealed record ReadyActionObject(int Id, GridPosition Position, ShipClass? ShipClass,
    string Name, ReadyActionKind Actions);

public sealed partial class BattleState
{
    private static readonly ShipClass[] UsefulShipyardClasses =
        { ShipClass.Garrison, ShipClass.Invader, ShipClass.Kolonel, ShipClass.Togus,
            ShipClass.CannonTower, ShipClass.Lighthouse };

    /// <summary>Command-derived turn guidance. Excludes ports, walls, radar, repairs,
    /// Support Brig production and suicidal replies. Resource sites qualify their
    /// collector once; they never become duplicate counter entries.</summary>
    public IReadOnlyList<ReadyActionObject> ReadyActions(Side side)
    {
        if (IsOver || PendingPresentation is not null || side != ActiveSide || PendingUpgrade(side) is not null)
            return Array.Empty<ReadyActionObject>();
        var ready = new List<ReadyActionObject>();
        foreach (var ship in OwnShips(side).OrderBy(s => s.Id))
        {
            if (ValidateActor(side, ship.Id, out _) is not null) continue;
            var actions = ReadyActionKind.None;
            if (HasLegalRemainingMove(ship)) actions |= ReadyActionKind.Move;
            if (HasSafeRemainingAttack(ship)) actions |= ReadyActionKind.Attack;
            if (ship.IsMothership && UsefulShipyardClasses.Any(kind => BuildBlockReason(side, ship.Id, kind) is null)
                || ship.Definition.Class == ShipClass.Fishing && new[] { ShipClass.CannonTower, ShipClass.Lighthouse }
                    .Any(kind => BuildBlockReason(side, ship.Id, kind) is null))
                actions |= ReadyActionKind.Build;
            if (ship.Definition.CollectionRange > 0)
            {
                if (Credits(side) >= CollectionCost(side) && CollectionCells(ship.Id).Count > 0)
                    actions |= ReadyActionKind.Collect;
                if (Credits(side) >= DockPrice(side) && DockCells(ship.Id).Count > 0)
                    actions |= ReadyActionKind.Dock;
            }
            if (CanLootTreasury(side, ship.Id)) actions |= ReadyActionKind.Loot;
            if (_villages.Any(v => CanCaptureVillage(side, v.Id) && CaptureCrew(v, side)?.Id == ship.Id))
                actions |= ReadyActionKind.Capture;
            if (actions != ReadyActionKind.None)
                ready.Add(new(ship.Id, ship.Position, ship.Definition.Class, ship.Name, actions));
        }
        foreach (var village in _villages.Where(v => v.Owner == side).OrderBy(v => v.Id))
        {
            var actions = ReadyActionKind.None;
            if (CanUpgradeVillage(side, village.Id)) actions |= ReadyActionKind.Upgrade;
            if (UsefulShipyardClasses.Any(kind => VillageBuildBlockReason(side, village.Id, kind) is null))
                actions |= ReadyActionKind.Build;
            if (actions != ReadyActionKind.None)
                ready.Add(new(village.Id, village.Position, null, village.Name, actions));
        }
        return ready.AsReadOnly();
    }

    private bool HasLegalRemainingMove(Ship ship)
    {
        if (!ship.CanMove) return false;
        if (ship.IsAirborne) return FlightRoutes(ship).Costs.Count > 1;
        var routes = Flood(ship, ship.MovementRemainingUnits + 4);
        var actual = NavigationQuery(ship, knowledge: false);
        foreach (var cell in routes.Costs.Keys)
        {
            if (cell == ship.Position || KnownOccupied(ship.Owner, cell)) continue;
            var path = routes.PathTo(cell);
            int spent = 0, run = ship.TradeStreak;
            var trade = TradePolicy(ship);
            for (int i = 1; i < path.Count; i++)
            {
                if (actual.StepCost(path[i - 1], path[i]) is not { } raw) break;
                var step = trade?.Step(path[i - 1], path[i], raw, run) ?? (raw, 0);
                spent += step.Item1;
                run = step.Item2;
                if (spent > ship.MovementRemainingUnits + 4) break;
                if (IsFreeWater(path[i])) return true;
            }
        }
        return false;
    }

    private bool HasSafeRemainingAttack(Ship attacker)
    {
        if (attacker.AttacksRemaining == 0) return false;
        foreach (var target in _ships.Where(target => CanAttack(attacker.Id, target.Id)))
        {
            if (!Vision.IsVisible(attacker.Owner, target.Position))
            {
                // Radar coordinates do not disclose target class, HP or whether
                // it would die. Only a worst-case reply may qualify this order.
                double worstReply = Rules.Ships.Where(s => s.Damage > 0)
                    .Max(s => Ship.Whole(s.Damage * 1.25) + 2);
                if (attacker.Health > Math.Max(1, worstReply - attacker.Definition.Armor)) return true;
                continue;
            }
            int charges = CanDoubleSalvo(attacker.Id, target.Position) ? 2 : 1;
            double remaining = target.Health - Damage(attacker, target) * charges;
            if (remaining <= 0 || !CanCounterattack(target, attacker)) return true;
            double reply = Math.Max(1, Ship.Whole(target.FullDamage * (.5 + .5 * remaining / target.MaxHealth))
                + target.CounterDamageBonus - attacker.Definition.Armor);
            if (attacker.Health > reply) return true;
        }
        foreach (var village in _villages.Where(v => CanAttackVillage(attacker.Id, v.Id)))
        {
            int charges = CanDoubleSalvo(attacker.Id, village.Position) ? 2 : 1;
            double raw = (UsesMortar(attacker, village.Position)
                ? attacker.CurrentMortarDamage + Rules.Mortar.VillageDamageBonus : attacker.CurrentDamage) + attacker.ShotDamageBonus;
            double damage = raw * (village.IsFortified ? .75 : 1);
            if (Rules.EqualDoubleSalvoDamage) damage = Ship.Whole(damage);
            bool replies = village.Health > damage * charges && village.IsFortified
                && (!Rules.VillageCombat.AutomaticAttack || village.Level >= 2)
                && Board.InRadius(village.Position, attacker.Position, Rules.VillageCombat.CounterRange);
            if (!replies || attacker.Health > Math.Max(1, VillageCounterDamage(village) - attacker.Definition.Armor)) return true;
        }
        return false;
    }
}
