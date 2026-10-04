using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private bool _endingStamp;
    private void InitializeTurnGuidance()
    {
        UiHints.Changed += Refresh;
        TreeExiting += () => UiHints.Changed -= Refresh;
        Hud.TurnConfirmed += () => RunSafely(EndPlayerTurn);
        Hud.TurnConfirmationClosed += () => { MapInput.CancelGesture(); Refresh(); };
        Hud.ReadyObjectSelected += item => RunSafely(() => ReturnToReadyObject(item));
    }
    private async Task RequestEndPlayerTurn()
    {
        if (!CanCommand || _endingStamp || Hud.TurnConfirmationVisible) return;
        _endingStamp = true;
        try
        {
            await Hud.AnimateEndTurnStamp(FastChecks);
            _endingStamp = false;
            if (!CanCommand) return;
            if (UiHints.Enabled)
            {
                MapInput.CancelGesture();
                ClearMode();
                Hud.ShowTurnConfirmation();
                Refresh();
            }
            else await EndPlayerTurn();
        }
        finally { _endingStamp = false; }
    }
    private async Task ReturnToReadyObject(ReadyActionObject item)
    {
        if (!CanCommand) return;
        ClearMode();
        SelectedShipId = item.ShipClass is null ? null : item.Id;
        SelectedVillageId = item.ShipClass is null ? item.Id : null;
        BoardView.Select(item.Position);
        Refresh();
        if (!FastChecks) await MapCamera.FocusAsync(item.ShipClass is null && Battle.VillageAt(item.Position) is { } town
            ? BoardView.VillageWorldAnchor(town) : BoardView.Projection.GridToWorld(item.Position), .25);
    }
}
