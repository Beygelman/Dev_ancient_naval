using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;
/// <summary>Decisions use this side's observations, radar contacts and terrain memory.</summary>
public static class SimpleOpponent
{
    public static CommandResult Step(BattleState battle)
    {
        var result = Decide(battle);
        // A fog preview can be legal while the first actual cell is blocked by
        // an undiscovered obstacle. Recover from that observed command result,
        // rather than repeating a rejected order indefinitely.
        return !result.Success && result.Message.StartsWith("Passage blocked", StringComparison.Ordinal)
            ? AINavigationRecovery.Step(battle) : result;
    }

    private static CommandResult Decide(BattleState battle)
    {
        if (battle.IsOver)
            return CommandResult.Rejected("The battle is over.");
        var side = battle.ActiveSide;
        if (side == Side.Pirates)
            return battle.PirateStep();
        if (battle.PendingUpgrade(side)is { } upgrading)
        {
            var preferred = upgrading.PendingUpgradeLevel switch
            {
                2 => UpgradeChoice.FishingBoat,
                3 => upgrading.HealthRatio < .75 ? UpgradeChoice.Restoration : UpgradeChoice.Vision,
                4 => UpgradeChoice.SecondAttack,
                _ => UpgradeChoice.Firepower
            };
            var choices = battle.UpgradeOptions(upgrading.Id);
            var result = battle.ChooseUpgrade(side, upgrading.Id, choices.Contains(preferred) ? preferred : choices[0]);
            // A completely full sea cannot accept the free fishing boat; choose the other reward.
            return result.Success ? result : battle.ChooseUpgrade(side, upgrading.Id, choices.First(c => c != preferred));
        }

        if (battle.Difficulty == AiDifficulty.Admiral) return AdmiralOpponent.Step(battle);
        if (battle.Difficulty == AiDifficulty.Boatswain) return BoatswainOpponent.Step(battle);
        var allies = battle.OwnShips(side).ToArray();
        var enemies = battle.ObservedShips(side).Where(s => s.Owner != side).ToArray();
        var villages = battle.ObservedVillages(side).ToArray();
        foreach (var mother in allies.Where(s => s.IsMothership))
            if (FlagshipSafety.Retreat(battle, mother, enemies)is { } retreat)
                return retreat;
        foreach (var ship in allies)
            if (battle.CanLootTreasury(side, ship.Id))
                return battle.LootTreasury(side, ship.Id);
        foreach (var village in villages)
            if (battle.CanCaptureVillage(side, village.Id))
                return battle.CaptureVillage(side, village.Id);
        foreach (var balloon in allies.Where(s => s.IsAirborne))
            if (battle.CanDropBomb(balloon.Id) && enemies.Any(e => e.Position == balloon.Position))
                return battle.DropBomb(side, balloon.Id);
        foreach (var ship in allies)
        {
            var target = enemies.Where(t => battle.CanAttack(ship.Id, t.Id)).OrderBy(t => t.Health <= battle.Damage(ship, t) ? 0 : 1).ThenBy(t => t.Definition.Class == ShipClass.Mothership ? 0 : 1).ThenBy(t => t.Health).FirstOrDefault();
            if (target is not null)
                return battle.Attack(side, ship.Id, target.Id, battle.CanDoubleSalvo(ship.Id, target.Position)
                    && target.Health > battle.Damage(ship, target));
        }

        foreach (var ship in allies.Where(s => s.IsArmed))
            foreach (var contact in battle.Vision.Contacts(side))
                if (battle.TargetCells(ship.Id).Contains(contact))
                    return battle.AttackAt(side, ship.Id, contact);
        foreach (var ship in allies.Where(s => s.IsArmed))
            foreach (var village in villages.Where(v => v.Owner != side && v.Health > 0))
                if (battle.CanAttackVillage(ship.Id, village.Id))
                    return battle.AttackVillage(side, ship.Id, village.Id);
        foreach (var ship in allies)
            if (ship.CanRepair && ship.HealthRatio <= 0.5)
                return battle.Repair(side, ship.Id);
        foreach (var mother in allies.Where(s => s.IsMothership && s.HasRadar))
            if (battle.Credits(side) >= 14 && battle.MortarBlockReason(side, mother.Id)is null)
                return battle.BuyMortar(side, mother.Id);
        foreach (var collector in allies.Where(s => s.Definition.CollectionRange > 0))
        {
            var sites = battle.DockCells(collector.Id);
            if (battle.Credits(side) >= battle.DockPrice(side) && sites.Count > 0)
                return battle.BuildDock(side, collector.Id, sites.First());
        }

        foreach (var collector in allies.Where(s => s.Definition.CollectionRange > 0))
        {
            var fish = battle.CollectionCells(collector.Id);
            if (battle.Credits(side) >= battle.CollectionCost(side) && fish.Count > 0)
                return battle.Collect(side, collector.Id, fish.First());
        }

        if (VillageDevelopment.Step(battle, enemies) is { } development)
            return development;
        foreach (var ship in allies.Where(s => s.Definition.RadarPrice > 0))
            if (battle.Credits(side) >= 8 && battle.RadarBlockReason(side, ship.Id)is null)
                return battle.BuyRadar(side, ship.Id);
        foreach (var village in villages.Where(v => v.Owner == side))
        {
            if (battle.Credits(side) >= battle.FortificationPrice(side) + 4 && battle.FortifyBlockReason(side, village.Id)is null)
                return battle.FortifyVillage(side, village.Id);
            foreach (var kind in new[]
            {
                ShipClass.Kolonel,
                ShipClass.Invader,
                ShipClass.Garrison
            }

            )
                if (battle.VillageBuildBlockReason(side, village.Id, kind)is null)
                    return battle.BuildFromVillage(side, village.Id, kind, battle.VillageSpawnCells(village.Id).OrderBy(p => enemies.Length > 0 ? enemies.Min(e => battle.Board.Distance(p, e.Position)) : 0).First());
        }

        foreach (var mother in allies.Where(s => s.Definition.Class == ShipClass.Mothership))
        {
            if (FlagshipSafety.IsCautious(battle, mother, enemies))
                continue;
            // Building locks movement. Reserve regular turns for advancing the flagship,
            // otherwise low-cost replacements can keep both fleets anchored indefinitely.
            if (battle.Round % 3 == 0 && mother.CanMove)
                continue;
            var preferred = allies.Count(s => s.Definition.Class == ShipClass.Fishing) < 2 && enemies.Length == 0 ? ShipClass.Fishing : battle.Round % 4 == 0 ? ShipClass.Togus : allies.Length % 3 == 0 ? ShipClass.Kolonel : ShipClass.Invader;
            foreach (var type in new[]
            {
                preferred,
                ShipClass.Garrison
            }.Distinct())
            {
                if (battle.BuildBlockReason(side, mother.Id, type)is not null)
                    continue;
                var spawn = battle.SpawnCells(mother.Id).OrderBy(p => enemies.Length > 0 ? enemies.Min(e => battle.Board.Distance(p, e.Position)) : battle.Board.Distance(p, battle.Board.CentralCell)).First();
                return battle.Build(side, mother.Id, type, spawn);
            }
        }

        foreach (var ship in allies)
        {
            // One movement order per ship per turn prevents oscillation when a route is blocked.
            if (!ship.CanMove || ship.HasMoved)
                continue;
            if (ship.IsMothership && FlagshipSafety.IsCautious(battle, ship, enemies))
                continue;
            if (ship.IsAirborne)
            {
                var cells = battle.Reachable(ship.Id).Keys.Where(p => p != ship.Position).ToArray();
                var target = !ship.BombUsed ? enemies.Where(e => cells.Contains(e.Position)).OrderByDescending(e => e.IsMothership).ThenByDescending(e => Math.Min(6, e.Health)).FirstOrDefault() : null;
                var destination = target?.Position ?? cells.OrderBy(p => battle.Vision.LastSeen(side, p)).FirstOrDefault(ship.Position);
                if (destination != ship.Position)
                    return battle.Move(side, ship.Id, destination);
                continue;
            }

            if (ship.IsArmed)
            {
                if (battle.TreasuryAt(ship.Position)is not null)
                    continue;
                var treasureRoute = battle.ObservedTreasuries(side).Select(t => battle.RouteToward(ship.Id, t.Position)).Where(p => p.Count > 1 && p.Count < 7).OrderBy(p => p.Count).FirstOrDefault();
                if (treasureRoute is not null)
                {
                    var destination = battle.AffordableDestination(ship.Id, treasureRoute);
                    if (destination != ship.Position)
                        return battle.Move(side, ship.Id, destination);
                }

                var settlements = villages.Where(v => v.Owner != side).ToArray();
                if (settlements.Any(v => battle.Board.GetSurrounding(v.Position).Contains(ship.Position) && (v.Health <= 0 || !ship.HasMortar)))
                    continue;
                var captureRoute = settlements.Select(v => battle.RouteToward(ship.Id, v.Position, v.Health > 0 && ship.HasMortar ? 4 : 1)).Where(p => p.Count > 1 && battle.PathCost(ship.Id, p) <= ship.MovementAllowance * 2).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
                if (captureRoute is not null)
                {
                    var destination = battle.AffordableDestination(ship.Id, captureRoute);
                    if (destination != ship.Position)
                        return battle.Move(side, ship.Id, destination);
                }
            }

            if (!ship.IsArmed)
            {
                if (battle.Mothership(side)is { Level: < 5 })
                {
                    var resourceRoute = battle.KnownFish(side).Select(p => battle.RouteToward(ship.Id, p, ship.Definition.CollectionRange)).Where(p => p.Count > 1).OrderBy(p => p.Count).FirstOrDefault();
                    if (resourceRoute is not null)
                    {
                        var destination = battle.AffordableDestination(ship.Id, resourceRoute);
                        if (destination != ship.Position)
                            return battle.Move(side, ship.Id, destination);
                    }
                }

                var mother = allies.First(s => s.Definition.Class == ShipClass.Mothership);
                if (battle.Board.Distance(ship.Position, mother.Position) > 2)
                    continue;
                var safe = battle.Reachable(ship.Id).Keys.Where(p => p != ship.Position).OrderByDescending(p => enemies.Length == 0 ? 0 : enemies.Min(e => battle.Board.Distance(p, e.Position))).ThenByDescending(p => battle.Board.Distance(p, mother.Position)).FirstOrDefault(ship.Position);
                if (safe != ship.Position)
                    return battle.Move(side, ship.Id, safe);
                continue;
            }

            var route = enemies.Where(e => !e.IsAirborne || ship.IsMothership).Select(e => battle.PathToAttackPosition(ship.Id, e.Id)).Where(p => p.Count > 1).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
            route ??= battle.Vision.Contacts(side).Select(p => battle.RouteToward(ship.Id, p, Math.Min(ship.Definition.VisualRange, ship.Definition.AttackRange))).Where(p => p.Count > 1).OrderBy(p => battle.PathCost(ship.Id, p)).FirstOrDefault();
            if (route is not null)
            {
                var destination = battle.AffordableDestination(ship.Id, route);
                if (destination != ship.Position)
                    return battle.Move(side, ship.Id, destination);
            }

            var available = battle.Reachable(ship.Id);
            int Discovery(GridPosition p) => battle.Board.Tiles.Count(t => !battle.Vision.IsExplored(side, t.Position) && battle.Board.InRadius(p, t.Position, ship.Definition.VisualRange));
            var frontier = available.Keys.Where(p => p != ship.Position).OrderByDescending(Discovery).ThenBy(p => battle.Vision.LastSeen(side, p)).ThenByDescending(p => available[p]).FirstOrDefault(ship.Position);
            if (frontier != ship.Position && Discovery(frontier) > 0)
                return battle.Move(side, ship.Id, frontier);
            var goal = battle.Board.Tiles.Where(t => battle.Vision.KnownTerrain(side, t.Position) != TerrainType.Land).OrderBy(t => battle.Vision.LastSeen(side, t.Position)).ThenBy(t => battle.Board.Distance(ship.Position, t.Position)).Take(12).Select(t => battle.RouteToward(ship.Id, t.Position)).FirstOrDefault(p => p.Count > 1);
            if (goal is not null)
            {
                var destination = battle.AffordableDestination(ship.Id, goal);
                if (destination != ship.Position)
                    return battle.Move(side, ship.Id, destination);
            }
        }

        return battle.EndTurn(side);
    }
}
