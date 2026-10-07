using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;

/// <summary>Bounded investment choices before discretionary hull purchases.
/// Uses owned accounts/settlements and observed danger; hypothetical relay
/// previews use remembered sea, never the omniscient live trade network.</summary>
internal static class FleetInvestment
{
    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        var side = battle.ActiveSide;
        var mother = allies.First(s => s.IsMothership);
        var towns = battle.Villages.Where(v => v.Owner == side && v.Health > 0).OrderBy(v => v.Id).ToArray();
        int escorts = allies.Count(s => s.CountsTowardFleet && !s.IsMothership);
        bool pressure = enemies.Any(e => e.IsArmed && battle.Board.Distance(e.Position, mother.Position) <= 6);
        int reserve = pressure && escorts < 2 ? battle.BuildPrice(side, ShipClass.Garrison) : 0;
        bool Budget(int price) => battle.Credits(side) >= (long)price + reserve;

        // Ports have no fixed income in current voyages. Invest in paired towns
        // rather than decorating an isolated settlement with an idle harbour.
        foreach (var town in towns.OrderByDescending(v => towns.Any(other => other.Id != v.Id && other.HasPort)))
        {
            bool partner = towns.Any(other => other.Id != town.Id && other.Level >= 2);
            if ((partner || !battle.Rules.Ports.ConnectedCityIncome) && Budget(battle.PortPrice(side))
                && battle.PortBlockReason(side, town.Id) is null)
                return battle.BuildPort(side, town.Id);
        }

        var builders = allies.Where(s => s.IsMothership || s.Definition.Class == ShipClass.Fishing)
            .OrderBy(s => s.IsMothership).ThenBy(s => s.Id).ToArray();
        // A battery protects a known frontier or threatened flagship. Space
        // batteries apart instead of spending all spare currency on one cell.
        foreach (var builder in builders)
        {
            if (FlagshipSafety.IsCautious(battle, mother, enemies) && builder.IsMothership) continue;
            if (!Budget(battle.BuildPrice(side, ShipClass.CannonTower))
                || battle.BuildBlockReason(side, builder.Id, ShipClass.CannonTower) is not null) continue;
            var tower = battle.Rules.Get(ShipClass.CannonTower);
            var site = battle.SpawnCells(builder.Id)
                .Where(p => !allies.Any(s => s.Definition.Class == ShipClass.CannonTower
                    && battle.Board.InRadius(s.Position, p, 3)))
                .Select(p => new { Cell = p, Threats = enemies.Count(e => e.IsArmed && !e.IsAirborne
                    && battle.Board.InRadius(p, e.Position, tower.AttackRange + 1)) })
                .Where(p => p.Threats > 0 && danger(builder, p.Cell) < tower.MaxHealth)
                .OrderByDescending(p => p.Threats).ThenBy(p => danger(builder, p.Cell))
                .ThenBy(p => p.Cell.Y).ThenBy(p => p.Cell.X).FirstOrDefault();
            if (site is not null)
                return battle.Build(side, builder.Id, ShipClass.CannonTower, site.Cell);
        }

        // Relays join separated OWN city pairs, or extend their known chain
        // toward a distant city that requires several beacons. Lighthouse nodes
        // are ordinary sea-lane steps, not income cities.
        var ports = towns.Where(t => t.HasPort).ToArray();
        if (ports.Length >= 2 && battle.Rules.LighthousesEnabled)
        {
            foreach (var builder in builders)
            {
                if (FlagshipSafety.IsCautious(battle, mother, enemies) && builder.IsMothership) continue;
                if (!Budget(battle.BuildPrice(side, ShipClass.Lighthouse))
                    || battle.BuildBlockReason(side, builder.Id, ShipClass.Lighthouse) is not null) continue;
                var nodes = ports.Select(battle.PortBerth).ToArray();
                var candidate = battle.SpawnCells(builder.Id).Where(p => danger(builder, p) == 0)
                    .Select(p =>
                    {
                        var preview = battle.PreviewLighthouseTradeRoutes(side, p);
                        return new { Cell = p, Links = ConnectedPairs(preview, nodes),
                            Progress = RelayProgress(battle, preview, nodes, allies, p) };
                    })
                    .OrderByDescending(p => p.Links).ThenByDescending(p => p.Progress)
                    .ThenBy(p => p.Cell.Y).ThenBy(p => p.Cell.X).ToArray();
                int existing = ports.Sum(battle.ConnectedPortCityCount) / 2;
                var relay = candidate.FirstOrDefault(p => p.Links > existing || p.Progress > 0);
                if (relay is not null)
                    return battle.Build(side, builder.Id, ShipClass.Lighthouse, relay.Cell);
            }
        }

