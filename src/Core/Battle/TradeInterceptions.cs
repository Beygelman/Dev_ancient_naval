using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;

/// <summary>Stationary intent, not evidence that a hidden enemy market actually exists.</summary>
public sealed record SavedTradeRaid(int Ship, GridPosition Position, int HeldEnds);

public enum PortTradeStatus { NotOwned, NoPort, NoMarket, Connected, Interrupted }

public sealed partial class BattleState
{
    private readonly Dictionary<int, SavedTradeRaid> _tradeRaids = new();
    private readonly Dictionary<Side, (TradeNetwork Base, string Blockers, TradeNetwork Network)> _operationalTrade = new();

    private static bool CanRaidTrade(Ship ship) => ship.Health > 0 && ship.IsArmed
        && !ship.IsMothership && !ship.IsAirborne && !ship.IsStructure;

    /// <summary>Only the owner's stationary stance is exposed. It says nothing about enemy income.</summary>
    public int TradeRaidHoldEnds(Side observer, int shipId) => Rules.ExperimentalEconomy
        && Find(shipId) is { } ship && ship.Owner == observer && CanRaidTrade(ship)
        && _tradeRaids.TryGetValue(shipId, out var state) && state.Position == ship.Position
            ? state.HeldEnds : 0;

    private void AdvanceTradeInterceptions(Side side)
    {
        if (!Rules.ExperimentalEconomy) return;
        foreach (var id in _tradeRaids.Keys.Where(id => Find(id) is not { } ship || !CanRaidTrade(ship)).ToArray())
            _tradeRaids.Remove(id);
        foreach (var ship in OwnShips(side).Where(CanRaidTrade))
        {
            int held = _tradeRaids.TryGetValue(ship.Id, out var state) && state.Position == ship.Position
                ? Math.Min(2, state.HeldEnds + 1) : 1;
            _tradeRaids[ship.Id] = new(ship.Id, ship.Position, held);
        }
    }

    private void ClearTradeInterception(int shipId)
    {
        _tradeRaids.Remove(shipId);
    }

    private SavedTradeRaid[] SaveTradeRaids() => _tradeRaids.Values
        .Where(state => Find(state.Ship) is { } ship && CanRaidTrade(ship) && ship.Position == state.Position)
        .OrderBy(state => state.Ship).ToArray();

    private void RestoreTradeRaids(IEnumerable<SavedTradeRaid> saved)
    {
        _tradeRaids.Clear();
        _operationalTrade.Clear();
        _portIncomes.Clear();
        foreach (var state in saved) _tradeRaids.Add(state.Ship, state);
    }

    private bool OnTradeCorridor(Ship raider, Side victim, TradeNetwork network) =>
        network.Edges.Any(edge => edge.From == raider.Position || edge.To == raider.Position)
        || OwnShips(victim).Any(ship => ship.Definition.Class == ShipClass.Lighthouse && ship.Health > 0
            && (ship.Position == raider.Position || Board.GetSurrounding(ship.Position).Contains(raider.Position)));

    /// <summary>Actual economic resolution only. Never hand this network to a renderer or AI planner:
    /// its missing geometry could disclose a raider behind fog.</summary>
    private TradeNetwork OperationalTradeRoutes(Side side)
    {
        var baseline = TradeRoutes(side);
        if (!Rules.ExperimentalEconomy) return baseline;
        var raiders = _tradeRaids.Values.Where(state => state.HeldEnds >= 2)
            .Select(state => (State: state, Ship: Find(state.Ship)))
            .Where(item => item.Ship is { } ship && ship.Owner != side && CanRaidTrade(ship)
                && ship.Position == item.State.Position && OnTradeCorridor(ship, side, baseline))
            .Select(item => item.Ship!).OrderBy(ship => ship.Id).ToArray();
        if (raiders.Length == 0) return baseline;
        string key = string.Join(';', raiders.Select(ship => $"{ship.Id}@{ship.Position.X},{ship.Position.Y}"));
        if (_operationalTrade.TryGetValue(side, out var cached)
            && ReferenceEquals(cached.Base, baseline) && cached.Blockers == key) return cached.Network;
        var closed = new HashSet<GridPosition>(_forbidden);
        foreach (var ship in raiders)
            foreach (var cell in Board.GetSurrounding(ship.Position).Append(ship.Position))
                if (Board.TryGetTile(cell, out var tile) && tile!.Terrain != TerrainType.Land) closed.Add(cell);
        var endpoints = TradeCities(side).OrderBy(town => town.Id).Select(PortBerth)
            .Concat(OwnShips(side).Where(ship => ship.Definition.Class == ShipClass.Lighthouse && ship.Health > 0)
                .OrderBy(ship => ship.Id).Select(ship => ship.Position)).Distinct().ToArray();
        var network = TradeNetwork.Create(Board, endpoints, closed, Rules.Ports.MaximumRouteLength,
            terminalEndpoints: TradeTerminalBerths(side));
        _operationalTrade[side] = (baseline, key, network);
        return network;
    }

