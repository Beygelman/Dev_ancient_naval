using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    private readonly List<Ship> _ships = new();
    private readonly int[] _credits;
    private readonly bool[] _everProduced = new bool[2];
    private readonly List<IncomeSource> _incomeSources = new();
    private int _nextId = 1;
    public GameBoard Board { get; }
    public BattleRules Rules { get; }
    public BattleVision Vision { get; }
    public IReadOnlyList<Ship> Ships { get; }
    public IReadOnlyList<IncomeSource> IncomeSources { get; }
    public Side ActiveSide { get; private set; } = Side.Player;
    public Side? Winner { get; private set; }
    public int Round { get; private set; } = 1;
    public bool IsOver => Winner is not null;

    public BattleState(GameBoard board, BattleRules rules, IEnumerable<(Side Owner, ShipClass Class, GridPosition Position)> setup,
        IEnumerable<GridPosition>? fishSpots = null, int resourceSeed = 1729)
    {
        Board = board; Rules = rules; Vision = new BattleVision(board);
        _credits = new[] { rules.StartingCredits, rules.StartingCredits };
        Ships = _ships.AsReadOnly(); IncomeSources = _incomeSources.AsReadOnly();
        foreach (var item in setup)
        {
            if (!Enum.IsDefined(item.Owner) || (item.Class == ShipClass.Balloon ? !Board.Contains(item.Position) : !IsFreeWater(item.Position)) ||
                (item.Class == ShipClass.Mothership && board.IsNarrowPassage(item.Position)))
                throw new ArgumentException("Invalid fleet deployment.");
            var ship = new Ship(_nextId++, item.Owner, rules.Get(item.Class), item.Position);
            _ships.Add(ship); RegisterShipIncome(ship);
        }
        foreach (var side in Enum.GetValues<Side>())
            if (_ships.Count(s => s.Owner == side && s.Definition.Class == ShipClass.Mothership) != 1 ||
                _ships.Count(s => s.Owner == side && !s.IsAirborne) > rules.FleetLimit)
                throw new ArgumentException("Each side needs one Mothership and must respect the fleet limit.");
        UpdateVision();
        InitializeFishing(fishSpots, resourceSeed);
    }

    public bool Creative { get; private set; }
    public void SetCreative(bool enabled) => Creative = enabled;
    public int BuildPrice(Side side, ShipClass kind) => Creative && side == Side.Player ? 0 : Rules.Get(kind).Price;
    public int CollectionCost(Side side) => Creative && side == Side.Player ? 0 : CollectionPrice;
    public int Credits(Side side) => _credits[(int)side];
    public Ship? Find(int id) => _ships.FirstOrDefault(s => s.Id == id);
    public Ship? At(GridPosition cell) => _ships.FirstOrDefault(s => !s.IsAirborne && s.Position == cell);
    public IEnumerable<Ship> OwnShips(Side side) => _ships.Where(s => s.Owner == side);
    public IEnumerable<Ship> ObservedShips(Side side) => _ships.Where(s => s.Owner == side || Vision.IsVisible(side, s.Position));
    public Ship? ObservedAt(Side side, GridPosition cell) => ObservedShips(side).Where(s => s.Position == cell).OrderBy(s => s.IsAirborne).FirstOrDefault();
    public Ship? FindObserved(Side side, int id) => ObservedShips(side).FirstOrDefault(s => s.Id == id);
    public static double Distance(GridPosition a, GridPosition b) => Math.Sqrt((double)(a.X - b.X) * (a.X - b.X) + (double)(a.Y - b.Y) * (a.Y - b.Y));
    public double Damage(Ship attacker, Ship target) => attacker.IsArmed && !target.IsAirborne ? Ship.Whole(Math.Max(1, attacker.CurrentDamage - target.Definition.Armor)) : 0;
    public bool IsFreeWater(GridPosition cell) => Board.TryGetTile(cell, out var tile) && tile!.Terrain != TerrainType.Land && At(cell) is null;
    private void UpdateVision() => Vision.Recompute(Ships, Round * 2 + (int)ActiveSide);

    private string? ValidateActor(Side requester, int id, out Ship? ship)
    {
        ship = Find(id);
        if (IsOver) return "Бой завершён.";
        if (requester != ActiveSide) return "Сейчас ход другой стороны.";
        if (ship is null || ship.Owner != requester) return "Выберите свой корабль.";
        if (PendingUpgrade(requester) is not null) return "Сначала выберите улучшение Mothership.";
        return null;
    }

    private bool KnownOccupied(Side side, GridPosition cell) => ObservedShips(side).Any(s => !s.IsAirborne && s.Position == cell) || Vision.State(side, cell) == VisibilityState.RadarContact;
    private TerrainType Terrain(Ship ship, GridPosition cell, bool knowledge) => knowledge
        ? Vision.KnownTerrain(ship.Owner, cell) ?? TerrainType.Water : Board.GetTile(cell).Terrain;
    private bool Narrow(Ship ship, GridPosition cell, bool knowledge) => GameBoard.IsNarrowPassage(cell,
        p => Board.Contains(p) && Terrain(ship, p, knowledge) == TerrainType.Land);
    private bool Passable(Ship ship, GridPosition cell, bool knowledge)
    {
        if (!Board.Contains(cell) || Terrain(ship, cell, knowledge) == TerrainType.Land) return false;
        if (ship.Definition.Class == ShipClass.Mothership && Narrow(ship, cell, knowledge)) return false;
        return cell == ship.Position || !(knowledge ? KnownOccupied(ship.Owner, cell) : At(cell) is not null);
    }

    /// <summary>Integer tenths: orthogonal 10, diagonal 14. Unknown cells are
    /// estimated as open water, so route previews cannot reveal terrain or hidden ships.
    /// A four-tenth tolerance rounds the total route down to the nearest whole move point.</summary>
    public int? StepCost(int id, GridPosition from, GridPosition to, bool knowledge = true)
    {
        var ship = Find(id);
        if (ship is null || from == to || Math.Abs(from.X - to.X) > 1 || Math.Abs(from.Y - to.Y) > 1 || !Passable(ship, to, knowledge)) return null;
        bool diagonal = from.X != to.X && from.Y != to.Y;
        if (diagonal && (!Passable(ship, new(from.X, to.Y), knowledge) || !Passable(ship, new(to.X, from.Y), knowledge))) return null;
        int multiplier = Narrow(ship, to, knowledge) ? ship.Definition.NarrowMovementCost :
            Terrain(ship, to, knowledge) == TerrainType.Coast ? ship.Definition.CoastMovementCost : 1;
        return (diagonal ? 14 : 10) * multiplier;
    }

    private (Dictionary<GridPosition, int> Costs, Dictionary<GridPosition, GridPosition> Previous) Flood(Ship ship, int budget)
    {
        var costs = new Dictionary<GridPosition, int> { [ship.Position] = 0 };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new PriorityQueue<GridPosition, (int, int)>();
        int order = 0;
        queue.Enqueue(ship.Position, (0, order++));
        while (queue.TryDequeue(out var current, out var priority))
        {
            if (priority.Item1 != costs[current]) continue;
            foreach (var next in Board.GetSurrounding(current))
            {
                var step = StepCost(ship.Id, current, next);
                if (step is null) continue;
                int cost = costs[current] + step.Value;
                if (cost > budget || (costs.TryGetValue(next, out int old) && old <= cost)) continue;
                costs[next] = cost; previous[next] = current;
                queue.Enqueue(next, (cost, order++));
            }
        }
        return (costs, previous);
    }

    private static IReadOnlyList<GridPosition> Reconstruct(GridPosition from, GridPosition to, IReadOnlyDictionary<GridPosition, GridPosition> previous)
    {
        var path = new List<GridPosition> { to };
        while (path[^1] != from) path.Add(previous[path[^1]]);
        path.Reverse(); return path.AsReadOnly();
    }
    public IReadOnlyDictionary<GridPosition, int> Reachable(int id)
    {
        var ship = Find(id);
        return ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove ? new Dictionary<GridPosition, int>() :
            ship.IsAirborne ? Board.Tiles.Where(t => FlightCost(ship.Position,t.Position) <= ship.MovementRemainingUnits).ToDictionary(t => t.Position, t => FlightCost(ship.Position,t.Position)) : Flood(ship, ship.MovementRemainingUnits + 4).Costs;
    }
    public IReadOnlyList<GridPosition> PathTo(int id, GridPosition destination)
    {
        var ship = Find(id);
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove) return Array.Empty<GridPosition>();
        if (ship.IsAirborne) return Board.Contains(destination) && destination != ship.Position && FlightCost(ship.Position,destination) <= ship.MovementRemainingUnits ? new[] { ship.Position, destination } : Array.Empty<GridPosition>();
        var flood = Flood(ship, ship.MovementRemainingUnits + 4);
        return flood.Costs.ContainsKey(destination) ? Reconstruct(ship.Position, destination, flood.Previous) : Array.Empty<GridPosition>();
    }
    public double PathCost(int id, IReadOnlyList<GridPosition> path)
    {
        int total = 0;
        for (int i = 1; i < path.Count; i++) total += StepCost(id, path[i - 1], path[i]) ?? 0;
        return total / 10.0;
    }
    public IReadOnlyList<GridPosition> RouteToward(int id, GridPosition destination, int stopRange = 0)
    {
        var ship = Find(id);
        if (ship is null) return Array.Empty<GridPosition>();
        var flood = Flood(ship, Board.Width * Board.Height * 60);
        var goal = flood.Costs.Where(p => BattleVision.InRadius(p.Key, destination, stopRange))
            .OrderBy(p => p.Value).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => (GridPosition?)p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
    }
    public IReadOnlyList<GridPosition> PathToAttackPosition(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : FindObserved(ship.Owner, targetId);
        return ship is null || !ship.IsArmed || target is null || target.Owner == ship.Owner ? Array.Empty<GridPosition>() :
            RouteToward(id, target.Position, ship.AttackRange);
    }
    public GridPosition AffordableDestination(int id, IReadOnlyList<GridPosition> path)
    {
        var ship = Find(id)!;
        int spent = 0;
        var destination = ship.Position;
        for (int i = 1; i < path.Count; i++)
        {
            int? step = StepCost(id, path[i - 1], path[i]);
            if (step is null || spent + step > ship.MovementRemainingUnits + 4) break;
            spent += step.Value; destination = path[i];
        }
        return destination;
    }

    public CommandResult Move(Side requester, int id, GridPosition destination)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        if (ship!.IsAirborne) return Fly(ship, destination);
        var planned = PathTo(id, destination);
        if (planned.Count < 2) return CommandResult.Rejected("Нет доступного пути или закончились очки движения.");
        var actual = new List<GridPosition> { ship!.Position };
        var frames = new List<MovementFrame> { MovementFrame(ship) };
        int spent = 0;
        foreach (var next in planned.Skip(1))
        {
            var step = StepCost(id, ship.Position, next, false);
            if (step is null || step > ship.MovementRemainingUnits + 4) break;
            ship.Position = next; ship.MovementSpentUnits += step.Value; spent += step.Value;
            ship.HasMoved = true; actual.Add(next); UpdateVision(); frames.Add(MovementFrame(ship));
        }
        if (actual.Count < 2) return CommandResult.Rejected("Проход недоступен. Выберите другой маршрут.");
        return new(true, $"{ship.Definition.Name}: перемещение." +
            (ship.Position != destination ? " Маршрут остановлен после разведки." : ""), CommandKind.Move, id, Path: actual, Movement: frames);
    }
    private MovementFrame MovementFrame(Ship ship) => new(ship.Position,
        ship.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position), Vision.State(Side.Player, ship.Position) == VisibilityState.RadarContact);

    public bool CanAttack(int id, int targetId)
    {
        var ship = Find(id);
        var target = ship is null ? null : Find(targetId);
        return !IsOver && ship is not null && target is not null && !target.IsAirborne && ship.Owner == ActiveSide && ship.Owner != target.Owner && (Vision.IsVisible(ship.Owner,target.Position) || ship.HasRadar && Vision.Contacts(ship.Owner).Contains(target.Position)) &&
            ship.AttacksRemaining > 0 && BattleVision.InRadius(ship.Position, target.Position, ship.AttackRange);
    }
    public IReadOnlyCollection<GridPosition> AttackCells(int id)
    {
        var ship = Find(id);
        if (ship is null || !ship.IsArmed || IsOver || ship.Owner != ActiveSide || ship.AttacksRemaining == 0) return Array.Empty<GridPosition>();
        return Board.Tiles.Where(t => (Vision.IsVisible(ship.Owner, t.Position) || ship.HasRadar) && BattleVision.InRadius(ship.Position, t.Position, ship.AttackRange))
            .Select(t => t.Position).ToArray();
    }
    public bool CanCounterattack(Ship defender, Ship attacker) => defender.Health > 0 && defender.IsArmed &&
        BattleVision.InRadius(defender.Position, attacker.Position, defender.AttackRange);
    public double PreviewCounterDamage(Ship attacker, Ship defender)
    {
        double remaining = Math.Max(0, defender.Health - Damage(attacker, defender));
        return remaining == 0 || !CanCounterattack(defender, attacker) ? 0 :
            Math.Max(1, Ship.Whole(defender.FullDamage * (0.5 + 0.5 * remaining / defender.MaxHealth)) - attacker.Definition.Armor);
    }
    public CommandResult AttackAt(Side requester, int id, GridPosition cell)
    {
        var target=At(cell);
        return target is null ? CommandResult.Rejected("Здесь нет доступной цели.") : Attack(requester,id,target.Id);
    }
    public IReadOnlyCollection<GridPosition> TargetCells(int id)
    {
        var ship=Find(id);
        return ship is null ? Array.Empty<GridPosition>() : _ships.Where(t=>CanAttack(id,t.Id)).Select(t=>t.Position).ToArray();
    }
    public CommandResult Attack(Side requester, int id, int targetId)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        if (!CanAttack(id, targetId)) return CommandResult.Rejected("Нужна видимая цель в пределах дальности и доступная атака.");
        var target = Find(targetId)!;
        Vision.RevealCombat(target.Owner, ship!.Position);
        Vision.RevealCombat(ship.Owner, target.Position);
        ship.AttacksUsed++;
        if (ship.Definition.ActionProfile == ActionProfile.Standard && ship.HasMoved) ship.MovementLocked = true;
        var shots = new List<CombatShot> { Fire(ship, target, false) };
        // One reply to each incoming attack; replies never recursively trigger replies.
        if (!IsOver && CanCounterattack(target, ship)) shots.Add(Fire(target, ship, true));
        UpdateVision();
        string message = $"{ship.Definition.Name}: {shots[0].Damage:0.##} урона";
        if (shots.Count > 1) message += $" · ответ: {shots[1].Damage:0.##}";
        if (shots.Any(s => s.Promoted)) message += " · ВЕТЕРАН!";
        if (shots.Any(s => s.TargetSunk)) message += " · корабль потоплен";
        return new(true, message, CommandKind.Attack, id, targetId, shots[0].Damage, Shots: shots);
    }
    private CombatShot Fire(Ship attacker, Ship target, bool counter)
    {
        var from = ShipSnapshot.From(attacker); var to = ShipSnapshot.From(target);
        double damage = Math.Min(target.Health, Damage(attacker, target));
        target.Health = Ship.Whole(target.Health - damage);
        bool sunk = target.Health <= 0, promoted = false;
        if (sunk)
        {
            _ships.Remove(target); RemoveIncomeSource($"ship:{target.Id}");
            if (!attacker.IsMothership) attacker.Kills++;
            if (!attacker.IsMothership && !attacker.IsVeteran && attacker.Kills >= 3)
            {
                attacker.IsVeteran = true; attacker.Health = attacker.MaxHealth; promoted = true;
            }
            if (target.Definition.Class == ShipClass.Mothership) Winner = attacker.Owner;
        }
        return new(from, to, damage, counter, sunk, promoted);
    }

    public CommandResult Repair(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        if (!ship!.CanRepair) return CommandResult.Rejected("Ремонт доступен повреждённому кораблю до других действий.");
        double amount = Math.Min(Rules.RepairAmount, ship.MaxHealth - ship.Health);
        ship.Health = Ship.Whole(ship.Health + amount); ship.IsExhausted = true;
        return new(true, $"{ship.Definition.Name}: ремонт +{amount:0.##} HP", CommandKind.Repair, id, Amount: amount);
    }

    public IReadOnlyList<GridPosition> SpawnCells(int mothershipId)
    {
        var ship = Find(mothershipId);
        return ship is null || ship.Definition.Class != ShipClass.Mothership ? Array.Empty<GridPosition>() :
            Board.GetNeighbors(ship.Position).Where(IsFreeWater).ToArray();
    }
    public string? BuildBlockReason(Side requester, int mothershipId, ShipClass shipClass)
    {
        var error = ValidateActor(requester, mothershipId, out var mother);
        if (error is not null) return error;
        if (!Enum.IsDefined(shipClass) || shipClass is ShipClass.Mothership or ShipClass.Balloon) return "Этот класс нельзя построить.";
        if (mother!.Definition.Class != ShipClass.Mothership) return "Корабли строит Mothership.";
        if (mother.HasProduced) return "Этот Mothership уже построил корабль в этом ходу.";
        if (_ships.Count(s => s.Owner == requester && !s.IsAirborne) >= Rules.FleetLimit) return $"Лимит флота: {Rules.FleetLimit}.";
        if (Credits(requester) < BuildPrice(requester,shipClass)) return "Недостаточно средств.";
        if (SpawnCells(mothershipId).Count == 0) return "Нет свободной соседней клетки воды.";
        return null;
    }
    public CommandResult Build(Side requester, int mothershipId, ShipClass shipClass, GridPosition spawn)
    {
        var error = BuildBlockReason(requester, mothershipId, shipClass);
        if (error is not null) return CommandResult.Rejected(error);
        if (!SpawnCells(mothershipId).Contains(spawn)) return CommandResult.Rejected("Выберите свободную клетку рядом с Mothership.");
        var definition = Rules.Get(shipClass);
        var ship = new Ship(_nextId++, requester, definition, spawn);
        bool first = !_everProduced[(int)requester];
        ship.IsExhausted = !first;
        _ships.Add(ship); RegisterShipIncome(ship);
        _credits[(int)requester] -= BuildPrice(requester,shipClass); _everProduced[(int)requester] = true;
        var mother = Find(mothershipId)!; mother.HasProduced = true; mother.MovementLocked = true;
        UpdateVision();
        return new(true, $"{definition.Name} построен. " + (first ? "Может действовать сразу." : "Готов к следующему ходу."), CommandKind.Build, mothershipId, ship.Id);
    }
    public CommandResult EndTurn(Side requester)
    {
        if (IsOver || requester != ActiveSide || PendingUpgrade(requester) is not null) return CommandResult.Rejected("Нельзя завершить ход до выбора улучшения.");
        ActiveSide = ActiveSide == Side.Player ? Side.Enemy : Side.Player;
        if (ActiveSide == Side.Player) Round++;
        Vision.ClearCombatFlashes();
        foreach (var ship in _ships.Where(s => s.Owner == ActiveSide)) ship.ResetTurn();
        _credits[(int)ActiveSide] += Income(ActiveSide); UpdateVision();
        return new(true, ActiveSide == Side.Player ? "Ваш ход." : "Ход противника.", CommandKind.EndTurn);
    }
    private void RegisterShipIncome(Ship ship)
    {
        int income = ship.IsMothership ? Rules.IncomePerMothership + 2 * (ship.Level - 1) + (ship.IncomeUpgrade ? 1 : 0) : ship.Definition.IncomePerTurn;
        if (income > 0) SetIncomeSource(new IncomeSource($"ship:{ship.Id}", ship.Owner, income, ship.Id));
    }
    public int Income(Side side) => _incomeSources.Where(s => s.Owner == side && (s.BoundShipId is null || Find(s.BoundShipId.Value) is not null)).Sum(s => s.Amount);
    public void SetIncomeSource(IncomeSource source)
    {
        if (string.IsNullOrWhiteSpace(source.Id) || !Enum.IsDefined(source.Owner) || source.Amount < 0 ||
            (source.BoundShipId is { } shipId && Find(shipId)?.Owner != source.Owner)) throw new ArgumentException("Invalid income source.");
        _incomeSources.RemoveAll(s => s.Id == source.Id); _incomeSources.Add(source);
    }
    public bool RemoveIncomeSource(string id) => _incomeSources.RemoveAll(s => s.Id == id) > 0;
}
