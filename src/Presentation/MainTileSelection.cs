using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private BattleState? _tileCycleBattle;
    private GridPosition? _tileCycleCell;
    private string _tileCycleSignature = "";
    private int _tileCycleIndex;

    /// <summary>Cycle only observed occupants. Attack/build orders keep priority.</summary>
    private bool CycleTileOccupants(GridPosition cell)
    {
        var ships = Battle.ObservedShips(Side.Player).Where(s => s.Position == cell)
            .OrderBy(s => s.IsAirborne).ThenBy(s => s.Id).ToArray();
        var towns = Battle.ObservedVillages(Side.Player).Where(v => v.Position == cell).ToArray();
        bool fish = Battle.Vision.IsVisible(Side.Player, cell)
            && (Battle.FishSpots.Contains(cell) || Battle.Shoals.Contains(cell));
        bool ruin = Battle.ObservedTreasuryRuins(Side.Player).Any(r => r.Position == cell && !r.IsCollected);
        int count = ships.Length + towns.Length + (fish ? 1 : 0) + (ruin ? 1 : 0);
        if (ships.Length == 0 || count < 2 || Mode != OrderMode.None)
        {
            _tileCycleCell = null;
            return false;
        }
        string signature = string.Join(",", ships.Select(s => "s" + s.Id).Concat(towns.Select(t => "v" + t.Id)))
            + (fish ? "f" : "") + (ruin ? "r" : "");
        if (!ReferenceEquals(_tileCycleBattle, Battle) || _tileCycleCell != cell || signature != _tileCycleSignature)
            _tileCycleIndex = 0;
        else _tileCycleIndex = (_tileCycleIndex + 1) % count;
        _tileCycleBattle = Battle;
        _tileCycleCell = cell;
        _tileCycleSignature = signature;
        ClearMode();
        SelectedShipId = null;
        SelectedVillageId = null;
        if (_tileCycleIndex < ships.Length)
            SelectedShipId = ships[_tileCycleIndex].Id;
        else if (_tileCycleIndex < ships.Length + towns.Length)
            SelectedVillageId = towns[_tileCycleIndex - ships.Length].Id;
        else if (fish && _tileCycleIndex == ships.Length + towns.Length && CanCommand
            && (Battle.CollectionCells(Side.Player).Contains(cell) || Battle.DockCells(Side.Player).Contains(cell)))
        {
            _resourceCell = cell;
            _resourceIsDock = Battle.DockCells(Side.Player).Contains(cell);
        }
        Refresh();
        if (_resourceCell is not null)
        {
            int price = _resourceIsDock ? Battle.DockPrice(Side.Player) : Battle.CollectionCost(Side.Player);
            Hud.ShowResource(_resourceIsDock, price, Battle.Credits(Side.Player) >= price);
            PositionActions();
        }
        return true;
    }
}
