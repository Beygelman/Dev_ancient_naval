using System;
<<<<<<< Updated upstream
using System.Collections.Generic;
=======
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
    private readonly List<Vector2> _actionTargetScreens = new(16);
=======
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        if (CanCommand && _salvoCell is { } target && SelectedShipId == _salvoActorId)
            Hud.ShowSalvoChoice(Battle.CanDoubleSalvo(_salvoActorId, target));
        else Hud.HideSalvoChoice();
        PositionActions();
        BoardView.RefreshOverlays();
        Fleet.QueueRedraw();
        RefreshOutcome();
        PresentPendingRewards();
=======
        PositionActions();
        BoardView.QueueRedraw();
        Fleet.QueueRedraw();
>>>>>>> Stashed changes
    }

    private void PositionActions()
    {
        Vector2 Screen(GridPosition p) => GetViewport().GetCanvasTransform() * BoardView.ToGlobal(BoardView.Projection.GridToWorld(p));
<<<<<<< Updated upstream
        Hud.PositionActions(_resourceCell is null ? (Selected is { } ship ? Screen(ship.Position) : SelectedVillage is { } village ? Screen(village.Position) : BoardView.Selected is { } inspected ? Screen(inspected) : null) : null,
            (Selected is { } selected ? ShipVisualProfile.ProgressY(selected.Definition.Class) : SelectedVillage is not null ? 69 : 0) * MapCamera.Zoom.Y);
        _actionTargetScreens.Clear();
        foreach (var attackCell in BoardView.Targets)
            _actionTargetScreens.Add(Screen(attackCell));
        // The ring must also leave neighboring friendly objects selectable.
        // Optical/owned air units use their raised drawing anchor, not the sea
        // tile beneath them. Anonymous radar contacts remain tile points above.
        foreach (var observed in Battle.ObservedShips(Side.Player))
            if (observed.Id != SelectedShipId)
                _actionTargetScreens.Add(Screen(observed.Position) +
                    (observed.IsAirborne ? new Vector2(0, -62) * MapCamera.Zoom : Vector2.Zero));
        foreach (var town in Battle.ObservedVillages(Side.Player))
            if (town.Id != SelectedVillageId)
                _actionTargetScreens.Add(Screen(town.Position));
        Hud.SetActionTargetHitExclusions(_actionTargetScreens);
        Hud.PositionResource(_resourceCell is { } cell ? Screen(cell) : null);
        Hud.PositionStories(Screen);
        if (_salvoCell is { } target)
        {
            var targetShip = Battle.ObservedAt(Side.Player, target);
            float offset = targetShip is not null ? ShipVisualProfile.ProgressY(targetShip.Definition.Class)
                : Battle.ObservedVillages(Side.Player).Any(v => v.Position == target) ? 69 : 24;
            Hud.PositionSalvoChoice(Screen(target), offset * MapCamera.Zoom.Y);
        }
=======
        Hud.PositionActions(_resourceCell is null ? (Selected is { } ship ? Screen(ship.Position) : SelectedVillage is { } village ? Screen(village.Position) : BoardView.Selected is { } inspected ? Screen(inspected) : null) : null);
        Hud.PositionResource(_resourceCell is { } cell ? Screen(cell) : null);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        // World-space overlay commands are retained; the renderer clips them after a pan.
=======
        BoardView.QueueRedraw(); // Refresh only culled live objects after a camera change.
>>>>>>> Stashed changes
    }
}
