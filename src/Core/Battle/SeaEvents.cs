using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
public enum TreasuryReward
{
    AncientGun,
    Currency,
    AncientBalloon,
    Resources,
    Whirlpool
}

public sealed record Treasury(int Id, GridPosition Position, bool IsCollected = false);
public sealed record Whirlpool(GridPosition Position, IReadOnlyCollection<GridPosition> Cells);
public sealed partial class BattleState
{
    private sealed record WaitingCrew(int ShipId, GridPosition Position, int Since);
    private readonly List<Treasury> _treasuries = new();
    private readonly List<Whirlpool> _whirlpools = new();
    private readonly HashSet<GridPosition> _forbidden = new();
    private readonly Dictionary<int, WaitingCrew> _treasuryWaits = new();
    private readonly Dictionary<(int Village, Side Side, int Ship), WaitingCrew> _captureWaits = new();
    private readonly Dictionary<int, GridPosition> _pirateHomes = new();
    private readonly Dictionary<int, TreasuryReward> _treasuryOutcomes = new();
    private Random _eventRandom = new(1);
    private int _eventSeed = 1, _eventDraws;
    private int NextEvent(int maximum = int.MaxValue)
    {
        _eventDraws++;
        return _eventRandom.Next(maximum);
    }

    private static readonly TreasuryRules DefaultTreasuryRules = new();
    public static TreasuryReward RewardForRoll(int roll) => DefaultTreasuryRules.RewardForRoll(roll);
    public int TurnSerial { get; private set; }
    public IReadOnlyList<Treasury> Treasuries => _treasuries.Where(t => !t.IsCollected).ToArray();
    public IReadOnlyList<Treasury> TreasuryRuins => _treasuries.AsReadOnly();
    public IReadOnlyList<Whirlpool> Whirlpools => _whirlpools.AsReadOnly();

    public bool IsForbidden(GridPosition cell) => _forbidden.Contains(cell);
    public Treasury? TreasuryAt(GridPosition cell) => _treasuries.FirstOrDefault(t => t.Position == cell && !t.IsCollected);
    public IEnumerable<Treasury> ObservedTreasuries(Side side) => _treasuries.Where(t => !t.IsCollected && Vision.IsVisible(side, t.Position));
    public IEnumerable<Treasury> ObservedTreasuryRuins(Side side) => _treasuries.Where(t => Vision.IsVisible(side, t.Position));
    public TreasuryReward? LastTreasuryReward { get; private set; }

    private void InitializeSeaEvents(bool populate, int seed)
    {
        _eventSeed = seed ^ 0x4F21;
        _eventDraws = 0;
        _eventRandom = new Random(_eventSeed);
        foreach (var pirate in OwnShips(Side.Pirates))
            _pirateHomes[pirate.Id] = pirate.Position;
        if (!populate)
            return;
        var candidates = Board.Tiles.Where(t => t.Terrain == TerrainType.Water && IsFreeWater(t.Position) && Ships.Where(s => s.Owner != Side.Pirates).All(s => Board.Distance(s.Position, t.Position) > 4) && Board.BlastCells(t.Position).Count == 9 && Board.BlastCells(t.Position).All(p => Board.GetTile(p).Terrain != TerrainType.Land)).Select(t => t.Position).OrderBy(_ => NextEvent()).ToArray();
        var chosen = Enumerable.Range(0, _factions.Count).Select(_ => new List<GridPosition>()).ToArray();
        foreach (var cell in candidates)
        {
            int side = Board.StartingTerritory(cell, _factions.Count);
            if (chosen[side].Count >= 3 || chosen.SelectMany(c => c).Any(p => Board.Distance(p, cell) < 4))
                continue;
            chosen[side].Add(cell);
        }

        int count = chosen.Min(group => group.Count);
        foreach (var cell in chosen.SelectMany(c => c.Take(count)))
        {
            _treasuries.Add(new(_nextId++, cell));
            _fish.Remove(cell);
            _shoals.Remove(cell);
        }

        // One patrol per starting territory gives each fleet equal pirate pressure.
        for (int side = 0; side < _factions.Count; side++)
        {
            var home = candidates.Where(p => Board.StartingTerritory(p, _factions.Count) == side && TreasuryAt(p)is null && IsFreeWater(p)).Select(p => (GridPosition? )p).FirstOrDefault();
            if (home is not { } position)
                continue;
            var pirate = new Ship(_nextId++, Side.Pirates, Rules.Get(ShipClass.PirateSchooner), position);
            _ships.Add(pirate);
            _pirateHomes[pirate.Id] = position;
        }
    }

