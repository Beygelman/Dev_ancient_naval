using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.AI;

/// <summary>One decision's next-turn exposure cache. Movement is bounded by
/// known terrain, observed blockers, coast policy and diagonal corner rules.
/// Unknown terrain is estimated as sea, never read from the full board.</summary>
internal sealed class AdmiralThreats
{
    private readonly BattleState _battle;
    private readonly Ship[] _enemies;
    private readonly Ship[] _observed;
    private readonly Dictionary<int, GridPosition[]> _positions = new();
    private readonly Dictionary<int, Ship> _probes = new();
    private readonly Dictionary<(int, GridPosition), double> _danger = new();

    internal AdmiralThreats(BattleState battle, IReadOnlyList<Ship> enemies)
    {
        _battle = battle;
        _enemies = enemies.ToArray();
        _observed = battle.ObservedShips(battle.ActiveSide).ToArray();
    }

    internal double At(Ship ship, GridPosition cell)
    {
        if (_danger.TryGetValue((ship.Id, cell), out var retained)) return retained;
        double danger = 0;
        foreach (var enemy in _enemies)
        {
            if (enemy.IsAirborne)
            {
                if (enemy.BombCooldown <= 1 && _battle.Board.InRadius(enemy.Position, cell, enemy.MovementAllowance))
                    danger += _battle.Rules.Balloon.BombDamage;
                continue;
            }
            if (!enemy.IsArmed) continue;
            if (!_probes.TryGetValue(enemy.Id, out var probe))
                _probes[enemy.Id] = probe = SavedShip.From(enemy).Restore(_battle.Rules);
            var firing = Positions(enemy).Any(position =>
            {
                probe.Position = position;
                return _battle.WeaponCovers(probe, cell) && (!ship.IsAirborne || _battle.AntiAirCovers(probe, position, cell));
            });
            if (!firing) continue;
            double damage = Ship.Whole(Math.Max(1, (_battle.UsesMortar(probe, cell)
                ? enemy.CurrentMortarDamage : enemy.CurrentDamage) + enemy.ShotDamageBonus - ship.Definition.Armor));
            int shots = (enemy.Definition.ActionProfile == ActionProfile.Heavy ? 2 : 1) + (enemy.SecondAttackUpgrade ? 1 : 0);
            danger += damage * shots;
        }
        return _danger[(ship.Id, cell)] = danger;
    }

    private GridPosition[] Positions(Ship enemy)
    {
        if (_positions.TryGetValue(enemy.Id, out var retained)) return retained;
        if (enemy.IsStructure || enemy.HasMortar || enemy.MovementAllowance <= 0)
            return _positions[enemy.Id] = new[] { enemy.Position };
        var side = _battle.ActiveSide;
        var board = _battle.Board;
        var blocked = _observed.Where(s => !s.IsAirborne && s.Owner != enemy.Owner).Select(s => s.Position).ToHashSet();
        var threatened = _observed.Where(s => !s.IsAirborne && s.IsArmed && s.Owner != enemy.Owner)
            .SelectMany(s => board.GetSurrounding(s.Position)).ToHashSet();
        TerrainType Known(GridPosition cell) => _battle.Vision.KnownTerrain(side, cell) ?? TerrainType.Water;
        bool Passable(GridPosition cell) => board.Contains(cell) && Known(cell) != TerrainType.Land && !_battle.IsForbidden(cell)
            && (_battle.Rules.FreeCoastalNavigation || !enemy.IsMothership || !board.IsNarrowAt(cell, p => board.Contains(p) && Known(p) == TerrainType.Land));
        int? Cost(GridPosition from, GridPosition to)
        {
            if (!Passable(to) || blocked.Contains(to)) return null;
            if (board.Mesh is not null && !board.GetNeighbors(from).Contains(to))
            {
                var edges = board.GetNeighbors(to).ToHashSet();
                if (board.GetNeighbors(from).Any(p=>edges.Contains(p) && !Passable(p))) return null;
            }
            else if (board.Mesh is null && from.X != to.X && from.Y != to.Y
                && (!Passable(new(from.X,to.Y)) || !Passable(new(to.X,from.Y)))) return null;
            double terrainCost = 1;
            if (!_battle.Rules.FreeCoastalNavigation)
                terrainCost = board.IsNarrowAt(to, p=>board.Contains(p) && Known(p)==TerrainType.Land)
                    ? enemy.Definition.NarrowMovementCost : Known(to)==TerrainType.Coast ? enemy.Definition.CoastMovementCost : 1;
            return Ship.Whole(10 * Math.Max(terrainCost, threatened.Contains(to) ? 2 : 1));
        }
        var route = PathSearch.Find(enemy.Position, enemy.MovementAllowance * 10 + 4, board.GetSurrounding, Cost);
        return _positions[enemy.Id] = route.Costs.Keys.Where(p => p == enemy.Position
            || !_observed.Any(s=>!s.IsAirborne && s.Position==p)).ToArray();
    }
}
