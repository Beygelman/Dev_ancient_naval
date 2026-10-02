using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private readonly HashSet<Side> _shownEncounters = new();
    private void ResetEncounterPresentation()
    {
        MapCamera.CancelFlight();
        _shownEncounters.Clear();
        foreach (var encounter in Battle.Encounters) _shownEncounters.Add(encounter.Side);
    }
    private Task FocusVisibleTarget(GridPosition cell) => FastChecks || !Battle.Vision.IsVisible(Side.Player, cell)
        ? Task.CompletedTask : MapCamera.FocusAsync(BoardView.ToGlobal(BoardView.Projection.GridToWorld(cell)), Camera.MapCamera.ActionSeconds);

    private async Task PresentEncounters()
    {
        foreach (var encounter in Battle.Encounters.OrderBy(e => e.Side))
        {
            if (!_shownEncounters.Add(encounter.Side)) continue;
            if (!FastChecks)
                await MapCamera.FocusAsync(BoardView.ToGlobal(BoardView.Projection.GridToWorld(encounter.Position)), Camera.MapCamera.EncounterSeconds);
            if (Battle.ActiveSide == Side.Player)
                Hud.ShowMessage($"Met {Battle.FactionName(encounter.Side)} · +{Battle.Rules.EncounterCurrencyReward} Thors");
        }
        if (Battle.ActiveSide != Side.Player) Hud.ShowOpponentTurn(Battle.ActiveSide);
    }

    private void SalvoAtScreen(Vector2 screen)
    {
        if (!CanCommand) return;
        if (Selected is not { Owner: Side.Player } ship || Mode != OrderMode.None)
        {
            SelectAtScreen(screen);
            return;
        }
        var air = Battle.ObservedShips(Side.Player).FirstOrDefault(s => s.IsAirborne
            && (GetViewport().GetCanvasTransform() * (BoardView.Projection.GridToWorld(s.Position) + new Vector2(0, -62))).DistanceTo(screen) < 24 * MapCamera.Zoom.X);
        var cell = air?.Position ?? BoardView.Projection.WorldToGrid(BoardView.ToLocal(MapCamera.ScreenToWorld(screen)));
        if (Battle.CanDoubleSalvo(ship.Id, cell))
        {
            int id = ship.Id;
            RunSafely(() => Perform(b => air is null ? b.AttackAt(Side.Player, id, cell, true) : b.Attack(Side.Player, id, air.Id, true), deferImpacts: true));
        }
        else SelectAtScreen(screen);
    }
}
