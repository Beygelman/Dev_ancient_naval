using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    internal async Task PresentHeavenlyAssistance(CommandResult result)
    {
        if (FastChecks || result.HeavenlyReceipts is not { Count: > 0 }) return;
        Refresh();
        if (Battle.ActiveSide == Core.Units.Side.Player)
            Hud.ShowPlayerTurn();
        else
            Hud.ShowOpponentTurn(Battle.ActiveSide);
        foreach (var receipt in result.HeavenlyReceipts)
            if (receipt.Owner != Core.Units.Side.Player || !Battle.Rules.DeferredRewards)
                await Hud.ShowHeavenlyAssistance(receipt, Battle);
    }
}