    /// <summary>Income status is available only for the observing faction's own city.</summary>
    public PortTradeStatus TradeStatusFor(Side observer, int villageId)
    {
        var town = _villages.FirstOrDefault(city => city.Id == villageId);
        if (town is null || town.Owner != observer) return PortTradeStatus.NotOwned;
        if (!town.HasPort || town.Health <= 0) return PortTradeStatus.NoPort;
        if (ConnectedPortCityCount(town) > 0) return PortTradeStatus.Connected;
        if (!Rules.ExperimentalEconomy) return PortTradeStatus.NoMarket;
        var network = TradeRoutes(observer);
        return TradeCities(observer).Any(other => other.Id != town.Id
            && network.AreConnected(PortBerth(town), PortBerth(other)))
                ? PortTradeStatus.Interrupted : PortTradeStatus.NoMarket;
    }

    /// <summary>Potential raid cells derived only from optically observed foreign objects and known sea.
    /// No actual foreign income, hidden city, hidden hazard or hidden raider is queried.</summary>
    public IReadOnlyList<GridPosition> KnownTradeRaidTargets(Side observer)
    {
        if (!Rules.ExperimentalEconomy) return Array.Empty<GridPosition>();
        var targets = new HashSet<GridPosition>();
        var visibleCities = ObservedVillages(observer).Where(city => city.Owner is { } owner
            && owner != observer && city.Health > 0).ToArray();
        var visibleBeacons = ObservedShips(observer).Where(ship => ship.Owner != observer
            && ship.Definition.Class == ShipClass.Lighthouse && ship.Health > 0).ToArray();
        bool KnownSea(GridPosition cell) => Vision.KnownTerrain(observer, cell) is { } terrain
            && terrain != TerrainType.Land;
        var knownHazards = _forbidden.Where(cell => Vision.IsVisible(observer, cell)).ToHashSet();
        foreach (var victim in visibleCities.Select(city => city.Owner!.Value)
            .Concat(visibleBeacons.Select(ship => ship.Owner)).Distinct())
        {
            var cities = visibleCities.Where(city => city.Owner == victim).ToArray();
            var beacons = visibleBeacons.Where(ship => ship.Owner == victim).ToArray();
            if (!cities.Any(city => city.HasPort) && beacons.Length == 0) continue;
            GridPosition? KnownBerth(Village city)
            {
                if (city.HasPort && city.PortCell is { } berth) return KnownSea(berth) ? berth : null;
                return VillageBerths(city).Where(KnownSea).OrderBy(cell => cell.Y).ThenBy(cell => cell.X)
                    .Select(cell => (GridPosition?)cell).FirstOrDefault();
            }
            var nodes = cities.Select(KnownBerth)
                .Where(cell => cell.HasValue).Select(cell => cell!.Value)
                .Concat(beacons.Select(ship => ship.Position)).Distinct().ToArray();
            var terminalMarkets = cities.Where(city => !city.HasPort).Select(KnownBerth)
                .Where(cell => cell.HasValue).Select(cell => cell!.Value).ToHashSet();
            terminalMarkets.ExceptWith(cities.Where(city => city.HasPort).Select(KnownBerth)
                .Where(cell => cell.HasValue).Select(cell => cell!.Value));
            terminalMarkets.ExceptWith(beacons.Select(ship => ship.Position));
            var network = TradeNetwork.Create(Board, nodes, knownHazards, Rules.Ports.MaximumRouteLength,
                KnownSea, terminalMarkets);
            foreach (var edge in network.Edges) { targets.Add(edge.From); targets.Add(edge.To); }
            foreach (var beacon in beacons)
                foreach (var cell in Board.GetSurrounding(beacon.Position).Append(beacon.Position))
                    if (KnownSea(cell) && !knownHazards.Contains(cell)) targets.Add(cell);
        }
        return targets.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
    }
}

internal static class TradeRaidValidation
{
    internal static void Validate(BattleSave saved, GameBoard board, BattleRules rules)
    {
        if (saved.TradeRaids is null || saved.TradeRaids.Length > saved.Ships.Length
            || saved.TradeRaids.Select(state => state?.Ship).Distinct().Count() != saved.TradeRaids.Length)
            throw new ArgumentException("Invalid saved trade interceptions.");
        var ships = saved.Ships.ToDictionary(ship => ship.Id);
        foreach (var state in saved.TradeRaids)
            if (state is null || !rules.ExperimentalEconomy || state.HeldEnds is < 1 or > 2
                || !board.Contains(state.Position) || !ships.TryGetValue(state.Ship, out var ship)
                || ship.Position != state.Position || ship.Kind is ShipClass.Mothership or ShipClass.Balloon
                    or ShipClass.Lighthouse or ShipClass.CannonTower or ShipClass.AncientGun or ShipClass.FishingDock
                || rules.Get(ship.Kind).Damage <= 0 || rules.Get(ship.Kind).Movement <= 0)
                throw new ArgumentException("Invalid saved trade interception stance.");
    }
}
