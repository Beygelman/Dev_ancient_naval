using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    private readonly Dictionary<Side, (string Key, TradeNetwork Network)> _tradeNetworks = new();
    public int PortPrice(Side side) => Creative && side == Side.Player ? 0 : Rules.Ports.Price;
    public int VillageBuildPrice(int villageId, ShipClass kind)
    {
        var town = _villages.First(v => v.Id == villageId);
        return Math.Max(Creative && town.Owner == Side.Player ? 0 : 1, (int)Math.Floor(BuildPrice(town.Owner ?? ActiveSide, kind) * (town.HasPort ? 1 - Rules.Ports.Discount : 1)));
    }

    public string? PortBlockReason(Side side, int villageId)
    {
        var error = ValidateVillage(side, villageId, out var town);
        if (error is not null)
            return error;
        if (town!.Level < 3)
            return "A port requires a level-3 city.";
        if (town.HasPort)
            return "This city already has a port.";
        if (town.HasProduced || town.HasRepaired)
            return "The city has already worked this turn.";
        return Credits(side) < PortPrice(side) ? "Not enough Thors." : null;
    }

    public CommandResult BuildPort(Side side, int villageId)
    {
        if (PendingPresentation is not null)
            return CommandResult.Rejected("Wait for the current order.");
        var error = PortBlockReason(side, villageId);
        if (error is not null)
            return CommandResult.Rejected(error);
        var town = _villages.First(v => v.Id == villageId);
        _credits[(int)side] -= PortPrice(side);
        town.HasPort = true;
        town.HasProduced = true;
        RegisterVillageIncome(town);
        UpdateVision();
        return new(true, $"Port opened: +{Rules.Ports.Income} income and {Rules.Ports.Discount:P0} shipyard discount.", CommandKind.Port, villageId);
    }

    public GridPosition PortBerth(Village town) => Board.GetNeighbors(town.Position).Where(p => Board.GetTile(p).Terrain != TerrainType.Land).OrderBy(p => p.Y).ThenBy(p => p.X).First();
    public TradeNetwork TradeRoutes(Side side)
    {
        var towns = _villages.Where(v => v.Owner == side && v.HasPort && v.Health > 0).OrderBy(v => v.Id).ToArray();
        // Topology depends on ports and permanent hazards, never cursor/camera position.
        string key = string.Join(',', towns.Select(v => v.Id)) + ":" + _forbidden.Count;
        if (_tradeNetworks.TryGetValue(side, out var cached) && cached.Key == key)
            return cached.Network;
        var network = TradeNetwork.Create(Board, towns.Select(PortBerth).ToArray(), _forbidden);
        _tradeNetworks[side] = (key, network);
        return network;
    }
}
