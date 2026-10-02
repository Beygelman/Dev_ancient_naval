using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;
/// <summary>Bounded one-turn tactical forecast. Every enemy and objective comes
/// from this captain's observation; anonymous radar marks never supply stats.</summary>
internal static class AdmiralOpponent
{
    internal static CommandResult Step(BattleState battle)
    {
        var side = battle.ActiveSide;
        var allies = battle.OwnShips(side).ToArray();
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side).ToArray();
        var towns = battle.ObservedVillages(side).ToArray();
        var mother = allies.First(s => s.IsMothership);
        var fleet = allies.Where(s => s.CountsTowardFleet && !s.IsMothership).ToArray();
        bool crisis = fleet.Length < 2;
        var threats = new AdmiralThreats(battle, enemies);
        double Danger(Ship ship, GridPosition cell) => threats.At(ship, cell);
        if (FlagshipSafety.Retreat(battle, mother, enemies)is { } escape)
            return escape;
        foreach (var ship in allies)
            if (battle.CanLootTreasury(side, ship.Id))
                return battle.LootTreasury(side, ship.Id);
        foreach (var town in towns)
            if (battle.CanCaptureVillage(side, town.Id))
                return battle.CaptureVillage(side, town.Id);
        foreach (var balloon in allies.Where(s => s.IsAirborne && battle.CanDropBomb(s.Id)))
            if (enemies.Any(e => e.Position == balloon.Position))
                return battle.DropBomb(side, balloon.Id);
        // Commit one legal order from a remaining-turn fleet plan, then replan
        // from the resulting observations and health at the next step.
        if (AdmiralTactics.Step(battle, allies, enemies, Danger) is { } coordinated)
            return coordinated;

        foreach (var town in towns.Where(v => v.Owner != side && v.Health > 0).OrderBy(v => v.Health))
        {
            var guns = allies.Where(s => battle.CanAttackVillage(s.Id, town.Id)).OrderByDescending(s => s.HasMortar).ToArray();
            if (guns.Length == 0 || crisis && enemies.Length > 0)
                continue;
            var shooter = guns.FirstOrDefault(s => !town.IsFortified || s.Health > battle.VillageCounterDamage(town) || s.HasMortar);
            if (shooter is not null)
                return battle.AttackVillage(side, shooter.Id, town.Id,
                    battle.CanDoubleSalvo(shooter.Id, town.Position) && town.Health > shooter.CurrentDamage * (town.IsFortified ? .75 : 1));
        }

        foreach (var ship in allies.Where(s => s.CanRepair && s.HealthRatio < .6))
            if (ship.IsStructure || Danger(ship, ship.Position) < ship.Health)
                return battle.Repair(side, ship.Id);
        // Fishing supports recovery, but do not build a dock that will be lost at once.
        foreach (var collector in allies.Where(s => s.Definition.CollectionRange > 0))
        {
            var dock = battle.DockCells(collector.Id).Where(p => Danger(collector, p) == 0).Select(p => (GridPosition? )p).FirstOrDefault();
            if (dock is { } site && battle.Credits(side) >= battle.DockPrice(side))
                return battle.BuildDock(side, collector.Id, site);
            var fish = battle.CollectionCells(collector.Id);
            if (fish.Count > 0 && battle.Credits(side) >= battle.CollectionCost(side))
                return battle.Collect(side, collector.Id, fish.First());
        }

        var owned = towns.Where(v => v.Owner == side && v.Health > 0).ToArray();
        foreach (var town in owned)
        {
            if (battle.PortBlockReason(side, town.Id)is null && (owned.Length >= 2 || battle.Credits(side) >= 12))
                return battle.BuildPort(side, town.Id);
            if (enemies.Any(e => battle.Board.InRadius(town.Position, e.Position, 3)) && battle.FortifyBlockReason(side, town.Id)is null)
                return battle.FortifyVillage(side, town.Id);
        }

