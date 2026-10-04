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
        if (Rules.DiagonalVillageBerths && !PortBerths(town).Any())
            return "No adjacent water tile is available for a port.";
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
        town.PortCell = Rules.DiagonalVillageBerths ? PreviewPortBerth(villageId) : null;
        _credits[(int)side] -= PortPrice(side);
        town.HasPort = true;
        town.HasProduced = true;
        RegisterVillageIncome(town);
        UpdateVision();
        return new(true, $"Port opened: +{Rules.Ports.Income} income and {Rules.Ports.Discount:P0} shipyard discount.", CommandKind.Port, villageId);
    }

    private IEnumerable<GridPosition> VillageBerths(Village town) => Rules.DiagonalVillageBerths
        ? Board.GetSurrounding(town.Position) : Board.GetNeighbors(town.Position);
    public IReadOnlyList<GridPosition> PortBerths(Village town) => VillageBerths(town)
        .Where(p => Board.GetTile(p).Terrain != TerrainType.Land && !_forbidden.Contains(p)
            && (!Rules.EmptyOuterRim || !Board.IsOuterCell(p)))
        .OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
    public GridPosition PortBerth(Village town) => town.PortCell ?? VillageBerths(town)
        .Where(p => Board.GetTile(p).Terrain != TerrainType.Land)
        .OrderBy(p => p.Y).ThenBy(p => p.X).First();

    /// <summary>Choose a shoreline once at construction; future ports never move existing docks.</summary>
    public GridPosition? PreviewPortBerth(int villageId)
    {
        var town = _villages.FirstOrDefault(v => v.Id == villageId);
        if (town is null) return null;
        if (town.HasPort) return PortBerth(town);
        var candidates = PortBerths(town);
        if (candidates.Count == 0) return null;
        if (!Rules.DiagonalVillageBerths) return candidates[0];
        var destinations = _villages.Where(v => v.Id != town.Id && v.Owner == town.Owner
            && v.HasPort && v.Health > 0).Select(PortBerth)
            .Concat(_ships.Where(s => s.Owner == town.Owner && s.Definition.Class == ShipClass.Lighthouse)
                .Select(s => s.Position)).Distinct().ToArray();
        bool Sea(GridPosition p) => Board.TryGetTile(p, out var tile)
            && tile!.Terrain != TerrainType.Land && !_forbidden.Contains(p);
        var costs = candidates.ToDictionary(p => p, _ => int.MaxValue);
        foreach (var destination in destinations)
        {
            var routes = PathSearch.Find(destination, Board.Tiles.Count, Board.GetSurrounding,
                (from, to) => TradeNetwork.SeaStepCost(Board, from, to, Sea));
            foreach (var cell in candidates)
                if (routes.Costs.TryGetValue(cell, out int cost)) costs[cell] = Math.Min(costs[cell], cost);
        }
        // With no reachable network, favor open sea; ties remain deterministic.
        return candidates.OrderBy(p => costs[p])
            .ThenByDescending(p => Board.GetSurrounding(p).Count(Sea))
            .ThenBy(p => p.Y).ThenBy(p => p.X).First();
    }
    /// <summary>Read-only pencil planning: never crosses undiscovered terrain or exposes a hidden hazard.</summary>
    public TradeNetwork PreviewLighthouseTradeRoutes(Side side, GridPosition proposed)
    {
        if (!Board.Contains(proposed) || !Vision.IsVisible(side, proposed) || !IsFreeWater(proposed))
            return new TradeNetwork();
        var ports = _villages.Where(v => v.Owner == side && v.HasPort && v.Health > 0).OrderBy(v => v.Id)
            .Select(PortBerth).Concat(_ships.Where(s => s.Owner == side && s.Definition.Class == ShipClass.Lighthouse
                && s.Health > 0).OrderBy(s => s.Id).Select(s => s.Position)).Append(proposed).Distinct().ToArray();
        var knownHazards = _forbidden.Where(p => Vision.IsVisible(side, p)).ToHashSet();
        return TradeNetwork.Create(Board, ports, knownHazards, Rules.Ports.MaximumRouteLength,
            p => Vision.KnownTerrain(side, p) is { } terrain && terrain != TerrainType.Land);
    }

    public TradeNetwork TradeRoutes(Side side)
    {
        var towns = _villages.Where(v => v.Owner == side && v.HasPort && v.Health > 0).OrderBy(v => v.Id).ToArray();
        var lighthouses = _ships.Where(s => s.Owner == side && s.Definition.Class == ShipClass.Lighthouse && s.Health > 0)
            .OrderBy(s => s.Id).ToArray();
        // Topology depends on ports and permanent hazards, never cursor/camera position.
        string key = string.Join(',', towns.Select(v => v.Id)) + ":" + string.Join(',', lighthouses.Select(s => s.Id)) + ":" +
            string.Join(';', _forbidden.OrderBy(p => p.Y).ThenBy(p => p.X).Select(p => $"{p.X},{p.Y}"));
        if (_tradeNetworks.TryGetValue(side, out var cached) && cached.Key == key)
            return cached.Network;
        var network = TradeNetwork.Create(Board, towns.Select(PortBerth).Concat(lighthouses.Select(s => s.Position))
            .Distinct().ToArray(), _forbidden, Rules.Ports.MaximumRouteLength);
        _tradeNetworks[side] = (key, network);
        return network;
    }
}
