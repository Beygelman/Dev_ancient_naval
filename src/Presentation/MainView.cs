using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    private BattleState? _presentedBattle;
    private long _presentedVision = -1;
    public void Refresh()
    {
        using var trace = DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Main.Refresh");
        bool worldChanged = !ReferenceEquals(_presentedBattle, Battle) || _presentedVision != Battle.Vision.Revision;
        if (_mapPreview && worldChanged)
        {
            foreach (var tile in Battle.Board.Tiles)
                Battle.Vision.RevealCombat(Side.Player, tile.Position);
            Battle.Vision.Recompute(Battle.Ships, Battle.Round * 2, Battle.Villages);
        }

        if (worldChanged)
        {
            BoardView.InvalidateWorld(force: false);
            Ambience.RefreshVisibility();
            _presentedBattle = Battle;
            _presentedVision = Battle.Vision.Revision;
        }

        var selected = Selected;
        var village = SelectedVillage;
        if (selected is null)
            SelectedShipId = null;
        if (village is null)
            SelectedVillageId = null;
        _previewCell = null;
        Fleet.SelectedId = SelectedShipId;
        BoardView.SelectedShipId = SelectedShipId;
        Hud.ShowTile(Battle, BoardView.Selected);
        BoardView.Reachable = CanCommand && Mode == OrderMode.Build && village?.Owner == Side.Player ? Battle.VillageSpawnCells(village.Id).ToArray() : CanCommand && selected?.Owner == Side.Player ? Mode == OrderMode.Build ? Battle.SpawnCells(selected.Id).ToArray() : Mode == OrderMode.None ? SelectedRoutes()?.Costs.Keys.ToArray() ?? Array.Empty<GridPosition>() : Array.Empty<GridPosition>() : Array.Empty<GridPosition>();
        BoardView.Targets = CanCommand && selected?.Owner == Side.Player && Mode == OrderMode.None ? Battle.TargetCells(selected.Id) : Array.Empty<GridPosition>();
        BoardView.AttackArea = Array.Empty<GridPosition>();
        BoardView.Collection = CanCommand && Mode == OrderMode.None ? Battle.CollectionCells(Side.Player) : Array.Empty<GridPosition>();
        BoardView.DockSites = CanCommand && Mode == OrderMode.None ? Battle.DockCells(Side.Player) : Array.Empty<GridPosition>();
        BoardView.Building = Mode == OrderMode.Build;
        Hud.UpdateBattle(Battle, selected, Busy, Mode, village);
        PositionActions();
        BoardView.RefreshOverlays();
        Fleet.QueueRedraw();
        RefreshOutcome();
    }

    private void PositionActions()
    {
        Vector2 Screen(GridPosition p) => GetViewport().GetCanvasTransform() * BoardView.ToGlobal(BoardView.Projection.GridToWorld(p));
        Hud.PositionActions(_resourceCell is null ? (Selected is { } ship ? Screen(ship.Position) : SelectedVillage is { } village ? Screen(village.Position) : BoardView.Selected is { } inspected ? Screen(inspected) : null) : null);
        Hud.PositionResource(_resourceCell is { } cell ? Screen(cell) : null);
        Hud.PositionStories(Screen);
    }

    public override void _Process(double delta)
    {
        var transform = GetViewport().GetCanvasTransform();
        var size = GetViewport().GetVisibleRect().Size;
        if (transform == _lastCanvasTransform && size == _lastViewportSize)
            return;
        _lastCanvasTransform = transform;
        _lastViewportSize = size;
        PositionActions();
        // World-space overlay commands are retained; the renderer clips them after a pan.
    }
}