        int fishers = allies.Count(s => s.Definition.Class == ShipClass.Fishing);
        int docks = allies.Count(s => s.Definition.Class == ShipClass.FishingDock);
        bool economy = fishers + docks < 2;
        var preferred = economy ? ShipClass.Fishing : crisis ? ShipClass.Garrison : towns.Any(v => v.Owner != side && v.Health > 0) && !fleet.Any(s => s.HasMortar) ? ShipClass.Togus : fleet.Length % 3 == 0 ? ShipClass.Kolonel : ShipClass.Invader;
        var classes = new[]
        {
            preferred,
            economy ? ShipClass.Fishing : ShipClass.Invader,
            ShipClass.Garrison
        }.Distinct().ToArray();
        foreach (var town in owned.OrderByDescending(v => v.HasPort))
            foreach (var kind in classes)
                if (battle.VillageBuildBlockReason(side, town.Id, kind)is null)
                    return battle.BuildFromVillage(side, town.Id, kind, battle.VillageSpawnCells(town.Id).OrderBy(p => Danger(mother, p)).First());
        if (!FlagshipSafety.IsCautious(battle, mother, enemies) && (!mother.CanMove || battle.Round % 3 != 0 || crisis || economy))
            foreach (var kind in classes)
                if (battle.BuildBlockReason(side, mother.Id, kind)is null)
                    return battle.Build(side, mother.Id, kind, battle.SpawnCells(mother.Id).OrderBy(p => Danger(mother, p)).First());
        foreach (var ship in allies.Where(s => s.Definition.RadarPrice > 0))
            if (battle.Credits(side) >= ship.Definition.RadarPrice + 5 && battle.RadarBlockReason(side, ship.Id)is null)
                return battle.BuyRadar(side, ship.Id);
        if (mother.HasRadar && fleet.Length >= 2 && battle.Credits(side) >= battle.Rules.Mortar.PurchasePrice + 5 && battle.MortarBlockReason(side, mother.Id)is null)
            return battle.BuyMortar(side, mother.Id);
        var contacts = battle.Vision.Contacts(side);
        // Cheap scouts take the uncertainty; the flagship never chases a radar mark.
        int? scout = fleet.Where(s => !s.HasMortar).OrderBy(s => s.Definition.Price).ThenBy(s => s.Id).FirstOrDefault()?.Id;
        foreach (var ship in allies.Where(s => s.CanMove && !s.HasMoved).OrderBy(s => s.Id == scout ? 0 : 1))
        {
            var reachable = battle.Reachable(ship.Id);
            if (reachable.Count < 2)
                continue;
            if (ship.IsAirborne)
            {
                var mark = enemies.Where(e => reachable.ContainsKey(e.Position) && Danger(ship, e.Position) < ship.Health).OrderBy(e => e.Health).FirstOrDefault();
                var goal = mark?.Position ?? reachable.Keys.OrderBy(p => battle.Vision.LastSeen(side, p)).First();
                if (goal != ship.Position)
                    return battle.Move(side, ship.Id, goal);
                continue;
            }

            if ((crisis && enemies.Length > 0 && ship.IsArmed
                && Danger(ship, ship.Position) >= ship.Health * .65
                && allies.Where(a => a.IsArmed && battle.Board.Distance(a.Position, ship.Position) <= 3).Sum(a => a.Health) < Danger(ship, ship.Position) * 1.25)
                || ship.IsMothership && Danger(ship, ship.Position) >= ship.Health * .5)
            {
                double SupportDistance(GridPosition p) => allies.Where(a=>a.Id!=ship.Id && a.IsArmed)
                    .Select(a=>battle.Board.Distance(p,a.Position)).DefaultIfEmpty(0).Min();
                var refuge = reachable.Keys.OrderBy(p => Danger(ship, p)).ThenBy(SupportDistance)
                    .ThenByDescending(p => enemies.Min(e => battle.Board.Distance(p, e.Position))).First();
                if (refuge != ship.Position && Danger(ship, refuge) <= Danger(ship, ship.Position))
                    return battle.Move(side, ship.Id, refuge);
                continue;
            }

            IReadOnlyList<GridPosition>? route = null;
            bool supportedAdvance = false;
            if (ship.Definition.CollectionRange > 0 && mother.Level < 5)
                route = BestRoute(battle.KnownFish(side).Concat(battle.KnownShoals(side)), ship, battle, ship.Definition.CollectionRange);
            if (route is null && ship.IsArmed && !ship.IsMothership)
            {
                if (battle.TreasuryAt(ship.Position)is not null)
                    continue;
                var defeated = towns.Where(v => v.Owner != side && v.Health <= 0).OrderBy(v => battle.Board.Distance(ship.Position, v.Position)).FirstOrDefault();
                if (defeated is not null && battle.Board.GetSurrounding(defeated.Position).Contains(ship.Position))
                    continue;
                route = BestRoute(towns.Where(v => v.Owner != side).Select(v => v.Position), ship, battle, ship.HasMortar ? 4 : 1);
                route ??= BestRoute(battle.ObservedTreasuries(side).Select(t => t.Position), ship, battle, 0);
            }

            if (route is null && ship.IsArmed && (!crisis || !ship.IsMothership))
            {
                var target = enemies.Where(e => !e.IsMothership || e.HealthRatio <= .5 || allies.Where(a => a.IsArmed && !a.IsMothership).Sum(a => battle.Damage(a, e) * (a.Definition.ActionProfile == ActionProfile.Heavy ? 2 : 1)) >= e.Health).OrderBy(e => e.IsMothership).ThenBy(e => e.Health).FirstOrDefault();
                if (target is not null)
                {
                    var candidate = battle.PathToAttackPosition(ship.Id, target.Id);
                    if (candidate.Count > 1)
                    {
                        var next = battle.AffordableDestination(ship.Id, candidate);
                        // A next-turn engagement is accepted only with nearby allied support.
                        double support = allies.Where(a => a.IsArmed && battle.Board.Distance(a.Position, next) <= a.MovementAllowance + a.AttackRange).Sum(a => a.Health);
                        if (Danger(ship, next) < ship.Health || support >= Danger(ship, next) * 2 && !ship.IsMothership)
                        {
                            route = candidate;
                            supportedAdvance = true;
                        }
                    }
                }
            }

            if (route is null && ship.Id == scout && contacts.Count > 0)
                route = BestRoute(contacts, ship, battle, ship.VisualRange);
            if (route is not null)
            {
                var next = battle.AffordableDestination(ship.Id, route);
                if (next != ship.Position && (Danger(ship, next) < ship.Health || supportedAdvance))
                    return battle.Move(side, ship.Id, next);
            }

            // Local frontiers are cheap to score; no full-map path search per fish/cell.
            int Discovery(GridPosition p) => battle.Board.GetSurrounding(p).Count(c => !battle.Vision.IsExplored(side, c));
            var frontier = reachable.Keys.Where(p => p != ship.Position && Danger(ship, p) < ship.Health).OrderByDescending(Discovery).ThenBy(p => battle.Vision.LastSeen(side, p)).ThenBy(p => reachable[p]).FirstOrDefault(ship.Position);
            if (frontier != ship.Position && Discovery(frontier) > 0)
                return battle.Move(side, ship.Id, frontier);
            var chart = battle.Board.Tiles.Where(t => battle.Vision.KnownTerrain(side, t.Position) != TerrainType.Land).OrderBy(t => battle.Vision.LastSeen(side, t.Position)).ThenBy(t => battle.Board.Distance(ship.Position, t.Position)).Take(3).Select(t => t.Position);
            route = BestRoute(chart, ship, battle, 0);
            if (route is not null)
            {
                var next = battle.AffordableDestination(ship.Id, route);
                if (next != ship.Position && Danger(ship, next) < ship.Health)
                    return battle.Move(side, ship.Id, next);
            }
        }

        // A shared radar is sufficient for every armed vessel; never read a contact's HP.
        foreach (var ship in allies.Where(s => s.IsArmed))
            foreach (var contact in contacts)
                if (battle.TargetCells(ship.Id).Contains(contact))
                    return battle.AttackAt(side, ship.Id, contact);
        return battle.EndTurn(side);
    }

    private static IReadOnlyList<GridPosition>? BestRoute(IEnumerable<GridPosition> goals, Ship ship, BattleState battle, int range) => goals.OrderBy(p => battle.Board.Distance(ship.Position, p)).Take(3).Select(p => battle.RouteToward(ship.Id, p, range)).Where(p => p.Count > 1).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
}
