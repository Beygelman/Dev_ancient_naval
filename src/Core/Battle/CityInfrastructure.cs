using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;

public enum CityFacility { None, TradeYard, RepairDock, NavalArsenal }

public sealed class InfrastructureRules
{
    public bool Enabled { get; init; }
    public int BuildPrice { get; init; } = 6;
    public int ReplacementSurcharge { get; init; } = 3;
    public int UpgradePrice { get; init; } = 6;
    public int TradeIncome { get; init; } = 2;
    public int ArsenalCapacity { get; init; } = 2;
    public int DockRepair { get; init; } = 2;
    public void Validate()
    {
        if (BuildPrice < 0 || ReplacementSurcharge < 0 || UpgradePrice < 0
            || TradeIncome < 0 || ArsenalCapacity < 0 || DockRepair < 0
            || BuildPrice > 1000 || ReplacementSurcharge > 1000 || UpgradePrice > 1000
            || TradeIncome > 20 || ArsenalCapacity > 20 || DockRepair > 20)
            throw new ArgumentException("Invalid city infrastructure rules.");
    }
}

public sealed partial class BattleState
{
    public int CityFacilitySlots(Village town) => !Rules.Infrastructure.Enabled ? 0 : town.Level >= 4 ? 2 : town.Level >= 2 ? 1 : 0;
    public static CityFacility CityFacilityAt(Village town, int slot) => slot == 0 ? town.Facility1 : slot == 1 ? town.Facility2 : CityFacility.None;
    public int CityFacilityStrength(Village town, CityFacility kind) => !Rules.Infrastructure.Enabled
        || town.Health <= 0 || kind == CityFacility.None || town.Facility1 != kind && town.Facility2 != kind
            ? 0 : town.ImprovedFacility == kind ? 2 : 1;
    public int CityFacilityPrice(Side side, int villageId, int slot, CityFacility kind)
    {
        if (Creative && side == Side.Player) return 0;
        var town = _villages.FirstOrDefault(v => v.Id == villageId);
        if (town is null) return 0;
        var installed = CityFacilityAt(town, slot);
        return installed == kind ? Rules.Infrastructure.UpgradePrice
            : Rules.Infrastructure.BuildPrice + (installed == CityFacility.None ? 0 : Rules.Infrastructure.ReplacementSurcharge);
    }
    public string? CityFacilityBlockReason(Side side, int villageId, int slot, CityFacility kind)
    {
        var error = ValidateVillage(side, villageId, out var town);
        if (error is not null) return error;
        if (!Rules.Infrastructure.Enabled) return "City infrastructure is unavailable in this voyage.";
        if (!Enum.IsDefined(kind) || kind == CityFacility.None || slot < 0 || slot >= CityFacilitySlots(town!))
            return "This infrastructure slot is locked.";
        if (town!.HasProduced || town.HasRepaired) return "The city has already worked this turn.";
        var installed = CityFacilityAt(town, slot);
        if (installed == kind)
        {
            if (town.Level < 5) return "Facility improvement requires a level-5 city.";
            if (town.ImprovedFacility != CityFacility.None) return "Only one facility can be improved.";
        }
        else if (town.Facility1 == kind || town.Facility2 == kind)
            return "The city already has this facility.";
        return Credits(side) < CityFacilityPrice(side, villageId, slot, kind) ? "Not enough Thors." : null;
    }
    public CommandResult ConfigureCityFacility(Side side, int villageId, int slot, CityFacility kind)
    {
        var error = CityFacilityBlockReason(side, villageId, slot, kind);
        if (error is not null) return CommandResult.Rejected(error);
        var town = _villages.First(v => v.Id == villageId);
        _credits[(int)side] -= CityFacilityPrice(side, villageId, slot, kind);
        var previous = CityFacilityAt(town, slot);
        if (previous == kind) town.ImprovedFacility = kind;
        else
        {
            if (town.ImprovedFacility == previous) town.ImprovedFacility = CityFacility.None;
            if (slot == 0) town.Facility1 = kind; else town.Facility2 = kind;
        }
        town.HasProduced = true;
        return new(true, "City infrastructure updated.", CommandKind.Upgrade, villageId, villageId);
    }
    private Village? AvailableRepairDock(Ship ship) => !Rules.Infrastructure.Enabled || ship.HasMoved ? null
        : _villages.Where(v => v.Owner == ship.Owner && !v.HasRepaired && !v.HasProduced
            && CityFacilityStrength(v, CityFacility.RepairDock) > 0
            && Board.GetSurrounding(v.Position).Contains(ship.Position))
        .OrderByDescending(v => CityFacilityStrength(v, CityFacility.RepairDock)).ThenBy(v => v.Id).FirstOrDefault();
    public int ShipRepairAmount(Ship ship)
    {
        var dock = AvailableRepairDock(ship);
        return Rules.RepairAmount + (dock is null ? 0
            : Rules.Infrastructure.DockRepair * CityFacilityStrength(dock, CityFacility.RepairDock));
    }
}