    private void InvalidateWaiting(int shipId)
    {
        foreach (var key in _treasuryWaits.Where(w => w.Value.ShipId == shipId).Select(w => w.Key).ToArray())
            _treasuryWaits.Remove(key);
        foreach (var key in _captureWaits.Where(w => w.Value.ShipId == shipId).Select(w => w.Key).ToArray())
            _captureWaits.Remove(key);
    }

    private bool ReadyCrew(WaitingCrew crew, Side side) => crew.Since < TurnSerial && Find(crew.ShipId)is { } ship && ship.Owner == side && ship.Position == crew.Position && !ship.IsExhausted && !ship.HasMoved && ship.AttacksUsed == 0;
    private void EndSeaEventTurn(Side side)
    {
        foreach (var treasury in _treasuries.Where(t => !t.IsCollected))
        {
            var ship = At(treasury.Position);
            if (ship?.Owner == side && side != Side.Pirates && IsCapturingShip(ship))
                _treasuryWaits[treasury.Id] = new(ship.Id, ship.Position, TurnSerial);
        }

        TurnSerial++;
    }

    public bool CanLootTreasury(Side side, int shipId)
    {
        var ship = Find(shipId);
        var treasury = ship is null ? null : TreasuryAt(ship.Position);
        return !IsOver && side == ActiveSide && side != Side.Pirates && PendingUpgrade(side)is null && ship?.Owner == side && IsCapturingShip(ship) && treasury is not null && _treasuryWaits.TryGetValue(treasury.Id, out var crew) && crew.ShipId == shipId && ReadyCrew(crew, side);
    }

    public CommandResult LootTreasury(Side side, int shipId)
    {
        if (!CanLootTreasury(side, shipId))
            return CommandResult.Rejected("Keep a combat ship on the treasury until its next turn, then plunder before taking other actions.");
        var ship = Find(shipId)!;
        var treasury = TreasuryAt(ship.Position)!;
        // A temporarily blocked tower berth must not let retries reroll a discovery.
        if (!_treasuryOutcomes.TryGetValue(treasury.Id, out var reward))
            _treasuryOutcomes[treasury.Id] = reward = Rules.Treasury.RewardForRoll(NextEvent(100));
        GridPosition? berth = null;
        if (reward == TreasuryReward.AncientGun)
        {
            berth = Board.Tiles.Where(t => IsFreeWater(t.Position)).OrderBy(t => Board.Distance(ship.Position, t.Position)).Select(t => (GridPosition? )t.Position).FirstOrDefault();
            if (berth is null)
                return CommandResult.Rejected("No free water remains for an ancient tower.");
        }

        if (Rules.PersistTreasuryRuins) _treasuries[_treasuries.IndexOf(treasury)] = treasury with { IsCollected = true };
        else _treasuries.Remove(treasury);
        _treasuryWaits.Remove(treasury.Id);
        LastTreasuryReward = reward;
        _treasuryOutcomes.Remove(treasury.Id);
        ship.IsExhausted = true;
        string message;
        switch (reward)
        {
            case TreasuryReward.AncientGun:
                _ships.Add(new Ship(_nextId++, side, Rules.Get(ShipClass.AncientGun), berth!.Value) { IsExhausted = true, IsAncient = true });
                message = "Ancient mortar tower claimed: damage 5, range/radar 5, sight 3. Ready next turn.";
                break;
            case TreasuryReward.Currency:
                _credits[(int)side] += Rules.Treasury.CurrencyReward;
                RecordCurrencyReceipt(side, Rules.Treasury.CurrencyReward);
                message = $"Treasury plundered: +{Rules.Treasury.CurrencyReward} Thors.";
                break;
            case TreasuryReward.AncientBalloon:
                _ships.Add(new Ship(_nextId++, side, Rules.Get(ShipClass.Balloon), ship.Position) { IsAncient = true, IsExhausted = true });
                message = "Ancient Balloon discovered. Ready next turn.";
                break;
            case TreasuryReward.Resources:
                GrantResources(Mothership(side)!, Rules.Treasury.ResourceReward);
                message = $"Treasury plundered: +{Rules.Treasury.ResourceReward} Mothership resources.";
                break;
            default:
                var cells = Board.BlastCells(ship.Position);
                _forbidden.UnionWith(cells);
                _whirlpools.Add(new(ship.Position, cells));
                RemoveDestroyedShip(ship);
                message = "A whirlpool swallowed the ship. Nine tiles are now impassable.";
                break;
        }

        UpdateVision();
        return new(true, message, CommandKind.Loot, shipId, treasury.Id, Path: new[] { treasury.Position });
    }