        foreach (var town in towns)
            if (enemies.Any(e => e.IsArmed && battle.Board.InRadius(town.Position, e.Position, 4))
                && Budget(battle.FortificationPrice(side)) && battle.FortifyBlockReason(side, town.Id) is null)
                return battle.FortifyVillage(side, town.Id);
        return null;
    }

    internal static CommandResult? Research(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies)
    {
        var side = battle.ActiveSide;
        var mother = allies.First(s => s.IsMothership);
        int escorts = allies.Count(s => s.CountsTowardFleet && !s.IsMothership);
        bool pressure = enemies.Any(e => e.IsArmed && battle.Board.Distance(e.Position, mother.Position) <= 6);
        int reserve = pressure && escorts < 2 ? battle.BuildPrice(side, ShipClass.Garrison) : 0;
        bool Budget(int price) => battle.Credits(side) >= (long)price + reserve;

        // The flagship's shared scanner precedes redundant escort scanners.
        // Research needs a replacement reserve under pressure, not a hardcoded
        // threshold that becomes wrong when the saved catalog changes.
        if ((escorts >= 2 || !pressure) && Budget(mother.Definition.RadarPrice)
            && battle.RadarBlockReason(side, mother.Id) is null)
            return battle.BuyRadar(side, mother.Id);
        if (escorts >= 2 && Budget(battle.MortarPrice)
            && battle.MortarBlockReason(side, mother.Id) is null)
            return battle.BuyMortar(side, mother.Id);
        return null;
    }

    internal static CommandResult? AdvanceRelay(BattleState battle, IReadOnlyList<Ship> allies,
        Func<Ship, GridPosition, double> danger)
    {
        var side = battle.ActiveSide;
        var ports = battle.Villages.Where(v => v.Owner == side && v.HasPort && v.Health > 0)
            .OrderBy(v => v.Id).ToArray();
        if (!battle.Rules.FishingLighthouses || ports.Length < 2 || battle.Rules.Ports.MaximumRouteLength == 0
            || battle.Credits(side) < battle.BuildPrice(side, ShipClass.Lighthouse)) return null;
        var nodes = ports.Select(battle.PortBerth).Concat(allies.Where(s => s.Definition.Class == ShipClass.Lighthouse)
            .Select(s => s.Position)).Distinct().ToArray();
        // Existing OWN economic components are already reflected in port income.
        // Do not spend the bounded search slots on nearby links within one of
        // those components while a distant unconnected city waits for a relay.
        var existing = battle.TradeRoutes(side);
        var candidates = ports.Where(p => battle.ConnectedPortCityCount(p) < ports.Length - 1)
            .SelectMany(port => nodes.Where(node => node != battle.PortBerth(port)
                    && !existing.AreConnected(node, battle.PortBerth(port)))
                .Select(node => (From: node, To: battle.PortBerth(port))))
            .OrderBy(pair => battle.Board.Distance(pair.From, pair.To)).Take(3);
        bool Sea(GridPosition p) => battle.Board.Contains(p)
            && battle.Vision.KnownTerrain(side, p) is { } terrain && terrain != TerrainType.Land
            && (!battle.Vision.IsVisible(side, p) || !battle.IsForbidden(p));
        foreach (var pair in candidates)
        {
            var search = PathSearch.Find(pair.From, battle.Board.Tiles.Count, battle.Board.GetSurrounding,
                (from, to) => TradeNetwork.SeaStepCost(battle.Board, from, to, Sea));
            if (!search.Costs.TryGetValue(pair.To, out int cost) || cost <= battle.Rules.Ports.MaximumRouteLength) continue;
            var lane = PathSearch.Reconstruct(pair.From, pair.To, search.Previous);
            var goal = lane[Math.Min(battle.Rules.Ports.MaximumRouteLength - 1, lane.Count / 2)];
            foreach (var builder in allies.Where(s => s.Definition.Class == ShipClass.Fishing && s.CanMove && !s.HasMoved)
                .OrderBy(s => battle.Board.Distance(s.Position, goal)).Take(2))
            {
                // A builder already beside the relay site waits for next turn's
                // construction opportunity instead of repeatedly walking past it.
                if (battle.Board.GetNeighbors(builder.Position).Contains(goal)) continue;
                var route = battle.RouteToward(builder.Id, goal, 1);
                if (route.Count < 2) continue;
                var next = battle.AffordableDestination(builder.Id, route);
                if (next != builder.Position && danger(builder, next) == 0)
                    return battle.Move(side, builder.Id, next);
            }
        }
        return null;
    }

    internal static bool HoldHullBudget(BattleState battle, IReadOnlyList<Ship> allies, IReadOnlyList<Ship> enemies)
    {
        var side = battle.ActiveSide;
        if (allies.Count(s => s.CountsTowardFleet && !s.IsMothership) < 2
            || enemies.Any(e => e.IsArmed && allies.Any(a => a.IsMothership
                && battle.Board.InRadius(a.Position, e.Position, 6)))) return false;
        int cash = battle.Credits(side);
        int nextTown = battle.Villages.Where(t => t.Owner == side && t.Health > 0 && t.Level < 3)
            .Select(t => battle.VillageUpgradePrice(side, t.Id) + battle.BuildPrice(side, ShipClass.Garrison))
            .DefaultIfEmpty(0).Min();
        if (battle.Rules.PaidVillageUpgrades && nextTown > 0 && cash < nextTown) return true;
        return allies.Any(s => s.Definition.CollectionRange > 0) && battle.KnownShoals(side).Any()
            && allies.Count(s => s.Definition.Class == ShipClass.FishingDock) < 2
            && cash < battle.DockPrice(side);
    }

    private static int ConnectedPairs(DevAncientNaval.Core.Navigation.TradeNetwork network, GridPosition[] nodes)
    {
        int count = 0;
        for (int first = 0; first < nodes.Length; first++)
            for (int second = first + 1; second < nodes.Length; second++)
                if (network.AreConnected(nodes[first], nodes[second])) count++;
        return count;
    }

    private static double RelayProgress(BattleState battle, TradeNetwork preview, GridPosition[] ports,
        IReadOnlyList<Ship> allies, GridPosition proposed)
    {
        double progress = 0;
        var nodes = ports.Concat(allies.Where(s => s.Definition.Class == ShipClass.Lighthouse)
            .Select(s => s.Position)).Distinct().ToArray();
        foreach (var from in nodes)
        {
            if (battle.Board.Distance(from, proposed) < 3 || !preview.AreConnected(from, proposed)) continue;
            foreach (var destination in ports)
            {
                if (preview.AreConnected(from, destination)) continue;
                double improvement = battle.Board.Distance(from, destination) - battle.Board.Distance(proposed, destination);
                if (improvement >= 3) progress = Math.Max(progress, improvement);
            }
        }
        return progress;
    }
}
