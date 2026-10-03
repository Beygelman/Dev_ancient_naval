using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Navigation;

namespace DevAncientNaval.Presentation;
<<<<<<< Updated upstream
=======

>>>>>>> Stashed changes
public partial class Main
{
    private BattleState? _routeBattle;
    private int _routeShip;
    private long _routeVision = -1;
    private int _routeMovement = -1;
    private GridPosition _routePosition;
    private MovementPreview? _routePreview;
<<<<<<< Updated upstream
    private MovementPreview? SelectedRoutes()
    {
        var ship = Selected;
        if (ship is null || !CanCommand || !ship.CanMove)
            return null;
        if (_routePreview is null || !ReferenceEquals(_routeBattle, Battle) || _routeShip != ship.Id || _routeVision != Battle.Vision.Revision || _routeMovement != ship.MovementRemainingUnits || _routePosition != ship.Position)
        {
            using var trace = Diagnostics.PerformanceTrace.Measure("Navigation.PreviewPlan");
            _routePreview = Battle.PreviewMovement(ship.Id);
            _routeBattle = Battle;
            _routeShip = ship.Id;
            _routeVision = Battle.Vision.Revision;
            _routeMovement = ship.MovementRemainingUnits;
            _routePosition = ship.Position;
        }

=======

    private MovementPreview? SelectedRoutes()
    {
        var ship = Selected;
        if (ship is null || !CanCommand) return null;
        if (_routePreview is null || !ReferenceEquals(_routeBattle, Battle) || _routeShip != ship.Id ||
            _routeVision != Battle.Vision.Revision || _routeMovement != ship.MovementRemainingUnits || _routePosition != ship.Position)
        {
            using var trace = Diagnostics.PerformanceTrace.Measure("Navigation.PreviewPlan");
            _routePreview = Battle.PreviewMovement(ship.Id);
            _routeBattle = Battle; _routeShip = ship.Id; _routeVision = Battle.Vision.Revision;
            _routeMovement = ship.MovementRemainingUnits; _routePosition = ship.Position;
        }
>>>>>>> Stashed changes
        return _routePreview;
    }

    private void InvalidateGameplayPresentation()
    {
        _routePreview = null;
        _presentedVision = -1;
    }
}
