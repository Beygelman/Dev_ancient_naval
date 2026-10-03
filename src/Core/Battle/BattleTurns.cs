using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Economy;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public CommandResult Repair(Side requester, int id)
    {
        var error = ValidateActor(requester, id, out var ship);
        if (error is not null)
            return CommandResult.Rejected(error);
        if (!ship!.CanRepair)
            return CommandResult.Rejected("A damaged ship can repair before taking other actions.");
        double amount = Math.Min(Rules.RepairAmount, ship.MaxHealth - ship.Health);
        ship.Health = Ship.Whole(ship.Health + amount);
        ship.IsExhausted = true;
        ship.HasRepaired = true;
        ship.MovementLocked = true;
        return new(true, $"{ship.Definition.Name}: repaired +{amount:0.##} HP", CommandKind.Repair, id, Amount: amount);
    }

    public CommandResult EndTurn(Side requester)
    {
        if (PendingPresentation is not null || IsOver || requester != ActiveSide || PendingUpgrade(requester)is not null)
            return CommandResult.Rejected("Cannot end this turn. Resolve any pending upgrade first.");
        var outpostShots = FireOutposts(requester);
        if (IsOver)
            return new(true, "The last flagship has fallen.", CommandKind.EndTurn, OutpostShots: outpostShots);
        var healing = AutomaticRepairs(requester);
        EndSeaEventTurn(requester);
        EndVillageTurn(requester);
        ActiveSide = NextFactionTurn(requester);
        int previousIndex = requester == Side.Pirates ? _factions.Count : _factions.IndexOf(requester);
        int nextIndex = ActiveSide == Side.Pirates ? _factions.Count : _factions.IndexOf(ActiveSide);
        if (nextIndex <= previousIndex)
            Round++;
        Vision.ClearCombatFlashes();
        foreach (var ship in _ships.Where(s => s.Owner == ActiveSide))
        {
            ship.ResetTurn();
            if (ship.BombCooldown > 0)
                ship.BombCooldown--;
            if (ship.RestorationUpgrade)
            {
                double before = ship.Health;
                ship.Health = Math.Min(ship.MaxHealth, ship.Health + 2);
                if (ship.Health > before)
                    healing.Add(new(ship.Position, ship.Health - before, ship.Owner == Side.Player || Vision.IsVisible(Side.Player, ship.Position)));
            }
        }

        StartVillageTurn(ActiveSide);
        var receipts = CreditTurnIncome(ActiveSide);
        var heavenly = StartHeavenlyTurn(ActiveSide);
        UpdateVision();
        return new(true, ActiveSide == Side.Player ? "Your turn." : $"{FactionName(ActiveSide)}'s turn.", CommandKind.EndTurn, IncomeReceipts: receipts, OutpostShots: outpostShots, HealingReceipts: healing, HeavenlyReceipts: heavenly);
    }

    private void RegisterShipIncome(Ship ship)
    {
        int income = ship.IsMothership ? Rules.IncomePerMothership + Rules.Economy.MothershipIncomePerLevel * (ship.Level - 1) + (ship.IncomeUpgrade ? 1 : 0) : ship.Definition.IncomePerTurn;
        if (income > 0)
            SetIncomeSource(new IncomeSource($"ship:{ship.Id}", ship.Owner, income, ship.Id));
    }

    public int GrossIncome(Side side) => _incomeSources.Where(s => s.Owner == side && (s.BoundShipId is null || Find(s.BoundShipId.Value)is not null)).Sum(s => s.Amount);
    public int Upkeep(Side side)
    {
        int group = Rules.Economy.CombatShipsPerUpkeep;
        if (group == 0)
            return 0;
        int maintained = Math.Max(0, OwnShips(side).Count(ship => ship.CountsTowardFleet) - Rules.Economy.FreeCombatShips);
        return (maintained + group - 1) / group;
    }

    public int Income(Side side) => Math.Max(0, GrossIncome(side) - Upkeep(side));
    public int VillageIncome(Village village) => (village.Level + Rules.Economy.VillageLevelsPerIncome - 1) / Rules.Economy.VillageLevelsPerIncome;
    private IReadOnlyList<IncomeReceipt> CreditTurnIncome(Side side)
    {
        var receipts = new List<IncomeReceipt>();
        foreach (var source in _incomeSources.Where(source => source.Owner == side))
        {
            GridPosition? position = source.BoundShipId is { } id ? Find(id)?.Position : null;
            if (source.BoundShipId is not null && position is null)
                continue;
            if (source.Id.StartsWith("village:", StringComparison.Ordinal) && int.TryParse(source.Id.AsSpan(8), out int villageId))
                position = _villages.FirstOrDefault(village => village.Id == villageId)?.Position;
            receipts.Add(new(source.Id, side, position, source.Amount));
        }

        int gross = receipts.Sum(receipt => receipt.Amount);
        int upkeep = Math.Min(gross, Upkeep(side));
        if (upkeep > 0)
            receipts.Add(new("fleet:upkeep", side, Mothership(side)?.Position, -upkeep, true));
        _credits[(int)side] += gross - upkeep;
        RecordCurrencyReceipt(side, gross);
        return receipts;
    }

    public void SetIncomeSource(IncomeSource source)
    {
        if (string.IsNullOrWhiteSpace(source.Id) || !Enum.IsDefined(source.Owner) || source.Amount < 0 || (source.BoundShipId is { } shipId && Find(shipId)?.Owner != source.Owner))
            throw new ArgumentException("Invalid income source.");
        _incomeSources.RemoveAll(s => s.Id == source.Id);
        _incomeSources.Add(source);
    }

    public bool RemoveIncomeSource(string id) => _incomeSources.RemoveAll(s => s.Id == id) > 0;
}
