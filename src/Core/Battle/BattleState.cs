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
    private readonly bool[] _everProduced = new bool[SideSlots];
    private readonly List<IncomeSource> _incomeSources = new();
    private int _nextId = 1;
    public GameBoard Board { get; }
    public BattleRules Rules { get; }
    public BattleVision Vision { get; }
    public IReadOnlyList<Ship> Ships { get; }
    public IReadOnlyList<IncomeSource> IncomeSources { get; }
    public Side ActiveSide { get; private set; } = Side.Player;
    public Side? Winner { get; private set; }
    public bool IsDraw { get; private set; }
    public int Round { get; private set; } = 1;
    public bool IsOver => Winner is not null || IsDraw;

    private BattleState(GameBoard board, BattleRules rules)
    {
        Board = board;
        Rules = rules;
        Vision = new BattleVision(board, rules.MountainSightShadows, rules.SmallHullRadarStealth);
        _credits = new int[SideSlots];
        Ships = _ships.AsReadOnly();
        IncomeSources = _incomeSources.AsReadOnly();
    }

    public BattleState(GameBoard board, BattleRules rules, IEnumerable<(Side Owner, ShipClass Class, GridPosition Position)> setup, IEnumerable<GridPosition>? fishSpots = null, int resourceSeed = 1729, IEnumerable<GridPosition>? villageSpots = null, bool seaEvents = false, bool generatedSettlements = false) : this(board, rules)
    {
        foreach (var side in PlayableSides)
            _credits[(int)side] = rules.StartingCredits;
        foreach (var item in setup)
        {
            if (!Enum.IsDefined(item.Owner) || (item.Class == ShipClass.Balloon ? !Board.Contains(item.Position) : !IsFreeWater(item.Position)) || (item.Class == ShipClass.Mothership && board.IsNarrowPassage(item.Position)))
                throw new ArgumentException("Invalid fleet deployment.");
            var ship = new Ship(_nextId++, item.Owner, rules.Get(item.Class), item.Position);
            _ships.Add(ship);
            RegisterShipIncome(ship);
        }

        InitializeFactions();
        _personalTurnStarts[(int)Side.Player] = 1;
        UpdateVision();
        InitializeFishing(fishSpots, resourceSeed);
        InitializeVillages(villageSpots, resourceSeed);
        if (generatedSettlements)
            InitializeSettlementLevels();
        InitializeSeaEvents(seaEvents, resourceSeed);
        AssignWorldNames();
        UpdateVision();
    }

    public bool Creative { get; private set; }

    public void SetCreative(bool enabled) => Creative = enabled;
    public int BuildPrice(Side side, ShipClass kind) => Creative && side == Side.Player ? 0 : Math.Max(1, (int)Math.Floor(Rules.Get(kind).Price * (Mothership(side)?.ShipwrightUpgrade == true ? .75 : 1)));
    public int CollectionCost(Side side) => Creative && side == Side.Player ? 0 : CollectionPrice;
    public int Credits(Side side) => _credits[(int)side];
    public Ship? Find(int id) => _ships.FirstOrDefault(s => s.Id == id);
    public Ship? At(GridPosition cell) => _ships.FirstOrDefault(s => !s.IsAirborne && s.Position == cell);
    public IEnumerable<Ship> OwnShips(Side side) => _ships.Where(s => s.Owner == side);
    public IEnumerable<Ship> ObservedShips(Side side) => _ships.Where(s => s.Owner == side || Vision.IsVisible(side, s.Position));
    public Ship? ObservedAt(Side side, GridPosition cell) => ObservedShips(side).Where(s => s.Position == cell).OrderBy(s => s.IsAirborne).FirstOrDefault();
    public Ship? FindObserved(Side side, int id) => ObservedShips(side).FirstOrDefault(s => s.Id == id);
    public static double Distance(GridPosition a, GridPosition b) => Math.Sqrt((double)(a.X - b.X) * (a.X - b.X) + (double)(a.Y - b.Y) * (a.Y - b.Y));
    public bool IsFreeWater(GridPosition cell) => Board.TryGetTile(cell, out var tile) && tile!.Terrain != TerrainType.Land && !_forbidden.Contains(cell) && At(cell)is null;
    private void UpdateVision()
    {
        Vision.SetAllSeeingPlayer(GodEye || Winner == Side.Player);
        Vision.Recompute(Ships, TurnSerial, _villages);
        DiscoverNations();
    }
    private string? ValidateActor(Side requester, int id, out Ship? ship)
    {
        ship = Find(id);
        if (PendingPresentation is not null)
            return "A projectile is still in flight.";
        if (IsOver)
            return "The battle is over.";
        if (requester != ActiveSide)
            return "It is the other side's turn.";
        if (ship is null || ship.Owner != requester)
            return "Select one of your ships.";
        if (ship.IsExhausted)
            return "This ship has no actions remaining this turn.";
        if (PendingUpgrade(requester)is not null)
            return "Choose the Mothership upgrade first.";
        return null;
    }
}
