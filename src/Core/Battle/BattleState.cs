using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;

namespace DevAncientNaval.Core.Battle;

public sealed class BattleState
{
    private readonly List<Ship> _ships = new();
    private readonly int[] _credits;
    private readonly bool[] _everProduced = new bool[2];
    private readonly List<IncomeSource> _incomeSources = new();
    private int _nextId = 1;
    public GameBoard Board { get; }
    public BattleRules Rules { get; }
    public IReadOnlyList<Ship> Ships { get; }
    public IReadOnlyList<IncomeSource> IncomeSources { get; }
    public Side ActiveSide { get; private set; } = Side.Player;
    public Side? Winner { get; private set; }
    public int Round { get; private set; } = 1;
    public bool IsOver => Winner is not null;

    public BattleState(GameBoard board, BattleRules rules, IEnumerable<(Side Owner, ShipClass Class, GridPosition Position)> setup)
    {
        Board = board;
        Rules = rules;
        _credits = new[] { rules.StartingCredits, rules.StartingCredits };
        Ships = _ships.AsReadOnly();
        IncomeSources = _incomeSources.AsReadOnly();
        foreach (var item in setup)
        {
            if (!Enum.IsDefined(item.Owner) || !IsFreeWater(item.Position)) throw new ArgumentException("Invalid fleet deployment.");
            _ships.Add(new Ship(_nextId++, item.Owner, rules.Get(item.Class), item.Position));
        }
        foreach (var side in Enum.GetValues<Side>())
            if (_ships.Count(s => s.Owner == side && s.Definition.Class == ShipClass.Mothership) != 1 ||
                _ships.Count(s => s.Owner == side) > rules.FleetLimit)
                throw new ArgumentException("Each side needs one Mothership and must respect the fleet limit.");
        foreach (var mother in _ships.Where(s => s.Definition.Class == ShipClass.Mothership))
            SetIncomeSource(new IncomeSource($"ship:{mother.Id}", mother.Owner, rules.IncomePerMothership, mother.Id));
    }

    public int Credits(Side side) => _credits[(int)side];
    public Ship? Find(int id) => _ships.FirstOrDefault(s => s.Id == id);
    public Ship? At(GridPosition cell) => _ships.FirstOrDefault(s => s.Position == cell);
    public static int Distance(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    public int Damage(Ship attacker, Ship target) => Math.Max(1, attacker.Definition.Damage - target.Definition.Armor);
    public bool IsFreeWater(GridPosition cell) => Board.TryGetTile(cell, out var tile) && tile!.Terrain == TerrainType.Water && At(cell) is null;

    private string? ValidateActor(Side requester, int id, out Ship? ship)
    {
        ship = Find(id);
        if (IsOver) return "Бой завершён.";
        if (requester != ActiveSide) return "Сейчас ход другой стороны.";
        if (ship is null || ship.Owner != requester) return "Выберите свой корабль.";
        return null;
    }

    public IReadOnlyDictionary<GridPosition, int> Reachable(int id)
    {
        var ship = Find(id);
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove)
            return new Dictionary<GridPosition, int>();
        return Flood(ship.Position, ship.MovementRemaining).Distances;
    }

    private (Dictionary<GridPosition, int> Distances, Dictionary<GridPosition, GridPosition> Previous) Flood(GridPosition start, int budget)
    {
        var distance = new Dictionary<GridPosition, int> { [start] = 0 };
        var previous = new Dictionary<GridPosition, GridPosition>();
        var queue = new Queue<GridPosition>();
        queue.Enqueue(start);
        while (queue.TryDequeue(out var current))
        {
            if (distance[current] >= budget) continue;
            foreach (var next in Board.GetNeighbors(current))
            {
                if (distance.ContainsKey(next) || !IsFreeWater(next)) continue;
                distance[next] = distance[current] + 1;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        return (distance, previous);
    }

    private static IReadOnlyList<GridPosition> Reconstruct(GridPosition from, GridPosition to,
        IReadOnlyDictionary<GridPosition, GridPosition> previous)
    {
        var path = new List<GridPosition> { to };
        while (path[^1] != from) path.Add(previous[path[^1]]);
        path.Reverse();
        return path.AsReadOnly();
    }

    public IReadOnlyList<GridPosition> PathTo(int id, GridPosition destination)
    {
        var ship = Find(id);
        if (ship is null || IsOver || ship.Owner != ActiveSide || !ship.CanMove || !IsFreeWater(destination))
            return Array.Empty<GridPosition>();
        var flood = Flood(ship.Position, ship.MovementRemaining);
        return flood.Distances.ContainsKey(destination)
            ? Reconstruct(ship.Position, destination, flood.Previous) : Array.Empty<GridPosition>();
    }

    // Full shortest route to a firing position. AI and UI share terrain/occupancy rules.
    public IReadOnlyList<GridPosition> PathToAttackPosition(int id, int targetId)
    {
        var ship = Find(id);
        var target = Find(targetId);
        if (ship is null || target is null || target.Owner == ship.Owner) return Array.Empty<GridPosition>();
        var flood = Flood(ship.Position, Board.Width * Board.Height);
        var goal = flood.Distances.Where(p => Distance(p.Key, target.Position) <= ship.Definition.AttackRange)
            .OrderBy(p => p.Value).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => (GridPosition?)p.Key).FirstOrDefault();
        return goal is { } cell ? Reconstruct(ship.Position, cell, flood.Previous) : Array.Empty<GridPosition>();
    }

    public CommandResult Move(Side requester, int id, GridPosition destination)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        var path = PathTo(id, destination);
        if (path.Count < 2) return CommandResult.Rejected("Нет доступного пути или закончились очки движения.");
        ship!.Position = destination;
        ship.MovementRemaining -= path.Count - 1;
        ship.HasMoved = true;
        return new(true, $"{ship.Definition.Name}: перемещение на {path.Count - 1} кл.", CommandKind.Move, id, Path: path);
    }

