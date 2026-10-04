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
        _rewards?.Close();
        MapCamera.CancelFlight();
        _shownEncounters.Clear();
        foreach (var encounter in Battle.Encounters) _shownEncounters.Add(encounter.Side);
    }
    private Task FocusVisibleTarget(GridPosition cell) => FastChecks || !Battle.Vision.IsVisible(Side.Player, cell)
        ? Task.CompletedTask : MapCamera.FocusAsync(BoardView.ToGlobal(Battle.VillageAt(cell) is { } town
            ? BoardView.VillageWorldAnchor(town) : BoardView.Projection.GridToWorld(cell)), Camera.MapCamera.ActionSeconds);

    private async Task PresentEncounters()
    {
        foreach (var encounter in Battle.Encounters.OrderBy(e => e.Side))
        {
            if (!_shownEncounters.Add(encounter.Side)) continue;
            if (!FastChecks)
                await MapCamera.FocusAsync(BoardView.ToGlobal(BoardView.Projection.GridToWorld(encounter.Position)), Camera.MapCamera.EncounterSeconds);
            if (Battle.ActiveSide == Side.Player && !Battle.Rules.DeferredRewards)
                Hud.ShowMessage($"Met {Battle.FactionName(encounter.Side)} · +{Battle.Rules.EncounterCurrencyReward} Thors");
        }
        if (Battle.ActiveSide != Side.Player) Hud.ShowOpponentTurn(Battle.ActiveSide);
    }

}
