using System.Threading.Tasks;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    private GridPosition? _salvoCell;
    private int? _salvoTargetId;
    private int _salvoActorId;
    private BattleState? _salvoBattle;

    private bool OfferSalvoChoice(Ship attacker, GridPosition cell, int? targetId = null)
    {
        if (!Battle.Rules.DoubleSalvo || !(attacker.Definition.Class == ShipClass.Kolonel
            || attacker.IsMothership && attacker.SecondAttackUpgrade) || Battle.UsesMortar(attacker, cell)) return false;
        _salvoCell = cell; _salvoTargetId = targetId; _salvoActorId = attacker.Id; _salvoBattle = Battle;
        Refresh();
        return true;
    }
    private Task ChooseSalvo(bool twice)
    {
        if (!CanCommand || _salvoCell is not { } cell || !ReferenceEquals(_salvoBattle, Battle)
            || SelectedShipId != _salvoActorId || !Battle.TargetCells(_salvoActorId).Contains(cell)
            || twice && !Battle.CanDoubleSalvo(_salvoActorId, cell)) return Task.CompletedTask;
        int actor = _salvoActorId;
        int? target = _salvoTargetId;
        _salvoCell = null;
        Hud.HideSalvoChoice();
        return Perform(b => target is { } id ? b.Attack(Side.Player, actor, id, twice)
            : b.AttackAt(Side.Player, actor, cell, twice), deferImpacts: true);
    }
}