    private void RewardPirateDefeat(Ship attacker, Ship target) => RewardPirateDefeat(attacker.Owner, target);
    private void RewardPirateDefeat(Side owner, Ship target)
    {
        if (target.Owner != Side.Pirates || owner == Side.Pirates)
            return;
        _credits[(int)owner] += Rules.PirateCurrencyReward;
        RecordCurrencyReceipt(owner, Rules.PirateCurrencyReward);
        if (Mothership(owner)is { } mother)
            GrantResources(mother, Rules.PirateResourceReward);
        _pirateHomes.Remove(target.Id);
    }

    private List<HealingReceipt> AutomaticRepairs(Side side)
    {
        var healed = new List<HealingReceipt>();
        foreach (var ship in OwnShips(side))
            if (ship.CanRepair || ship.Definition.Class == ShipClass.AncientGun && !ship.IsExhausted && ship.AttacksUsed == 0 && !ship.HasRepaired && ship.Health < ship.MaxHealth && Rules.AncientAutoRepairAmount > 0)
            {
                double before = ship.Health;
                ship.Health = Math.Min(ship.MaxHealth, ship.Health + (ship.Definition.Class == ShipClass.AncientGun ? Rules.AncientAutoRepairAmount : Rules.AutoRepairAmount));
                if (ship.Health > before)
                    healed.Add(new(ship.Position, ship.Health - before, ship.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position)));
                ship.HasRepaired = true;
            }

        foreach (var village in _villages.Where(v => v.Owner == side && v.Health > 0 && !v.HasRepaired && !v.HasAttacked))
        {
            double before = village.Health;
            village.Health = Math.Min(village.MaxHealth, village.Health + Rules.RepairAmount);
            if (village.Health > before)
                healed.Add(new(village.Position, village.Health - before, village.Owner == Side.Player || Vision.IsVisible(Side.Player, village.Position), true));
            village.HasRepaired = true;
        }

        return healed;
    }

    public CommandResult PirateStep()
    {
        if (ActiveSide != Side.Pirates || IsOver)
            return CommandResult.Rejected("It is not the pirates' turn.");
        foreach (var ship in OwnShips(Side.Pirates).ToArray())
        {
            var enemies = ObservedShips(Side.Pirates).Where(s => s.Owner != Side.Pirates && !s.IsAirborne).ToArray();
            var target = enemies.Where(s => CanAttack(ship.Id, s.Id)).OrderBy(s => s.Health).FirstOrDefault();
            if (target is not null)
                return Attack(Side.Pirates, ship.Id, target.Id);
            if (!ship.CanMove || ship.HasMoved)
                continue;
            var pursuit = enemies.Select(e => PathToAttackPosition(ship.Id, e.Id)).Where(p => p.Count > 1).OrderBy(p => p.Count).FirstOrDefault();
            if (pursuit is not null)
            {
                var end = AffordableDestination(ship.Id, pursuit);
                if (end != ship.Position)
                    return Move(Side.Pirates, ship.Id, end);
            }

            var cells = Reachable(ship.Id).Keys.Where(p => p != ship.Position).ToArray();
            if (cells.Length > 0)
                return Move(Side.Pirates, ship.Id, cells[NextEvent(cells.Length)]);
        }

        return EndTurn(Side.Pirates);
    }
}
