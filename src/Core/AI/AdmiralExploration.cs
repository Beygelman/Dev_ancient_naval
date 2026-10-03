using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;

/// <summary>Search follows optically recorded flagship sightings, never hidden
/// fleet objects. With no sighting, two separated scouts advance across the
/// remembered coastline to unknown frontiers. At most three route searches per
/// scout/objective are made; there is no path search for every map cell.</summary>
internal static class AdmiralExploration
{
    internal static CommandResult? Step(BattleState battle, IReadOnlyList<Ship> allies,
        IReadOnlyList<Ship> enemies, Func<Ship, GridPosition, double> danger)
    {
        var side = battle.ActiveSide;
        var mobile = allies.Where(s => !s.IsMothership && !s.IsStructure
            && s.CanMove && !s.HasMoved && (s.IsArmed || s.IsAirborne))
            .OrderBy(s => s.HasMortar).ThenBy(s => s.Definition.Price).ThenBy(s => s.Id).ToArray();
        if (mobile.Length == 0 || enemies.Any(e => e.IsMothership)) return null;
        var sightings = battle.KnownFlagships(side).OrderByDescending(s => s.Turn)
            .ThenBy(s => s.Owner).ToArray();
        if (sightings.Length > 0)
        {
            foreach (var ship in mobile.Take(24))
            {
                foreach (var sighting in sightings.Take(3))
                {
                    var route = battle.RouteToward(ship.Id, sighting.Position, Math.Max(0, ship.VisualRange - 1));
                    if (route.Count < 2) continue;
                    var next = battle.AffordableDestination(ship.Id, route);
                    if (next != ship.Position && danger(ship, next) < ship.Health)
                        return battle.Move(side, ship.Id, next);
                }
            }
            return null;
        }
        // Visible combat gets its own policy. An unseen nation is not an excuse
        // to pull a ready gun away from the encounter presently in view.
        if (enemies.Any(e => e.IsArmed && !e.IsStructure)) return null;
        var scouts = mobile.OrderByDescending(s => s.IsAirborne)
            .ThenBy(s => s.HasMortar).ThenBy(s => s.Definition.Price).ThenBy(s => s.Id).Take(2).ToArray();
        var frontiers = battle.Board.Tiles.Where(t => !battle.Vision.IsExplored(side, t.Position)
            && battle.Board.GetSurrounding(t.Position).Any(p => battle.Vision.IsExplored(side, p)
                && battle.Vision.KnownTerrain(side, p) != TerrainType.Land)).Select(t => t.Position).ToArray();
        foreach (var ship in scouts)
        {
            var others = scouts.Where(s => s.Id != ship.Id).ToArray();
            double Separation(GridPosition p) => others.Select(s => battle.Board.Distance(s.Position, p)).DefaultIfEmpty(0).Min();
            int Discovery(GridPosition p) => battle.Board.GetSurrounding(p)
                .Count(c => !battle.Vision.IsExplored(side, c));
            var goals = frontiers.OrderBy(p => battle.Board.Distance(ship.Position, p))
                .ThenByDescending(Separation).ThenByDescending(Discovery)
                .ThenBy(p => p.Y).ThenBy(p => p.X).Take(3).ToArray();
            foreach (var goal in goals)
            {
                var route = battle.RouteToward(ship.Id, goal, 0);
                if (route.Count < 2) continue;
                var next = battle.AffordableDestination(ship.Id, route);
                if (next != ship.Position && danger(ship, next) < ship.Health)
                    return battle.Move(side, ship.Id, next);
            }
        }
        return null;
    }
}