    public bool CanAttack(int id, int targetId)
    {
        var ship = Find(id);
        var target = Find(targetId);
        return !IsOver && ship is not null && target is not null && ship.Owner == ActiveSide &&
            ship.Owner != target.Owner && ship.AttacksRemaining > 0 && Distance(ship.Position, target.Position) <= ship.Definition.AttackRange;
    }

    public CommandResult Attack(Side requester, int id, int targetId)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        if (!CanAttack(id, targetId)) return CommandResult.Rejected("Цель вне дальности или атаки закончились.");
        var target = Find(targetId)!;
        int damage = Math.Min(target.Health, Damage(ship!, target));
        target.Health -= damage;
        ship!.AttacksUsed++;
        if (ship.Definition.ActionProfile == ActionProfile.Standard && ship.HasMoved) ship.MovementLocked = true;
        string message = $"{ship.Definition.Name} → {target.Definition.Name}: −{damage} HP";
        if (target.Health == 0)
        {
            _ships.Remove(target);
            message += " · уничтожен";
            if (target.Definition.Class == ShipClass.Mothership) Winner = requester;
        }
        return new(true, message, CommandKind.Attack, id, targetId, damage);
    }

    public CommandResult Repair(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null) return CommandResult.Rejected(error);
        if (!ship!.CanRepair) return CommandResult.Rejected("Ремонт доступен повреждённому кораблю до других действий.");
        int amount = Math.Min(Rules.RepairAmount, ship.Definition.MaxHealth - ship.Health);
        ship.Health += amount;
        ship.IsExhausted = true;
        ship.MovementRemaining = 0;
        return new(true, $"{ship.Definition.Name}: ремонт +{amount} HP", CommandKind.Repair, id, Amount: amount);
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
        if (!Enum.IsDefined(shipClass) || shipClass == ShipClass.Mothership) return "Этот класс нельзя построить.";
        if (mother!.Definition.Class != ShipClass.Mothership) return "Корабли строит Mothership.";
        if (mother.HasProduced) return "Этот Mothership уже построил корабль в этом ходу.";
        if (_ships.Count(s => s.Owner == requester) >= Rules.FleetLimit) return $"Лимит флота: {Rules.FleetLimit}.";
        if (Credits(requester) < Rules.Get(shipClass).Price) return "Недостаточно средств.";
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
        if (!first) { ship.IsExhausted = true; ship.MovementRemaining = 0; }
        _ships.Add(ship);
        _credits[(int)requester] -= definition.Price;
        _everProduced[(int)requester] = true;
        Find(mothershipId)!.HasProduced = true;
        return new(true, $"{definition.Name} построен. " + (first ? "Первое подкрепление может действовать сразу." : "Готов к следующему ходу."),
            CommandKind.Build, mothershipId, ship.Id);
    }

    public CommandResult EndTurn(Side requester)
    {
        if (IsOver || requester != ActiveSide) return CommandResult.Rejected("Нельзя завершить этот ход.");
        ActiveSide = ActiveSide == Side.Player ? Side.Enemy : Side.Player;
        if (ActiveSide == Side.Player) Round++;
        foreach (var ship in _ships.Where(s => s.Owner == ActiveSide)) ship.ResetTurn();
        _credits[(int)ActiveSide] += Income(ActiveSide);
        return new(true, ActiveSide == Side.Player ? "Ваш ход." : "Ход противника.", CommandKind.EndTurn);
    }

    public int Income(Side side) => _incomeSources.Where(s => s.Owner == side &&
        (s.BoundShipId is null || Find(s.BoundShipId.Value) is not null)).Sum(s => s.Amount);

    public void SetIncomeSource(IncomeSource source)
    {
        if (string.IsNullOrWhiteSpace(source.Id) || !Enum.IsDefined(source.Owner) || source.Amount < 0 ||
            (source.BoundShipId is { } shipId && Find(shipId)?.Owner != source.Owner))
            throw new ArgumentException("Invalid income source.");
        _incomeSources.RemoveAll(s => s.Id == source.Id);
        _incomeSources.Add(source);
    }

    public bool RemoveIncomeSource(string id) => _incomeSources.RemoveAll(s => s.Id == id) > 0;
}
