using System.Threading.Tasks;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    private ScuttleConfirmationHud _scuttleConfirmation = null!;
    internal bool ScuttleConfirmationVisible => _scuttleConfirmation?.IsOpen == true;
    private void InitializeScuttleConfirmation()
    {
        _scuttleConfirmation = new ScuttleConfirmationHud { Name = "ScuttleConfirmationHud" };
        AddChild(_scuttleConfirmation);
    }
    internal async Task RequestScuttle()
    {
        if (!CanCommand || SelectedShipId is not { } id || !Battle.CanScuttle(Side.Player, id)) return;
        var battle = Battle;
        var ship = battle.Find(id)!;
        MapInput.CancelGesture();
        Busy = true;
        Refresh();
        bool accepted;
        try
        {
            accepted = await _scuttleConfirmation.Ask(ship.Definition.Name, battle.ScuttleRefund(Side.Player, id),
                battle.PlayerColor, FastChecks);
        }
        finally { Busy = false; }
        // Dialog interruption, a replacement voyage or stale selection cannot issue an order.
        if (accepted && IsInsideTree() && ReferenceEquals(battle, Battle) && SelectedShipId == id
            && CanCommand && battle.CanScuttle(Side.Player, id))
            await Perform(b => b.Scuttle(Side.Player, id), deferImpacts: true);
        else if (IsInsideTree()) Refresh();
    }
}
